using System;
using System.IO;
using Microsoft.Kinect;
using System.Collections.Generic;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    internal class ConfigFile
    {
        // Netwoek
        public int UdpBroadcastPort { get; private set; }
        public int UdpUnicastPort { get; private set; }

        // Surface Calibrated
        private CameraSpacePoint _bl = new CameraSpacePoint();
        public CameraSpacePoint SurfaceBottomLeft { get { return _bl; } set { _bl = value; } }
        private CameraSpacePoint _br = new CameraSpacePoint();
        public CameraSpacePoint SurfaceBottomRight { get { return _br; } set { _br = value; } }
        private CameraSpacePoint _tl = new CameraSpacePoint();
        public CameraSpacePoint SurfaceTopLeft { get { return _tl; } set { _tl = value; } }
        private CameraSpacePoint _tr = new CameraSpacePoint();
        public CameraSpacePoint SurfaceTopRight { get { return _tr; } set { _tr = value; } }


        public ConfigFile()
        {
            UdpBroadcastPort = 0;
            UdpUnicastPort = 0;
        }

        public bool Load(string filename)
        {
            if (File.Exists(filename))
            {
                foreach (string line in File.ReadAllLines(filename))
                {
                    if (line.Length != 0 && line[0] != '%')
                    {
                        string[] s = line.Split('=');
                        if (s.Length == 2)
                        {
                            if (s[0] == "udp.broadcast.port") this.UdpBroadcastPort = int.Parse(s[1]);
                            else if (s[0] == "udp.unicast.port") this.UdpUnicastPort = int.Parse(s[1]);

                            else if (s[0] == "surface.bottom.left") { if (!_parseSurface(s[1], out _bl)) { return false; } }
                            else if (s[0] == "surface.bottom.right") { if (!_parseSurface(s[1], out _br)) { return false; } }
                            else if (s[0] == "surface.top.left") { if (!_parseSurface(s[1], out _tl)) { return false; } }
                            else if (s[0] == "surface.top.right") { if (!_parseSurface(s[1], out _tr)) { return false; } }
                        }
                        else return false;
                    }
                }

                return true;
            }
            else return false;
        }

        public void Save(string filename)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename, false))
            {
                file.WriteLine("% " + DateTime.Now.ToShortDateString());
                file.WriteLine("udp.broadcast.port=" + UdpBroadcastPort);
                file.WriteLine("udp.unicast.port=" + UdpUnicastPort);
                file.WriteLine("%");
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

        private bool _parseSurface(string str, out CameraSpacePoint csPoint)
        {
            
            csPoint = new CameraSpacePoint();
            string[] line = str.Split(':');
            if (line.Length == 3)
            {
                try
                {
                    csPoint.X = float.Parse(line[0]);
                    csPoint.X = float.Parse(line[1]);
                    csPoint.X = float.Parse(line[2]);
                }
                catch
                {
                    return false;
                }
            }
            else return false;

            return true;
        }
    }
}