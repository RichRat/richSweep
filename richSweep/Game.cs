using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;


namespace richSweep
{
    public class Game
    {
        public event Action<int> SecondPassed;
        public event Action<int> RemainingBombsChanged;

        public event Action<String> PushDebugMessage;

        int m_sizeX;
        int m_sizeY;
        int m_bombCount;
        Stopwatch m_stopWatch = new Stopwatch();
        Random m_rand = new Random((int)DateTime.Now.TimeOfDay.Ticks);
        int m_flaggedCount;
        int m_remainingFields;
        Solver m_solver;

        Dispatcher m_UIdisp;

        const int FASTLINEAPPEAR = 25;
        const int SLOWLINEAPPEAR = 150;


        enum GameBoardState
        {
            INNITIALIZED,
            PLAY,
            LOST,
            WON
        }

        GameBoardState m_state = GameBoardState.INNITIALIZED;

        List<List<Field>> m_gameBoard = new List<List<Field>>();

        public int SizeX
        {
            get { return m_sizeX; }
            set
            {
                if (value > 0)
                    m_sizeX = value;
            }
        }
        public int SizeY
        {
            get { return m_sizeY; }
            set
            {
                if (value > 0)
                    m_sizeY = value;
            }
        }

        public int Bombs
        {
            get { return m_bombCount; }
            set
            {
                if (value > 0)
                    m_bombCount = value;
            }
        }

        public List<List<Field>> Board { get { return m_gameBoard; } }

        public Game(Dispatcher uiDisp)
        {
            m_UIdisp = uiDisp;
            //TODO load previous setup 
            //but for now set expert everyTime
            SetGame(30, 16, 99);
            Init();
            
            //TODO look up if there is a better way
            List<List<IRestrictedField>> restList = new List<List<IRestrictedField>>();
            for (int i = 0; i < m_gameBoard.Count; i++)
            {
                List<IRestrictedField> subList = new List<IRestrictedField>();
                for (int j = 0; j < m_gameBoard[i].Count; j++)
                    subList.Add(m_gameBoard[i][j]);
                restList.Add(subList);
            }   
 
            m_solver = new Solver(restList, m_sizeX, m_sizeY);
        }

        public void Init()
        {
            m_flaggedCount = 0;
            //adjust size of m_gameBoard
            if (m_gameBoard.Count > m_sizeX)
                m_gameBoard.RemoveRange(m_sizeX - 1, m_gameBoard.Count - m_sizeX);
            if (m_gameBoard.Count < m_sizeX)
                for (int i = m_gameBoard.Count; i < m_sizeX; i++)
                    m_gameBoard.Add(new List<Field>());



            for (int x = 0; x < m_gameBoard.Count; x++)
                if (m_gameBoard[x].Count > m_sizeY)
                    m_gameBoard[x].RemoveRange(m_sizeY - 1, m_gameBoard[x].Count - m_sizeY);
                else if (m_gameBoard[x].Count < m_sizeY)
                    for (int y = m_gameBoard[x].Count; y < m_sizeY; y++)
                    {
                        Field f = new Field(x, y);
                        f.FirstClicked += OnFirstClicked;
                        f.BombClicked += OnBombClicked;
                        f.FlagModeChanged += OnFlagged;
                        f.ModeChanged += OnFieldModeChanged;
                        m_gameBoard[x].Add(f);
                    }

            for (int x = 0; x < m_gameBoard.Count; x++)
                for (int y = 0; y < m_gameBoard[x].Count; y++)
                {
                    m_gameBoard[x][y].GetNeighbours(m_gameBoard);
                }
            //AnimateStart();
            //m_gameBoard[x].Count < m_sizeY
        }

        void OnFieldModeChanged(Field field)
        {
            if (m_state == GameBoardState.PLAY && field.FieldMode == Field.Mode.REVEALED)
            {
                m_remainingFields = 0;
                foreach (List<Field> fl in m_gameBoard)
                    foreach (Field f in fl)
                        if (f.FieldMode != Field.Mode.REVEALED)
                            m_remainingFields++;
            }

            if (m_remainingFields == m_bombCount)
            {
                //TODO a winner is you
                m_state = GameBoardState.WON;
                //TODO: win animation, save time to timespan, to properties, display stats
            }
        }

        void OnBombClicked(int xSource, int ySource)
        { //TODO REWORK
            
            m_state = GameBoardState.LOST;

            new Thread(() => 
            {
                int xm = xSource;
                int xp = xSource + 1;
                while (m_state == GameBoardState.LOST)
                {
                    if (xm >= 0)
                    {
                        LooseLineReveal(xm);
                        xm--;
                    }

                    Thread.Sleep(m_rand.Next(FASTLINEAPPEAR, SLOWLINEAPPEAR));
                    if (xp < m_sizeX)
                    {
                        LooseLineReveal(xp);
                        xp++;                        
                    }

                    Thread.Sleep(m_rand.Next(FASTLINEAPPEAR, SLOWLINEAPPEAR));

                    if (xp >= m_sizeX && xm < 0)
                        break;
                }
            }).Start();

            m_stopWatch.Stop();
            //TODO save elapsed time to somewhere
        }

        private void LooseLineReveal(int x)
        {
            m_UIdisp.BeginInvoke(new Action<int>(_x =>
            {
                for (int y = 0; y < m_sizeY; y++)
                    m_gameBoard[_x][y].RevealBomb();
            }), x);
        }

        void OnFlagged(Field f)
        {
            if (f.FieldMode == Field.Mode.FLAGGED)
                m_flaggedCount++;
            else
                m_flaggedCount--;

            if (RemainingBombsChanged != null)
                RemainingBombsChanged.Invoke(m_bombCount - m_flaggedCount);
        }

        void OnFirstClicked(Field sender, int x, int y)
        {
            m_state = GameBoardState.PLAY;
            m_remainingFields = m_sizeX * m_sizeY;
            SetBombs(x, y);
            sender.Click();

            m_stopWatch.Restart();
            new Thread(() =>
            {
                while (m_state == GameBoardState.PLAY)
                {
                    Thread.Sleep(1000);
                    if (SecondPassed != null)
                        SecondPassed.Invoke(m_stopWatch.Elapsed.Seconds);
                }
            }).Start();

            if (this.SecondPassed != null)
                this.SecondPassed.Invoke(0);
            if (this.RemainingBombsChanged != null)
                this.RemainingBombsChanged.Invoke(m_bombCount);
        }

        public void Reset()
        {
            foreach (List<Field> collumn in m_gameBoard)
                foreach (Field f in collumn)
                    f.Reset(m_gameBoard);

            m_remainingFields = m_sizeX * m_sizeY;
            m_flaggedCount = 0;

            if (SecondPassed != null)
                SecondPassed.Invoke(999);
            if (RemainingBombsChanged != null)
                RemainingBombsChanged.Invoke(999);

            m_state = GameBoardState.INNITIALIZED;
        }

        public void SetGame(int sizeX, int sizeY, int bombs = 0)
        {
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            this.Bombs = bombs;
        }

        /// <summary>
        /// Distributes the bombs on the Gameboard
        /// </summary>
        /// <param name="startX">first click x Coordinate</param>
        /// <param name="startY">first click y Coordinate</param>
        private void SetBombs(int startX, int startY)
        {
            m_state = GameBoardState.PLAY;
            int bombs = 0;
            while (bombs < m_bombCount)
            {
                int x = m_rand.Next(m_sizeX - 1);
                int y = m_rand.Next(m_sizeY - 1);
                if (CalcSraightDist(x, y, startX, startY) > 1)
                    if(m_gameBoard[x][y].SetBomb())
                        bombs++;
            }

            int count = 0;
            foreach (List<Field> lf in m_gameBoard)
                foreach (Field f in lf)
                    if(f.Value == -1)  count++;
            Console.WriteLine("COUNT : " + count.ToString());

            foreach (List<Field> lf in m_gameBoard)
                foreach (Field f in lf)
                    f.CountValue();
        }

        private int CalcSraightDist(int xStart, int yStart, int xEnd, int yEnd)
        {
            int x = Math.Abs(xEnd - xStart);
            int y = Math.Abs(yEnd - yStart);
            return x > y ? x : y;
        } 

        public void SolveStep()
        {
            if (m_state == GameBoardState.PLAY || m_state == GameBoardState.INNITIALIZED)
                m_solver.calcStep(m_state == GameBoardState.INNITIALIZED);
        }

        static bool onlyOne = false;
        public void TestSolver()
        {
            if (onlyOne == true)
                return;
            onlyOne = true;
            int testRuns = 100;
            int wait = 1; // ms

            new Thread(() =>
                {
                    Action solveStepHandle = () => SolveStep();
                    Action resetHandle = () => Reset();

                    int wins = 0;
                    int losses = 0;

                    for (int i = 0; i < testRuns; i++)
                    {
                        while (m_state == GameBoardState.PLAY || m_state == GameBoardState.INNITIALIZED)
                        {
                            m_UIdisp.Invoke(solveStepHandle);
                            Thread.Sleep(wait);
                        }

                        if (m_state == GameBoardState.LOST)
                            losses++;
                        else
                            wins++;

                        String s = String.Format("WINS {0} LOSSES {1}", wins, losses);
                        m_UIdisp.Invoke(resetHandle);
                        m_UIdisp.Invoke(PushDebugMessage, s);
                        Console.WriteLine(s);
                        Thread.Sleep(100);
                    }
                    Console.WriteLine("TERST FINISHED!");
                    onlyOne = false;
                }).Start();

        }
    }
}
