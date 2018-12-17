using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class Compressor
    {
        int width = 424;
        int height = 512;
        int stride = 53;

        public int Compress(byte[] buffer, byte[] output,int bytes)
        {
            //PNG 
            using (MemoryStream memory = new MemoryStream())
            {
                
                WriteableBitmap wbm = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                wbm.WritePixels(new Int32Rect(0, 0, width, height), buffer, 4 * width, 0);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbm));
                encoder.Save(memory);

                memory.Position = 0;
                memory.Read(output, 0, (int)memory.Length);
                return (int)memory.Length;
            }

            //GZIP
            //using (MemoryStream memory = new MemoryStream())
            //{
            //    using (GZipStream gzip = new GZipStream(memory,
            //        CompressionLevel.Fastest, true))
            //    {
            //        gzip.Write(buffer, 0, buffer.Length);
            //    }
            //    memory.Position = 0;
            //    memory.Read(output, 0, (int)memory.Length);

            //    return (int)memory.Length;
            //}



        }


    }
}

