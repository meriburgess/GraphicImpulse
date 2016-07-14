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

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// Collection of colors to be used to display the BodyIndexFrame data.
        /// </summary>
        private static readonly uint[] BodyColor =
        {
           0xCCBFBFBF, 0xCCBFBFBF, 0xCCBFBFBF, 0xCCBFBFBF, 0xCCBFBFBF, 0xCCBFBFBF,
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

        private readonly Brush transparentBrush = new SolidColorBrush(Color.FromArgb(00, 0, 0, 0));
        private readonly Brush whiteBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
        private readonly Brush blueBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
        private readonly Brush yellowBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
        private readonly Brush redBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        private readonly Brush blackBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        private readonly Brush greyBrush = new SolidColorBrush(Color.FromArgb(255, 125, 125, 125));

        

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));

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
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        private Dictionary<JointType, Point> oldPositions = new Dictionary<JointType, Point>()
            {
            { JointType.Head, new Point(-1, -1) },
            { JointType.Neck,  new Point(-1, -1) },
            { JointType.SpineShoulder, new Point(-1, -1) },
            { JointType.SpineMid, new Point(-1, -1) },
            { JointType.SpineBase, new Point(-1, -1) },
            { JointType.ShoulderRight, new Point(-1, -1) },
            { JointType.ElbowRight, new Point(-1, -1) } ,
            { JointType.WristRight, new Point(-1, -1) } ,
            { JointType.HandRight, new Point(-1, -1) },
            { JointType.HandTipRight, new Point(-1, -1) },
            { JointType.ThumbRight, new Point(-1, -1) },
            { JointType.ShoulderLeft, new Point(-1, -1) },
            { JointType.ElbowLeft, new Point(-1, -1) },
            { JointType.WristLeft, new Point(-1, -1) } ,
            { JointType.HandLeft, new Point(-1, -1) },
            { JointType.HandTipLeft, new Point(-1, -1) },
            { JointType.ThumbLeft, new Point(-1, -1) },
            { JointType.HipLeft, new Point(-1, -1) } ,
            { JointType.KneeLeft, new Point(-1, -1) } ,
            { JointType.AnkleLeft, new Point(-1, -1) } ,
            { JointType.FootLeft, new Point(-1, -1) },
            { JointType.HipRight, new Point(-1, -1) },
            { JointType.KneeRight, new Point(-1, -1) },
            { JointType.AnkleRight, new Point(-1, -1) },
            { JointType.FootRight, new Point(-1, -1) }
            };

        private Point notFoundPt = new Point(-10, -10);
        private Point handLeftOriginPt = new Point(-10, -10);
        private Point handRightOriginPt = new Point(-10, -10);
        private Point footLeftOriginPt = new Point(-10, -10);
        private Point footRightOriginPt = new Point(-10, -10);

        private Point XYOriginPoint = new Point(-10, -10);

        private double[] velocitiesX = new double[25];
        private double[] velocitiesY = new double[25];
        private double avgVelocity = 0;


        private double[] jointXCoordinates = new double[25];
        private double[] jointYCoordinates = new double[25];
        private double[] jointZCoordinates = new double[25];
        private double[] avgXYZCoordinates = new double[3];
        

        private Timer myTimer = new Timer();
        private TimeSpan myTS = new TimeSpan();
        private string minutes;
        private string seconds;

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

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 3));
            this.bodyColors.Add(new Pen(Brushes.Orange, 3));
            this.bodyColors.Add(new Pen(Brushes.Green, 3));
            this.bodyColors.Add(new Pen(Brushes.Blue, 3));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 3));
            this.bodyColors.Add(new Pen(Brushes.Violet, 3));

            this.myTimer.Elapsed += new ElapsedEventHandler(timer_Tick);
            this.myTimer.Interval = 1000;
            this.myTimer.Enabled = true;

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

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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



        /// <summary>
        /// Handles the body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame msf = e.FrameReference.AcquireFrame();
            bool bodyIndexFrameProcessed = false;
            bool dataReceived = false;

            timeText.Text = this.minutes + ":" + this.seconds;
            if ((this.myTS.Minutes <= 0 && this.myTS.Seconds >= 10 && this.myTS.Seconds <= 30) || (this.myTS.Minutes <= 0 && this.myTS.Seconds >= 40 && this.myTS.Seconds <= 59))
            {
              //  testRect.Fill = new BrushConverter().ConvertFromString("#FFFFFF");
            }
            else
            {
                testRect.Fill = whiteBrush;
            }


            if (msf != null)
            {
                using (BodyFrame bodyFrame = msf.BodyFrameReference.AcquireFrame())
                {
                    using (BodyIndexFrame bodyIndexFrame = msf.BodyIndexFrameReference.AcquireFrame())
                    {
                        if (bodyFrame != null && bodyIndexFrame != null)
                        {
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

                        if (bodyIndexFrameProcessed)
                        {
                            this.RenderBodyIndexPixels();
                        }

                        if (dataReceived)
                        {
                            int jointCount = 0;

                            using (DrawingContext dc = this.drawingGroup.Open())
                            {
                                // Draw a transparent background to set the render size
                                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(00, 00, 00, 00)), null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                                int penIndex = 0;
                                foreach (Body body in this.bodies)
                                {
                                    Pen drawPen = this.bodyColors[penIndex++];

                                    if (body.IsTracked)
                                    {
                                     //   this.DrawClippedEdges(body, dc);

                                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                        // convert the joint points to depth (display) space
                                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

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
                                            if (oldPositions[jointType].X == -1 && oldPositions[jointType].Y == -1)
                                            {
                                                oldPositions[jointType] = new Point(jointPoints[jointType].X, jointPoints[jointType].Y);
                                            }
                                            else
                                            {
                                                //calculate the velocity of current joint, comparing old position to new position
                                                //velocitiesX[jointCount] = getVelocity(oldPositions[jointType].X, jointPoints[jointType].X);
                                                velocitiesY[jointCount] = getVelocity(oldPositions[jointType].Y, jointPoints[jointType].Y);

                                                //update old position
                                                oldPositions[jointType] = new Point(jointPoints[jointType].X, jointPoints[jointType].Y);

                                                jointXCoordinates[jointCount] = joints[jointType].Position.X;
                                                jointYCoordinates[jointCount] = joints[jointType].Position.Y;
                                                jointZCoordinates[jointCount] = joints[jointType].Position.Z;

                                            }
                                           
                                            //update joint count tracker
                                            jointCount++;
                                        }
                                        

                                        getAvgCoordinates();
                                        getAvgVelocity();

                                        if (XYOriginPoint.X == -10 && XYOriginPoint.Y  == -10)
                                        {
                                            XYOriginPoint = new Point(avgXYZCoordinates[0], avgXYZCoordinates[1]);
                                        }
                                        else
                                        {
                                            Point updatedPoint = new Point(avgXYZCoordinates[0], avgXYZCoordinates[1]);
                                            checkLeftRightMovement(XYOriginPoint, updatedPoint);
                                            checkUpDownMovement(XYOriginPoint, updatedPoint);
                                            XYOriginPoint = new Point(updatedPoint.X, updatedPoint.Y);
                                        }

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

                                        bodyOrientation(jointPoints[JointType.HipLeft], jointPoints[JointType.HipRight], jointPoints[JointType.ShoulderLeft], jointPoints[JointType.ShoulderRight], jointPoints[JointType.FootLeft], jointPoints[JointType.FootRight]);
                                       
                                   //     this.DrawBody(joints, jointPoints, dc, drawPen);
                                        this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc, true);
                                       this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc, false);
                                        //   Console.WriteLine("X: " + avgXYZCoordinates[0] + " Y: " + avgXYZCoordinates[1] + " Z: " + avgXYZCoordinates[2]);
                                   
                                        getRegionOccupied();
                                    }
                                    
                                }
                                // prevent drawing outside of our render area
                                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                            }
                        }
                    }
                }
            }
        }

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

            // convert body index to a visual representation
            for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {
                // the BodyColor array has been sized to match
                // BodyFrameSource.BodyCount
                if (frameData[i] < BodyColor.Length)
                {
                    // this pixel is part of a player,
                    // display the appropriate color
                    this.bodyIndexPixels[i] = BodyColor[frameData[i]];
                }
                else
                {
                    // this pixel is not part of a player
                    // display black
                    this.bodyIndexPixels[i] = 0x00000000;
                }
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            this.bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this.bodyIndexBitmap.PixelWidth, this.bodyIndexBitmap.PixelHeight),
                this.bodyIndexPixels,
                this.bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }

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
   
            switch (handState)
            {
                case HandState.Closed:
                  //  drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    if (leftHand)
                    {
                        leftHandRect.Fill = redBrush;
                    }
                    else
                    {
                        rightHandRect.Fill = redBrush;
                    }
                    break;

                case HandState.Open:
                //    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    if (leftHand)
                    {
                        leftHandRect.Fill = blueBrush;
                    }
                    else
                    {
                        rightHandRect.Fill = blueBrush;
                    }
                    break;

                case HandState.Lasso:
                  //  drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    if (leftHand)
                    {
                        leftHandRect.Fill = yellowBrush;
                    }
                    else
                    {
                        rightHandRect.Fill = yellowBrush;
                    }
                    break;

                case HandState.NotTracked:
                    if(leftHand)
                    {
                        leftHandRect.Fill = blackBrush;
                    }
                    else
                    {
                        rightHandRect.Fill = blackBrush;
                    }
                    break;

                case HandState.Unknown:
                    if (leftHand)
                    {
                        leftHandRect.Fill = greyBrush;
                    }
                    else
                    {
                        rightHandRect.Fill = greyBrush;
                    }
                    break;
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

        private double getVelocity(double oldPoint, double newPoint)
        {
            double velocity = newPoint - oldPoint;
            return velocity;
        }

        private void getAvgVelocity()
        {

            double total1 = 0;
            double total2 = 0;
            int length = 0;

            foreach (double v in velocitiesX)
            {
                length++;
                total1 += v;
            }

            foreach (double v in velocitiesY)
            {
                total2 += v;
            }

            avgVelocity = Math.Abs((total1 + total2) / (length * 2));

            if (avgVelocity < 1.0)
            {
                velocityRect.Fill = whiteBrush;
            }
            else if (avgVelocity >= 1.0 && avgVelocity < 2.0)
            {
                velocityRect.Fill = yellowBrush;

            }
            else if (avgVelocity >= 2.0 && avgVelocity < 2.75)
            {
                velocityRect.Fill = blueBrush;

            }
            else if (avgVelocity >= 2.75 && avgVelocity < 3.75)
            {
                velocityRect.Fill = redBrush;
            }
            else if (avgVelocity >= 3.75 && avgVelocity < 4.85)
            {
                velocityRect.Fill = greyBrush;
            }
            else
            {
                velocityRect.Fill = blackBrush;
            }

            

        }
        

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

        private void getRegionOccupied()
        {
            int xRegion = 0;
            int yRegion = 0;
            int zRegion = 0;

            if (avgXYZCoordinates[0] < -0.6)
            {
                xRegion = 1;
            }
            else if (avgXYZCoordinates[0] >=  0.6 && avgXYZCoordinates[0] < 1.0)
            {
                xRegion = 2;
            }
            else
            {
                xRegion = 3;
            }

            if (avgXYZCoordinates[1] < -0.25)
            {
                yRegion = 1;
            }
            else if (avgXYZCoordinates[1] >= -0.25 && avgXYZCoordinates[1] < 0.25)
            {
                yRegion = 2;
            }
            else
            {
                yRegion = 3;
            }


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

          //  regionColorRect.Fill = regionBrushes[xRegion-1, yRegion-1, zRegion-1];
          if (zRegion == 1)
            {
                regionColorRect.Fill = redBrush;
            }
          else if (zRegion == 2)
            {
                regionColorRect.Fill = whiteBrush;
            }
            else
            {
                regionColorRect.Fill = yellowBrush;
            }
        }

        private void checkLeftRightMovement(Point oldPoint, Point newPoint)
        {
            
                if (oldPoint.X - newPoint.X > 0.05)
                {
                    if (avgVelocity >= 1.0)
                    {
                        sidewaysMvmtRect.Fill = yellowBrush;
                    }
                }
                else if (oldPoint.X - newPoint.X < -0.05)
                {
                    if (avgVelocity >= 1.0)
                    {
                        sidewaysMvmtRect.Fill = blueBrush;
                    }
                }
                else
                {
                    sidewaysMvmtRect.Fill = blackBrush;
                }
            
        }

        private void checkUpDownMovement(Point oldPoint, Point newPoint)
        {
            
                if (oldPoint.Y - newPoint.Y > 0.05)
                {
                    if (avgVelocity > 1.5)
                    {
                        verticalMvmtRect.Fill = redBrush;
                    }
                }
                else if (oldPoint.Y - newPoint.Y < -0.05)
                {
                    if (avgVelocity > 1.5)
                    {
                        verticalMvmtRect.Fill = yellowBrush;
                    }
                }
                else
                {
                    verticalMvmtRect.Fill = whiteBrush;
                }
            
        }

        private void checkForSweepMovement(Point handRightOld, Point handRightNew, Point handLeftOld, Point handLeftNew, Point footLeftOld, Point footLeftNew, Point footRightOld, Point footRightNew)
        {

            double significantChange = 15.0;

            if (avgVelocity < 2.0)
            {
                if ( Math.Abs(handRightOld.X - handRightNew.X) > significantChange)
                {
                    handRightSweepXRect.Fill = redBrush;

                }
                else
                {
                    handRightSweepXRect.Fill = whiteBrush;
                }

                //
                //

                if (Math.Abs(handRightOld.Y - handRightNew.Y) > significantChange) 
                {
                    handRightSweepYRect.Fill = yellowBrush;
                }
                else
                {
                    handRightSweepYRect.Fill = whiteBrush;
                }

                //
                //

                if ( Math.Abs(handLeftOld.X - handLeftNew.X) > significantChange)
                {
                    handLeftSweepXRect.Fill = blueBrush;

                }
                else
                {
                    handLeftSweepXRect.Fill = whiteBrush;
                }

                if (Math.Abs(handLeftOld.Y - handLeftNew.Y) > significantChange)
                {
                    handLeftSweepYRect.Fill = redBrush;

                }
                else
                {
                    handLeftSweepYRect.Fill = whiteBrush;
                }
            }

            
                if (Math.Abs(footRightOld.X - footRightNew.X) > significantChange)
                {
                 if (avgVelocity < 1.0)
                    {
                     footRightXSweepRect.Fill = redBrush;
                    }
                 }
                else
                {
                    footRightXSweepRect.Fill = whiteBrush;
                }



                if (Math.Abs(footRightOld.Y - footRightNew.Y) > significantChange)
                {
                    if (avgVelocity < 1.0)
                    {
                        footRightYSweepRect.Fill = blueBrush;
                    }

                }
                else
                {
                    footRightYSweepRect.Fill = whiteBrush;
                }



                if (Math.Abs(footLeftOld.X - footLeftNew.X) > significantChange)
                {
                    if (avgVelocity < 1.0)
                    {
                     footLeftSweepXRect.Fill = yellowBrush;
                    }
                }
                else
                {
                    footLeftSweepXRect.Fill = whiteBrush;
                }

                if (Math.Abs(footLeftOld.Y - footLeftNew.Y) > significantChange)
                {
                    if (avgVelocity < 1.0)
                    {
                        footLeftSweepYRect.Fill = redBrush;
                    }
                }
                else
                {
                    footLeftSweepYRect.Fill = whiteBrush;
                }


        }

        private void bodyOrientation(Point hipLeft, Point hipRight, Point shoulderLeft, Point shoulderRight, Point footLeft, Point footRight)
        {
            

            if (hipRight.X - hipLeft.X <= (0.6*(50-10*avgXYZCoordinates[2])) && shoulderRight.X - shoulderLeft.X <= (0.6*(80-14*avgXYZCoordinates[2])))
            {
                orientationRect.Fill = yellowBrush;
            }
            else
            {
                orientationRect.Fill = whiteBrush;
            }
        }

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
        }


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

       
    }
}
