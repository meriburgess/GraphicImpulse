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
using System.Diagnostics;
using System.Timers;

namespace AnimationTests
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private Brush[] myBrushes =
        {
            new SolidColorBrush(Color.FromRgb(232, 102, 63)),
            new SolidColorBrush(Color.FromRgb(232, 221, 63)),
            new SolidColorBrush(Color.FromRgb(78, 200, 119)),
            new SolidColorBrush(Color.FromRgb(24, 219,193)),
            new SolidColorBrush(Color.FromRgb(99, 149, 255)),
            new SolidColorBrush(Color.FromRgb(121, 103, 219)),
        };

        private Storyboard mySBoard;

        private const int linePoints = 70;
        private const int circleNum = 80;
        private const int rainCirNum = 200;

        private Ellipse[] myCircles = new Ellipse[circleNum];
        private Ellipse[] rainCircle = new Ellipse[rainCirNum];

        private const int linePtCirNum = 4;
        private Ellipse[] linePointCircles = new Ellipse[linePtCirNum];

        private Random rnd = new Random();
        
        private Timer myTimer = new Timer();
        private TimeSpan myTS = new TimeSpan();

        public MainWindow()
        {
            NameScope.SetNameScope(this, new NameScope());
            
            InitializeComponent();
            
            myTimer.Elapsed += new ElapsedEventHandler(timer_Tick);
            myTimer.Interval = 1000;
            myTimer.Enabled = true;

            mySBoard = new Storyboard();

            //this.wanderingCircles();
            //this.insertLine();
            //this.rainChunk();
            // this.insertSimpleLine();
            this.insertSimplestLine(10, 10, 110, 100);
            Console.WriteLine(myTimer);

            


        }
        private void addCircle(int width, int height, Brush myBrush, int x, int y, Ellipse myCircle)
        {
            myCircle.Fill = myBrush;
            myCircle.Height = height;
            myCircle.Width = width;
            Canvas.SetLeft(myCircle, x);
            Canvas.SetTop(myCircle, y);
            myCanvas.Children.Add(myCircle);   
        }

        private void insertLine()
        {
            Polyline myLine = new Polyline();
            myLine.Stroke = Brushes.BlueViolet;
            myLine.StrokeThickness = 2;
            PointCollection myPCollection = new PointCollection();
            Random rndLn = new Random();
            int[] randomNums = new int[linePoints];

            for (int i = 0; i < linePoints; i += 2)
            {
                randomNums[i] = rndLn.Next(0, 600);
                randomNums[i + 1] = rndLn.Next(0, 600);
                myPCollection.Add(new Point(randomNums[i], randomNums[i + 1]));
            }
            myLine.Points = myPCollection;
            myCanvas.Children.Add(myLine);



        }

        private void insertSimpleLine()
        {
            Polyline mySimpleLine = new Polyline();
            mySimpleLine.Stroke = Brushes.BlueViolet;
            mySimpleLine.StrokeThickness = 2;
            
            PointCollection myPCollection = new PointCollection();
            myPCollection.Add(new Point(25, 250));
            myPCollection.Add(new Point(175, 200));
            myPCollection.Add(new Point(325, 250));
            myPCollection.Add(new Point(475, 200));

           
            PointCollection myEndPts = new PointCollection();
            myEndPts.Add(new Point(25, 200));
            myEndPts.Add(new Point(175, 250));
            myEndPts.Add(new Point(325, 200));
            myEndPts.Add(new Point(475, 250));

            
            mySimpleLine.Points = myPCollection;
            myCanvas.Children.Add(mySimpleLine);


            for (int i = 0; i < linePtCirNum; i++)
            {
                linePointCircles[i] = new Ellipse();
                int size = 20;
                int canvas_x = 25 * (i * 6) + 25 - size / 2;
                int canvas_y;
                int pathTime = 3;
                if (i % 2 == 0)
                {
                    canvas_y = 250 - size / 2;
                }
                else
                {
                    canvas_y = 200 - size / 2;
                }
                addCircle(size, size, myBrushes[i], canvas_x, canvas_y, linePointCircles[i]);
                this.animateCircleOpacity(linePointCircles[i], 1.0, 0.0, 5);
                this.animateCirclePath(0, 0, linePointCircles[i], i, 3, false, true);
            }
        }

        private void insertSimplestLine(int x1, int y1, int x2, int y2)
        {
            Line myLine = new Line();
            myLine.Stroke = Brushes.Crimson;
            myLine.StrokeThickness = 2;
            myLine.X1 = x1;
            myLine.Y2 = y2;
           // myLine.X2 = x2;
            //myLine.Y2 = y2;
            myCanvas.Children.Add(myLine);
            Storyboard sb = new Storyboard();
            DoubleAnimation Yanim = new DoubleAnimation(myLine.Y2, 10, new Duration(new TimeSpan(1,0,1)));
            DoubleAnimation Xanim = new DoubleAnimation(myLine.X2, 10, new Duration(new TimeSpan(1, 0, 1)));
            Storyboard.SetTargetProperty(Yanim, new PropertyPath("(Line.Y2)"));
            sb.Children.Add(Yanim);
            Storyboard.SetTargetProperty(Xanim, new PropertyPath("(Line.X2)"));
            sb.Children.Add(Xanim);

            myLine.BeginStoryboard(sb);
        }



        private void animateSimplestLine(Line myLine, int startX, int startY, int endX, int endY, int Y2Time)
        {
            DoubleAnimation Yanim = new DoubleAnimation();
            Yanim.From = startY;
            Yanim.To = endY;
            Yanim.Duration= new Duration(TimeSpan.FromSeconds(Y2Time));
            Yanim.RepeatBehavior = RepeatBehavior.Forever;
            Yanim.AutoReverse = true;

            Storyboard.SetTargetProperty(Yanim, new PropertyPath("(Line.Y2"));
            mySBoard.Children.Add(Yanim);

        }


        private void animateCircleSize(Ellipse myCircle, int width, int height, int widthTime, int heightTime)
        {
            DoubleAnimation widthAnim = new DoubleAnimation();
            widthAnim.From = width;
            widthAnim.To = width / 2;
            widthAnim.Duration = new Duration(TimeSpan.FromSeconds(widthTime));
            widthAnim.RepeatBehavior = RepeatBehavior.Forever;
            widthAnim.AutoReverse = true;

            DoubleAnimation heightAnim = new DoubleAnimation();
            heightAnim.From = height;
            heightAnim.To = height / 2;
            heightAnim.Duration = new Duration(TimeSpan.FromSeconds(heightTime));
            heightAnim.RepeatBehavior = RepeatBehavior.Forever;
            heightAnim.AutoReverse = true;

            Storyboard.SetTarget(widthAnim, myCircle);
            Storyboard.SetTarget(heightAnim, myCircle);

            Storyboard.SetTargetProperty(widthAnim, new PropertyPath(Ellipse.WidthProperty));
            Storyboard.SetTargetProperty(heightAnim, new PropertyPath(Ellipse.HeightProperty));

            mySBoard.Children.Add(widthAnim);
            mySBoard.Children.Add(heightAnim);
            
            myCircle.Loaded += new RoutedEventHandler(myCircleLoaded);
        }

        private void animateCircleOpacity(Ellipse myCircle, Double opacityStart, Double opacityEnd, int animateTime)
        {
            DoubleAnimation opacityAnim = new DoubleAnimation();
            opacityAnim.From = opacityStart;
            opacityAnim.To = opacityEnd;
            opacityAnim.Duration = new Duration(TimeSpan.FromSeconds(animateTime));
            opacityAnim.RepeatBehavior = RepeatBehavior.Forever;
            opacityAnim.AutoReverse = true;
            
            Storyboard.SetTarget(opacityAnim, myCircle);

            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(Ellipse.OpacityProperty));
            
            mySBoard.Children.Add(opacityAnim);
            
            myCircle.Loaded += new RoutedEventHandler(myCircleLoaded);
            
            
        }
        

        private void animateCirclePath(int x, int y, Ellipse myCircle, int cirNum, int timeSpan, bool rain, bool upAndDown)
        {
            TranslateTransform animTT = new TranslateTransform();
            
            myCircle.RenderTransform = animTT;

            this.RegisterName("AnimTT" + cirNum.ToString(), animTT);

            PathGeometry animationPath = new PathGeometry();
            PathFigure pFigure = new PathFigure();
            pFigure.StartPoint = new Point(x, y);
            
            if (rain == true)
            {
                int placement = cirNum % 50;
                PolyLineSegment pSeg = new PolyLineSegment();
                pSeg.Points.Add(new Point(placement * 10, 500));
                pSeg.Points.Add(new Point(placement * 10, 500));
                pSeg.Points.Add(new Point(x, y));
                pFigure.Segments.Add(pSeg);
            }
            else if (upAndDown == true)
            {
                PolyLineSegment pSeg = new PolyLineSegment();
                int yChange = y; 
                if (cirNum % 2 == 0)
                {
                    yChange = y - 50; 
                }
                else
                {
                    yChange = y + 50;
                }
                pSeg.Points.Add(new Point(x, yChange));
                pSeg.Points.Add(new Point(x, y));
                pFigure.Segments.Add(pSeg);
            }
            else
            {
                PolyBezierSegment pSeg = new PolyBezierSegment();
                int[] randomPoints = new int[8];
                for (int i = 0; i < 8; i += 2)
                {
                    randomPoints[i] = rnd.Next(0, 300);
                    randomPoints[i + 1] = rnd.Next(0, 300);
                    pSeg.Points.Add(new Point(randomPoints[i], randomPoints[i + 1]));
                    pFigure.Segments.Add(pSeg);
                }

            }


            animationPath.Figures.Add(pFigure);

            animationPath.Freeze();

            if (rain == false || upAndDown == false)
            {
                DoubleAnimationUsingPath translateXAnim = new DoubleAnimationUsingPath();
                translateXAnim.PathGeometry = animationPath;
                translateXAnim.Duration = TimeSpan.FromSeconds(timeSpan);
                translateXAnim.Source = PathAnimationSource.X;

                Storyboard.SetTargetName(translateXAnim, "AnimTT" + cirNum.ToString());
                Storyboard.SetTargetProperty(translateXAnim, new PropertyPath(TranslateTransform.XProperty));

                mySBoard.Children.Add(translateXAnim);

                translateXAnim.RepeatBehavior = RepeatBehavior.Forever;

                translateXAnim.AutoReverse = true;
            }

            DoubleAnimationUsingPath translateYAnim = new DoubleAnimationUsingPath();
            translateYAnim.PathGeometry = animationPath;
            translateYAnim.Duration = TimeSpan.FromSeconds(timeSpan);
            translateYAnim.Source = PathAnimationSource.Y;

            Storyboard.SetTargetName(translateYAnim, "AnimTT" + cirNum.ToString());
            Storyboard.SetTargetProperty(translateYAnim, new PropertyPath(TranslateTransform.YProperty));
            
            mySBoard.Children.Add(translateYAnim); 

            translateYAnim.RepeatBehavior = RepeatBehavior.Forever;
            
         
            if (rain != true)
            {
                
                translateYAnim.AutoReverse = true;
            }

            myCircle.Loaded += new RoutedEventHandler(myCircleLoaded);
            
        }
        

        private void removeCircle(Ellipse myCircle)
        {
            myCanvas.Children.Remove(myCircle);
        }


        private void updatePoint(Ellipse myEllipse, int x, int y)
        {
            Canvas.SetLeft(myEllipse, x);
            Canvas.SetTop(myEllipse, y);
        }

        private void wanderingCircles()
        {
            int startPtX;
            int startPtY;
            int sizeOfCir;
            int sizeTime;
            int opacityTime;
            double opacityLow;
            int pathTime;

            for (int i = 0; i < circleNum; i++)
            {
                myCircles[i] = new Ellipse();
                int ofBrushes = i % myBrushes.Length;
                startPtX = rnd.Next(0, 300);
                startPtY = rnd.Next(0, 300);
                sizeOfCir = rnd.Next(5, 40);
                sizeTime = rnd.Next(2, 10);
                opacityTime = rnd.Next(2, 10);
                opacityLow = rnd.NextDouble();
                pathTime = rnd.Next(5, 15);

                this.addCircle(sizeOfCir, sizeOfCir, myBrushes[ofBrushes], startPtX, startPtY, myCircles[i]);
                this.animateCircleSize(myCircles[i], sizeOfCir, sizeOfCir, sizeTime, sizeTime);
                this.animateCircleOpacity(myCircles[i], 1.0, opacityLow, opacityTime);
                this.animateCirclePath(0, 0, myCircles[i], i, pathTime, false, false);
            }
        }

        private void rainChunk()
        {
            int XCount = 0;
            int YCount = 0;

            for (int i = 0; i < rainCirNum; i++)
            {
                rainCircle[i] = new Ellipse();
                if (i % 50 == 0)
                {
                    XCount = 0;
                }
                if (YCount >= rainCirNum / 10)
                {
                    YCount = 0;
                }

                this.addCircle(3, 3, myBrushes[4], XCount * 10, YCount, rainCircle[i]);
                this.animateCirclePath(XCount * 10, YCount, rainCircle[i], i, 1, true, false);

                XCount++;
                YCount += 10;
            }

        }

        private void myCircleLoaded(object sender, RoutedEventArgs e)
        {
            mySBoard.Begin(this);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            myTS = myTS.Add(TimeSpan.FromSeconds(2));
            Console.WriteLine(string.Format("{0}:{1}", myTS.Minutes, myTS.Seconds));
            
        }
        

    }

}
