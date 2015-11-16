using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace richSweep
{
    /// <summary>
    /// contains a brute force solving algorythm that calculates all probabilities and then sets one flag and then checks if there 
    /// </summary>
    class Solver
    {
        List<List<IRestrictedField>> m_board;
        float[,] m_values;
        int m_sizeX;
        int m_sizeY;
        //random with current time as seed
        Random m_rand = new Random((int)DateTime.Now.ToBinary());

        public Solver(List<List<IRestrictedField>> board, int sizeX, int sizeY)
        {
            m_board = board;
            m_sizeX = sizeX;
            m_sizeY = sizeY;
            m_values = new float[m_sizeX, m_sizeY];

        }

        public void calcStep(bool innit, int remainingBombs)
        {
            bool indirectSuccess = false;
            if (innit)
            {
                m_board[m_sizeX / 2][m_sizeY / 2].Click();
                return;
            }

            CleanValuesArray();

            //calculate field probabilities
            for (int x = 0; x < m_sizeX; x++)
                for (int y = 0; y < m_sizeY; y++)
                    if (m_board[x][y].FieldMode == Field.Mode.REVEALED)
                        CalculateProbabilities(x, y);

            //check calculated probabilities and get the one with the higest prob. in case none has p = 1
            float maxChance = 0, minChance = 1;
            int max_x = 0, max_y = 0, min_x = 0, min_y = 0;
            for (int x = 0; x < m_sizeX; x++)
                for (int y = 0; y < m_sizeY; y++)
                {
                    // always flag fields with probability 1
                    if (m_values[x, y] > 0.95f)
                    {
                        Console.WriteLine("flag x{0} y{1} prob : 1", x, y);
                        m_board[x][y].RightClick(); //TODO if this happens there is no need to do anything after this (ie max chance set to 1)
                        indirectSuccess = true;
                    }
                    if (m_values[x, y] > maxChance)
                    {
                        maxChance = m_values[x, y];
                        max_x = x;
                        max_y = y;
                    }
                    if (m_values[x, y] > 0 && m_values[x, y] < minChance)
                    {
                        minChance = m_values[x, y];
                        min_x = x;
                        min_y = y;
                    }

                }

            Console.WriteLine("max: " + maxChance);
            if (maxChance < 0.95f && minChance > 0.05f && !indirectSuccess)
            {
                
                for (int x = 0; x < m_sizeX; x++)
                    for (int y = 0; y < m_sizeY; y++)
                        if (m_board[x][y].FieldMode == Field.Mode.REVEALED && m_board[x][y].RValue > 0)
                            if (IndirectRule(x, y))  //the true magic happens here
                            {
                                indirectSuccess = true;
                                x = m_sizeX;
                                y = m_sizeY;
                            }

                //TODO calculate possible bomb distributions and if bombcount <= remaining bombs
                if (!indirectSuccess /*&& minChance == 1 && maxChance == 0*/)
                {
                    int remainingFields = 0;
                    //count remaining boms and fields
                    for (int x = 0; x < m_sizeX; x++)
                        for (int y = 0; y < m_sizeY; y++)
                            if (m_board[x][y].FieldMode == Field.Mode.HIDDEN)
                                remainingFields ++;

                    
                    if (remainingFields == remainingBombs)
                        flagAllHiddenFields();

                    int remainingSafeFields = remainingFields - remainingBombs;
                    if (remainingFields > 0)
                        if (remainingSafeFields > remainingBombs)
                        {
                            float p = 1 - ((float)remainingSafeFields / remainingFields);
                            if (p < minChance)
                            {
                                IRestrictedField f = getRandomHiddenField(remainingFields);
                                Console.WriteLine("GLOBAL CHANCE CLICK x{0} y{1} c {2}", f.X, f.Y, p);
                                f.Click();
                                indirectSuccess = true;
                            }
                        }
                        else
                        {
                            float p = (float)remainingBombs / remainingFields;
                            if (p > maxChance)
                            {
                                IRestrictedField f = getRandomHiddenField(remainingFields);
                                Console.WriteLine("GLOBAL CHANCE CLICK x{0} y{1} c{2}", f.X, f.Y, p);
                                f.RightClick();
                                indirectSuccess = true;
                            }
                        }
                    
                }
                

                //this is basically an informed guess
                if (!indirectSuccess)
                    if (maxChance > 1 - minChance)
                    {
                        Console.WriteLine("CHANCE FLAG x{0} y{1} prob : {2}", max_x, max_y, maxChance);
                        m_board[max_x][max_y].RightClick();
                    }
                    else
                    {
                        Console.WriteLine("CHANCE CLICK x{0} y{1} prob : {2}", min_x, min_y, minChance);
                        m_board[min_x][min_y].Click();
                    }
            }


            // clean up (reveal) all fields that cannot be bombs (determined by flags)
            for (int x = 0; x < m_sizeX; x++)
                for (int y = 0; y < m_sizeY; y++)
                {
                    IRestrictedField field = m_board[x][y];
                    int flagCount = 0;
                    foreach (IRestrictedField neighbour in field)
                        if (neighbour.FieldMode == Field.Mode.FLAGGED)
                            flagCount++;

                    if (flagCount == field.RValue)
                        field.ClickArea();
                }
        }

        private void flagAllHiddenFields()
        {
            for (int x = 0; x < m_sizeX; x++)
                for (int y = 0; y < m_sizeY; y++)
                    if (m_board[x][y].FieldMode == Field.Mode.HIDDEN)
                        m_board[x][y].RightClick();
        }

        private void CleanValuesArray()
        {
            for (int x = 0; x < m_sizeX; x++)
                for (int y = 0; y < m_sizeY; y++)
                    m_values[x, y] = 0;
        }

        /// <summary>
        /// looks at the relation of two revealed (not bomb!) fields
        /// </summary>
        /// <param name="x">x-coord of the field</param>
        /// <param name="y">y-coord of the field</param>
        /// <returns>true if a sure flag or save field could be concluded</returns>
        private bool IndirectRule(int x, int y)
        {
            // prefix n : neighbour, f : field, i : intersecting

            IRestrictedField field = m_board[x][y];
            int fFlags = 0;
            int fHidden = 0;
            foreach (IRestrictedField neighbour in field)
                switch (neighbour.FieldMode)
                {
                    case Field.Mode.HIDDEN:
                        fHidden++;
                        break;
                    case Field.Mode.FLAGGED:
                        fFlags++;
                        break;
                }

            int fRemaining = field.RValue - fFlags;

            if (fHidden == 0 || fFlags >= field.RValue)
                return false;

            List<IRestrictedField> commonHiddenNeighbours = new List<IRestrictedField>();
            foreach (IRestrictedField neighbour in field)
            {
                if (neighbour.FieldMode != Field.Mode.REVEALED || neighbour.RValue == 0)
                    continue;

                int nHidden = 0;
                int nFlags = 0;
                commonHiddenNeighbours.Clear();

                foreach (IRestrictedField f in neighbour)
                {
                    switch (f.FieldMode)
                    {
                        case Field.Mode.HIDDEN:
                            nHidden++;
                            //check if field is in intersection
                            if (field.Contains(f))
                                commonHiddenNeighbours.Add(f);
                            break;
                        case Field.Mode.FLAGGED:
                            nFlags++;
                            break;
                    }
                }

                if (fHidden == 0)
                    continue;

                int nRemaining = neighbour.RValue - nFlags;

                foreach (IRestrictedField f in field)
                    m_values[f.X, f.Y] = f.FieldMode == Field.Mode.HIDDEN ? 1 : 0;

                foreach (IRestrictedField f in commonHiddenNeighbours)
                    m_values[f.X, f.Y] = -1;

                int fCount = fHidden; //TODO rework again :(
                int iCount = commonHiddenNeighbours.Count;

                //maximum intersection bomb count
                int iMaxFlags = iCount < nRemaining ? iCount : nRemaining;
                //minimum intersection bomb count
                int iMinFlags = nRemaining - (nHidden - iCount);
                iMinFlags = iMinFlags < 0 ? 0 : iMinFlags;

                bool foundSomething = false;
                //secure non bomb
                //all f bombs in intersection
                //click non intersecting hidden fields
                //probably best to only do one click
                if (iMinFlags == fRemaining)
                {
                    foreach (IRestrictedField f in field)
                        if (f.FieldMode == Field.Mode.HIDDEN && !commonHiddenNeighbours.Contains(f))
                        {
                            f.Click();
                            foundSomething = true;
                            Console.WriteLine("   iclick {0} {1}", f.X, f.Y);
                            break;
                        }
                }
                //secure bomb
                //non intersecting fields are equal to the remaining flags in case of maximum intersecting flags
                //flag all non intersecting hidden fields
                else if (fRemaining - iMaxFlags == fHidden - iCount && fHidden - iCount != 0)
                {
                    foreach (IRestrictedField f in field)
                        if (f.FieldMode == Field.Mode.HIDDEN && !commonHiddenNeighbours.Contains(f))
                        {
                            f.RightClick();
                            Console.WriteLine("   iflag {0} {1}", f.X, f.Y);
                            foundSomething = true;
                        }
                }

                if (foundSomething)
                    return true;
            }

            return false;
        }

        private void CalculateProbabilities(int x, int y)
        {
            int hiddenCount = 0;
            int flaggedCount = 0;
            IRestrictedField field = m_board[x][y];
            foreach (IRestrictedField neighbour in field)
                switch (neighbour.FieldMode)
                {
                    case Field.Mode.HIDDEN:
                        hiddenCount++;
                        break;
                    case Field.Mode.FLAGGED:
                        flaggedCount++;
                        break;
                    default:
                        break;
                }

            if (hiddenCount > 0)
            {
                int remainingFlags = field.RValue - flaggedCount;
                if (remainingFlags > 0)
                {
                    float probability = (float)remainingFlags / hiddenCount;
                    foreach (IRestrictedField neighbour in field)
                        if (neighbour.FieldMode == Field.Mode.HIDDEN && probability > m_values[neighbour.X, neighbour.Y])
                            m_values[neighbour.X, neighbour.Y] = probability;
                }
            }
        }

        private IRestrictedField getRandomHiddenField(int hiddenCount)
        {
            int tmp = 0;
            int target = m_rand.Next(hiddenCount - 1);
            for (int x = 0; x < m_sizeX; x++)
                for (int y = 0; y < m_sizeY; y++)
                    if (m_board[x][y].FieldMode == Field.Mode.HIDDEN && tmp++ == target)
                        return m_board[x][y];
            
            return null;
        }
        
    }
}
