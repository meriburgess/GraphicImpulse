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
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;

namespace rectangleRainbowFill
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Timer myTimer = new Timer();
        private TimeSpan myTS = new TimeSpan();
        private int buttonClickCtr = 0;
        private int redCtr = 0;
        private int greenCtr = 0;
        private int blueCtr = 0;
        int colorChangeCtr = 0;

        public MainWindow()
        {
            this.myTimer.Elapsed += new ElapsedEventHandler(timer_Tick);
            this.myTimer.Interval = 1000;
            this.myTimer.Enabled = true;
            

            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Red: " + redCtr + " Blue: " + blueCtr + " Green: " + greenCtr + " Counter: " + colorChangeCtr);
            byte myByte1 = Convert.ToByte(redCtr % 255);
            byte myByte2 = Convert.ToByte(greenCtr % 255);
            byte myByte3 = Convert.ToByte(blueCtr % 255);
            
            SolidColorBrush myBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, myByte1, myByte2, myByte3));
            myRectangle.Fill = myBrush;

            if ((redCtr >= 250 && blueCtr >= 250 && greenCtr >= 250) || ((redCtr <= 0 && blueCtr <= 0 && greenCtr <= 0) && colorChangeCtr > 0))
            {
                colorChangeCtr += 1;
            }

            if (colorChangeCtr % 2 == 0)
            {
                redCtr += 10;
                greenCtr += 10;
                blueCtr += 10;
            }
            else if (colorChangeCtr % 2 == 1)
            {
                redCtr -= 10;
                greenCtr -= 10;
                blueCtr -= 10;
            }

            

        }
        

        private void timer_Tick(object sender, EventArgs e)
        {
            myTS = myTS.Add(TimeSpan.FromSeconds(1));
          //  Console.WriteLine(string.Format("{0}:{1}", myTS.Minutes, myTS.Seconds));
            
        }
    }
}
