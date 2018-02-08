using System;
using System.IO;
using System.IO.Compression;
namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class Compressor
    {
        
        public int Compress(byte[] buffer, byte[] output,int bytes)
        {

            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory,
                    CompressionLevel.Fastest, true))
                {
                    gzip.Write(buffer, 0, buffer.Length);
                }
                memory.Position = 0;
                memory.Read(output, 0, (int)memory.Length);

                return (int)memory.Length;
            }
            

        }

      
    }
}

