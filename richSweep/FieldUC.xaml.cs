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
using System.Windows.Media.Effects;
using System.Threading;

namespace richSweep
{
    /// <summary>
    /// Interaction logic for FieldUC.xaml
    /// </summary>
    public partial class FieldUC : UserControl
    {
        Field m_field;
        DropShadowEffect m_Glow;
        bool m_buttonDownR = false;
        bool m_buttonDownL = false;
        bool m_mouseOver = false;
        bool m_bothWereDown = false;

        //TODO rework
        const int AnimationCount = 60;

        double m_size = 10;


        public FieldUC(Field field)
        {
            InitializeComponent();

            this.MouseEnter += FieldUC_MouseEnter;
            this.MouseLeave += FieldUC_MouseLeave;
            this.MouseLeftButtonDown += FieldUC_MouseButtonStateChanged;
            this.MouseRightButtonDown += FieldUC_MouseButtonStateChanged;
            this.MouseLeftButtonUp += FieldUC_MouseButtonStateChanged;
            this.MouseRightButtonUp += FieldUC_MouseButtonStateChanged;
            this.MouseDoubleClick += FieldUC_MouseDoubleClick;

            m_field = field;
            m_field.ModeChanged += OnFieldModeChanged;
            m_field.AreaClickHighlighted += OnAreaClickHighlighted;
            m_field.AreaClickFailedAnimation += OnAreaClickFailedAnimation;

            m_Glow = new DropShadowEffect();
            m_Glow.BlurRadius = 5;
            m_Glow.ShadowDepth = 1;
            m_Glow.Direction = 270;
            m_Glow.Opacity = 0.75;
            this.NumberBlock.Effect = m_Glow;
        }

        void OnAreaClickFailedAnimation(bool center)
        {
            //TODO maybe singelton animation class ?
            new Thread(() =>
            {
                for (int i = 0; i < AnimationCount; i++)
                {
                    if (center)
                    {
                        //TODO background flash red
                    }
                    else
                    {
                        //TODO flash pressed on / off
                    }

                    Thread.Sleep(1000 / 30);
                }
            }).Start();
        }

        void OnAreaClickHighlighted(bool on)
        {
            //TODO mit maus testen!!!
            m_mouseOver = on;
            UpdateVisuals();
        }

        public double Size
        {
            get { return m_size; }
            set
            {
                if (value > 0)
                {
                    m_size = value;
                    this.Width = m_size;
                    this.Height = m_size;
                }
            }
        }

        void OnFieldModeChanged(Field f)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            Color c = GetColorForValue();

            //NOTE should be executed by dispatcher or from wpfEvent
            switch (m_field.FieldMode)
            {
                case Field.Mode.HIDDEN:
                    this.NumberBlock.Visibility = System.Windows.Visibility.Hidden;
                    this.NumberBlock.Text = "";

                    if (m_mouseOver)
                        this.MyRectangle.Fill = new SolidColorBrush(Colors.Orange);
                    else
                        this.MyRectangle.Fill = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
                    break;

                case Field.Mode.REVEALED:
                    if (m_field.Value > 0)
                    {
                        m_Glow.Color = c;
                        this.NumberBlock.Foreground = new SolidColorBrush(c);
                        this.NumberBlock.Text = m_field.Value.ToString();
                    }
                    else if (m_field.Value < 0)
                    {
                        this.NumberBlock.Foreground = new SolidColorBrush(c);
                        m_Glow.Color = c;
                        this.NumberBlock.Text = "B";
                    }
                    this.MyRectangle.Fill = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));

                    this.NumberBlock.Visibility = System.Windows.Visibility.Visible;
                    break;

                case Field.Mode.CORRECTFLAG:
                    c = Colors.Green;
                    this.NumberBlock.Foreground = new SolidColorBrush(c);
                    m_Glow.Color = c;
                    break;

                case Field.Mode.FLAGGED:
                    c = Colors.Black;
                    this.NumberBlock.Foreground = new SolidColorBrush(c);
                    m_Glow.Color = c;
                    this.NumberBlock.Text = "!";
                    this.NumberBlock.Visibility = System.Windows.Visibility.Visible;
                    break;

                case Field.Mode.REMINDER:
                    this.NumberBlock.Text = "?";
                    this.NumberBlock.Visibility = System.Windows.Visibility.Visible;
                    break;

                case Field.Mode.NOTFOUND:
                    //TODO include in getcolorfromvalue
                    c = Colors.Red;
                    this.NumberBlock.Visibility = System.Windows.Visibility.Visible;
                    this.NumberBlock.Foreground = new SolidColorBrush(c);
                    m_Glow.Color = c;
                    this.NumberBlock.Text = "B";
                    break;

                default:
                    break;
            }
        }

        void FieldUC_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (m_field.FieldMode == Field.Mode.REVEALED)
                m_field.ClickArea();
        }

        void FieldUC_MouseButtonStateChanged(object sender, MouseButtonEventArgs e)
        {
            bool tmp_buttonDownL = e.LeftButton == MouseButtonState.Pressed;
            bool tmp_buttonDownR = e.RightButton == MouseButtonState.Pressed;
            m_bothWereDown |= m_buttonDownL && m_buttonDownR;

            if (m_bothWereDown && tmp_buttonDownL != tmp_buttonDownR)
            {
                m_field.ClickArea();
                m_bothWereDown = false;
            }
            else if (!tmp_buttonDownL && m_buttonDownL)
                m_field.Click();
            else if (tmp_buttonDownR && !m_buttonDownR && !m_buttonDownL && !tmp_buttonDownL)
                m_field.RightClick();

            m_buttonDownL = tmp_buttonDownL;
            m_buttonDownR = tmp_buttonDownR;

            UpdateVisuals();
        }

        void FieldUC_MouseLeave(object sender, MouseEventArgs e)
        {
            m_mouseOver = false;
            m_buttonDownL = false;
            m_buttonDownR = false;
            m_bothWereDown = false;
            UpdateVisuals();
        }

        void FieldUC_MouseEnter(object sender, MouseEventArgs e)
        {
            m_mouseOver = true;
            m_buttonDownL = e.LeftButton == MouseButtonState.Pressed;
            m_buttonDownR = e.RightButton == MouseButtonState.Pressed;
            m_bothWereDown = m_buttonDownL && m_buttonDownR;
            UpdateVisuals();

            MainWindow.debugLine.Text = m_field.ToString();
        }

        Color GetColorForValue()
        {
            //TODO include mode in decision
            Color c;
            switch (m_field.Value)
            {
                case -1:
                    c = Colors.Pink;
                    break;
                case 1:
                    c = Colors.LightBlue;
                    break;
                case 2:
                    c = Colors.Blue;
                    break;
                case 3:
                    c = Colors.Green;
                    break;
                case 4:
                    c = Colors.Orange;
                    break;
                case 5:
                    c = Colors.Yellow;
                    break;
                case 6:
                    c = Colors.White;
                    break;
                case 7:
                    c = Colors.Red;
                    break;
                case 8:
                    c = Colors.Violet;
                    break;
                default:
                    c = Colors.Black;
                    break;
            }

            return c;
        }
    }
}