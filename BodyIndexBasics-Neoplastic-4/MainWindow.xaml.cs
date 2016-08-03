//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyIndexBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using Microsoft.Kinect;
    using System.Timers;
    using System.Windows.Controls;
    using System.Diagnostics;

    #region Main window class
    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Mainwindow private variables


        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// Collection of colors to be used to display the BodyIndexFrame data.
        /// </summary>
        private static readonly uint[] BodyColor =
        {
           0xFFBFBFBF, 0xFFBFBFBF, 0xFFBFBFBF, 0xFFBFBFBF, 0xFFBFBFBF, 0xFFBFBFBF,
        };

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        //Various brushes for use
        private readonly Brush transparentBrush = new SolidColorBrush(Color.FromArgb(00, 0, 0, 0));
        private readonly Brush whiteBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
        private readonly Brush blueBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
        private readonly Brush yellowBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
        private readonly Brush redBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        private readonly Brush blackBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        private readonly Brush greyBrush = new SolidColorBrush(Color.FromArgb(255, 127, 127, 127));
        

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for body index frames
        /// </summary>
        private MultiSourceFrameReader MultiSourceFrameReader = null;

        /// <summary>
        /// Description of the data contained in the body index frame
        /// </summary>
        private FrameDescription bodyIndexFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap bodyIndexBitmap = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private uint[] bodyIndexPixels = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private double displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private double displayHeight;
        
        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        //list of joint positions for comparison (to calculate velocity)
        private Dictionary<JointType, Point> oldPositions = new Dictionary<JointType, Point>()
            {
            { JointType.Head, new Point(-10, -10) },
            { JointType.Neck,  new Point(-10, -10) },
            { JointType.SpineShoulder, new Point(-10, -10) },
            { JointType.SpineMid, new Point(-10, -10) },
            { JointType.SpineBase, new Point(-10, -10) },
            { JointType.ShoulderRight, new Point(-10, -10) },
            { JointType.ElbowRight, new Point(-10, -10) } ,
            { JointType.WristRight, new Point(-10, -10) } ,
            { JointType.HandRight, new Point(-10, -10) },
            { JointType.HandTipRight, new Point(-10, -10) },
            { JointType.ThumbRight, new Point(-10, -10) },
            { JointType.ShoulderLeft, new Point(-10, -10) },
            { JointType.ElbowLeft, new Point(-10, -10) },
            { JointType.WristLeft, new Point(-10, -10) } ,
            { JointType.HandLeft, new Point(-10, -10) },
            { JointType.HandTipLeft, new Point(-10, -10) },
            { JointType.ThumbLeft, new Point(-10, -10) },
            { JointType.HipLeft, new Point(-10, -10) } ,
            { JointType.KneeLeft, new Point(-10, -10) } ,
            { JointType.AnkleLeft, new Point(-10, -10) } ,
            { JointType.FootLeft, new Point(-10, -10) },
            { JointType.HipRight, new Point(-10, -10) },
            { JointType.KneeRight, new Point(-10, -10) },
            { JointType.AnkleRight, new Point(-10, -10) },
            { JointType.FootRight, new Point(-10, -10) }
            };

        //points for comparison
        private Point notFoundPt = new Point(-10, -10);
        private Point XYOriginPoint = new Point(-10, -10);
        private Point handLeftOriginPt = new Point(-10, -10);
        private Point handRightOriginPt = new Point(-10, -10);
        private Point footLeftOriginPt = new Point(-10, -10);
        private Point footRightOriginPt = new Point(-10, -10);

        //Variable arrays for velocity calculations
        private double[] velocitiesX = new double[25];
        private double[] velocitiesY = new double[25];
        private double avgVelocity = 0;

        //Arrays for X,Y, and Z coordinates of each joint
        private double[] jointXCoordinates = new double[25];
        private double[] jointYCoordinates = new double[25];
        private double[] jointZCoordinates = new double[25];
        private double[] avgXYZCoordinates = new double[3];
        
        //Timer variables
        private Timer myTimer = new Timer();
        private TimeSpan myTS = new TimeSpan();
        private Stopwatch mySW = new Stopwatch();
        private Stopwatch overallSW = new Stopwatch();
        private string minutes;
        private string seconds;

        //Particle emmitter variables 
        private ParticleEmitter[] emitters = {
            new ParticleEmitter(),
            new ParticleEmitter(),
            new ParticleEmitter(),
            new ParticleEmitter(),
            new ParticleEmitter(),
            new ParticleEmitter(),
            new ParticleEmitter(),
            new ParticleEmitter()
        };

        private DrawingVisualElement element = new DrawingVisualElement();

        //Random variable 
        private Random rnd = new Random();

        private uint[] myOutlinePixels = null;
        private double bodyHeight;
        private double bodyWidth;
        private double avgBodyX;
        private double avgBodyY;

        private Point rightExtreme;
        private Point leftExtreme;
        private Point lowerExtreme;
        private Point upperExtreme;

        private Canvas canvas = new Canvas();

        private bool leftMovement;
        private bool rightMovement;

        Polygon bodyPoly = new Polygon();

        #endregion

        #region MainWindow initialize
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // open the reader for the depth frames
            this.MultiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);

            // wire handler for frame arrival
            //this.bodyIndexFrameReader.FrameArrived += this.Reader_FrameArrived;
            this.MultiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            this.bodyIndexFrameDescription = this.kinectSensor.BodyIndexFrameSource.FrameDescription;

            // allocate space to put the pixels being converted
            this.bodyIndexPixels = new uint[(this.bodyIndexFrameDescription.Width )* this.bodyIndexFrameDescription.Height];
            this.myOutlinePixels = new uint[(this.bodyIndexFrameDescription.Width) * this.bodyIndexFrameDescription.Height];

            // create the bitmap to display
            this.bodyIndexBitmap = new WriteableBitmap(this.bodyIndexFrameDescription.Width , this.bodyIndexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgra32, null);

            // get size of joint space
            this.displayWidth = bodyIndexFrameDescription.Width ;
            this.displayHeight = bodyIndexFrameDescription.Height;

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            //Update/Initialize timer info
            this.myTimer.Elapsed += new ElapsedEventHandler(timer_Tick);
            this.myTimer.Interval = 1000;
            this.myTimer.Enabled = true;
            this.mySW.Start();
            this.overallSW.Start();

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
            
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;
            
            // initialize the components (controls) of the window
            this.InitializeComponent();
        }
        #endregion

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #region ImageSource and bitmap getters
        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource BodyImageSource
        {
            get
            {
                return this.bodyIndexBitmap;
            }
        }
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }
        #endregion

        #region Status text update
        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }
        #endregion

        #region MainWindow Closing
        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
            if (this.MultiSourceFrameReader != null)
            {
                // remove the event handler
                this.MultiSourceFrameReader.MultiSourceFrameArrived -= this.Reader_MultiSourceFrameArrived;

                // BodyIndexFrameReder is IDisposable
                this.MultiSourceFrameReader.Dispose();
                this.MultiSourceFrameReader = null;
            }


            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }
        #endregion


        #region frame arrived event handler
        /// <summary>
        /// Handles the body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            
            //Stopwatch update
            double millisecondsElapsed = mySW.ElapsedMilliseconds;
            mySW.Restart();
            //  Console.WriteLine(overallSW.ElapsedMilliseconds);
            timeText.Text = this.minutes + ":" + this.seconds; 

            updateMondrianSquares(overallSW.ElapsedMilliseconds);

            //Acquire new frame 
            MultiSourceFrame msf = e.FrameReference.AcquireFrame();
            
            bool bodyIndexFrameProcessed = false;
            bool dataReceived = false;
            
            if (msf != null)
            {
                using (BodyFrame bodyFrame = msf.BodyFrameReference.AcquireFrame())
                {
                    using (BodyIndexFrame bodyIndexFrame = msf.BodyIndexFrameReference.AcquireFrame())
                    {
                        #region process bodies, silhouette
                        if (bodyFrame != null && bodyIndexFrame != null)
                        {
                            //If there are no bodies currently being tracked, add them to bodies array
                            if (this.bodies == null)
                            {
                                this.bodies = new Body[bodyFrame.BodyCount];
                            }

                            // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                            // As long as those body objects are not disposed and not set to null in the array,
                            // those body objects will be re-used.
                            bodyFrame.GetAndRefreshBodyData(this.bodies);
                            dataReceived = true;

                            // the fastest way to process the body index data is to directly access 
                            // the underlying buffer
                            using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
                            {
                                // verify data and write the color data to the display bitmap
                                if (((this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height) == bodyIndexBuffer.Size) &&
                                (this.bodyIndexFrameDescription.Width == this.bodyIndexBitmap.PixelWidth) && (this.bodyIndexFrameDescription.Height == this.bodyIndexBitmap.PixelHeight))
                                {
                                    this.ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                                    bodyIndexFrameProcessed = true;
                                }
                            }
                        }

                        //if the bodyIndexFrame has been processed fully, update the pixels/bitmap 
                        if (bodyIndexFrameProcessed)
                        {
                            this.RenderBodyIndexPixels();
                        }
                        #endregion 

                        if (dataReceived)
                        {
                            int jointCount = 0;
                            
                            using (DrawingContext dc = this.drawingGroup.Open())
                            {
                                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(00, 00, 00, 00)), null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                                foreach (Body body in this.bodies)
                                {
                                    if (body.IsTracked)
                                    {
                                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                        // convert the joint points to depth (display) space
                                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                                        #region update joint points, velocities
                                        foreach (JointType jointType in joints.Keys)
                                        {

                                            // sometimes the depth(Z) of an inferred joint may show as negative
                                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                            CameraSpacePoint position = joints[jointType].Position;
                                            if (position.Z < 0)
                                            {
                                                position.Z = InferredZPositionClamp;
                                            }

                                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);

                                            //If first coordinates for velocity have not been established yet, assign current coordinates 
                                            if (oldPositions[jointType].X == -10 && oldPositions[jointType].Y == -10)
                                            {
                                                oldPositions[jointType] = new Point(jointPoints[jointType].X, jointPoints[jointType].Y);
                                            }
                                            else
                                            {
                                                //calculate the velocity of current joint, comparing old position to new position, divide by milliseconds elapsed
                                                velocitiesX[jointCount] = getVelocity(oldPositions[jointType].X, jointPoints[jointType].X, millisecondsElapsed, joints[jointType].Position.Z);
                                                velocitiesY[jointCount] = getVelocity(oldPositions[jointType].Y, jointPoints[jointType].Y, millisecondsElapsed, joints[jointType].Position.Z);
                                                
                                                //update old position
                                                oldPositions[jointType] = new Point(jointPoints[jointType].X, jointPoints[jointType].Y);

                                                jointXCoordinates[jointCount] = joints[jointType].Position.X;
                                                jointYCoordinates[jointCount] = joints[jointType].Position.Y;
                                                jointZCoordinates[jointCount] = joints[jointType].Position.Z;
                                            }
                                           
                                            //update joint count tracker
                                            jointCount++;
                                        }
                                        #endregion
                                        
                                        getAvgCoordinates();

                                        getAvgVelocity();

                                        bodyPolygon(jointPoints[JointType.Head], jointPoints[JointType.HandLeft], jointPoints[JointType.FootLeft], jointPoints[JointType.FootRight], jointPoints[JointType.HandRight]);

                                        #region check sideways and vertical movement
                                        if (XYOriginPoint.X == -10 && XYOriginPoint.Y  == -10)
                                        {
                                            XYOriginPoint = new Point(avgXYZCoordinates[0], avgXYZCoordinates[1]);
                                        }
                                        else
                                        {
                                            Point updatedPoint = new Point(avgXYZCoordinates[0], avgXYZCoordinates[1]);
                                            checkLeftRightMovement(XYOriginPoint, updatedPoint, avgXYZCoordinates[2]);
                                            checkUpDownMovement(XYOriginPoint, updatedPoint, avgXYZCoordinates[2]);
                                            XYOriginPoint = new Point(updatedPoint.X, updatedPoint.Y);
                                        }
                                        #endregion

                                        #region check for sweep movements
                                        if (handRightOriginPt == notFoundPt || handLeftOriginPt == notFoundPt || footLeftOriginPt == notFoundPt || footRightOriginPt == notFoundPt)
                                        {
                                            handRightOriginPt = jointPoints[JointType.HandRight];
                                            handLeftOriginPt = jointPoints[JointType.HandLeft];
                                            footLeftOriginPt = jointPoints[JointType.FootLeft];
                                            footRightOriginPt = jointPoints[JointType.FootRight];
                                        }
                                        else
                                        {
                                            checkForSweepMovement(handRightOriginPt, jointPoints[JointType.HandRight], handLeftOriginPt, jointPoints[JointType.HandLeft], footLeftOriginPt, jointPoints[JointType.FootLeft], footRightOriginPt, jointPoints[JointType.FootRight]);
                                            handRightOriginPt = jointPoints[JointType.HandRight];
                                            handLeftOriginPt = jointPoints[JointType.HandLeft];
                                            footLeftOriginPt = jointPoints[JointType.FootLeft];
                                            footRightOriginPt = jointPoints[JointType.FootRight];
                                        }
                                        #endregion 

                                        bodyOrientation(jointPoints[JointType.HipLeft], jointPoints[JointType.HipRight], jointPoints[JointType.ShoulderLeft], jointPoints[JointType.ShoulderRight], jointPoints[JointType.FootLeft], jointPoints[JointType.FootRight]);
                                       
                                       this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc, true);
                                       this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc, false);
                                       
                                       getRegionOccupied();
                                       updateRegionSquares();

                                        //if (this.overallSW.ElapsedMilliseconds >= 67552)
                                        if (this.overallSW.ElapsedMilliseconds >= 67552)
                                        {
                                            myGrid.Background = new SolidColorBrush(Colors.Black);

                                            #region particle emitters
                                            updateParticleEmitters(jointPoints[JointType.Head], 
                                                jointPoints[JointType.SpineShoulder],
                                                jointPoints[JointType.SpineMid], 
                                                jointPoints[JointType.SpineBase], 
                                                jointPoints[JointType.FootLeft],
                                                jointPoints[JointType.FootRight],
                                                jointPoints[JointType.HandLeft],
                                                jointPoints[JointType.HandRight],
                                                dc);
                                            #endregion
                                        }
                                    }

                                }
                               
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Process and Render body index pixels

        /// <summary>
        /// Directly accesses the underlying image buffer of the BodyIndexFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the bodyIndexFrameData pointer.
        /// </summary>
        /// <param name="bodyIndexFrameData">Pointer to the BodyIndexFrame image data</param>
        /// <param name="bodyIndexFrameDataSize">Size of the BodyIndexFrame image data</param>
        private unsafe void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {
            byte* frameData = (byte*)bodyIndexFrameData;

            int x = 0;
            int y = 0;

            double bodyXMin = this.kinectSensor.BodyIndexFrameSource.FrameDescription.Width;
            double bodyYMin = this.kinectSensor.BodyIndexFrameSource.FrameDescription.Height;
            double bodyXMax = 0;
            double bodyYMax = 0;

            bodyHeight = 0;
            bodyWidth = 0;

            // convert body index to a visual representation
            for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {

                //update x and y coordinates (x first, then y)
                if (x >= this.kinectSensor.BodyIndexFrameSource.FrameDescription.Width - 1)
                {
                    x = 0;
                    y++;
                }
                else
                {
                    x++;
                }


                // the BodyColor array has been sized to match
                // BodyFrameSource.BodyCount
                if (frameData[i] < BodyColor.Length)
                {
                    // this pixel is part of a player,
                    // display the appropriate color
                    this.bodyIndexPixels[i] = BodyColor[frameData[i]];

                    if (i > 0)
                    {
                        //if last pixel was transparent, add this [black] pixel to outline
                        if (this.bodyIndexPixels[i - 1] == 0x000000FF)
                        {
                             this.myOutlinePixels[i] = 0xFFFF0000;
                         }
                        //otherwise, add transparent pixel to outline array
                        else
                        {
                            this.myOutlinePixels[i] = 0x000000FF;
                        }
                    }

                    //Functions to find minimum and maximum of body pixels on X and Y planes 
                    if (x < bodyXMin)
                    {
                        bodyXMin = x;
                        leftExtreme = new Point(bodyXMin, y);
                    }
                    if (y < bodyYMin)
                    {
                        bodyYMin = y;
                        upperExtreme = new Point(x, bodyYMin);
                    }
                    if (x > bodyXMax)
                    {
                        bodyXMax = x;
                        rightExtreme = new Point(bodyXMax, y);
                    }
                    if (y > bodyYMax)
                    {
                        bodyYMax = y;
                        lowerExtreme = new Point(x, bodyYMax);
                    }
                }
                else
                {
                    // this pixel is not part of a player
                    // display transparent
                    this.bodyIndexPixels[i] = 0x000000FF;

                    if (i > 0)
                    {
                        //if the last pixel was not transparent, add curent pixel to outline
                        if (this.bodyIndexPixels[i - 1] != 0x000000FF)
                        {
                              this.myOutlinePixels[i] = 0xFF0000FF;
                        }
                        //otherwise, add transparent pixel to outline array
                        else
                        {
                            this.myOutlinePixels[i] = 0x000000FF;
                        }
                    }
                }
            }

            //Calculate average of X plane, Y plane based on mins and maxes
            avgBodyX = ((bodyXMax - (bodyXMax * 0.05)) + (bodyXMin + (bodyXMin * 0.05))) / 2;
            avgBodyY = ((bodyYMax - (bodyYMax * 0.05)) + (bodyYMin + (bodyYMin * 0.05))) / 2;

            //Find body width and height, given that a body of at least a pixel width occupies the frame
            if (bodyXMax > bodyXMin)
            {
                bodyWidth = bodyXMax - bodyXMin;
            }
            if (bodyYMax > bodyYMin)
            {
                bodyHeight = bodyYMax - bodyYMin;
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            this.bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this.bodyIndexBitmap.PixelWidth, this.bodyIndexBitmap.PixelHeight),
                this.myOutlinePixels,
                this.bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }
        #endregion

        #region Draw body, bones, hands, and clipped edges
        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext, bool leftHand)
        {
            int level = 255;

            switch (handState)
            {
                case HandState.NotTracked:
                    level = 255;
                    break;

                case HandState.Unknown:
                    level = 255;
                    break;

                case HandState.Open:
                    level = 230;
                    break;

                case HandState.Lasso:
                    level = 215;
                    break;

                case HandState.Closed:
                    level = 200;
                    break;
            }
            byte gradientByte = Convert.ToByte(level);


            if (this.overallSW.ElapsedMilliseconds < 12050)
            {
                leftHandRect.Fill = whiteBrush;
                rightHandRect.Fill = whiteBrush;
            }
            else if (this.overallSW.ElapsedMilliseconds >= 12050 && this.overallSW.ElapsedMilliseconds < 36500)
            {
                if (leftHand)
                {
                    leftHandRect.Fill = new SolidColorBrush(Color.FromArgb(255, gradientByte, gradientByte, gradientByte));
                }
                else
                {
                    rightHandRect.Fill = new SolidColorBrush(Color.FromArgb(255, gradientByte, gradientByte, gradientByte));
                }
            }
            else if (this.overallSW.ElapsedMilliseconds >= 36500 && this.overallSW.ElapsedMilliseconds < 53500)
            {
                if (leftHand)
                {
                    leftHandRect.Fill = new SolidColorBrush(Color.FromArgb(255, gradientByte, gradientByte, 255));
                }
                else
                {
                    rightHandRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, gradientByte));
                }
            }
            else if (overallSW.ElapsedMilliseconds >= 53500 && overallSW.ElapsedMilliseconds < 62758)
            {
                leftHandRect.Fill = blueBrush;
                rightHandRect.Fill = yellowBrush;
            }
            else
            {

                leftHandRect.Fill = whiteBrush;
                rightHandRect.Fill = whiteBrush;
            }

        }
        

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }
        #endregion

        #region Velocity Functions
        private double getVelocity(double oldPoint, double newPoint, double seconds, double distance)
        {
            //Get inverse value of joint distance from camera
            double relativeDistance = Math.Abs(3.5 - distance);
            
            double velocity = ((newPoint - oldPoint)/relativeDistance)/ seconds;
            return Math.Abs(velocity);
        }

        private void getAvgVelocity()
        {
            double total1 = 0;
            double total2 = 0;
            int length = 0;

            //Calculate avg velocities for x and y planes of joints
            foreach (double v in velocitiesX)
            {
                length++;
                total1 += v;
            }

            foreach (double v in velocitiesY)
            {
                total2 += v;
            }

            avgVelocity = 100*Math.Abs((total1 + total2) / (length * 2));
            
            double toBecomeByte = checkDoubleForByteConversion(255 - (avgVelocity/2));
          
            byte colorByte = Convert.ToByte(toBecomeByte);

            if (this.overallSW.ElapsedMilliseconds < 12050)
            {
                velocityRect.Fill = whiteBrush;
            }
            else if (this.overallSW.ElapsedMilliseconds >= 12050 && this.overallSW.ElapsedMilliseconds < 36500)
            {
                velocityRect.Fill = new SolidColorBrush(Color.FromArgb(255, colorByte, colorByte, colorByte));
            }
            else if (this.overallSW.ElapsedMilliseconds >= 36500 && this.overallSW.ElapsedMilliseconds < 53500)
            {
                velocityRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, colorByte, colorByte));
            }
            else if (overallSW.ElapsedMilliseconds >= 53500 && overallSW.ElapsedMilliseconds < 62758)
            {
                velocityRect.Fill = redBrush;
            }
            else
            {
                velocityRect.Fill = whiteBrush;
            }
        }
        #endregion

        #region getAvgCoordinates
        private void getAvgCoordinates()
        {
            double totalX = 0;
            double totalY = 0;
            double totalZ = 0;

            for (int i = 0; i < jointXCoordinates.Length; i++)
            {
                totalX += jointXCoordinates[i];
                totalY += jointYCoordinates[i];
                totalZ += jointZCoordinates[i];
            }

            avgXYZCoordinates[0] = totalX / jointXCoordinates.Length;
            avgXYZCoordinates[1] = totalY / jointYCoordinates.Length;
            avgXYZCoordinates[2] = totalZ / jointZCoordinates.Length;

        }
        #endregion

        #region getRegionOccupied, updateRegionSquares
        private void getRegionOccupied()
        {
            int xRegion = 0;
            int yRegion = 0;
            int zRegion = 0;

            double distance = avgXYZCoordinates[2];
            //Determine relative x and y ranges based on distance from camera
            double xRange = 0.6153 * distance;
            double yRange = 0.4615 * distance;

            //Determine appropriate 1/3 step ranges 
            double xStep = xRange - ((xRange * 2) / 3);
            double yStep = yRange - ((yRange * 2) / 3);

            //Check X region
            if (avgXYZCoordinates[0] < (-xStep))
            {
                xRegion = 1;
            }
            else if (avgXYZCoordinates[0] >= (-xStep) && avgXYZCoordinates[0] < xStep)
            {
                xRegion = 2;
            }
            else
            {
                xRegion = 3;
            }

            //Check Y region
            if (avgXYZCoordinates[1] < (-yStep))
            {
                yRegion = 1;
            }
            else if (avgXYZCoordinates[1] >= -yStep && avgXYZCoordinates[1] < yStep)
            {
                yRegion = 2;
            }
            else
            {
                yRegion = 3;
            }

            //Check Z region
            if (avgXYZCoordinates[2] < 2.0)
            {
                zRegion = 1;
            }
            else if (avgXYZCoordinates[2] >= 2.0 && avgXYZCoordinates[2] < 3.3)
            {
                zRegion = 2;
            }
            else
            {
                zRegion = 3;
            }
            regionText.Text = "XYZ region: " + xRegion + ", " + yRegion + ", " + zRegion;
            
        }
        
        private void updateRegionSquares()
        {
            byte xByte = Convert.ToByte(checkDoubleForByteConversion(255 - Math.Abs(avgXYZCoordinates[0] * 92.3)));
            byte yByte = Convert.ToByte(checkDoubleForByteConversion(255 - Math.Abs(avgXYZCoordinates[1] * 122.5)));
            byte zByte = Convert.ToByte(checkDoubleForByteConversion(255 - Math.Abs(avgXYZCoordinates[2] * 56.6)));

            if (this.overallSW.ElapsedMilliseconds < 12050)
            {
                xregionColorRect.Fill = whiteBrush;
                yregionColorRect.Fill = whiteBrush;
                zregionColorRect.Fill = whiteBrush;
            }
            else if (this.overallSW.ElapsedMilliseconds >= 12050 && this.overallSW.ElapsedMilliseconds < 36500)
            {
                xregionColorRect.Fill = new SolidColorBrush(Color.FromArgb(255, xByte, xByte, xByte));
                yregionColorRect.Fill = new SolidColorBrush(Color.FromArgb(255, yByte, yByte, yByte));
                zregionColorRect.Fill = new SolidColorBrush(Color.FromArgb(255, zByte, zByte, zByte));
            }
            else if (this.overallSW.ElapsedMilliseconds >= 36500 && this.overallSW.ElapsedMilliseconds < 53500)
            {
                xregionColorRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, xByte, xByte));
                yregionColorRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, yByte));
                zregionColorRect.Fill = new SolidColorBrush(Color.FromArgb(255, zByte, zByte, 255));
            }
            else if (this.overallSW.ElapsedMilliseconds >= 53500 && this.overallSW.ElapsedMilliseconds < 62758)
            {
                xregionColorRect.Fill = redBrush;
                yregionColorRect.Fill = yellowBrush;
                zregionColorRect.Fill = blueBrush;
            }
            else
            {
                xregionColorRect.Fill = whiteBrush;
                yregionColorRect.Fill = whiteBrush;
                zregionColorRect.Fill = whiteBrush;
            }
        }
        #endregion

        #region check for various movements
        private void checkLeftRightMovement(Point oldPoint, Point newPoint, double distance)
        {
            double xRange = 0.6153 * distance;
            double displacement = oldPoint.X - newPoint.X;
            

            if (displacement > xRange/10)
            {
                leftMovement = true;
                rightMovement = false;
            }
            else if (displacement < -(xRange/10))
            {
                rightMovement = true;
                leftMovement = false;
            }
            else
            {
                leftMovement = false;
                rightMovement = false;
            }

            byte gradientByte = Convert.ToByte(checkDoubleForByteConversion(255 - (Math.Abs(displacement)*100)));

            if (this.overallSW.ElapsedMilliseconds < 12050)
            {
                sidewaysMvmtRect.Fill = whiteBrush;
            }
            else if (this.overallSW.ElapsedMilliseconds >= 12050 && this.overallSW.ElapsedMilliseconds <= 36500)
            {
                sidewaysMvmtRect.Fill = new SolidColorBrush(Color.FromArgb(255, gradientByte, gradientByte, gradientByte));
            }
            else if (this.overallSW.ElapsedMilliseconds > 36500 && this.overallSW.ElapsedMilliseconds < 53500)
            {
                sidewaysMvmtRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, gradientByte));

            }
            else if (this.overallSW.ElapsedMilliseconds > 53500 && this.overallSW.ElapsedMilliseconds < 62758)
            {
                sidewaysMvmtRect.Fill = yellowBrush;
            }
            else
            {
                sidewaysMvmtRect.Fill = whiteBrush;
            }


        }

        private void checkUpDownMovement(Point oldPoint, Point newPoint, double distance)
        {
            double yRange = 0.4615 * distance;
            double displacement = oldPoint.Y - newPoint.Y;
            bool up;
            bool down;

            if (displacement > yRange / 10)
            {
                up = true;
                down = false;
            }
            else if (displacement < -(yRange / 10))
            {
                up = false;
                down = true;
            }
            else
            {
                up = false;
                down = false;
            }

            double toBecomeByte = checkDoubleForByteConversion(255 - (Math.Abs(displacement) * 1000));

            byte gradientByte = Convert.ToByte(toBecomeByte);

            if(this.overallSW.ElapsedMilliseconds < 12050)
            {
                verticalMvmtRect.Fill = whiteBrush;
            }
            else if (this.overallSW.ElapsedMilliseconds >= 12050 && this.overallSW.ElapsedMilliseconds <= 36500)
            {
                verticalMvmtRect.Fill = new SolidColorBrush(Color.FromArgb(255, gradientByte, gradientByte, gradientByte));
            }
            else if (this.overallSW.ElapsedMilliseconds > 36500 && this.overallSW.ElapsedMilliseconds < 53500)
            {
                verticalMvmtRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, gradientByte, gradientByte));
            }
            else if (this.overallSW.ElapsedMilliseconds > 53500 && this.overallSW.ElapsedMilliseconds < 62758)
            {
                verticalMvmtRect.Fill = redBrush;
            }
            else
            {
                verticalMvmtRect.Fill = whiteBrush;
            }
        }

        private void checkForSweepMovement(Point handRightOld, Point handRightNew, Point handLeftOld, Point handLeftNew, Point footLeftOld, Point footLeftNew, Point footRightOld, Point footRightNew)
        {
            double leftHandXDisplace = 0.6 *( Math.Abs(handLeftOld.X - handLeftNew.X) * avgXYZCoordinates[2]);
            double leftHandYDisplace = 0.4 * (Math.Abs(handLeftOld.Y - handLeftNew.Y) * avgXYZCoordinates[2]);
            double rightHandXDisplace = 0.6 * (Math.Abs(handRightOld.X - handRightNew.X)  * avgXYZCoordinates[2]);
            double rightHandYDisplace = 0.6 * (Math.Abs(handRightOld.Y - handRightNew.Y) * avgXYZCoordinates[2]);
            double leftFootDisplace = 0.6 * ( Math.Abs(footLeftOld.X - footLeftNew.X) * avgXYZCoordinates[2]);
            double rightFootDisplace = 0.6 * ( Math.Abs(footRightOld.X - footRightNew.X) * avgXYZCoordinates[2]);


            double toBecomeLHXByte = checkDoubleForByteConversion(255 - (leftHandXDisplace * 2));
            double toBecomeLHYByte = checkDoubleForByteConversion(255 - (leftHandYDisplace * 2));
            double toBecomeRHXByte = checkDoubleForByteConversion(255 - (rightHandXDisplace * 2));
            double toBecomeRHYByte = checkDoubleForByteConversion(255 - (rightHandYDisplace * 2));
            double toBecomeLFByte = checkDoubleForByteConversion(255 - (leftFootDisplace * 2));
            double toBecomeRFByte = checkDoubleForByteConversion(255 - (rightFootDisplace * 2));
            
            byte LHXByte = Convert.ToByte(toBecomeLHXByte);
            byte LHYByte = Convert.ToByte(toBecomeLHYByte);
            byte RHXByte = Convert.ToByte(toBecomeRHXByte);
            byte RHYByte = Convert.ToByte(toBecomeRHYByte);
            byte LFByte = Convert.ToByte(toBecomeLFByte);
            byte RFByte = Convert.ToByte(toBecomeRFByte);

            if(this.overallSW.ElapsedMilliseconds < 12050)
            {
                handLeftSweepXRect.Fill = whiteBrush;
                handLeftSweepYRect.Fill = whiteBrush;
                handRightSweepXRect.Fill = whiteBrush;
                handRightSweepYRect.Fill = whiteBrush;
                footLeftSweepRect.Fill = whiteBrush;
                footRightSweepRect.Fill = whiteBrush;
            }
            else if (this.overallSW.ElapsedMilliseconds >= 12050 && this.overallSW.ElapsedMilliseconds <= 36500)
            {
                handLeftSweepXRect.Fill = new SolidColorBrush(Color.FromArgb(255, LHXByte, LHXByte, LHXByte));
                handLeftSweepYRect.Fill = new SolidColorBrush(Color.FromArgb(255, LHYByte, LHYByte, LHYByte));
                handRightSweepXRect.Fill = new SolidColorBrush(Color.FromArgb(255, RHXByte, RHXByte, RHXByte));
                handRightSweepYRect.Fill = new SolidColorBrush(Color.FromArgb(255, RHYByte, RHYByte, RHYByte));
                footLeftSweepRect.Fill = new SolidColorBrush(Color.FromArgb(255, LFByte, LFByte, LFByte));
                footRightSweepRect.Fill = new SolidColorBrush(Color.FromArgb(255, RFByte, RFByte, RFByte));
            }
            else if (this.overallSW.ElapsedMilliseconds > 36500 && this.overallSW.ElapsedMilliseconds < 53500)
            {
                handLeftSweepXRect.Fill = new SolidColorBrush(Color.FromArgb(255, LHXByte, LHXByte, 255));
                handLeftSweepYRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, LHYByte, LHYByte ));
                handRightSweepXRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, RHXByte));
                handRightSweepYRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, RHYByte, RHYByte));
                footLeftSweepRect.Fill = new SolidColorBrush(Color.FromArgb(255, LFByte, LFByte, 255));
                footRightSweepRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, RFByte));
            }
            else if (this.overallSW.ElapsedMilliseconds > 53500 && this.overallSW.ElapsedMilliseconds < 62758)
            {
                handLeftSweepXRect.Fill = blueBrush;
                handLeftSweepYRect.Fill = redBrush;
                handRightSweepXRect.Fill = yellowBrush;
                handRightSweepYRect.Fill = redBrush;
                footLeftSweepRect.Fill = blueBrush;
                footRightSweepRect.Fill = yellowBrush;
            }
            else
            {
                handLeftSweepXRect.Fill = whiteBrush;
                handLeftSweepYRect.Fill = whiteBrush;
                handRightSweepXRect.Fill = whiteBrush;
                handRightSweepYRect.Fill = whiteBrush;
                footLeftSweepRect.Fill = whiteBrush;
                footRightSweepRect.Fill = whiteBrush;
            }


        }
        #endregion

        #region body orientation
        private void bodyOrientation(Point hipLeft, Point hipRight, Point shoulderLeft, Point shoulderRight, Point footLeft, Point footRight)
        {

            double hipDist = 0.6 * ((hipRight.X - hipLeft.X) * avgXYZCoordinates[2]);
            double shoulderDist = 0.6 * ((shoulderRight.X - shoulderLeft.X) * avgXYZCoordinates[2]);

            double displacement = ((hipDist * 2) + shoulderDist) / 2;
 
            double toBecomeByte = checkDoubleForByteConversion((Math.Abs(displacement) * 4.0));
            

            byte gradientByte = Convert.ToByte(toBecomeByte);

            if (this.overallSW.ElapsedMilliseconds < 12050)
            {
                orientationRect.Fill = whiteBrush;
            }
            if (this.overallSW.ElapsedMilliseconds >= 12050 && this.overallSW.ElapsedMilliseconds <= 36500)
            {
                orientationRect.Fill = new SolidColorBrush(Color.FromArgb(255, gradientByte, gradientByte, gradientByte));
            }
            else if (this.overallSW.ElapsedMilliseconds > 36500 && this.overallSW.ElapsedMilliseconds < 53500)
            {
                orientationRect.Fill = new SolidColorBrush(Color.FromArgb(255, gradientByte, gradientByte, 255));
            }
            else if (this.overallSW.ElapsedMilliseconds > 53500 && this.overallSW.ElapsedMilliseconds < 62758)
            {
                orientationRect.Fill = yellowBrush;
            }
            else
            {
                orientationRect.Fill = whiteBrush;
            }
        }
        #endregion

        private void bodyPolygon(Point head, Point leftHand, Point leftFoot, Point rightFoot, Point rightHand)
        {
            PointCollection bodyPolyPts = new PointCollection();
            bodyPolyPts.Add(head);
            bodyPolyPts.Add(leftHand);
            bodyPolyPts.Add(leftFoot);
            bodyPolyPts.Add(rightFoot);
            bodyPolyPts.Add(rightHand);
            bodyPoly.Points = bodyPolyPts;
        }

        #region particle functions
        private void updateParticlesEmitter0(ParticleEmitter emitter, DrawingContext dc)
        {
            emitter.particles.ForEach(p =>
            {
                var c = p.Color;

                c.A /= 2;

                if (this.overallSW.ElapsedMilliseconds < 88000)
                {
                    p.Color = Color.FromArgb(50, 255, 255, 255);

                    dc.DrawLine(new Pen(new SolidColorBrush(p.Color), 1), 
                        new Point(p.Position.X, p.Position.Y),
                        new Point(p.Position.X, p.Position.Y + rnd.Next(-200, 200))
                              );
                }
                else if (this.overallSW.ElapsedMilliseconds >= 88000 && this.overallSW.ElapsedMilliseconds < 106000)
                {
                    // Rect myRect = new Rect(p.Position.X, p.Position.Y, 5, 5);
                    p.Color = Color.FromArgb(50, 255, 255, 255);


                    dc.DrawEllipse(new SolidColorBrush(p.Color), null, p.Position, 5, 5);
                    dc.DrawEllipse(new SolidColorBrush(p.Color), null, p.Position, 2, 2);
                }
                else if (this.overallSW.ElapsedMilliseconds >= 106000)
                {
                    p.Color = Color.FromArgb(50, 0, 0, 255);


                    dc.DrawEllipse(new SolidColorBrush(p.Color), null, p.Position, 7, 7);
                    dc.DrawEllipse(new SolidColorBrush(p.Color), null, p.Position, 5, 5);
                }
            });
        }

        private void updateParticlesEmitter1(ParticleEmitter emitter, DrawingContext dc)
        {
            emitter.particles.ForEach(p =>
            {
                var c = p.Color;

                c.A /= 2;

                if (this.overallSW.ElapsedMilliseconds < 88000)
                {
                    p.Color = Color.FromArgb(50, 255, 255, 255);

                    dc.DrawLine(new Pen(new SolidColorBrush(p.Color), 1),
                        new Point(p.Position.X, p.Position.Y),
                        new Point(p.Position.X, p.Position.Y + rnd.Next(-200, 200))
                              );
                }
                else if (this.overallSW.ElapsedMilliseconds >= 88000 && this.overallSW.ElapsedMilliseconds < 106000)
                {
                    p.Color = Color.FromArgb(50, 255, 255, 255);

                    // Rect myRect = new Rect(p.Position.X, p.Position.Y, 5, 5);

                    dc.DrawEllipse(new SolidColorBrush(p.Color), null, p.Position, 5, 5);
                    dc.DrawEllipse(new SolidColorBrush(p.Color), null, p.Position, 2, 2);
                }
                else if (this.overallSW.ElapsedMilliseconds >= 106000)
                {
                    p.Color = Color.FromArgb(50, 0, 0, 255);

                    dc.DrawEllipse( new SolidColorBrush(p.Color), null, p.Position, 7, 7);
                    dc.DrawEllipse(new SolidColorBrush(p.Color), null, p.Position, 5, 5);
                }
            });
        }


        private void updateParticlesEmitter2(ParticleEmitter emitter, DrawingContext dc)
        {
            emitter.particles.ForEach(p =>
           {
               if (this.overallSW.ElapsedMilliseconds < 88000)
               {
                   p.Color = Color.FromArgb(50, 255, 255, 255); 

                   dc.DrawRectangle(new SolidColorBrush(p.Color),
                       null,
                       new Rect(new Point(p.Position.X - (bodyWidth/2) - 25 , p.Position.Y - (bodyHeight/2) - 25),
                       new Size(bodyWidth+50, bodyHeight+50))
                       );
               }
               else if (this.overallSW.ElapsedMilliseconds >= 88000 && this.overallSW.ElapsedMilliseconds < 106000)
               {
                   //dc.DrawLine(new Pen(new SolidColorBrush(p.Color), 3), p.Position, leftExtreme);
                   //dc.DrawLine(new Pen(new SolidColorBrush(p.Color), 3), leftExtreme, lowerExtreme);
                   //dc.DrawLine(new Pen(new SolidColorBrush(p.Color), 3), lowerExtreme, rightExtreme);
                   //dc.DrawLine(new Pen(new SolidColorBrush(p.Color), 3), rightExtreme, p.Position);

                   p.Color = Color.FromArgb(50, 255, 255, 255);
                   //   dc.DrawRectangle(new SolidColorBrush(p.Color), null, );

                   dc.DrawEllipse(null, new Pen(new SolidColorBrush(p.Color), 1), p.Position, bodyWidth, bodyHeight);
               }
               else if (this.overallSW.ElapsedMilliseconds >= 106000)
               {
                   p.Color = Color.FromArgb(50, 255, 0, 0);

                   dc.DrawEllipse(
                         new SolidColorBrush(p.Color),
                         null, p.Position, 4, 4);

                   dc.DrawEllipse(
                        new SolidColorBrush(p.Color),
                        null, p.Position, 2, 2);
               }

           });
        }

        private void updateParticlesEmitter3(ParticleEmitter emitter, DrawingContext dc)
        {
            
            emitter.particles.ForEach(p =>
            {
                if (this.overallSW.ElapsedMilliseconds < 106000)
                {
                    p.Color = Color.FromArgb(0, 0, 0, 0);
                }
                else if (this.overallSW.ElapsedMilliseconds >= 106000)
                {
                    p.Color = Color.FromArgb(50, 0, 0, 255);
                }

                dc.DrawEllipse(
                         new SolidColorBrush(p.Color),
                         null, p.Position, 4, 4);

                dc.DrawEllipse(
                     new SolidColorBrush(p.Color),
                     null, p.Position, 2, 2);
            });
        }


        private void updateParticleEmitters(Point p0, Point p1, Point p2, Point p3, Point p4, Point p5, Point p6, Point p7, DrawingContext dc)
        {
            emitters[0].Update();
            emitters[1].Update();
            emitters[2].Update();
            emitters[3].Update();

            updateParticlesEmitter0(emitters[0], dc);
            updateParticlesEmitter1(emitters[1], dc);
            updateParticlesEmitter2(emitters[2], dc);
            updateParticlesEmitter3(emitters[3], dc);

            canvas.Children.Clear();
            canvas.Children.Add(element);

            if ( this.overallSW.ElapsedMilliseconds < 88000)
            {
                #region section 1
                emitters[0].Center = p3;
                emitters[1].Center = p3;
                emitters[2].Center = p3;
                emitters[3].Center = p3;

                emitters[0].angle = 1.6;
                emitters[0].speed = 250;
                emitters[0].lifespan = 0.5;


                emitters[1].angle = 4.75;
                emitters[1].speed = 250;
                emitters[1].lifespan = 0.5;

                emitters[2].angle = 1.5;
                emitters[2].speed = 5;
                emitters[2].lifespan = 0.5;

                emitters[3].angle = 0;
                emitters[3].speed = 0;
                emitters[3].lifespan = 0;
                #endregion
            }
            else if (this.overallSW.ElapsedMilliseconds >= 88000 && this.overallSW.ElapsedMilliseconds < 106000)
            {
                #region section 2
                emitters[0].Center = new Point(0, avgBodyY);
                emitters[1].Center = new Point(500, avgBodyY);
                emitters[2].Center = p1;
                emitters[3].Center = p3;

                emitters[0].angle = (2 * Math.PI * rnd.NextDouble()) / 4 + 0.75;
                emitters[0].speed = 240;
                emitters[0].lifespan = 2;

                emitters[1].angle = (2 * Math.PI * rnd.NextDouble()) / 4 + 4;
                emitters[1].speed = 450;
                emitters[1].lifespan = 2;

                emitters[2].angle = 0;
                emitters[2].speed = 5;
                emitters[2].lifespan = 0.5;

                emitters[3].angle = 0;
                emitters[3].speed = 0;
                emitters[3].lifespan = 0;
                #endregion
            }
            else if (this.overallSW.ElapsedMilliseconds >= 106000)
            {
                #region section 3

                emitters[0].Center = p0;
                emitters[1].Center = p1;
                emitters[2].Center = p2;
                emitters[3].Center = p3;

                #region angles
                if (leftMovement == true && rightMovement == false)
                {
                    emitters[0].angle = 1.6;
                    emitters[1].angle = 1.6;
                    emitters[2].angle = 1.6;
                    emitters[3].angle = 1.6;
                }
                else if (rightMovement == true && leftMovement == false)
                {
                    emitters[0].angle = 4.75;
                    emitters[1].angle = 4.75;
                    emitters[2].angle = 4.75;
                    emitters[3].angle = 4.75;
                }
                else if (rightMovement == false && leftMovement == false)
                {
                    emitters[0].angle = (2 * Math.PI * rnd.NextDouble());
                    emitters[1].angle = (2 * Math.PI * rnd.NextDouble());
                    emitters[2].angle = (2 * Math.PI * rnd.NextDouble());
                    emitters[3].angle = (2 * Math.PI * rnd.NextDouble());
                }
                #endregion

                emitters[0].speed = 10;
                emitters[1].speed = 10;
                emitters[2].speed = 10;
                emitters[3].speed = 10;

                emitters[0].lifespan = 1;
                emitters[1].lifespan = 1;
                emitters[2].lifespan = 1;
                emitters[3].lifespan = 1;
                #endregion
            }
        }
        #endregion

        private void updateMondrianSquares(double milliseconds)
        {
            #region stroke
            if (milliseconds < 507)
            {
                xregionColorRect.Stroke = whiteBrush;
                yregionColorRect.Stroke = whiteBrush;
                zregionColorRect.Stroke = whiteBrush;
                sidewaysMvmtRect.Stroke = whiteBrush;
                verticalMvmtRect.Stroke = whiteBrush;
                handLeftSweepXRect.Stroke = whiteBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = whiteBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 507 && milliseconds < 1308)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = whiteBrush;
                zregionColorRect.Stroke = whiteBrush;
                sidewaysMvmtRect.Stroke = whiteBrush;
                verticalMvmtRect.Stroke = whiteBrush;
                handLeftSweepXRect.Stroke = whiteBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = whiteBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 1308 && milliseconds < 2028)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = whiteBrush;
                sidewaysMvmtRect.Stroke = whiteBrush;
                verticalMvmtRect.Stroke = whiteBrush;
                handLeftSweepXRect.Stroke = whiteBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = whiteBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 2028 && milliseconds < 2775)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = whiteBrush;
                verticalMvmtRect.Stroke = whiteBrush;
                handLeftSweepXRect.Stroke = whiteBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = whiteBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 2775 && milliseconds < 4616)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = whiteBrush;
                verticalMvmtRect.Stroke = whiteBrush;
                handLeftSweepXRect.Stroke = whiteBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 4616 && milliseconds < 5604)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = whiteBrush;
                handLeftSweepXRect.Stroke = whiteBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 5604 && milliseconds < 5750)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = whiteBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 5750 && milliseconds < 6137)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 6137 && milliseconds < 6524)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = blackBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 6524 && milliseconds < 7298)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = blackBrush;
                rightHandRect.Stroke = blackBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 7298 && milliseconds < 8045)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = blackBrush;
                footRightSweepRect.Stroke = blackBrush;
                rightHandRect.Stroke = blackBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 8045 && milliseconds < 8792)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = blackBrush;
                footLeftSweepRect.Stroke = blackBrush;
                footRightSweepRect.Stroke = blackBrush;
                rightHandRect.Stroke = blackBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;
            }
            else if (milliseconds >= 8792 && milliseconds < 10620)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = blackBrush;
                footLeftSweepRect.Stroke = blackBrush;
                footRightSweepRect.Stroke = blackBrush;
                rightHandRect.Stroke = blackBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = blackBrush;
            }
            else if (milliseconds >= 10620 && milliseconds < 11621)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = blackBrush;
                footLeftSweepRect.Stroke = blackBrush;
                footRightSweepRect.Stroke = blackBrush;
                rightHandRect.Stroke = blackBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = blackBrush;
                orientationRect.Stroke = blackBrush;
            }
            else if (milliseconds >= 11621 && milliseconds < 11781)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = blackBrush;
                handRightSweepYRect.Stroke = blackBrush;
                footLeftSweepRect.Stroke = blackBrush;
                footRightSweepRect.Stroke = blackBrush;
                rightHandRect.Stroke = blackBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = blackBrush;
                orientationRect.Stroke = blackBrush;
            }
            else if (milliseconds >= 11781 && milliseconds < 62758)
            {
                xregionColorRect.Stroke = blackBrush;
                yregionColorRect.Stroke = blackBrush;
                zregionColorRect.Stroke = blackBrush;
                sidewaysMvmtRect.Stroke = blackBrush;
                verticalMvmtRect.Stroke = blackBrush;
                handLeftSweepXRect.Stroke = blackBrush;
                handLeftSweepYRect.Stroke = blackBrush;
                handRightSweepXRect.Stroke = blackBrush;
                handRightSweepYRect.Stroke = blackBrush;
                footLeftSweepRect.Stroke = blackBrush;
                footRightSweepRect.Stroke = blackBrush;
                rightHandRect.Stroke = blackBrush;
                leftHandRect.Stroke = blackBrush;
                velocityRect.Stroke = blackBrush;
                orientationRect.Stroke = blackBrush;
            }
            else if (milliseconds >= 62758)
            {
                xregionColorRect.Stroke = whiteBrush;
                yregionColorRect.Stroke = whiteBrush;
                zregionColorRect.Stroke = whiteBrush;
                sidewaysMvmtRect.Stroke = whiteBrush;
                verticalMvmtRect.Stroke = whiteBrush;
                handLeftSweepXRect.Stroke = whiteBrush;
                handLeftSweepYRect.Stroke = whiteBrush;
                handRightSweepXRect.Stroke = whiteBrush;
                handRightSweepYRect.Stroke = whiteBrush;
                footLeftSweepRect.Stroke = whiteBrush;
                footRightSweepRect.Stroke = whiteBrush;
                rightHandRect.Stroke = whiteBrush;
                leftHandRect.Stroke = whiteBrush;
                velocityRect.Stroke = whiteBrush;
                orientationRect.Stroke = whiteBrush;

            }
            #endregion

            #region height and widths
            double[] startingHeights = { 248, 485, 107, 95, 172, 212, 112, 310, 137, 97, 112, 209, 394, 399, 164 };
            double[] heights = new double[15];
            double[] startingWidths = { 197, 440, 432, 331, 301, 322, 322, 254, 301, 187, 364, 440, 454, 616, 214 };
            double[] widths = new double[15];
            double[] heightChanges = { 41, 80, 17, 15, 28, 35, 18, 51, 22, 16, 18, 34, 65, 66, 27 };
            double[] widthChanges = { 32, 73, 72, 55, 50, 53, 53, 42, 50, 31, 60, 73, 75, 102, 35 };

            if (this.overallSW.ElapsedMilliseconds < 54512)
            {
                xregionColorRect.Height = startingHeights[0];
                yregionColorRect.Height = startingHeights[1];
                zregionColorRect.Height = startingHeights[2];
                sidewaysMvmtRect.Height = startingHeights[3];
                verticalMvmtRect.Height = startingHeights[4];
                handLeftSweepXRect.Height = startingHeights[5];
                handLeftSweepYRect.Height = startingHeights[6];
                footLeftSweepRect.Height = startingHeights[7];
                handRightSweepXRect.Height = startingHeights[8];
                handRightSweepYRect.Height = startingHeights[9];
                footRightSweepRect.Height = startingHeights[10];
                rightHandRect.Height = startingHeights[11];
                leftHandRect.Height = startingHeights[12];
                velocityRect.Height = startingHeights[13];
                orientationRect.Height = startingHeights[14];

                xregionColorRect.Width = startingWidths[0];
                yregionColorRect.Width = startingWidths[1];
                zregionColorRect.Width = startingWidths[2];
                sidewaysMvmtRect.Width = startingWidths[3];
                verticalMvmtRect.Width = startingWidths[4];
                handLeftSweepXRect.Width = startingWidths[5];
                handLeftSweepYRect.Width = startingWidths[6];
                footLeftSweepRect.Width = startingWidths[7];
                handRightSweepXRect.Width = startingWidths[8];
                handRightSweepYRect.Width = startingWidths[9];
                footRightSweepRect.Width = startingWidths[10];
                rightHandRect.Width = startingWidths[11];
                leftHandRect.Width = startingWidths[12];
                velocityRect.Width = startingWidths[13];
                orientationRect.Width = startingWidths[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 54512 && this.overallSW.ElapsedMilliseconds < 55224)
            {
                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] = startingHeights[i] - (heightChanges[i] * 1);
                }
                xregionColorRect.Height = heights[0];
                yregionColorRect.Height = heights[1];
                zregionColorRect.Height = heights[2];
                sidewaysMvmtRect.Height = heights[3];
                verticalMvmtRect.Height = heights[4];
                handLeftSweepXRect.Height = heights[5];
                handLeftSweepYRect.Height = heights[6];
                footLeftSweepRect.Height = heights[7];
                handRightSweepXRect.Height = heights[8];
                handRightSweepYRect.Height = heights[9];
                footRightSweepRect.Height = heights[10];
                rightHandRect.Height = heights[11];
                leftHandRect.Height = heights[12];
                velocityRect.Height = heights[13];
                orientationRect.Height = heights[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 55224 && this.overallSW.ElapsedMilliseconds < 56105)
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] = startingWidths[i] - (widthChanges[i] * 1);
                }

                xregionColorRect.Width = widths[0];
                yregionColorRect.Width = widths[1];
                zregionColorRect.Width = widths[2];
                sidewaysMvmtRect.Width = widths[3];
                verticalMvmtRect.Width = widths[4];
                handLeftSweepXRect.Width = widths[5];
                handLeftSweepYRect.Width = widths[6];
                footLeftSweepRect.Width = widths[7];
                handRightSweepXRect.Width = widths[8];
                handRightSweepYRect.Width = widths[9];
                footRightSweepRect.Width = widths[10];
                rightHandRect.Width = widths[11];
                leftHandRect.Width = widths[12];
                velocityRect.Width = widths[13];
                orientationRect.Width = widths[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 56105 && this.overallSW.ElapsedMilliseconds < 56766)
            {
                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] = startingHeights[i] - (heightChanges[i] * 2);
                }
                xregionColorRect.Height = heights[0];
                yregionColorRect.Height = heights[1];
                zregionColorRect.Height = heights[2];
                sidewaysMvmtRect.Height = heights[3];
                verticalMvmtRect.Height = heights[4];
                handLeftSweepXRect.Height = heights[5];
                handLeftSweepYRect.Height = heights[6];
                footLeftSweepRect.Height = heights[7];
                handRightSweepXRect.Height = heights[8];
                handRightSweepYRect.Height = heights[9];
                footRightSweepRect.Height = heights[10];
                rightHandRect.Height = heights[11];
                leftHandRect.Height = heights[12];
                velocityRect.Height = heights[13];
                orientationRect.Height = heights[14];

            }
            else if (this.overallSW.ElapsedMilliseconds >= 56766 && this.overallSW.ElapsedMilliseconds < 58496)
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] = startingWidths[i] - (widthChanges[i] * 2);
                }

                xregionColorRect.Width = widths[0];
                yregionColorRect.Width = widths[1];
                zregionColorRect.Width = widths[2];
                sidewaysMvmtRect.Width = widths[3];
                verticalMvmtRect.Width = widths[4];
                handLeftSweepXRect.Width = widths[5];
                handLeftSweepYRect.Width = widths[6];
                footLeftSweepRect.Width = widths[7];
                handRightSweepXRect.Width = widths[8];
                handRightSweepYRect.Width = widths[9];
                footRightSweepRect.Width = widths[10];
                rightHandRect.Width = widths[11];
                leftHandRect.Width = widths[12];
                velocityRect.Width = widths[13];
                orientationRect.Width = widths[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 58496 && this.overallSW.ElapsedMilliseconds < 59616)
            {
                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] = startingHeights[i] - (heightChanges[i] * 3);
                }
                xregionColorRect.Height = heights[0];
                yregionColorRect.Height = heights[1];
                zregionColorRect.Height = heights[2];
                sidewaysMvmtRect.Height = heights[3];
                verticalMvmtRect.Height = heights[4];
                handLeftSweepXRect.Height = heights[5];
                handLeftSweepYRect.Height = heights[6];
                footLeftSweepRect.Height = heights[7];
                handRightSweepXRect.Height = heights[8];
                handRightSweepYRect.Height = heights[9];
                footRightSweepRect.Height = heights[10];
                rightHandRect.Height = heights[11];
                leftHandRect.Height = heights[12];
                velocityRect.Height = heights[13];
                orientationRect.Height = heights[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 59616 && this.overallSW.ElapsedMilliseconds < 59765)
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] = startingWidths[i] - (widthChanges[i] * 3);
                }

                xregionColorRect.Width = widths[0];
                yregionColorRect.Width = widths[1];
                zregionColorRect.Width = widths[2];
                sidewaysMvmtRect.Width = widths[3];
                verticalMvmtRect.Width = widths[4];
                handLeftSweepXRect.Width = widths[5];
                handLeftSweepYRect.Width = widths[6];
                footLeftSweepRect.Width = widths[7];
                handRightSweepXRect.Width = widths[8];
                handRightSweepYRect.Width = widths[9];
                footRightSweepRect.Width = widths[10];
                rightHandRect.Width = widths[11];
                leftHandRect.Width = widths[12];
                velocityRect.Width = widths[13];
                orientationRect.Width = widths[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 59765 && this.overallSW.ElapsedMilliseconds < 60523)
            {
                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] = startingHeights[i] - (heightChanges[i] * 4);
                }
                xregionColorRect.Height = heights[0];
                yregionColorRect.Height = heights[1];
                zregionColorRect.Height = heights[2];
                sidewaysMvmtRect.Height = heights[3];
                verticalMvmtRect.Height = heights[4];
                handLeftSweepXRect.Height = heights[5];
                handLeftSweepYRect.Height = heights[6];
                footLeftSweepRect.Height = heights[7];
                handRightSweepXRect.Height = heights[8];
                handRightSweepYRect.Height = heights[9];
                footRightSweepRect.Height = heights[10];
                rightHandRect.Height = heights[11];
                leftHandRect.Height = heights[12];
                velocityRect.Height = heights[13];
                orientationRect.Height = heights[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 60523 && this.overallSW.ElapsedMilliseconds < 61294)
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] = startingWidths[i] - (widthChanges[i] * 4);
                }

                xregionColorRect.Width = widths[0];
                yregionColorRect.Width = widths[1];
                zregionColorRect.Width = widths[2];
                sidewaysMvmtRect.Width = widths[3];
                verticalMvmtRect.Width = widths[4];
                handLeftSweepXRect.Width = widths[5];
                handLeftSweepYRect.Width = widths[6];
                footLeftSweepRect.Width = widths[7];
                handRightSweepXRect.Width = widths[8];
                handRightSweepYRect.Width = widths[9];
                footRightSweepRect.Width = widths[10];
                rightHandRect.Width = widths[11];
                leftHandRect.Width = widths[12];
                velocityRect.Width = widths[13];
                orientationRect.Width = widths[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 61294 && this.overallSW.ElapsedMilliseconds < 62020)
            {
                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] = startingHeights[i] - (heightChanges[i] * 5);
                }
                xregionColorRect.Height = heights[0];
                yregionColorRect.Height = heights[1];
                zregionColorRect.Height = heights[2];
                sidewaysMvmtRect.Height = heights[3];
                verticalMvmtRect.Height = heights[4];
                handLeftSweepXRect.Height = heights[5];
                handLeftSweepYRect.Height = heights[6];
                footLeftSweepRect.Height = heights[7];
                handRightSweepXRect.Height = heights[8];
                handRightSweepYRect.Height = heights[9];
                footRightSweepRect.Height = heights[10];
                rightHandRect.Height = heights[11];
                leftHandRect.Height = heights[12];
                velocityRect.Height = heights[13];
                orientationRect.Height = heights[14];
            }
            else if (this.overallSW.ElapsedMilliseconds >= 62020 && this.overallSW.ElapsedMilliseconds < 62758)
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] = startingWidths[i] - (widthChanges[i] * 5);
                }

                xregionColorRect.Width = widths[0];
                yregionColorRect.Width = widths[1];
                zregionColorRect.Width = widths[2];
                sidewaysMvmtRect.Width = widths[3];
                verticalMvmtRect.Width = widths[4];
                handLeftSweepXRect.Width = widths[5];
                handLeftSweepYRect.Width = widths[6];
                footLeftSweepRect.Width = widths[7];
                handRightSweepXRect.Width = widths[8];
                handRightSweepYRect.Width = widths[9];
                footRightSweepRect.Width = widths[10];
                rightHandRect.Width = widths[11];
                leftHandRect.Width = widths[12];
                velocityRect.Width = widths[13];
                orientationRect.Width = widths[14];

            }
            else if (this.overallSW.ElapsedMilliseconds >= 62758)
            {
                xregionColorRect.Width = 0;
                yregionColorRect.Width = 0;
                zregionColorRect.Width = 0;
                sidewaysMvmtRect.Width = 0;
                verticalMvmtRect.Width = 0;
                handLeftSweepXRect.Width = 0;
                handLeftSweepYRect.Width = 0;
                footLeftSweepRect.Width = 0;
                handRightSweepXRect.Width = 0;
                handRightSweepYRect.Width = 0;
                footRightSweepRect.Width = 0;
                rightHandRect.Width = 0;
                leftHandRect.Width = 0;
                velocityRect.Width = 0;
                orientationRect.Width = 0;

                xregionColorRect.Height = 0;
                yregionColorRect.Height = 0;
                zregionColorRect.Height = 0;
                sidewaysMvmtRect.Height = 0;
                verticalMvmtRect.Height = 0;
                handLeftSweepXRect.Height = 0;
                handLeftSweepYRect.Height = 0;
                footLeftSweepRect.Height = 0;
                handRightSweepXRect.Height = 0;
                handRightSweepYRect.Height = 0;
                footRightSweepRect.Height = 0;
                rightHandRect.Height = 0;
                leftHandRect.Height = 0;
                velocityRect.Height = 0;
                orientationRect.Height = 0;

                xregionColorRect.StrokeThickness = 0;
                yregionColorRect.StrokeThickness = 0;
                zregionColorRect.StrokeThickness= 0;
                sidewaysMvmtRect.StrokeThickness = 0;
                verticalMvmtRect.StrokeThickness = 0;
                handLeftSweepXRect.StrokeThickness = 0;
                handLeftSweepYRect.StrokeThickness = 0;
                footLeftSweepRect.StrokeThickness = 0;
                handRightSweepXRect.StrokeThickness = 0;
                handRightSweepYRect.StrokeThickness = 0;
                footRightSweepRect.StrokeThickness = 0;
                rightHandRect.StrokeThickness = 0;
                leftHandRect.StrokeThickness = 0;
                velocityRect.StrokeThickness= 0;
                orientationRect.StrokeThickness = 0;
            }

            #endregion

        }

        #region checkDoubleForByteConversion
        private double checkDoubleForByteConversion(double check)
        {
            if (check > 255)
            {
                return 255;
            }
            else if ( check < 0)
            {
                return 0;
            }
            else
            {
                return check;
            }
        }
        #endregion

        #region timer functions
        private void timer_Tick(object sender, EventArgs e)
        {
            myTS = myTS.Add(TimeSpan.FromSeconds(1));
          //  Console.WriteLine(string.Format("{0}:{1}", myTS.Minutes, myTS.Seconds));
            this.minutes = myTS.Minutes.ToString();
            this.seconds = myTS.Seconds.ToString();
        }

        private void timerResetButton_Click(object sender, RoutedEventArgs e)
        {
            myTS = TimeSpan.Zero;
            overallSW.Reset();
            overallSW.Start();
        }
        #endregion

        #region Sensor change event handler
        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
        #endregion

        #region Labels for squares button event handlers
        private void showLabelsButton_Click(object sender, RoutedEventArgs e)
        {
            xregionIntextBox.Foreground = greyBrush;
            yregionIntextBox.Foreground = greyBrush;
            zregionIntextBox.Foreground = greyBrush;
            sidewaysMvmttextBox.Foreground = greyBrush;
            verticalMvmttextBox.Foreground = greyBrush;
            handLeftSweepXtextBox.Foreground = greyBrush;
            handLeftSweepYtextBox.Foreground = greyBrush;
            handRightSweepXtextBox.Foreground = greyBrush;
            handRightSweepYtextBox.Foreground = greyBrush;
            footLeftSweeptextBox.Foreground = greyBrush;
            footRightSweeptextBox.Foreground = greyBrush;
            handRighttextBox.Foreground = greyBrush;
            handLefttextBox.Foreground = greyBrush;
            velocitytextBox.Foreground = greyBrush;
            orientationtextBox.Foreground = greyBrush;

        }

        private void hideLabelsButton_Click(object sender, RoutedEventArgs e)
        {
            xregionIntextBox.Foreground = transparentBrush;
            yregionIntextBox.Foreground = transparentBrush;
            zregionIntextBox.Foreground = transparentBrush;
            sidewaysMvmttextBox.Foreground = transparentBrush;
            verticalMvmttextBox.Foreground = transparentBrush;
            handLeftSweepXtextBox.Foreground = transparentBrush;
            handLeftSweepYtextBox.Foreground = transparentBrush;
            handRightSweepXtextBox.Foreground = transparentBrush;
            handRightSweepYtextBox.Foreground = transparentBrush;
            footLeftSweeptextBox.Foreground = transparentBrush;
            footRightSweeptextBox.Foreground = transparentBrush;
            handRighttextBox.Foreground = transparentBrush;
            handLefttextBox.Foreground = transparentBrush;
            velocitytextBox.Foreground = transparentBrush;
            orientationtextBox.Foreground = transparentBrush;
        }
        #endregion
    }


    #region Particle Emitter classes
    class DrawingVisualElement : FrameworkElement
    {
        public DrawingVisual visual;

        public DrawingVisualElement() { visual = new DrawingVisual(); }

        protected override int VisualChildrenCount { get { return 1; } }

        protected override Visual GetVisualChild(int index) { return visual; }
    }

    public class Particle
    {
        public Point Position;
        public Point Velocity;
        public Color Color;
        public double Lifespan;
        public double Elapsed;

        public void Update(double elapsedSeconds)
        {
            Elapsed += elapsedSeconds;
            if (Elapsed > Lifespan)
            {
                Color.A = 0;
                return;
            }
            Color.A = (byte)(255 - ((255 * Elapsed)) / Lifespan);
            Position.X += Velocity.X * elapsedSeconds;
            Position.Y += Velocity.Y * elapsedSeconds;
        }
    }

    #endregion


    public class ParticleEmitter
    {
        public Point Center { get; set; }
        public double angle { get; set; }
        public double speed { get; set; }
        public double lifespan { get; set; }
        public List<Particle> particles = new List<Particle>();
        Random rand = new Random();
        public WriteableBitmap TargetBitmap;
        public WriteableBitmap ParticleBitmap;

        void CreateParticle()
        {
            //var speed = rand.Next(20) + 140;
            //var angle = (2 * Math.PI * rand.NextDouble());
            
            //triangle
            //var angle = (2 * Math.PI * (rand.NextDouble() / 8)) + 1.1;

            // straight down
            //var angle = 6.25;

            //straight up
            // var angle = 3.1;

            particles.Add(
                new Particle()
                {
                    Position = new Point(Center.X, Center.Y),
                    Velocity = new Point(
                        Math.Sin(angle) * speed,
                        Math.Cos(angle) * speed),
                    Color = Color.FromRgb(50, 50, 50),
                    Lifespan = lifespan
                });
        }

        public void Update()
        {
            var updateInterval = .003;

            CreateParticle();

            particles.RemoveAll(p =>
            {
                p.Update(updateInterval);
                return p.Color.A == 0;
            });
        }
    }
    #endregion

}
