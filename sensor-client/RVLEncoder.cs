using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.BodyBasics
{

    public class RVLEncoder {

        public RVLEncoder()
        {
            pBuffer = word = nibblesWritten = 0;
        }

        int pBuffer;
        int word, nibblesWritten;

       void EncodeVLE(int value,byte[] output)
        {
            do

            {
               int nibble = value & 0x7; // lower 3 bits
                if ((value >>= 3)!= 0) nibble |= 0x8; // more to come
                word <<= 4;
                word |= nibble;
                if (++nibblesWritten == 8) // output word
                {
                    output[pBuffer++] = (byte) word;
                    output[pBuffer++] = (byte) (word >> 8);
                    output[pBuffer++] = (byte) (word >> 0x10);
                    output[pBuffer++] = (byte) (word >> 0x18);
                    nibblesWritten = 0;
                    word = 0;
                }
            } while (value!=0);
        }

      
        public int CompressRVL(ushort[] input, byte[] output,int step,int width,int height)
        {
            pBuffer = 0; // set deles igual ao início
            nibblesWritten = 0;
            int end = width*height; //ponteiro para o fim
            ushort previous = 0;
            int k = 0;
            int w = 0, h = 0;
            while (k < end) //enquanto não cheguei no fim do input
            {
                int zeros = 0, nonzeros = 0;
                k = h * width + w;
                for (; (k < end) && input[k] == 0; zeros++)
                {
                    w += step;
                    if (w >= width)
                    {
                        w = 0;
                        h += step;
                    }
                    k = h * width + w;
                } //contar o número de zeros seguidos (!*input)
                EncodeVLE(zeros,output); // encode do número of zeros
                for (int j = k,tempw = w,temph = h; (j < end) && input[j] != 0; nonzeros++)
                {
                    tempw += step;
                    if (tempw >= width)
                    {
                        tempw = 0;
                        temph += step;
                    }
                    j = temph * width + tempw;
                }
                //contar o número de não zeros seguidos, começando onde a gente tinha parado, 
                EncodeVLE(nonzeros,output); // number of nonzeros
                for (int i = 0; i < nonzeros; i++)
                {
                    ushort current = input[k];
                    w += step;
                    if (w >= width)
                    {
                        w = 0;
                        h += step;
                    }
                    k = h * width + w;
                    int delta = current - previous;
                    int positive = (delta << 1) ^ (delta >> 31);
                    EncodeVLE(positive,output); // nonzero value
                    previous = current;
                }
            }
            if (nibblesWritten != 0)
            {// last few values
                int last = word << 4 * (8 - nibblesWritten);
                output[pBuffer++] = (byte)last;
                output[pBuffer++] = (byte)(last >> 8);
                output[pBuffer++] = (byte)(last >> 0x10);
                output[pBuffer++] = (byte)(last >> 0x18);
            }
           // pBuffer /= 4;
            return pBuffer; // num bytes
        }

        public int CopyDontCompress(ushort[] input, byte[] output,int step, int width, int height)
        {
            int k = 0, i = 0;

            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    i = y * width + x;
                    output[k++] = (byte)input[i];
                    output[k++] = (byte)(input[i] >> 8);
                    output[k++] = (byte)(input[i] >> 0x10);
                    output[k++] = (byte)(input[i] >> 0x18);
                }
            }
            return k; // num bytes
        }
    }
}
