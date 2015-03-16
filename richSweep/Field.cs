using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace richSweep
{
    /// <summary>
    /// represents a single Field on the Gameboard
    /// </summary>
    public class Field : IRestrictedField
    {
        public event Action<int, int> BombClicked;
        public event Action<Field> ModeChanged;
        public event Action<Field, int, int> FirstClicked;
        public event Action<bool> AreaClickHighlighted;
        public event Action<bool> AreaClickFailedAnimation;
        public event Action<Field> FlagModeChanged;

        List<Field> m_neighbours = new List<Field>();
        int m_value = 0;
        Mode m_mode = Mode.HIDDEN;
        int m_X;
        int m_Y;


        static bool s_first = true;
        static bool s_game = true;

        public int Value { get { return m_value; } }

        public int RValue { get { return m_value >= 0 ? m_value : 0; } }

        public Mode FieldMode { get { return m_mode; } }

        //TODO figure out a better way
        public int X { get { return m_X; } }
        public int Y { get { return m_Y; } }

        public enum Mode
        {
            HIDDEN = 0,
            REVEALED = 1,
            FLAGGED = 2,
            REMINDER = 3,
            CORRECTFLAG = 4,
            NOTFOUND = 5,
            INCORRECTFLAG = 6
        }

        public Field(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        public void GetNeighbours(List<List<Field>> board)
        {
            int x, y;
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                {
                    x = m_X + i;
                    y = m_Y + j;
                    if (x >= 0 && x < board.Count && y >= 0 && y < board[0].Count && board[x][y] != this)
                        m_neighbours.Add(board[x][y]);
                }
        }

        public void Click()
        {
            if (!s_game)
                return;

            if (m_mode == Mode.HIDDEN || m_mode == Mode.REMINDER)
            {
                if (m_value == 0)
                {
                    // the first click will be ignored invoking the FirstClicked Event
                    if (!s_first)
                    {
                        m_mode = Mode.REVEALED;
                        foreach (Field f in m_neighbours)
                        {
                            if (f.Value > 0)
                                f.Click();
                            else if (f.m_value == 0)
                                f.Click();
                        }
                    }
                    else if (FirstClicked != null)
                    {
                        s_first = false;
                        FirstClicked.Invoke(this, m_X, m_Y);
                    }

                }
                else if (m_value < 0 && BombClicked != null)
                {
                    m_mode = Mode.REVEALED;
                    s_game = false;
                    BombClicked.Invoke(m_X, m_Y);
                }
                else
                {
                    m_mode = Mode.REVEALED;
                }
            }

            InvokeModeChanged();
        }

        private void InvokeModeChanged()
        {
            if (ModeChanged != null)
                ModeChanged.Invoke(this);
        }

        public void RightClick()
        {
            if (m_mode != Mode.REVEALED && s_game)
            {
                switch (m_mode)
                {
                    case Mode.HIDDEN:
                        m_mode = Mode.FLAGGED;
                        break;

                    case Mode.FLAGGED:
                        m_mode = Mode.REMINDER;
                        break;

                    case Mode.REMINDER:
                        m_mode = Mode.HIDDEN;
                        break;

                    default:
                        break;
                }

                if (FlagModeChanged != null)
                    FlagModeChanged.Invoke(this);
                InvokeModeChanged();
            }
        }

        public void ClickArea()
        {
            if (m_mode == Mode.REVEALED && m_value > 0 && s_game)
            {
                short flagCount = 0;
                foreach (Field f in m_neighbours)
                    if (f.m_mode == Mode.FLAGGED)
                        flagCount++;

                if (flagCount == m_value)
                    foreach (Field f in m_neighbours)
                        f.Click();
                else
                {
                    foreach (Field f in m_neighbours)
                        if (f.AreaClickFailedAnimation != null)
                            f.AreaClickFailedAnimation.Invoke(false);

                    if (AreaClickFailedAnimation != null)
                        AreaClickFailedAnimation.Invoke(true);
                }
            }
        }

        public void AreaClickMark(bool on)
        {
            foreach (Field f in m_neighbours)
                if (f.AreaClickHighlighted != null)
                    f.AreaClickHighlighted.Invoke(on);
        }

        public bool SetBomb()
        {
            if (m_value == -1)
                return false;
            m_value = -1;
            return true;
        }

        public void RevealBomb()
        {
            if (m_value < 0 && m_mode != Mode.REVEALED)
            {
                if (m_mode == Mode.FLAGGED)
                {
                    m_mode = Mode.CORRECTFLAG;
                }
                else if (m_mode == Mode.HIDDEN)
                {
                    m_mode = Mode.NOTFOUND;
                }

                InvokeModeChanged();
            }
        }

        /// <summary>
        /// counts all bombs in the neighbourhood
        /// also resets m_value
        /// </summary>
        public void CountValue()
        {
            if (m_value >= 0)
            {
                m_value = 0;
                foreach (Field f in m_neighbours)
                    if (f.m_value < 0)
                        m_value++;
            }
        }

        public void Reset(List<List<Field>> board)
        {
            //GetNeighbours(board);
            m_value = 0;
            s_first = true;
            s_game = true;
            m_mode = Mode.HIDDEN;

            if (ModeChanged != null)
                ModeChanged.Invoke(this);
        }

        public override string ToString()
        {
            return String.Format("x: {0} y:{1} val:{2} mode:{3}", m_X, m_Y, m_value, m_mode);
        }

        // implementation of IEnumerable

        public IEnumerator<IRestrictedField> GetEnumerator()
        {
            return new RestrictedFieldIterator(m_neighbours);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new RestrictedFieldIterator(m_neighbours);
        }


        class RestrictedFieldIterator : IEnumerator<IRestrictedField> 
        {
            int m_position = -1;
            List<Field> m_list;

            public RestrictedFieldIterator (List<Field> list)
            {
                m_list = list;
            }

            public IRestrictedField Current
            {
                get { return m_list[m_position]; }
            }

            public void Dispose()
            {
                m_list = null;
            }

            object System.Collections.IEnumerator.Current
            {
                get { return m_list[m_position]; }
            }

            public bool MoveNext()
            {
                m_position++;
                if (m_position >= m_list.Count)
                    return false;
                else
                    return true;
            }

            public void Reset()
            {
                m_position = -1;
            }
        }

    }
}