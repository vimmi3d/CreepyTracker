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
        public int Compress(byte[] buffer, byte[] output)
        {
            _ms = new MemoryStream();
            _zip = new GZipStream(_ms, CompressionMode.Compress, true);
            _zip.Write(buffer, 0, buffer.Length);
            _zip.Close();
            _ms.Position = 0;


            _ms.Read(output, 0, (int)_ms.Length);

            return (int)_ms.Length;
        }

      
    }
}

