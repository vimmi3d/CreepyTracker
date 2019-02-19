#include "CloudLocalReader.h"



CloudLocalReader::CloudLocalReader(FFDecoder* color,FFDecoder* normal,RVLDecoder* depth, int w, int h): CloudReader(w,h)
{
	colorStream = color;
	normalStream = normal;
	depthStream = depth;
	sizec = width * height * 4;
	sized = width * height * 4;
	compressed = false;
	
}


CloudLocalReader::~CloudLocalReader()
{
	delete colorStream;
	delete depthStream;
	if (normalStream != NULL) delete normalStream;
}

bool CloudLocalReader::getFrame(byte * colorFrame, byte * depthFrame, byte * normalFrame)
{
	//Decompress
	bool gotFrame = colorStream->getVideoFrame();
	if (normalStream != NULL) normalStream->getVideoFrame();
	depthStream->DecompressRVL(width*height);
	FILE *f;
	fopen_s(&f, "getFrame", "w");
	fprintf(f, "got frame yay %d\n", gotFrame ? 1 : 0);
	fclose(f);
	//Copy
	av_image_copy_to_buffer(colorFrame, sizec, (const uint8_t * const *)colorStream->_gl_frame->data, colorStream->_gl_frame->linesize, MY_AV_PIXEL_TYPE, width, height, 1);
	depthFrame = (byte*)memcpy(depthFrame, depthStream->_depthBuffer, sized);
	if (normalStream != NULL)av_image_copy_to_buffer(normalFrame, sizec, (const uint8_t * const *)normalStream->_gl_frame->data, normalStream->_gl_frame->linesize, MY_AV_PIXEL_TYPE, width, height, 1);

	return true;
}


