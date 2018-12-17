#include "CloudNetworkReader.h"
#include "lodepng.h"

CloudNetworkReader::CloudNetworkReader(int w, int h): CloudReader(w, h)
{

	dec.InitDecoder(w,h,"");
	dirty = false;
	
}


CloudNetworkReader::~CloudNetworkReader()
{
}

stringstream sstream;
void CloudNetworkReader::DecompressPNG(byte* in, byte* out) 
{
	unsigned int w, h;
	unsigned char *outbuf;
	lodepng_decode32(&outbuf, &w, &h, in, sizec);
	for (int i = 0; i < w*h * 4; i+=4) 
	{
		byte r = *(outbuf + i);
		*(outbuf + i) = *(outbuf + i + 2);
		*(outbuf + i + 2) = r;
	}
	memcpy(out, outbuf,w*h*4);
}

stringstream ss;
bool CloudNetworkReader::getFrame(byte* colorFrame, byte* depthFrame, byte* normalFrame) {
	if (!dirty) return false;

	result_mutex.lock();

	if(compressed){
		dec.DecompressRVLInOut(depthBuffer, depthFrame,width*height);
		DecompressPNG(colorBuffer,colorFrame);

	}
	else 
	{
		memcpy(depthFrame, depthBuffer, sized);
		memcpy(colorFrame, colorBuffer, sizec);
	}
	result_mutex.unlock();
	dirty = false;
	return true;
}
