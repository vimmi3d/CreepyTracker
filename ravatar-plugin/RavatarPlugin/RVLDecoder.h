#pragma once
#include <Windows.h>
#include <string>

using namespace std;

class RVLDecoder
{
private:
	int *_buffer, *_pBuffer, _word, _nibblesWritten;
	byte *_input;
	HANDLE _inFile;
	byte _sizeBuffer[4];
	int _width, _height;
	string _inputPath;
	
public:
	RVLDecoder();
	virtual ~RVLDecoder();
	byte *_depthBuffer;

private:
	int DecodeVLE();

public:
	bool	InitDecoder(int width, int height, string inputPath);
	void	ResetDecoder();
	void	DecompressRVL( int numPixels);
	void	DecompressRVLInOut(byte* in, byte* out, int numPixels);

};