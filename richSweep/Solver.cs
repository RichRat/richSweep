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
            if(innit)
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
                    if (m_values[x, y] > 0.9f)
                    {
                        Console.WriteLine("x{0} y{1} prob : 1", x, y);
                        m_board[x][y].RightClick();
                    }
                    if (m_values[x, y] > max)
                    {
                        max = m_values[x, y];
                        _x = x;
                        _y = y;
                    }
                }

            
            if (max < 1)
            {
                for (int x = 0; x < m_sizeX; x++)
                    for (int y = 0; y < m_sizeY; y++)
                        if (m_board[x][y].FieldMode == Field.Mode.REVEALED)
                            IndirectRule(x, y);

                //TODO calculate possible bomb distributions and if bombcount <= remaining bombs
                Console.WriteLine("x{0} y{1} prob : {2}", _x, _y, max);

                m_board[_x][_y].RightClick(); //this is basically an informed guess
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

        private void IndirectRule(int x, int y)
        {
            IRestrictedField field = m_board[x][y];
            int flags = 0;
            int hidden = 0;
            foreach (IRestrictedField neighbour in field)
                switch (neighbour.FieldMode)
                {
                    case Field.Mode.HIDDEN:
                        hidden++;
                        break;
                    case Field.Mode.FLAGGED:
                        flags++;
                        break;
                }

            List<IRestrictedField> commonNeighbours = new List<IRestrictedField>();
            if (hidden > 0 && flags < field.RValue)
            {
                commonNeighbours.Clear();
                foreach (IRestrictedField neighbour in field)
                    foreach (IRestrictedField f in neighbour)
                    {
                        switch (f.FieldMode)
                        {
                            case Field.Mode.HIDDEN:
                                hidden++;
                                break;
                            case Field.Mode.FLAGGED:
                                flags++;
                                break;
                        }

                        //check if field is in intersection - better by coords then by lists (perfromance!!)
                    }
            }
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
                /*else
                {
                    //TODO do this after assigning values
                    m_board[x][y].ClickArea();
                }*/
            }
        }
    }
}
