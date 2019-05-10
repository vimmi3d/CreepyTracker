#pragma once
#include <string>
typedef unsigned char byte;

using namespace std;

class CloudReader
{
public:
	CloudReader(int w, int h) { width = w; height = h;};
	~CloudReader(){};
	int sizec;
	int sized;
	bool compressed;
	bool dirty;
	int width;
	int height;
	virtual bool getFrame(byte* colorFrame, byte* depthFrame, byte* normalFrame) = 0;
	virtual bool skip5sec() = 0;
	virtual bool back5sec() = 0;
	virtual bool resetStreams() = 0;
};

