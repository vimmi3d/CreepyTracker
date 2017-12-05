using UnityEngine;

public class RVLDecoder
{

    int word,buffer, pBuffer;
	int nibblesWritten;


    public RVLDecoder()
    {
        buffer = pBuffer =  nibblesWritten = 0;
		word = 0;

    }


    int DecodeVLE(byte[] input)
    {
		uint nibble;

		int value = 0, bits = 29;
        do
        {
            if (nibblesWritten == 0)
            {
				word = (int) (input[pBuffer] | (input[pBuffer + 1] << 8)) | ((input[pBuffer + 2] << 0x10) | (input[pBuffer + 3] << 0x18));  // load word
				pBuffer += 4;
                nibblesWritten = 8;
            }
            uint mask = 0xf0000000;
            nibble = (uint) word & mask;
			uint nibblebits =  (nibble<<1) >> bits;
			value = value | (int)nibblebits;
            word <<= 4;
            nibblesWritten--;
            bits -= 3;
		} while ((nibble & 0x80000000) != 0);
        return value;
    }

    public void DecompressRVL(byte[] input, byte[] output, int numPixels)
    {
        buffer = pBuffer = 0;
        nibblesWritten = 0;
        int current, previous = 0;
        int numPixelsToDecode = numPixels;
        int k = 0;
        while (numPixelsToDecode > 0)
        {
            int zeros = DecodeVLE(input); // number of zeros
            numPixelsToDecode -= zeros;
			for (; zeros != 0; zeros--) {
                if(k+4 > output.Length)
                {
                    Debug.Log("Happy little error");
                    return;
                }
				output [k++] = 0;
				output [k++] = 0;
				output [k++] = 0;
				output [k++] = 0;
			}
            if (numPixelsToDecode == 0) return;
            int nonzeros = DecodeVLE(input); // number of nonzeros
            numPixelsToDecode -= nonzeros;
            for (; nonzeros != 0; nonzeros--)
            {
                int positive = DecodeVLE(input); // nonzero value
                int delta = (positive >> 1) ^ -(positive & 1);
                current = (previous + delta);
                if (k + 4 > output.Length)
                {
                    Debug.Log("Happy little error");
                    return;
                }
                output[k++] = (byte)current;
                output[k++] = (byte)(current >> 8);
                output[k++] = (byte)(current >> 0x10);
                output[k++] = (byte)(current >> 0x18); ;
                previous = current;
            }
        }

    }

}