using Microsoft.Kinect;
using System;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    public class SurfaceFile
    {
        private string _filename;

        public CameraSpacePoint SurfaceBottomLeft { get; set; }
        public CameraSpacePoint SurfaceBottomRight { get; set; }
        public CameraSpacePoint SurfaceTopLeft { get; set; }
        public CameraSpacePoint SurfaceTopRight { get; set; }

        public SurfaceFile(string filename)
        {
            _filename = filename;
            SurfaceBottomLeft = new CameraSpacePoint();
            SurfaceBottomRight = new CameraSpacePoint();
            SurfaceTopLeft = new CameraSpacePoint();
            SurfaceTopRight = new CameraSpacePoint();
        }

        public void saveFile()
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_filename, false))
            {
                file.WriteLine("% " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                file.WriteLine("surface.name=" + "Unknown Surface");
                file.WriteLine("surface.kinect=" + Environment.MachineName);
                file.WriteLine("surface.bottom.left=" + _pointToString(SurfaceBottomLeft));
                file.WriteLine("surface.bottom.right=" + _pointToString(SurfaceBottomRight));
                file.WriteLine("surface.top.left=" + _pointToString(SurfaceTopLeft));
                file.WriteLine("surface.top.right=" + _pointToString(SurfaceTopRight));
            }
        }

        private string _pointToString(CameraSpacePoint p)
        {
            return "" + p.X + ":" + p.Y + ":" + p.Z;
        }
    }
}