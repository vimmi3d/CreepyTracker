#version 300


in uvec2 vert;
uniform sampler2DRect depthTex;

uniform int height;
uniform int width;

void main() {
	gl_Position = vec4(vert.x*width,vert.y*height,0,1);
}
	




