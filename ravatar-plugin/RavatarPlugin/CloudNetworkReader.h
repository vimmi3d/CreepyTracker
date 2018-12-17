#pragma once
#include "CloudReader.h"
#include "RVLDecoder.h"
#include <boost/thread.hpp>

class CloudNetworkReader:
	public CloudReader
{
public:
	CloudNetworkReader(int w, int h);
	~CloudNetworkReader();
	
	byte colorBuffer[868352];
	byte depthBuffer[868352];
	byte colorNetworkBuffer[868352];
	byte depthNetworkBuffer[868352];

	RVLDecoder dec;
	bool getFrame(byte* colorFrame, byte* depthFrame, byte* normalFrame);
	void DecompressPNG(byte* in, byte* out);
	boost::mutex result_mutex;

};

