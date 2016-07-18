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

        public MainWindow()
        {
            this.myTimer.Elapsed += new ElapsedEventHandler(timer_Tick);
            this.myTimer.Interval = 1000;
            this.myTimer.Enabled = true;
            

            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            // System.Drawing.Color c = ColorTranslator.FromHtml("#FFCCDD");
            //  System.Windows.Media.Color color = System.Windows.Media.Color.FromRgb(c.R, c.G, c.B);
            //rectangleColor.Color = color;
            byte myByte1 = Convert.ToByte(redCtr % 255);
            byte myByte2 = Convert.ToByte(greenCtr % 255);
            byte myByte3 = Convert.ToByte(blueCtr % 255);

            byte c_R = myByte1;
            byte c_G = myByte2;
            byte c_B = myByte3;
            SolidColorBrush myBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, c_R, c_G, c_B));
            myRectangle.Fill = myBrush;
            
                redCtr += 10;
                greenCtr += 10;
                // blueCtr += 10;
           
        }
        

        private void timer_Tick(object sender, EventArgs e)
        {
            myTS = myTS.Add(TimeSpan.FromSeconds(1));
          //  Console.WriteLine(string.Format("{0}:{1}", myTS.Minutes, myTS.Seconds));
            
        }
    }
}
