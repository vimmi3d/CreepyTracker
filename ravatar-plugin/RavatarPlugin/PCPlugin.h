#pragma once
#include <vector>
#include <string>
#include <map>
#include <boost/thread.hpp>
#include <winsock2.h> 
#include "CloudLocalReader.h"
#include "CloudNetworkReader.h"
#include <boost/property_tree/ptree.hpp>
#include <boost/property_tree/ini_parser.hpp>
#include <boost/asio.hpp>
#include <boost/algorithm/string.hpp>


#define RAVATARDLL_API __declspec(dllexport) 

typedef unsigned int uint;
using namespace std;
using boost::asio::ip::tcp;

const char L0 = '$'; // header separator
const char L1 = '#'; // top level separator -> bodies
const char L2 = '/'; // -> body attributes
const char L3 = ':'; // -> 3D values
const char SET = '=';
const int max_length_udp = 1024;

map<string, CloudReader*> clouds;
boost::thread_group threadsGroup;
vector<boost::thread*> threads;

int vidWidth;
int vidHeight;
int layerNum;
bool gotNormals;
bool network;
bool running;

void close();
void getNextFrame(const char* cloudID, byte* colorFrame, byte* depthFrame, byte* normalFrame);
const char* InitFromDisk(const char* filename);
void InitSockets(int myPort,int numberOfClients);
vector<string> split(const std::string &s, char delim);
void AcceptClients(int tcpPort, int clients);
void TCPLoop(boost::shared_ptr<tcp::socket> socket);
void TCPLupe(boost::shared_ptr<tcp::socket> socket);


extern "C" {
		RAVATARDLL_API void initNetwork(int myPort, int numberOfClients)
		{
			network = true;
			InitSockets(myPort, numberOfClients);
		}

		RAVATARDLL_API const char* initLocal(const char* configLocation)
		{	
			network = false;
			return InitFromDisk(configLocation);
		}

		RAVATARDLL_API void getFrameAndNormal(const char* cloudID, byte* colorFrame, byte* depthFrame, byte* normalFrame)
		{
			return getNextFrame(cloudID, colorFrame, depthFrame, normalFrame);
		}


		RAVATARDLL_API void stopClouds()
		{
			close();
		}
	
}
