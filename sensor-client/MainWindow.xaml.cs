//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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
        private const double ClipBoundsThickness = 1;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

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
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;
        
        /// <summary>
        /// Reader for depth/color/body index frames
        /// </summary>
        private MultiSourceFrameReader multiFrameSourceReader = null;

        /// <summary>
        /// Intermediate storage for receiving depth frame data from the sensor
        /// </summary>
        private ushort[] depthFrameData = null;

        /// <summary>
        /// Intermediate storage for receiving color frame data from the sensor
        /// </summary>
        private byte[] colorFrameData = null;

        /// <summary>
        /// Intermediate storage for the depth to color mapping
        /// </summary>
        private ColorSpacePoint[] colorPoints = null;

        /// <summary>
        /// Intermediate storage for the depth to color mapping
        /// </summary>
        private CameraSpacePoint[] cameraPoints = null;

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Intermediate storage for receiving body index frame data from the sensor
        /// </summary>
        private byte[] bodyIndexFrameData = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;


        private int numberOfBodies;

        private string udpPort;

        private UdpBroadcast udp;
        private UdpListener udpListener;
        private Dictionary<string, int> JointsConfidenceWeight;

        private int step = 1;

        private byte[] depths = new byte[868352]; // TMA: To store the bytes
        private byte[] colors = new byte[868352];
            
        private List<Vector4> head_pos = new List<Vector4>(); // TMA: To keep track of the bodies' heads
        private List<Vector4> hand_pos = new List<Vector4>(); // TMA: To keep track of the bodies' hands
        private float radius_head = 0.22f; // TMA: Radius around head where the sampling value is lower than the input
        private float radius_hand = 0.15f; // TMA: Radius around hands where the sampling value is lower than the input

        RVLEncoder depthEncoder;
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // open multiframereader for depth, color, and bodyindex frames
            this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color| FrameSourceTypes.BodyIndex);

            // wire handler for frames arrival
            this.multiFrameSourceReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            int depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;

            // allocate space to put the pixels being received and converted
            this.depthFrameData = new ushort[depthWidth * depthHeight];
            this.colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
            this.cameraPoints = new CameraSpacePoint[depthWidth * depthHeight];
            this.bodyIndexFrameData = new byte[depthWidth * depthHeight];

            // get FrameDescription from ColorFrameSource
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;

            // allocate space to put the pixels being received
            this.colorFrameData = new byte[colorWidth * colorHeight * this.bytesPerPixel];

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

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

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

            this.numberOfBodies = 0;

            // use the window object as the view model in this simple example
            this.DataContext = this;



            // initialize the components (controls) of the window
            this.InitializeComponent();

            depthEncoder = new RVLEncoder();

            NetworkConfigFile f = new NetworkConfigFile("network.conf");
            UdpPort = f.Port;
            JointsConfidenceWeight = f.JointConfidenceWeight;
            udpListener = new UdpListener(int.Parse(f.ListenPort));
            ListenPort = f.ListenPort;
            udpListener.udpRestart();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        public int NumberOfBodies
        {
            get
            {
                return this.numberOfBodies;
            }
            set
            {
                this.numberOfBodies = value;
                numberOfBodiesLabel.Content = "" + numberOfBodies;
                usersLabel.Content = numberOfBodies == 1 ? "Body" : "Bodies";
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

        public string UdpPort
        {
            get
            {
                return udpPort;
            }

            set
            {
                udpPort = value;
                sendTextBox.Text = udpPort;
            }
        }

        public string ListenPort
        {
            get
            {
                return udpListener.Port.ToString();
            }

            set
            {
                udpListener.Port = udpListener.Port = int.Parse(value); ;
                reqTextBox.Text = value;
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            udp = new UdpBroadcast(int.Parse(UdpPort));

            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            if (this.udpListener != null)
            {
                this.udpListener.OnApplicationQuit();
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
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
                }
                
            }

            if (dataReceived)
            {

                NumberOfBodies = 0;
                List<Body> bodiesToSend = new List<Body>();

                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.White, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            NumberOfBodies += 1;
                            bodiesToSend.Add(body);

                            //this.DrawClippedEdges(body, dc);

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
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));


                    BodiesMessage message = new BodiesMessage(bodiesToSend.ToArray(), JointsConfidenceWeight);
                    udp.send(message.Message);
                }
            }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            Array.Clear(depths, 0, 217088); // rk: clear the array
            if (udpListener.PendingRequests.Count > 0 || udpListener.Clients.Count > 0)
            {

                int depthWidth = 0;
                int depthHeight = 0;

                int colorWidth = 0;
                int colorHeight = 0;

                int bodyIndexWidth = 0;
                int bodyIndexHeight = 0;

                bool multiSourceFrameProcessed = false;
                bool colorFrameProcessed = false;
                bool depthFrameProcessed = false;
                bool bodyIndexFrameProcessed = false;

                MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

                if (multiSourceFrame != null)
                {
                    // Frame Acquisition should always occur first when using multiSourceFrameReader
                    using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    {
                        using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                        {
                            using (BodyIndexFrame bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame())
                            {
                                if (depthFrame != null)
                                {
                                    FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                                    depthWidth = depthFrameDescription.Width;
                                    depthHeight = depthFrameDescription.Height;

                                    if ((depthWidth * depthHeight) == this.depthFrameData.Length)
                                    {
                                        depthFrame.CopyFrameDataToArray(this.depthFrameData);

                                        depthFrameProcessed = true;
                                    }
                                }

                                if (colorFrame != null)
                                {
                                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                                    colorWidth = colorFrameDescription.Width;
                                    colorHeight = colorFrameDescription.Height;

                                    if ((colorWidth * colorHeight * this.bytesPerPixel) == this.colorFrameData.Length)
                                    {
                                        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                        {
                                            colorFrame.CopyRawFrameDataToArray(this.colorFrameData);
                                        }
                                        else
                                        {
                                            colorFrame.CopyConvertedFrameDataToArray(this.colorFrameData, ColorImageFormat.Bgra);
                                        }

                                        colorFrameProcessed = true;
                                    }
                                }
                                if (bodyIndexFrame != null)
                                {
                                    FrameDescription bodyIndexFrameDescription = bodyIndexFrame.FrameDescription;
                                    bodyIndexWidth = bodyIndexFrameDescription.Width;
                                    bodyIndexHeight = bodyIndexFrameDescription.Height;

                                    if ((bodyIndexWidth * bodyIndexHeight) == this.bodyIndexFrameData.Length)
                                    {
                                        bodyIndexFrame.CopyFrameDataToArray(this.bodyIndexFrameData);

                                        bodyIndexFrameProcessed = true;
                                    }
                                }

                                multiSourceFrameProcessed = true;
                            }
                        }
                    }
                }

                // we got all frames
                if (multiSourceFrameProcessed && depthFrameProcessed && colorFrameProcessed && bodyIndexFrameProcessed)
                {
                  
                    this.coordinateMapper.MapDepthFrameToColorSpace(this.depthFrameData, this.colorPoints);
                    int i = 0;


                    for (int y = 0; y < depthHeight; y++)
                    {
                        for (int x = 0; x < depthWidth; x++)
                        {

                            int depthIndex = (y * depthWidth) + x;
                            byte player = this.bodyIndexFrameData[depthIndex];
                            bool? c = onlyPlayers.IsChecked;
                            bool val = c != null ? (bool)c : false;
                            // if we're tracking a player for the current pixel, sets its color and alpha to full
                            bool added = false;
                            if (!val || (val && player != 0xff))
                            {
                                CameraSpacePoint p = this.cameraPoints[depthIndex];

                                // retrieve the depth to color mapping for the current depth pixel
                                ColorSpacePoint colorPoint = this.colorPoints[depthIndex];
                                byte r = 0; byte g = 0; byte b = 0;
                                // make sure the depth pixel maps to a valid point in color space
                                int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                                int colorY = (int)Math.Floor(colorPoint.Y + 0.5);
                                if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                                {
                                    // calculate index into color array
                                    int colorIndex = ((colorY * colorWidth) + colorX) * this.bytesPerPixel;

                                    // set source for copy to the color pixel
                                    int displayIndex = depthIndex * this.bytesPerPixel;

                                    colors[i++] = this.colorFrameData[colorIndex++];
                                    colors[i++] = this.colorFrameData[colorIndex++];
                                    colors[i++] = this.colorFrameData[colorIndex++];
                                    colors[i++] = 0xf;
                                    added = true;
                                }

                            }
                            if (!added)
                            {
                                colors[i++] = 0;
                                colors[i++] = 0;
                                colors[i++] = 0;
                                colors[i++] = 0x0;
                            }
                        }
                    }
                    //this.coordinateMapper.MapDepthFrameToCameraSpace(this.depthFrameData, this.cameraPoints);
                    bool? comp = compressed.IsChecked;
                    bool compval = comp != null ? (bool)comp : false;
                    int byt = 0;
                    if (compval) { 
                        byt = depthEncoder.CompressRVL(depthFrameData, depths, 217088);
                    }else {
                       byt = depthEncoder.CopyDontCompress(depthFrameData, depths, 217088);
                    }
                    if (byt > 0)
                    {
                        List<TcpSender> todelete = null;
                        foreach (TcpSender client in udpListener.Clients)
                        {
                            if (client.Connected)
                            {
                                client.sendData(depths, udpListener.messageCount, byt, compval);
                                client.sendData(colors, udpListener.messageCount, colors.Length, compval);
                            }
                            else
                            {
                                if (todelete == null) todelete = new List<TcpSender>();
                                todelete.Add(client);
                            }
                        }

                        if (todelete != null)
                        {
                            foreach (TcpSender c in todelete)
                            {
                                udpListener.Clients.Remove(c);
                            }
                        }
                        if (udpListener.PendingRequests.Count > 0)
                        {
                            udpListener.processRequests(depths, colors, byt,compval);
                        }

                        udpListener.messageCount++;
                    }
                }
            }
        }


        private bool checkStep(int x, int y, int step)
        {
            if (x % step == 0 && y % step == 0)
                return true;
            else
                return false;
        }
        /// <summary>
        /// Checks if a point belongs inside a sphere with center in any head with euclidian distance.
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        /// <param name="z">z coordinate</param>
        private bool checkHead(float x, float y, float z)
        {
            foreach (Vector4 head in head_pos)
            {
                if (Math.Sqrt(Math.Pow(head.X - x, 2) + Math.Pow(head.Y - y, 2) + Math.Pow(head.Z - z, 2)) <= radius_head)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a point belongs inside a sphere with center in any hand with euclidian distance.
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        /// <param name="z">z coordinate</param>
        private bool checkHands(float x, float y, float z)
        {
            foreach (Vector4 hand in hand_pos)
            {
                if (Math.Sqrt(Math.Pow(hand.X - x, 2) + Math.Pow(hand.Y - y, 2) + Math.Pow(hand.Z - z, 2)) <= radius_hand)
                    return true;
            }
            return false;
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
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
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

        private void button_Click(object sender, RoutedEventArgs e)
        {
            resetBroadcast();
        }

        private void portTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) resetBroadcast();
        }

        private void resetBroadcast()
        {
            UdpPort = sendTextBox.Text;
            udp.reset(int.Parse(UdpPort));
            ListenPort = reqTextBox.Text;
            udpListener.udpRestart();
            expander.IsExpanded = false;   
        }
    }
}
