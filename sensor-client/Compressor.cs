using System;
using System.IO;
using System.IO.Compression;
namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class Compressor
    {
        MemoryStream _ms;
        GZipStream _zip;
        MemoryStream _outStream;
         

        public Compressor()
        {

           _outStream = new MemoryStream();

        }
        public int Compress(byte[] buffer, byte[] output,int bytes)
        {
            _ms = new MemoryStream();
            _zip = new GZipStream(_ms, CompressionLevel.Fastest, true);
           
            _zip.Write(buffer, 0, bytes);
            _zip.Close();
            _ms.Position = 0;
            _ms.Read(output, 0, (int)_ms.Length);

            return (int)_ms.Length;

        }

      
    }
}

