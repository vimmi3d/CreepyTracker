
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

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private FrameCounter _frameCounter;
        private KinectSensor kinectSensor = null;
        private MultiSourceFrameReader multiFrameSourceReader = null;
        private WriteableBitmap colorBitmap = null;

        private ushort[] _depthFrameData = null;
        private byte[] _colorFrameData = null;
        private byte[] _displayFrame = null;
        private ColorSpacePoint[] _colorSpacePoints = null;

        private string statusText = null;

      
        private CoordinateMapper coordinateMapper = null;

        private ColorFrameReader colorFrameReader = null;

        private BodyFrameReader bodyFrameReader = null;        

        private CameraSpacePoint[] _cameraPoints = null;

        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private byte[] bodyIndexFrameData = null;

        private Body[] bodies = null;

        private ConfigFile _configFile;

        private bool calibratingSurface = false;
        private List<Point> _pointsToDepth;
        private List<CameraSpacePoint> _calibrationPoints;


        public MainWindow()
        {
            _configFile = new ConfigFile();
            _loadConfig();

            _frameCounter = new FrameCounter();
            _frameCounter.PropertyChanged += (o, e) => this.StatusText = String.Format("FPS = {0:N1} / CPU = {1:N6}", _frameCounter.FramesPerSecond, _frameCounter.CpuTimePerFrame);

            this.kinectSensor = KinectSensor.GetDefault();

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            _pointsToDepth = new List<Point>();
            _calibrationPoints = new List<CameraSpacePoint>();

            this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
            this.multiFrameSourceReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            int depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;
            this._depthFrameData = new ushort[depthWidth * depthHeight];
            this._colorSpacePoints = new ColorSpacePoint[depthWidth * depthHeight];
            this._cameraPoints = new CameraSpacePoint[depthWidth * depthHeight];
            this.bodyIndexFrameData = new byte[depthWidth * depthHeight];

            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            //FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;
            _colorFrameData = new byte[colorWidth * colorHeight * this.bytesPerPixel];
            _displayFrame = new byte[colorWidth * colorHeight * this.bytesPerPixel];

            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            this.kinectSensor.Open();
            this.DataContext = this;
            this.InitializeComponent();

            

        }

        private void _loadConfig()
        {
            if (!_configFile.Load("../../../config.txt"))
            {
                Console.WriteLine("no such config file");
            }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            var reference = e.FrameReference;

            MultiSourceFrame multiSourceFrame = null;
            ColorFrame colorFrame = null;
            DepthFrame depthFrame = null;
            BodyFrame bodyFrame = null;
            BodyIndexFrame bodyIndexFrame = null;

            try
            {
                using (_frameCounter.Increment())
                {
                    multiSourceFrame = reference.AcquireFrame();
                    if (multiSourceFrame == null)
                    {
                        return;
                    }

                    colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                    depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                    bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();
                    bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();
                    if (colorFrame == null | depthFrame == null | bodyFrame == null | bodyIndexFrame == null)
                    {
                        return;
                    }
                        

                    var colorDesc = colorFrame.FrameDescription;
                    int colorWidth = colorDesc.Width;
                    int colorHeight = colorDesc.Height;
                    if (_colorFrameData == null)
                    {
                        int size = colorDesc.Width * colorDesc.Height;
                        _colorFrameData = new byte[size * bytesPerPixel];
                        _displayFrame = new byte[size * bytesPerPixel];
                    }

                    var depthDesc = depthFrame.FrameDescription;
                    uint depthSize = depthDesc.LengthInPixels;
                    _depthFrameData = new ushort[depthSize];
                    _colorSpacePoints = new ColorSpacePoint[depthSize];

                    FrameDescription bodyIndexFrameDescription = bodyIndexFrame.FrameDescription;
                    int bodyIndexWidth = bodyIndexFrameDescription.Width;
                    int bodyIndexHeight = bodyIndexFrameDescription.Height;
                    if ((bodyIndexWidth * bodyIndexHeight) == bodyIndexFrameData.Length)
                    {
                        bodyIndexFrame.CopyFrameDataToArray(bodyIndexFrameData);
                    }
                    
                    Array.Clear(_displayFrame, 0, _displayFrame.Length);


                    colorFrame.CopyConvertedFrameDataToArray(_colorFrameData, ColorImageFormat.Bgra);
                    depthFrame.CopyFrameDataToArray(_depthFrameData);
                    kinectSensor.CoordinateMapper.MapDepthFrameToColorSpace(_depthFrameData, _colorSpacePoints);
                    kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(_depthFrameData, _cameraPoints);

                    for (int depthIndex = 0; depthIndex < _depthFrameData.Length; ++depthIndex)
                    {

                        byte player = bodyIndexFrameData[depthIndex];
                        bool? c = OnlyPlayersMenuItem.IsChecked;
                        bool val = c != null ? (bool)c : false;
                        if (!val || player != 0xff)
                        {
                            ColorSpacePoint point = _colorSpacePoints[depthIndex];
                            CameraSpacePoint p = this._cameraPoints[depthIndex];

                            int colorX = (int)Math.Floor(point.X + 0.5);
                            int colorY = (int)Math.Floor(point.Y + 0.5);
                            int colorImageIndex = ((colorWidth * colorY) + colorX) * bytesPerPixel;

                            if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                            {
                                if (p.Z > 0)
                                {
                                    _displayFrame[colorImageIndex] = _colorFrameData[colorImageIndex]; // b
                                    _displayFrame[colorImageIndex + 1] = _colorFrameData[colorImageIndex + 1]; // g
                                    _displayFrame[colorImageIndex + 2] = _colorFrameData[colorImageIndex + 2]; // r
                                    _displayFrame[colorImageIndex + 3] = _colorFrameData[colorImageIndex + 3]; // a
                                }
                            }
                        }
                    }

                    colorBitmap.WritePixels(
                    new Int32Rect(0, 0, colorDesc.Width, colorDesc.Height),
                    _displayFrame,
                    //_colorFrameData,
                    colorDesc.Width * bytesPerPixel,
                    0);

                    if (calibratingSurface)
                    {
                        if (_pointsToDepth.Count > 0)
                        {
                            foreach (Point p in _pointsToDepth)
                            {
                                int depthIndex = Convert.ToInt32(p.Y) * depthDesc.Width + Convert.ToInt32(p.X);
                                try
                                {
                                    CameraSpacePoint cameraPoint = _cameraPoints[depthIndex];
                                    if (!(Double.IsInfinity(cameraPoint.X)) && !(Double.IsInfinity(cameraPoint.Y)) && !(Double.IsInfinity(cameraPoint.Z) && cameraPoint.Z > 0))
                                    {

                                        Console.WriteLine("" + p.X + " " + p.Y + "  ---> " + cameraPoint.X + " " + cameraPoint.Y + " " + cameraPoint.Z);

                                        _calibrationPoints.Add(cameraPoint);
                                        drawEllipse(p.X, p.Y);
                                    }
                                }
                                catch { }
                            }
                            _pointsToDepth = new List<Point>();
                        }
                        
                        if (false && _calibrationPoints.Count == 3)
                        {
                            canvas.Children.Clear();


                            CameraSpacePoint a = VectorTools.subPoint(_calibrationPoints[0], _calibrationPoints[1]);
                            CameraSpacePoint b = VectorTools.subPoint(_calibrationPoints[2], _calibrationPoints[1]);
                            CameraSpacePoint up = VectorTools.cross(a, b);
                            CameraSpacePoint c1 = VectorTools.cross(b, up);
                            CameraSpacePoint c2 = VectorTools.mult(c1, -1f);
                            CameraSpacePoint c;

                            if (VectorTools.distance(_calibrationPoints[2], VectorTools.addPoint(_calibrationPoints[1], c1)) < VectorTools.distance(_calibrationPoints[2], VectorTools.addPoint(_calibrationPoints[1], c2)))
                                c = VectorTools.mult(VectorTools.normalize(c1), 9.0f / 16.0f * VectorTools.norm(a)/*norm(b)*/);
                            else
                                c = VectorTools.mult(VectorTools.normalize(c2), 9.0f / 16.0f * VectorTools.norm(a)/*norm(b)*/);


                            CameraSpacePoint BL = _calibrationPoints[0];
                            CameraSpacePoint BR = _calibrationPoints[1];
                            CameraSpacePoint TR = VectorTools.addPoint(BR, c);
                            CameraSpacePoint TL = VectorTools.addPoint(BL, c);

                            VectorTools.DebugPoint(BL);
                            VectorTools.DebugPoint(BR);
                            VectorTools.DebugPoint(TR);
                            VectorTools.DebugPoint(TL);

                            //_drawSurface(coordinateMapper.MapCameraPointToColorSpace(BL),
                            //    coordinateMapper.MapCameraPointToColorSpace(BR),
                            //    coordinateMapper.MapCameraPointToColorSpace(TR),
                            //    coordinateMapper.MapCameraPointToColorSpace(TL));

                            _calibrationPoints.Clear();
                            calibratingSurface = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Source);
            }
            finally
            {
                if (colorFrame != null) colorFrame.Dispose();
                if (depthFrame != null) depthFrame.Dispose();
                if (bodyFrame != null) bodyFrame.Dispose();
                if (bodyIndexFrame != null) bodyIndexFrame.Dispose();
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
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

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

        private void AddNewSurface_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OnlyPlayersMenuItem.IsChecked = false;
            calibratingSurface = true;
            _calibrationPoints.Clear();
            canvas.Children.Clear();
            _drawLine(0, 0, 0, canvas.ActualHeight);
            _drawLine(0, 0, canvas.ActualWidth, 0);
            _drawLine(0, canvas.ActualHeight, canvas.ActualWidth, canvas.ActualHeight);
            _drawLine(canvas.ActualWidth, 0, canvas.ActualWidth, canvas.ActualHeight);
        }

        private void ReloadConfigFile_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            _loadConfig();
        }

        private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Console.WriteLine(e.GetPosition(ImageGui).X + " " + e.GetPosition(ImageGui).Y);

            if (calibratingSurface)
            {
                _pointsToDepth.Add(e.GetPosition(ImageGui));
            }
        }

        private void drawEllipse(double x, double y)
        {
            Ellipse ellipse = new Ellipse
            {
                Fill = Brushes.Red,
                Width = 5,
                Height = 5
            };

            Canvas.SetLeft(ellipse, x - ellipse.Width / 2);
            Canvas.SetTop(ellipse, y - ellipse.Height / 2);
            canvas.Children.Add(ellipse);
        }

        private void _drawSurface(ColorSpacePoint a, ColorSpacePoint b, ColorSpacePoint c, ColorSpacePoint d)
        {
            _drawLine(a, b);
            _drawLine(b, c);
            _drawLine(c, d);
            _drawLine(d, a);
        }

        private void _drawLine(double X1, double Y1, double X2, double Y2)
        {
            Line myLine = new Line();
            myLine.Stroke = Brushes.LightSteelBlue;
            myLine.X1 = X1;
            myLine.X2 = X2;
            myLine.Y1 = Y1;
            myLine.Y2 = Y2;
            myLine.HorizontalAlignment = HorizontalAlignment.Left;
            myLine.VerticalAlignment = VerticalAlignment.Center;
            myLine.StrokeThickness = 2;
            canvas.Children.Add(myLine);
        }

        private void _drawLine(ColorSpacePoint a, ColorSpacePoint b)
        {
            _drawLine(a.X, a.Y, b.X, b.Y);
        }
    }
}
