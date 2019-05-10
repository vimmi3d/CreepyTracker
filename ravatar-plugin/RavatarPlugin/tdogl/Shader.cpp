/*
 tdogl::Shader
 
 Copyright 2012 Thomas Dalling - http://tomdalling.com/
 
 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at
 
 http://www.apache.org/licenses/LICENSE-2.0
 
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
 */

#include "Shader.h"
#include <stdexcept>
#include <fstream>
#include <string>
#include <cassert>
#include <sstream>
#include <vector>
#include <iostream>

using namespace tdogl;

Shader::Shader(const std::string& shaderCode, GLenum shaderType) :
    _object(0),
    _refCount(NULL)
{
    //create the shader object
    _object = glCreateShader(shaderType);
    if(_object == 0)
        throw std::runtime_error("glCreateShader failed");
    
    //set the source code
    const char* code = shaderCode.c_str();
    glShaderSource(_object, 1, (const GLchar**)&code, NULL);
    
    //compile
    glCompileShader(_object);
    
    //throw exception if compile error occurred
    GLint status;
    glGetShaderiv(_object, GL_COMPILE_STATUS, &status);
    if (status == GL_FALSE) {
        std::string msg("Compile failure in shader:\n");
        
        GLint infoLogLength;
        glGetShaderiv(_object, GL_INFO_LOG_LENGTH, &infoLogLength);
        char* strInfoLog = new char[infoLogLength + 1];
        glGetShaderInfoLog(_object, infoLogLength, NULL, strInfoLog);
        msg += strInfoLog;
        delete[] strInfoLog;
        
        glDeleteShader(_object); _object = 0;
        throw std::runtime_error(msg);
    }
    
    _refCount = new unsigned;
    *_refCount = 1;
}

Shader::Shader(const Shader& other) :
    _object(other._object),
    _refCount(other._refCount)
{
    _retain();
}

Shader::~Shader() {
    //_refCount will be NULL if constructor failed and threw an exception
    if(_refCount) _release();
}

GLuint Shader::object() const {
    return _object;
}

Shader& Shader::operator = (const Shader& other) {
    _release();
    _object = other._object;
    _refCount = other._refCount;
    _retain();
    return *this;
}


//Text file loading for shaders sources
std::string loadTextFile(const char *name){
	//Source file reading
	std::string buff("");
	std::ifstream file;
	file.open(name);

	if (file.fail())
		std::cout << "loadFile: unable to open file: " << name;

	buff.reserve(1024 * 1024);

	std::string line;
	while (std::getline(file, line)){
		buff += line + "\n";
	}

	const char *txt = buff.c_str();

	return std::string(txt);
}

std::string manageIncludes(std::string src, std::string sourceFileName){
	std::string res;
	res.reserve(100000);

	char buff[512];
	sprintf_s(buff, "#include");


	size_t includepos = src.find(buff, 0);

	while (includepos != std::string::npos){
		bool comment = src.substr(includepos - 2, 2) == std::string("//");

		if (!comment){

			size_t fnamestartLoc = src.find("\"", includepos);
			size_t fnameendLoc = src.find("\"", fnamestartLoc + 1);

			size_t fnamestartLib = src.find("<", includepos);
			size_t fnameendLib = src.find(">", fnamestartLib + 1);

			size_t fnameEndOfLine = src.find("\n", includepos);

			size_t fnamestart;
			size_t fnameend;

			bool uselibpath = false;
			if ((fnamestartLoc == std::string::npos || fnamestartLib < fnamestartLoc) && fnamestartLib < fnameEndOfLine){
				fnamestart = fnamestartLib;
				fnameend = fnameendLib;
				uselibpath = true;
			}
			else if (fnamestartLoc != std::string::npos && fnamestartLoc < fnameEndOfLine){
				fnamestart = fnamestartLoc;
				fnameend = fnameendLoc;
				uselibpath = false;
			}
			else{
				std::cerr << "manageIncludes : invalid #include directive into \"" << sourceFileName.c_str() << "\"\n";
				return src;
			}

			std::string incfilename = src.substr(fnamestart + 1, fnameend - fnamestart - 1);
			std::string incsource;

			if (uselibpath){
				std::string usedPath;

				//TODO: Add paths types into the manager -> search only onto shaders paths.
				std::vector<std::string> pathsList;
				//ResourcesManager::getManager()->getPaths(pathsList);
				pathsList.push_back("./");

				for (std::vector<std::string>::iterator it = pathsList.begin(); it != pathsList.end(); it++){
					std::string fullpathtmp = (*it) + incfilename;

					FILE *file = 0;
					errno_t err;
					if ((err = fopen_s(&file, fullpathtmp.c_str(), "r"))!=0){
						usedPath = (*it);
						fclose(file);
						break;
					}
					else{
						usedPath = "";
					}
				}

				if (usedPath != ""){
					incsource = loadTextFile((usedPath + incfilename).c_str());
				}
				else{
					std::cerr << "manageIncludes : Unable to find included file \""
						<< incfilename.c_str() << "\" in system paths.\n";
					return src;
				}
			}
			else{
				incsource = loadTextFile(
					(sourceFileName.substr(0, sourceFileName.find_last_of("\\", sourceFileName.size()) + 1)
					+ incfilename).c_str()
					);
			}


			incsource = manageIncludes(incsource, sourceFileName);
			incsource = incsource.substr(0, incsource.size() - 1);

			std::string preIncludePart = src.substr(0, includepos);
			std::string postIncludePart = src.substr(fnameend + 1, src.size() - fnameend);

			int numline = 0;
			size_t newlinepos = 0;
			do{
				newlinepos = preIncludePart.find("\n", newlinepos + 1);
				numline++;
			} while (newlinepos != std::string::npos);
			numline--;

			char buff2[512];
			sprintf_s(buff2, "\n#line 0\n");
			std::string linePragmaPre(buff2);
			sprintf_s(buff2, "\n#line %d\n", numline);
			std::string linePragmaPost(buff2);


			res = preIncludePart + linePragmaPre + incsource + linePragmaPost + postIncludePart;

			src = res;
		}
		includepos = src.find(buff, includepos + 1);
	}

	return src;
}


void defineMacro(std::string &shaderSource, const char *macro, const char *value){
	char buff[512];


	sprintf_s(buff, "#define %s", macro);

	int mstart = (int)shaderSource.find(buff);
	sprintf_s(buff, "#define %s %s\n", macro, value);
	if (mstart >= 0){
		//std::cout<<"Found at "<<mstart<<"\n";
		int mlen = (int)shaderSource.find("\n", mstart) - mstart + 1;
		std::string prevval = shaderSource.substr(mstart, mlen);
		if (strcmp(prevval.c_str(), buff)){
			shaderSource.replace(mstart, mlen, buff);
		}
	}
	else{
		int versionstart = (int)shaderSource.find("\n");
		shaderSource.insert(versionstart + 2, buff);
	}

}

Shader Shader::shaderFromFile(const std::string& filePath, GLenum shaderType, std::vector<tdogl::ShaderMacroStruct>	shadersMacroList) {
  

	// macros and includes 
	std::string shaderSource = loadTextFile(filePath.c_str());

	shaderSource = manageIncludes(shaderSource, std::string(filePath));

	//Define global macros
	for (unsigned int i = 0; i<shadersMacroList.size(); i++){
		defineMacro(shaderSource, shadersMacroList[i].macro.c_str(), shadersMacroList[i].value.c_str());
	}

    //return new shader
    Shader shader(shaderSource, shaderType);
    return shader;
}

Shader Shader::shaderFromFile(const std::string& filePath, GLenum shaderType) {


	// macros and includes 
	std::string shaderSource = loadTextFile(filePath.c_str());

	shaderSource = manageIncludes(shaderSource, std::string(filePath));

	//return new shader
	Shader shader(shaderSource, shaderType);
	return shader;
}

void Shader::_retain() {
    assert(_refCount);
    *_refCount += 1;
}

void Shader::_release() {
    assert(_refCount && *_refCount > 0);
    *_refCount -= 1;
    if(*_refCount == 0){
        glDeleteShader(_object); _object = 0;
        delete _refCount; _refCount = NULL;
    }
}

