#include "CloudLocalReader.h"
#include <sstream>


CloudLocalReader::CloudLocalReader(FFDecoder* color,FFDecoder* normal,RVLDecoder* depth, int w, int h): CloudReader(w,h)
{
	colorStream = color;
	normalStream = normal;
	depthStream = depth;
	sizec = width * height * 4;
	sized = width * height * 4;
	compressed = false;
	currentFrame = 0;
}


CloudLocalReader::~CloudLocalReader()
{
	delete colorStream;
	delete depthStream;
	if (normalStream != NULL) delete normalStream;
}

stringstream s;
bool CloudLocalReader::getFrame(byte * colorFrame, byte * depthFrame, byte * normalFrame)
{
	//Reached the end! Looping back.

	
	bool gotFrame = colorStream->getVideoFrame();

	if (!gotFrame) {
		resetStreams();
		colorStream->getVideoFrame();
		currentFrame = 0;
	}
	if (normalStream != NULL) normalStream->getVideoFrame();
	depthStream->DecompressRVL(width*height);
	
	currentFrame++;

	//Copy
	av_image_copy_to_buffer(colorFrame, sizec, (const uint8_t * const *)colorStream->_gl_frame->data, colorStream->_gl_frame->linesize, MY_AV_PIXEL_TYPE, width, height, 1);
	depthFrame = (byte*)memcpy(depthFrame, depthStream->_depthBuffer, sized);
	if (normalStream != NULL)av_image_copy_to_buffer(normalFrame, sizec, (const uint8_t * const *)normalStream->_gl_frame->data, normalStream->_gl_frame->linesize, MY_AV_PIXEL_TYPE, width, height, 1);

	return true;
}

bool CloudLocalReader::skip5sec()
{
	int frameRate = 30;
	int seekFrame = currentFrame + (5 * frameRate);

	if(!colorStream->seekFrame(seekFrame))
		return false;

	if(!depthStream->seekFrame(seekFrame))
		return false;
	
	return true;
}

bool CloudLocalReader::back5sec()
{
	int frameRate = 30;
	int seekFrame = currentFrame - (5 * frameRate);
	
	if (seekFrame < 0) seekFrame = 0;

	colorStream->seekFrame(seekFrame);
	depthStream->seekFrame(seekFrame);
	
	return true;
}

bool CloudLocalReader::resetStreams() 
{
	colorStream->seekFrame(0);
	depthStream->ResetDecoder();
	return true;
}

