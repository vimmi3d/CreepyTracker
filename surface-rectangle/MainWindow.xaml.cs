
namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Shapes;
    using System.Collections.Generic;

    public enum Screen
    {
        landscape_16_9,
        portrait_9_16,
        landscape_4_3
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private KinectSensor kinectSensor = null;

        private CoordinateMapper coordinateMapper = null;

        private MultiSourceFrameReader multiFrameSourceReader = null;

        private WriteableBitmap colorBitmap = null;

        private ushort[] depthFrameData = null;

        private byte[] colorFrameData = null;

        private byte[] bodyIndexFrameData = null;

        private ColorSpacePoint[] colorPoints = null;

        private CameraSpacePoint[] cameraPoints = null;

        private Body[] bodies = null;

        private byte[] displayFrame = null;

        private string statusText = null;

        private SurfaceFile surfaceFile;

        private bool calibratingSurface = false;
        private List<Vector3> pointsToDepth;
        private List<CameraSpacePoint> surfacePoints;

        private FrameCounter _frameCounter;

        private string MachineName;

        private string _notificationText = "";

        private Screen screen = Screen.landscape_16_9;

        public MainWindow()
        {
            MachineName = Environment.MachineName;

            this.kinectSensor = KinectSensor.GetDefault();

            this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);

            this.multiFrameSourceReader.MultiSourceFrameArrived += MultiFrameSourceReader_MultiSourceFrameArrived;

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            int depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;

            this.depthFrameData = new ushort[depthWidth * depthHeight];
            this.bodyIndexFrameData = new byte[depthWidth * depthHeight];
            this.colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
            this.cameraPoints = new CameraSpacePoint[depthWidth * depthHeight];

            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;

            Console.WriteLine(depthWidth + "x"+ depthHeight);

            this.colorFrameData = new byte[colorWidth * colorHeight * this.bytesPerPixel];
            this.displayFrame = new byte[depthWidth * depthHeight * this.bytesPerPixel];

            this.colorBitmap = new WriteableBitmap(depthWidth, depthHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            pointsToDepth = new List<Vector3>();
            surfacePoints = new List<CameraSpacePoint>();

            surfaceFile = new SurfaceFile("rectangle.txt");

            _frameCounter = new FrameCounter();
            _frameCounter.PropertyChanged += (o, e) => this.StatusText = String.Format("FPS = {0:N1} / CPU = {1:N6} / " + _notificationText, _frameCounter.FramesPerSecond, _frameCounter.CpuTimePerFrame);



            this.kinectSensor.Open();
            this.DataContext = this;
            this.InitializeComponent();

            SaveSurfaceMenuItem.IsEnabled = false;

        }

        private void MultiFrameSourceReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
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
            bool bodyFrameProcessed = false;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame != null)
            {
                using (_frameCounter.Increment())
                {
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

                            using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                            {
                                if (bodyFrame != null)
                                {
                                    if (this.bodies == null)
                                    {
                                        this.bodies = new Body[bodyFrame.BodyCount];
                                    }
                                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                                    bodyFrameProcessed = true;
                                }
                            }
                        }
                    }
                }

                if (multiSourceFrameProcessed && depthFrameProcessed && colorFrameProcessed && bodyIndexFrameProcessed && bodyFrameProcessed)
                {
                    this.displayFrame = new byte[depthWidth * depthHeight * this.bytesPerPixel];

                    this.coordinateMapper.MapDepthFrameToColorSpace(this.depthFrameData, this.colorPoints);
                    this.coordinateMapper.MapDepthFrameToCameraSpace(this.depthFrameData, this.cameraPoints);

                    Array.Clear(displayFrame, 0, displayFrame.Length);

                    for (int depthIndex = 0; depthIndex < depthFrameData.Length; ++depthIndex)
                    {
                        byte player = this.bodyIndexFrameData[depthIndex];

                        bool? c = OnlyPlayersMenuItem.IsChecked;
                        bool val = c != null ? (bool)c : false;
                        if (!val || player != 0xff)
                        {
                            CameraSpacePoint p = this.cameraPoints[depthIndex];
                            ColorSpacePoint colorPoint = this.colorPoints[depthIndex];

                            // make sure the depth pixel maps to a valid point in color space
                            int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                            int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

                            // set source for copy to the color pixel
                            int displayIndex = depthIndex * this.bytesPerPixel;

                            if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight) && p.Z > 0)
                            {
                                // calculate index into color array
                                int colorIndex = ((colorY * colorWidth) + colorX) * this.bytesPerPixel;

                                this.displayFrame[displayIndex] = this.colorFrameData[colorIndex];
                                this.displayFrame[displayIndex + 1] = this.colorFrameData[colorIndex + 1];
                                this.displayFrame[displayIndex + 2] = this.colorFrameData[colorIndex + 2];
                                this.displayFrame[displayIndex + 3] = this.colorFrameData[colorIndex + 3];
                            }
                            else if (calibratingSurface)
                            {
                                this.displayFrame[displayIndex] = 0;
                                this.displayFrame[displayIndex + 1] = 0;
                                this.displayFrame[displayIndex + 2] = 255;
                                this.displayFrame[displayIndex + 3] = 100;
                            }

                            if (player != 0xff && (!(Double.IsInfinity(p.X)) && !(Double.IsInfinity(p.Y)) && !(Double.IsInfinity(p.Z))))
                            {

                            }
                        }
                    }

                    colorBitmap.WritePixels(
                    new Int32Rect(0, 0, depthWidth, depthHeight),
                    this.displayFrame,
                    depthWidth * bytesPerPixel,
                    0);

                    if (calibratingSurface)
                    {
                        if (pointsToDepth.Count > 0)
                        {
                            foreach (Vector3 p in pointsToDepth)
                            {
                                int depthIndex = Convert.ToInt32(p.Y) * depthWidth + Convert.ToInt32(p.X);
                                try
                                {
                                    CameraSpacePoint cameraPoint = this.cameraPoints[depthIndex];
                                    if (!(Double.IsInfinity(cameraPoint.X)) && !(Double.IsInfinity(cameraPoint.Y)) && !(Double.IsInfinity(cameraPoint.Z) && cameraPoint.Z > 0))
                                    {
                                        surfacePoints.Add(cameraPoint);
                                        DepthSpacePoint depthPoint = coordinateMapper.MapCameraPointToDepthSpace(cameraPoint);
                                        drawEllipse(depthPoint.X, depthPoint.Y);
                                    }
                                }
                                catch { }
                            }
                            pointsToDepth.Clear();
                        }

                        if (surfacePoints.Count == 3)
                        {
                            canvas.Children.Clear();

                            CameraSpacePoint a = Vector3.subPoint(surfacePoints[0], surfacePoints[1]);
                            CameraSpacePoint b = Vector3.subPoint(surfacePoints[2], surfacePoints[1]);
                            CameraSpacePoint up = Vector3.cross(a, b);
                            CameraSpacePoint c1 = Vector3.cross(a, up);
                            CameraSpacePoint c2 = Vector3.mult(c1, -1f);
                            CameraSpacePoint c;

                            float m;
                            if (screen == Screen.landscape_16_9) m = 9.0f / 16.0f;
                            else if (screen == Screen.portrait_9_16) m = 16.0f / 9.0f;
                            else m = 3.0f / 4.0f;

                            //if (Vector3.distance(surfacePoints[2], Vector3.addPoint(surfacePoints[1], c1)) < Vector3.distance(surfacePoints[2], Vector3.addPoint(surfacePoints[1], c2)))
                            //    c = Vector3.mult(Vector3.normalize(c1), (screen169 ? 9.0f / 16.0f : 3.0f / 4.0f) * Vector3.norm(a)/*norm(b)*/);
                            //else
                            //    c = Vector3.mult(Vector3.normalize(c2), (screen169 ? 9.0f / 16.0f : 3.0f / 4.0f) * Vector3.norm(a)/*norm(b)*/);

                            if (Vector3.distance(surfacePoints[2], Vector3.addPoint(surfacePoints[1], c1)) < Vector3.distance(surfacePoints[2], Vector3.addPoint(surfacePoints[1], c2)))
                                c = Vector3.mult(Vector3.normalize(c1), m * Vector3.norm(a)/*norm(b)*/);
                            else
                                c = Vector3.mult(Vector3.normalize(c2), m * Vector3.norm(a)/*norm(b)*/);


                            CameraSpacePoint BL = surfacePoints[0];
                            CameraSpacePoint BR = surfacePoints[1];
                            CameraSpacePoint TR = Vector3.addPoint(BR, c);
                            CameraSpacePoint TL = Vector3.addPoint(BL, c);

                            DepthSpacePoint pBL = coordinateMapper.MapCameraPointToDepthSpace(BL);
                            DepthSpacePoint pBR = coordinateMapper.MapCameraPointToDepthSpace(BR);
                            DepthSpacePoint pTR = coordinateMapper.MapCameraPointToDepthSpace(TR);
                            DepthSpacePoint pTL = coordinateMapper.MapCameraPointToDepthSpace(TL);

                            surfaceFile.SurfaceBottomLeft = BL;
                            surfaceFile.SurfaceBottomRight = BR;
                            surfaceFile.SurfaceTopLeft = TL;
                            surfaceFile.SurfaceTopRight = TR;

                            _drawSurface(pBL, pBR, pTR, pTL);

                            _notificationText = "Surface Calibrated";
                            SaveSurfaceMenuItem.IsEnabled = true;


                            surfacePoints.Clear();
                            calibratingSurface = false;
                        }
                    }
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

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

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            this.multiFrameSourceReader.Dispose();
            this.multiFrameSourceReader = null;

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private void AddNewSurface()
        {
            SaveSurfaceMenuItem.IsEnabled = false;
            OnlyPlayersMenuItem.IsChecked = false;
            calibratingSurface = true;
            surfacePoints.Clear();
            canvas.Children.Clear();
        }

        private void AddNewSurface_169_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            screen = Screen.landscape_16_9;
            AddNewSurface();
        }

        private void AddNewSurface_43_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            screen = Screen.landscape_4_3;
            AddNewSurface();
        }

        private void AddNewSurface_916_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            screen = Screen.portrait_9_16;
            AddNewSurface();
        }

        private void _drawLine(int X1, int Y1, int X2, int Y2)
            
        {
            Line myLine = new Line();
            myLine.Stroke = Brushes.Violet;
            myLine.X1 = X1;
            myLine.X2 = X2;
            myLine.Y1 = Y1;
            myLine.Y2 = Y2;
            myLine.HorizontalAlignment = HorizontalAlignment.Left;
            myLine.VerticalAlignment = VerticalAlignment.Center;
            myLine.StrokeThickness = 5;
            canvas.Children.Add(myLine);
        }

        private void _drawLine(DepthSpacePoint a, DepthSpacePoint b)
        {
            _drawLine((int)a.X, (int)a.Y, (int)b.X, (int)b.Y);
        }

        private void drawEllipse(double x, double y)
        {
            Ellipse ellipse = new Ellipse
            {
                Fill = Brushes.LimeGreen,
                Width = 10,
                Height = 10
            };

            Canvas.SetLeft(ellipse, x - ellipse.Width / 2);
            Canvas.SetTop(ellipse, y - ellipse.Height / 2);
            canvas.Children.Add(ellipse);
        }

        private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (calibratingSurface)
            {
                pointsToDepth.Add(new Vector3(e.GetPosition(ImageGui)));
            }
        }

        private void _drawSurface(DepthSpacePoint a, DepthSpacePoint b, DepthSpacePoint c, DepthSpacePoint d)
        {
            _drawLine(a, b);
            _drawLine(b, c);
            _drawLine(c, d);
            _drawLine(d, a);
        }

        private void SaveSurfaceToFile(object sender, RoutedEventArgs e)
        {
            if (SaveSurfaceMenuItem.IsEnabled)
            {
                surfaceFile.saveFile();
                SaveSurfaceMenuItem.IsEnabled = false;
                _notificationText = "File rectangle.txt Saved";
            }
        }
    }
}
