#include "RVLDecoder.h"
#include <sstream>
#include <iostream>

RVLDecoder::RVLDecoder() {
	_input = NULL;
}

RVLDecoder::~RVLDecoder() {
	CloseHandle(_inFile);
	delete _input;
}

bool RVLDecoder::InitDecoder(int width, int height, string inputPath) {
	_width = width;
	_height = height;
	_inputPath = inputPath;
	if(_inputPath != ""){
		std::wstring stempb = std::wstring(inputPath.begin(), inputPath.end());
		LPCWSTR swb = stempb.c_str();
		_inFile = CreateFileW(swb,GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		if(_input == NULL)
			_input = (byte*)malloc(sizeof(short)*width*height);
		if(_depthBuffer == NULL)
			_depthBuffer = (byte*)malloc(sizeof(byte)*width*height*4);
	
		if(!prepared){
			DWORD bytesRead;
			byte* output = _depthBuffer;

			ReadFile(_inFile, _sizeBuffer, 4, &bytesRead, NULL);

			long index = 0;
			long i = 0;
			while (bytesRead != 0)
			{
				_FrameIndex.push_back(index);

				int size = (_sizeBuffer[0] << 24) | (_sizeBuffer[1] << 16) | (_sizeBuffer[2] << 8) | (_sizeBuffer[3]);
				index += 4 + size;
				SetFilePointer(_inFile, index, NULL, FILE_BEGIN);
				ReadFile(_inFile, _sizeBuffer, 4, &bytesRead, NULL);
				//if(i % 30 == 0) yield return null;
			}
			SetFilePointer(_inFile, 0, NULL, FILE_BEGIN);
		}
	}

	return true;
}

	

void RVLDecoder::ResetDecoder() {
	SetFilePointer(_inFile, 0, NULL, FILE_BEGIN);
}

int RVLDecoder::DecodeVLE()
{
	unsigned int nibble;
	int value = 0, bits = 29;
	do
	{
		if (!_nibblesWritten)
		{
			_word = *_pBuffer++; // load word
			_nibblesWritten = 8;
		}
		nibble = _word & 0xf0000000;
		unsigned int nibblebits = (nibble << 1) >> bits;
		value |= nibblebits;
		_word <<= 4;
		_nibblesWritten--;
		bits -= 3;
	} while (nibble & 0x80000000);
	return value;
}

void RVLDecoder::DecompressRVL(int numPixels)
{
	
	DWORD bytesRead;
	byte* output = _depthBuffer;
	
	ReadFile(_inFile, _sizeBuffer, 4, &bytesRead, NULL);
	if (bytesRead == 0) {
		return;
		//ReadFile(_inFile, _sizeBuffer, 4, &bytesRead, NULL);
	}
	int size = (_sizeBuffer[0] << 24) | (_sizeBuffer[1] << 16) | (_sizeBuffer[2] << 8) | (_sizeBuffer[3]);

	ReadFile(_inFile, _input, size, &bytesRead, NULL);
	_buffer = _pBuffer = (int*)_input;
	_nibblesWritten = 0;
	int current, previous = 0;
	int numPixelsToDecode = numPixels;
	while (numPixelsToDecode)
	{
		int zeros = DecodeVLE(); // number of zeros
		numPixelsToDecode -= zeros;
		for (; zeros; zeros--){
 			*output++ = 0;
			*output++ = 0;
			*output++ = 0;
			*output++ = 0;
			
		}

		int nonzeros = DecodeVLE(); // number of nonzeros
		numPixelsToDecode -= nonzeros;
		for (; nonzeros; nonzeros--)
		{
			int positive = DecodeVLE(); // nonzero value
			int delta = (positive >> 1) ^ -(positive & 1);
			current = previous + delta;
			*output++ = current;
			*output++ = (current >>8);
			*output++ = (current >> 0x10);
			*output++ = (current >> 0x18);
			previous = current;
		}
		
	}

}

void RVLDecoder::DecompressRVLInOut(byte* in, byte* out, int numPixels)
{

	DWORD bytesRead;
	byte* output = out;
	
	_buffer = _pBuffer = (int*)in;
	_nibblesWritten = 0;
	int current, previous = 0;
	int numPixelsToDecode = numPixels;

	while (numPixelsToDecode)
	{
		
		int zeros = DecodeVLE(); // number of zeros
		numPixelsToDecode -= zeros;
		for (; zeros; zeros--) {
			*output++ = 0;
			*output++ = 0;
			*output++ = 0;
			*output++ = 0;

		}

		int nonzeros = DecodeVLE(); // number of nonzeros
		numPixelsToDecode -= nonzeros;
		for (; nonzeros; nonzeros--)
		{
			int positive = DecodeVLE(); // nonzero value
			int delta = (positive >> 1) ^ -(positive & 1);
			current = previous + delta;
			*output++ = current;
			*output++ = (current >> 8);
			*output++ = (current >> 0x10);
			*output++ = (current >> 0x18);
			previous = current;
		}

	}

}

bool RVLDecoder::seekFrame(int frame)
{
	if (frame > _FrameIndex.size())
		return false;

	SetFilePointer(_inFile, _FrameIndex[frame], NULL, FILE_BEGIN);
	
	return true;
}
