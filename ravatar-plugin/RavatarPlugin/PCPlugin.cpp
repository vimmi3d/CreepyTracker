#include "PCPlugin.h"
#include <fstream>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <iostream>
#include <sstream>


vector<string> split(const std::string &s, char delim) {
	vector<string> elems;
	std::stringstream ss(s);
	std::string item;
	while (std::getline(ss, item, delim)) {
		elems.push_back(item);
	}
	return elems;
}

const char* InitFromDisk(const char* filename) {
	

	boost::property_tree::ptree pt;
	boost::property_tree::ini_parser::read_ini(filename, pt);
	string videosDir = pt.get<std::string>("videosDir");
	string colorStreamName = pt.get<std::string>("colorStreamName");
	string depthStreamName = pt.get<std::string>("depthStreamName");
	string normalStreamName = pt.get<std::string>("normalStreamName");
	if (normalStreamName == "") gotNormals = false;
	vidWidth = pt.get<int>("vidWidth");
	vidHeight = pt.get<int>("vidHeight");
	layerNum = pt.get<int>("numLayers");
	stringstream ss, ssout;

	for (int i = 0; i < layerNum; i++)
	{
		glm::mat4 calibration;
		stringstream ssempty;
		ss.swap(ssempty);
		ss << i;
		string calib = pt.get<std::string>(ss.str());
		ssout << calib << "#";
	}	
	
	for (int i = 0; i < layerNum; i++)
	{
		stringstream ssempty;
		ssempty << videosDir << "\\" << i << colorStreamName;
		FFDecoder *dec = new FFDecoder(ssempty.str());
		dec->init();

		stringstream ssempty2;
		ssempty2 << videosDir << "\\" << i << depthStreamName;
		RVLDecoder *decD = new RVLDecoder();
		decD->InitDecoder(vidWidth, vidHeight, ssempty2.str());

		FFDecoder *decN = NULL;
		if (gotNormals) {
			stringstream ssempty3;
			ssempty3 << videosDir << "\\" << i << normalStreamName;
			decN = new FFDecoder(ssempty3.str());
			decN->init();
		}
		CloudReader *c = new CloudLocalReader(dec, decN,decD,vidWidth,vidHeight);
		stringstream number;
		number << i;
		clouds.insert(pair<string,CloudReader*>(number.str(), c));
	}

	return ssout.str().c_str();
}

void InitSockets(int myPort, int numberOfClients)
{
	running = true;
	vidWidth = 512;
	vidHeight = 424;
	boost::thread acceptLoop(boost::bind(AcceptClients, myPort,numberOfClients));
}


void AcceptClients(int tcpPort,int clients)
{
	boost::asio::io_service io_service;
	tcp::acceptor acceptor(io_service, tcp::endpoint(tcp::v4(), tcpPort));

	while (clients>0)
	{
		boost::shared_ptr<tcp::socket> sock(new tcp::socket(io_service));
		acceptor.accept(*sock);
		boost::thread t(boost::bind(TCPLupe, sock));
		threadsGroup.add_thread(&t);
		threads.push_back(&t);
		clients--;
	}
	
	while (running) boost::this_thread::sleep(boost::posix_time::seconds(1));

}

void TCPLoop(boost::shared_ptr<tcp::socket> socket)
{

	CloudNetworkReader *cloud = new CloudNetworkReader(vidWidth,vidHeight);
	
	char message[200];
	char header[13];
	boost::system::error_code error;
	std::size_t n = socket->read_some(boost::asio::buffer(message), error);
	//Login
	string l(message);
	std::vector<std::string> results;
	boost::split(results,l, [](char c) {return c == '/'; });



	if (results.size() == 3 && results[0] == "k")
	{
		clouds.insert(pair<string, CloudReader*>(results[1], cloud));
		bool colorFrame = false;
		
		while (running)
		{

			std::size_t n = boost::asio::read(socket, boost::asio::buffer(header,13), boost::asio::transfer_exactly(13));
			if (n == 0 || !running)
			{
				break;
			}
			
			if (!running) break;

			uint id = (header[3] << 24) | (header[2] << 16) | (header[1] << 8) | (header[0]);
			int size = (header[7] << 24) | (header[6] << 16) | (header[5] << 8) | (header[4]);
			int scale = (header[12] << 24) | (header[11] << 16) | (header[10] << 8) | (header[9]);
			

			if (colorFrame)
				cloud->sizec = size;
			else
				cloud->sized = size;

			if (header[8] == '1')
				cloud->compressed = true;
			else
				cloud->compressed = false;

			if (colorFrame)
				n = boost::asio::read(socket, boost::asio::buffer(cloud->colorNetworkBuffer,size), boost::asio::transfer_exactly(size));
			else
				n = boost::asio::read(socket, boost::asio::buffer(cloud->depthNetworkBuffer,size), boost::asio::transfer_exactly(size));

			if (n == 0 || !running)
			{
				break;
			}

		
			
			//send okey
			//boost::asio::write(*socket, boost::asio::buffer(okey,3), boost::asio::transfer_all());

			if (colorFrame) {

				cloud->result_mutex.lock();
				if (cloud == NULL || cloud->colorBuffer == NULL || cloud->colorNetworkBuffer == NULL || cloud->depthNetworkBuffer == NULL || cloud->depthBuffer == NULL) break;
				std::swap(cloud->colorNetworkBuffer, cloud->colorBuffer);
				std::swap(cloud->depthNetworkBuffer, cloud->depthBuffer);
				cloud->result_mutex.unlock();
				cloud->dirty = true;

			}
			
			colorFrame = !colorFrame;
		}
	}
	socket->close();
}


void TCPLupe(boost::shared_ptr<tcp::socket> socket)
{

	CloudNetworkReader *cloud = new CloudNetworkReader(vidWidth, vidHeight);

	char message[200];
	byte header[13];
	boost::system::error_code error;
	std::size_t n = socket->read_some(boost::asio::buffer(message), error);
	//Login
	string l(message);
	std::vector<std::string> results;
	boost::split(results, l, [](char c) {return c == '/'; });
	
	//header + uncompressed package
	boost::asio::streambuf networkStreamBuffer{ 13 + 868352 };


	if (results.size() == 3 && results[0] == "k")
	{
		clouds.insert(pair<string, CloudReader*>(results[1], cloud));
		bool colorFrame = false;
		
		std::size_t n = 0;
		while (running)
		{
			while(n < 13){
				n += boost::asio::read(*socket, networkStreamBuffer, boost::asio::transfer_at_least(13-n));
			}
		

			if (n == 0 || !running)
			{
				break;
			}

			memcpy(header, boost::asio::buffer_cast<const byte*>(networkStreamBuffer.data()),13);
			networkStreamBuffer.consume(13);

			uint id = (header[3] << 24) | (header[2] << 16) | (header[1] << 8) | (header[0]);
			int size = (header[7] << 24) | (header[6] << 16) | (header[5] << 8) | (header[4]);
			int scale = (header[12] << 24) | (header[11] << 16) | (header[10] << 8) | (header[9]);

			
			if (colorFrame)
				cloud->sizec = size;
			else
				cloud->sized = size;

			if (header[8] == 1)
				cloud->compressed = true;
			else
				cloud->compressed = false;


		
			n -= 13;
			while (n < size) {
				n += boost::asio::read(*socket, networkStreamBuffer, boost::asio::transfer_at_least(size - n));
			}

			if (n == 0 || !running)
			{
				break;
			}

			if (colorFrame){
				memcpy(cloud->colorNetworkBuffer, boost::asio::buffer_cast<const void*>(networkStreamBuffer.data()), size);
				networkStreamBuffer.consume(size);
			}
			else{
				memcpy(cloud->depthNetworkBuffer, boost::asio::buffer_cast<const void*>(networkStreamBuffer.data()), size);
				networkStreamBuffer.consume(size);
			}

			n -= size;
			
			//send okey
			//boost::asio::write(*socket, boost::asio::buffer(okey,3), boost::asio::transfer_all());

			if (colorFrame) {

				cloud->result_mutex.lock();
				if (cloud == NULL || cloud->colorBuffer == NULL || cloud->colorNetworkBuffer == NULL || cloud->depthNetworkBuffer == NULL || cloud->depthBuffer == NULL) break;
				std::swap(cloud->colorNetworkBuffer, cloud->colorBuffer);
				std::swap(cloud->depthNetworkBuffer, cloud->depthBuffer);
				cloud->result_mutex.unlock();
				cloud->dirty = true;

			}

			colorFrame = !colorFrame;
		}
	}
	socket->close();
}

void getNextFrame(const char* cloudID, byte* colorFrame, byte* depthFrame, byte* normalFrame)
{
	string s(cloudID);
	if(clouds.find(s) != clouds.end())
		clouds[s]->getFrame(colorFrame, depthFrame, normalFrame);
	
}

void close() {
	
	//clean threads
	if(network)
	{
		running = false;
		threadsGroup.join_all();
	}

	for (const auto& sm_pair : clouds)
	{
		delete sm_pair.second;
	}
	clouds.clear();
	for (const auto& t : threads) 
	{
		threadsGroup.remove_thread(t);
	}
	threads.clear();
}