using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class Decompressor {

    MemoryStream _ms;
    GZipStream _zip;

    public Decompressor()
    {
    }

    public void Decompress(byte[] gzBuffer, byte[] output,int lenght)
    {

        //using (var ms = new MemoryStream(gzBuffer))
        //{
        //using (var gzs = new GZipStream(ms,CompressionMode.Decompress),))
        //    {
        //        gzs.CopyTo(decompressedMs);
        //    }
        //    return decompressedMs.ToArray();
        //}

        try { 
            _ms = new MemoryStream();
            _ms.Write(gzBuffer, 0, lenght);
            _zip = new GZipStream(_ms, CompressionMode.Decompress);
            _ms.Position = 0;
            _zip.Read(output, 0, output.Length);
        }catch(Exception e)
        {
            Debug.Log("Bug here " + e.Message);
        }
     }
}
