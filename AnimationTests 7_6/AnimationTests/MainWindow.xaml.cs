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
        private Storyboard myLineSB;

        private const int linePoints = 70;
        private const int circleNum = 80;
        private const int rainCirNum = 200;

        private Ellipse[] myCircles = new Ellipse[circleNum];
        private Ellipse[] rainCircle = new Ellipse[rainCirNum];

        private const int linePtCirNum = 4;
        private Ellipse[] linePointCircles = new Ellipse[linePtCirNum];

        private Random rnd = new Random();

        private const int numOfLines = 5;
        private Line[] myLines = new Line[numOfLines];
        private int startX;
        private int startY;
        private int endX;
        private int endY;
        private int YMove = 10;
        private int XMove = 10;
        private int XlineDuration = 3;
        private int YlineDuration = 3;
        
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
            myLineSB = new Storyboard();


            //this.wanderingCircles();
            //this.insertLine();
            //this.rainChunk();
            // this.insertSimpleLine();


            this.createLines();
            bool keepLinesMoving = true;

            //while (keepLinesMoving == true)
            //{
            //    this.animateLines();
            //}
            



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
                randomNums[i] = rndLn.Next(0, (int)myCanvas.Width);
                randomNums[i + 1] = rndLn.Next(0, (int)myCanvas.Height);
                myPCollection.Add(new Point(randomNums[i], randomNums[i + 1]));
            }
            myLine.Points = myPCollection;
            myCanvas.Children.Add(myLine);
            
        }

        private void insertSimplestLine(Line line, int x1, int y1, int x2, int y2, int lineCount)
        {
            line.Stroke = Brushes.Crimson;
            line.StrokeThickness = 2;
            line.X1 = x1;
            line.Y1 = y1;
            line.X2 = x2;
            line.Y2 = y2;
            myCanvas.Children.Add(line);
        }

        private void animateSimplestLine(Line line, int x1, int y1, int x2, int y2, int lineIndex)
        {
            int newY1 = y1;
            int newY2 = y2;
            int newX1 = x1;
            int newX2 = x2;
            
            newY1 += YMove;

            YMove = rnd.Next(-75, 75);
            newY2 += YMove;
           
            newX1 += XMove;

            XMove = rnd.Next(-75, 75);
            newX2 += XMove;

            DoubleAnimation Y1anim = new DoubleAnimation(line.Y1, newY1, TimeSpan.FromSeconds(YlineDuration));
            DoubleAnimation X1anim = new DoubleAnimation(line.X1, newX1, TimeSpan.FromSeconds(XlineDuration));
            Storyboard.SetTargetProperty(Y1anim, new PropertyPath("(Line.Y1)"));
            Storyboard.SetTargetProperty(X1anim, new PropertyPath("(Line.X1)"));

            //Y1anim.RepeatBehavior = RepeatBehavior.Forever;
            //X1anim.RepeatBehavior = RepeatBehavior.Forever;
            //Y1anim.AutoReverse = true;
            //X1anim.AutoReverse = true;

            YlineDuration = rnd.Next(5, 10);
            XlineDuration = rnd.Next(5, 10);

            DoubleAnimation Y2anim = new DoubleAnimation(line.Y2, newY2, TimeSpan.FromSeconds(YlineDuration));
            DoubleAnimation X2anim = new DoubleAnimation(line.X2, newX2, TimeSpan.FromSeconds(XlineDuration));
            Storyboard.SetTargetProperty(Y2anim, new PropertyPath("(Line.Y2)"));
            Storyboard.SetTargetProperty(X2anim, new PropertyPath("(Line.X2)"));


            //Y2anim.RepeatBehavior = RepeatBehavior.Forever;
            //X2anim.RepeatBehavior = RepeatBehavior.Forever;
            //Y2anim.AutoReverse = true;
            //X2anim.AutoReverse = true;

            line.X1 = newX1;
            line.Y1 = newY1;
            line.X2 = newX2;
            line.Y2 = newY2;
            

            myLineSB.Children.Add(Y1anim);
            myLineSB.Children.Add(X1anim);
            myLineSB.Children.Add(Y2anim);
            myLineSB.Children.Add(X2anim);

            line.BeginStoryboard(myLineSB);
            
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

        private void createLines()
        {
            startX = rnd.Next(15, (int)myCanvas.Width - 15);
            startY = rnd.Next(15, (int)myCanvas.Height - 15);
            endX = rnd.Next(15, (int)myCanvas.Width - 15);
            endY = rnd.Next(15, (int)myCanvas.Height - 15);

            for (int i = 0; i < numOfLines; i++)
            {
                myLines[i] = new Line();

                this.insertSimplestLine(myLines[i], startX, startY, endX, endY, i);

                startX = endX;
                startY = endY;

                endX = rnd.Next(15, (int)myCanvas.Width - 15);
                endY = rnd.Next(15, (int)myCanvas.Height - 15);
            }
        }

        private void animateLines()
        {
            for (int i = 0; i < numOfLines; i++)
            {
                this.animateSimplestLine(myLines[i], (int)myLines[i].X1, (int)myLines[i].Y1, (int)myLines[i].X2, (int)myLines[i].Y2, i);
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
