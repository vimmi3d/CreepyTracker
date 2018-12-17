#pragma once
#include "CloudReader.h"
#include "FFDecoder.h"
#include "RVLDecoder.h"

class CloudLocalReader :
	public CloudReader
{
public:
	CloudLocalReader(FFDecoder* color, FFDecoder* normal, RVLDecoder* depth,int w, int h);
	~CloudLocalReader();

	FFDecoder* colorStream;
	FFDecoder* normalStream;
	RVLDecoder* depthStream;

	bool getFrame(byte* colorFrame, byte* depthFrame, byte* normalFrame);
};

