// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/Simple Mesh"
{
	Properties 
	{
		_ColorTex ("Texture", 2D) = "white" {}
		_DepthTex ("TextureD", 2D) = "white" {}
		_SizeFilter("SizeFilter",Int) = 2
		_sigmaS("SigmaS",Range(0.1,20)) = 3
		_sigmaL("SigmaL",Range(0.1,20)) = 3
		[Toggle] _calculateNormals("Normals", Float) = 0
		_ShaderDistance ("ShaderDistance", Range(0, 1.0)) = 0.1
	}

	SubShader 
	{
		Pass
		{
			Tags { "RenderType"="Transparent" }
			
			Cull Off // render both back and front faces

			CGPROGRAM

				#pragma target 5.0
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				#pragma geometry GS_Main
				#include "UnityCG.cginc" 

				// **************************************************************
				// Data structures												*
				// **************************************************************
				struct v2f
				{
					float4	pos		: POSITION;
					float4 color	: COLOR;
					float4 normal	: NORMAL;
				};


				// **************************************************************
				// Vars															*
				// **************************************************************

				sampler2D _ColorTex;				
				sampler2D _DepthTex; 
				int _TexScale;
				float4 _Color;
				float _calculateNormals;
				int _SizeFilter;
				bool _swapBR;
				float _sigmaL;
				float _sigmaS;
				float _ShaderDistance;



				// **************************************************************
				// Shader Programs												*
				// **************************************************************


				int textureToDepth(float x, float y)
				{
						float4 d = tex2Dlod(_DepthTex,float4(x, y,0,0));
						int dr = d.r*255;
						int dg = d.g*255;
						int db = d.b*255;
						int da = d.a*255;
						int dValue = (int)(db | (dg << 0x8) | (dr << 0x10) | (da << 0x18));
						return dValue;
				}


				#define EPS 1e-5
				float bilateralFilterDepth(float depth, float x, float y)
				{	
					if(_sigmaS ==0 || _sigmaL ==0) return depth;
					float sigS = max(_sigmaS, EPS);
					float sigL = max(_sigmaL, EPS);

					float facS = -1./(2.*sigS*sigS);
					float facL = -1./(2.*sigL*sigL);

					float sumW = 0.;
					float4  sumC = float4(0.0,0.0,0.0,0.0);
					float halfSize = sigS * 2;
					float2 textureSize2 = float2(512,424);
					float2 texCoord = float2(x,y);
					float l = depth;
				
					for (float i = -halfSize; i <= halfSize; i ++){
						for (float j = -halfSize; j <= halfSize; j ++){
						  float2 pos = float2(i, j);
						  
						  float2 coords = texCoord + pos/textureSize2;
						  int offsetDepth = textureToDepth(coords.x,coords.y);
						  float distS = length(pos);
						  float distL = offsetDepth-l;

						  float wS = exp(facS*float(distS*distS));
						  float wL = exp(facL*float(distL*distL));
						  float w = wS*wL;

						  sumW += w;
						  sumC += offsetDepth * w;
						}
					}
					return sumC/sumW;
				}

				
				float medianFilterDepth(int depth,float x, float y)
				{	
					if(_SizeFilter == 0) return depth;
					float2 texCoord = float2(x,y);
					float2 textureSize2 = float2(512,424);
					int sizeArray = (_SizeFilter*2 + 1)*(_SizeFilter*2 + 1);

					int arr[121];

					int k = 0;
					for (float i = -_SizeFilter; i <= _SizeFilter; i ++){
						for (float j = -_SizeFilter; j <= _SizeFilter; j ++){
						  float2 pos = float2(i, j);
						  float2 coords = texCoord + pos/textureSize2;
						  int d = textureToDepth(coords.x,coords.y);
						  arr[k] = d;
						  k++;
						}
					}

					for (int j = 1; j < sizeArray; ++j)
					{
						float key = arr[j];
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
					//return depth;
				}

				float4 estimateNormal(float x, float y)
				{
					int width = 512;
					int height = 424;
					float yScale = 0.1;
					float xzScale = 1;
					float deltax =  1.0/width;
					float deltay = 1.0/height;
					float sx = textureToDepth(x< width-deltax ? x+deltax : x, y) -textureToDepth(x>0 ? x-deltax : x, y);
					if (x == 0 || x == width-deltax)
						sx *= 2;

					float sy = textureToDepth(x, y<height-deltay ? y+deltay : y) - textureToDepth(x, y>0 ?  y-deltay : y);
					if (y == 0 || y == height -deltay)
						sy *= 2;

					float4 n =  float4(-sx*yScale, sy*yScale,2*xzScale,1);
					n = normalize(n);
					return n;
				}

				// Vertex Shader ------------------------------------------------
				v2f VS_Main(appdata_full v)
				{
					v2f output = (v2f)0;

					float4 c = tex2Dlod(_ColorTex,float4(v.vertex.x,v.vertex.y,0,0));
					int dValue = textureToDepth(v.vertex.x,v.vertex.y);
					dValue = 5000;
					if(dValue == 0)	{
						output.color = float4(0,0,0,0);
						return output;
						}
					
					float4 pos;
					//Median
					dValue = medianFilterDepth(dValue,v.vertex.x,v.vertex.y);
					//float dValue2 =  dValue / 1000.0;
					
					//Bilateral
					float dValue2 = bilateralFilterDepth(dValue,v.vertex.x,v.vertex.y)/1000.0;
	
					pos.z = dValue2; 
					
					float x = 512*v.vertex.x;
					float y = 424*v.vertex.y;
					float vertx = float(x);
					float verty = float(424 -y);
					pos.x =  pos.z*(vertx- 255.5)/351.001462;
					pos.y =  pos.z*(verty-  211.5)/351.001462;
					pos.w = 1;	
					
					output.pos =  pos;
					output.color = c;
					//int intpart;
					//float dColor = modf(dValue2,intpart);
					//output.color = float4(dColor,dColor,dColor,1);
					if(_calculateNormals != 0)
					{
						output.normal = estimateNormal(v.vertex.x,v.vertex.y);
					}
					else
					{
						output.normal= float4(0,0,0,0);
					}
					//output.color = output.n;
					
					return output;
				}


			
				// Geometry Shader -----------------------------------------------------
			[maxvertexcount(3)]
			void GS_Main(triangle v2f input[3], inout TriangleStream<v2f> OutputStream)
			{

				float lod = 0; // your lod level ranging from 0 to number of mipmap levels.
				float c0 = input[0].color.a;
				float c1 = input[1].color.a;
				float c2 = input[2].color.a;

				if (distance(input[0].pos, input[1].pos) < _ShaderDistance & distance(input[0].pos, input[2].pos) < _ShaderDistance & distance(input[1].pos, input[2].pos) < _ShaderDistance
					& c0 != 0 & c1 != 0 & c2 != 0)
				{
					v2f outV;
					outV.pos = UnityObjectToClipPos(input[0].pos);
					outV.color = input[0].color;
					outV.normal = input[0].normal;
					OutputStream.Append(outV);
					outV.pos = UnityObjectToClipPos(input[1].pos);
					outV.color = input[1].color;
					outV.normal = input[1].normal;
					OutputStream.Append(outV);
					outV.pos = UnityObjectToClipPos(input[2].pos);
					outV.color = input[2].color;
					outV.normal = input[2].normal;
					OutputStream.Append(outV);	
				}
			}
			
			// Fragment Shader -----------------------------------------------
			float4 FS_Main(v2f input) : COLOR
			{
				// sample the texture
				fixed4 col = input.color;
				// apply fog
				UNITY_APPLY_FOG(input.fogCoord, col);
				return col;
			}

			ENDCG
		}
	} 
}
