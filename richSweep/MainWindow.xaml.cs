using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace richSweep
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Game m_game;
        GameBoardUC m_board;

        static public TextBlock debugLine;

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;


            m_game = new Game(this.Dispatcher);
            m_game.SecondPassed += OnSecondPassed;
            m_game.RemainingBombsChanged += OnRemainingBombsChanged;

            m_board = new GameBoardUC(m_game);
            Grid.SetRow(m_board, 1);
            this.MainGrid.Children.Add(m_board);

            debugLine = this.debugBlock;
        }

        void OnSecondPassed(int seconds)
        {
            this.Dispatcher.BeginInvoke(new Action<int>(sec =>
                {
                    this.TimePassedBlock.Text = sec.ToString();
                }), seconds);
        }

        void OnRemainingBombsChanged(int num)
        {
            this.Dispatcher.BeginInvoke(new Action<int>(n =>
            {
                this.BombesRemainingBlock.Text = n.ToString();
            }), num);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            //TODO remember to clean everything up!!!
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
                m_game.Reset();
            else if (e.Key == Key.Space)
                m_game.SolveStep();
        }
    }
}
