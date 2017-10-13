using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    class Vector3
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3()
        {
            X = 0; Y = 0; Z = 0;
        }
        public Vector3(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public Vector3(Point point)
        {
            X = point.X; Y = point.Y; Z = 0;
        }

        internal static Vector3 subPoint(Vector3 a, Vector3 b)
        {
            Vector3 r = new Vector3();
            r.X = a.X - b.X;
            r.Y = a.Y - b.Y;
            r.Z = a.Z - b.Z;
            return r;
        }

        internal static Vector3 cross(Vector3 a, Vector3 b)
        {
            Vector3 r = new Vector3();
            r.X = a.Y * b.Z - b.Y * a.Z;
            r.Y = b.X * a.Z - a.X * b.Z;
            r.Z = a.X * b.Y - a.Y * b.X;
            return r;
        }

        internal static Vector3 mult(Vector3 v, float c)
        {
            Vector3 r = new Vector3();
            r.X = v.X * c;
            r.Y = v.Y * c;
            r.Z = v.Z * c;
            return r;
        }

        internal static float distance(Vector3 a, Vector3 b)
        {
            return norm(subPoint(a, b));    
        }

        internal static Vector3 addPoint(Vector3 a, Vector3 b)
        {
            Vector3 r = new Vector3();
            r.X = a.X + b.X;
            r.Y = a.Y + b.Y;
            r.Z = a.Z + b.Z;
            return r;
        }

        internal static float norm(Vector3 v)
        {
            return (float) Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        internal static Vector3 normalize(Vector3 v)
        {
            float n = norm(v);
            Vector3 r = new Vector3();
            r.X = v.X / n;
            r.Y = v.Y / n;
            r.Z = v.Z / n;
            return r;
        }

        internal static void DebugPoint(Vector3 p)
        {
            Console.WriteLine(p.X + " " + p.Y + " " + p.Z);
        }

        internal static CameraSpacePoint subPoint(CameraSpacePoint a, CameraSpacePoint b)
        {
            CameraSpacePoint r = new CameraSpacePoint();
            r.X = a.X - b.X;
            r.Y = a.Y - b.Y;
            r.Z = a.Z - b.Z;
            return r;
        }

        internal static CameraSpacePoint cross(CameraSpacePoint a, CameraSpacePoint b)
        {
            CameraSpacePoint r = new CameraSpacePoint();
            r.X = a.Y * b.Z - b.Y * a.Z;
            r.Y = b.X * a.Z - a.X * b.Z;
            r.Z = a.X * b.Y - a.Y * b.X;
            return r;
        }

        internal static CameraSpacePoint mult(CameraSpacePoint v, float c)
        {
            CameraSpacePoint r = new CameraSpacePoint();
            r.X = v.X * c;
            r.Y = v.Y * c;
            r.Z = v.Z * c;
            return r;
        }

        internal static float distance(CameraSpacePoint a, CameraSpacePoint b)
        {
            return norm(subPoint(a, b));
        }

        internal static CameraSpacePoint addPoint(CameraSpacePoint a, CameraSpacePoint b)
        {
            CameraSpacePoint r = new CameraSpacePoint();
            r.X = a.X + b.X;
            r.Y = a.Y + b.Y;
            r.Z = a.Z + b.Z;
            return r;
        }

        internal static float norm(CameraSpacePoint v)
        {
            return (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        internal static CameraSpacePoint normalize(CameraSpacePoint v)
        {
            float n = norm(v);
            CameraSpacePoint r = new CameraSpacePoint();
            r.X = v.X / n;
            r.Y = v.Y / n;
            r.Z = v.Z / n;
            return r;
        }

        internal static void DebugPoint(CameraSpacePoint p)
        {
            Console.WriteLine(p.X + " " + p.Y + " " + p.Z);
        }

        internal static string ToString(CameraSpacePoint p)
        {
            return "" + p.X + " " + p.Y + " " + p.Z;
        }

        internal static string ToString(DepthSpacePoint p)
        {
            return "" + p.X + " " + p.Y;
        }
    }
}
