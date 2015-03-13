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

        public Solver(List<List<IRestrictedField>> board, int sizeX, int sizeY)
        {
            m_board = board;
            m_sizeX = sizeX;
            m_sizeY = sizeY;
            m_values = new float[m_sizeX, m_sizeY];

        }

        public void calcStep(bool innit)
        {
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
            float max = 0;
            int _x = 0;
            int _y = 0;
            for (int x = 0; x < m_sizeX; x++)
                for (int y = 0; y < m_sizeY; y++)
                {
                    // always flag fields with probability 1
                    if (m_values[x, y] > 0.95f)
                    {
                        Console.WriteLine("flag x{0} y{1} prob : 1", x, y);
                        m_board[x][y].RightClick();
                    }
                    if (m_values[x, y] > max)
                    {
                        max = m_values[x, y];
                        _x = x;
                        _y = y;
                    }
                }

            Console.WriteLine("max: " + max);
            if (max < 0.95f)
            {
                bool indirectSuccess = false;
                for (int x = 0; x < m_sizeX; x++)
                    for (int y = 0; y < m_sizeY; y++)
                        if (m_board[x][y].FieldMode == Field.Mode.REVEALED && m_board[x][y].RValue > 0)
                            if (IndirectRule(x, y))
                            {
                                indirectSuccess = true;
                                x = m_sizeX;
                                y = m_sizeY;
                            }


                //TODO calculate possible bomb distributions and if bombcount <= remaining bombs
                /*
                if (!indirectSuccess)
                {
                    Console.WriteLine("x{0} y{1} prob : {2}", _x, _y, max);
                    m_board[_x][_y].RightClick(); //this is basically an informed guess
                }
                 * */
            }


            // clean up all fields that cannot be bombs (determined by flags)
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
        /// <returns>true if a shure flag or save field could be concluded</returns>
        private bool IndirectRule(int x, int y)
        {
            // prefix n : neighbour, f : field, i : intersecting

            IRestrictedField field = m_board[x][y];
            Console.WriteLine("indir {0} {1}", x, y);
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
    }
}
