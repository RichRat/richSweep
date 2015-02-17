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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class GameBoardUC : UserControl
    {
        List<List<FieldUC>> m_fields = new List<List<FieldUC>>();
        //TODO 
        const double SPACER = 2;
        Game m_game;


        public GameBoardUC(Game game)
        {
            InitializeComponent();

            m_game = game;
            RefillField();
            PositionFields(m_game.SizeX, m_game.SizeY);
        }

        private void GameBoardUC_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionFields(m_game.SizeX, m_game.SizeY);
            //TODO anything else?
        }

        public void RefillField()
        {
            this.GameBoardCanvas.Children.Clear();
            m_fields.Clear();
            for (int i = 0; i < m_game.Board.Count; i++)
            {
                List<FieldUC> flist = new List<FieldUC>();
                for (int j = 0; j < m_game.Board[i].Count; j++)
                {
                    FieldUC f = new FieldUC(m_game.Board[i][j]);
                    flist.Add(f);
                    this.GameBoardCanvas.Children.Add(f);
                }

                m_fields.Add(flist);
            }
        }

        private void PositionFields(int countX, int countY)
        {
            double fieldSizeX = (this.GameBoardCanvas.ActualWidth - (countX + 1) * SPACER) / countX;
            double fieldSizeY = (this.GameBoardCanvas.ActualHeight - (countY + 1) * SPACER) / countY;
            double fieldSize = fieldSizeX < fieldSizeY ? fieldSizeX : fieldSizeY;
            
            double xOffset = (this.GameBoardCanvas.ActualWidth - (SPACER * (countX + 1) + countX * fieldSize)) / 2;
            double yOffset = (this.GameBoardCanvas.ActualHeight - (SPACER * (countY + 1) + countY * fieldSize)) / 2;

            for (int x = 0; x < countX; x++)
                for (int y = 0; y < countY; y++)
                {
                    FieldUC f = m_fields[x][y];
                    f.Size = fieldSize;
                    Canvas.SetLeft(f,xOffset + x * (fieldSize + SPACER) + SPACER);
                    Canvas.SetTop(f,yOffset + y * (fieldSize + SPACER)+ SPACER);
                }
        }
    }
}
