
#pragma once
#include <GL/glew.h>
#include <iostream>
#include <string>
#include <vector>

class FrameBuffer{
		
private:

	unsigned int FBO;                   //framebuffer object
	std::vector<unsigned int> texture_color_array;
	unsigned int texture_depth;			
	std::vector<GLenum> drawbuffer;     //add texture attachements

public:

	FrameBuffer(){
	//	GenerateFBO(800, 600);//initial width and height
	}

	~FrameBuffer(){
		destroy();
	}

private:

	//delete objects
	void destroy();

	//generate an empty color texture with 4 channels (RGBA8) using bilinear filtering
	void GenerateColorTexture(unsigned int width, unsigned int height);

	//generate an empty depth texture with 1 depth channel using bilinear filtering
	void GenerateDepthTexture(unsigned int width, unsigned int height);

public:

	//Generate FBO and two empty textures
	void GenerateFBO(unsigned int width, unsigned int height,int ntextures);

	//return color texture from the framebuffer
	unsigned int getColorTexture(int index);

	//return depth texture from the framebuffer
	unsigned int getDepthTexture();

	//resize window
	void resize(unsigned int width, unsigned int height);
		
	//bind framebuffer to pipeline. We will  call this method in the render loop
	void bind();
				
	//unbind framebuffer from pipeline. We will call this method in the render loop
	void unbind();
};