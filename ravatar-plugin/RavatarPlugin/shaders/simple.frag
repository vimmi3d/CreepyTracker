#version 300

in Vertex vertex;
out vec4 outColor;

int textureToDepth(uint x, uint y)
{
	vec4 d = texture(depthTex,vec2(x,y));
	int dr = int(d.r*255);
	int dg = int(d.g*255);
	int db = int(d.b*255);
	int da = int(d.a*255);
	int dValue = int(db | (dg << 0x8) | (dr << 0x10) | (da << 0x18));
	return dValue;
}

vec4 depthToTexture(int depth)
{
	vec4 c;
	int mask = 0xFF;
	int b = current & mask;
	c.b = b/255.0;
	mask = mask <<0x8;
	int g = current  & mask;
	c.g = g/255.0;
	mask = mask <<0x8;
	int r = current  & mask;
	c.r = r/255.0;
	mask = mask <<0x8;
	int a = current  & mask;
	c.a = a/255.0;
	return c;
}

int medianFilterDepth(int depth,uint x, uint y)
{	
	int _SizeFilter = 2;

	if(_SizeFilter == 0) return depth;
	uvec2 texCoord = uvec2(x,y);
	int sizeArray = (_SizeFilter*2 + 1)*(_SizeFilter*2 + 1);

	int arr[121];

	int k = 0;
	for (float i = -_SizeFilter; i <= _SizeFilter; i ++){
		for (float j = -_SizeFilter; j <= _SizeFilter; j ++){
			uvec2 pos = uvec2(i, j);
			uvec2 coords = texCoord + pos;
			int d = textureToDepth(coords.x,coords.y);
			arr[k] = d;
			k++;
		}
	}

	for (int j = 1; j < sizeArray; ++j)
	{
		int key = arr[j];
		int i = j - 1;
		while (i >= 0 && arr[i] > key)
		{
			arr[i+1] = arr[i];
			--i;
		}
		arr[i+1] = key;
	}
	int index = (_SizeFilter*2)+1;
	return arr[index];
}


void main(){
	int dValue = textureToDepth(vert.x,vert.y);
	dValue = medianFilterDepth(dValue,vert.x,vert.y);
	vec4 out= depthToTex(dValue);
	outColor = out;
}

