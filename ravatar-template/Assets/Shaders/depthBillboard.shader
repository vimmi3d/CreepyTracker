// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/Depth Billboard"
{
	Properties 
	{
		_Size ("Size", Range(0, 3)) = 0.03 //patch size
		_ColorTex ("Texture", 2D) = "white" {}
		_DepthTex ("TextureD", 2D) = "white" {}

		_Dev ("Dev", Range(-5, 5)) = 0
		_Gamma ("Gamma", Range(0, 6)) =3.41
		_SigmaX ("SigmaX", Range(-3, 3)) = 0.10
		_SigmaY ("SigmaY", Range(-3, 3)) = 0.10
		_Alpha ("Alpha", Range(0,1)) = 0.015
		
		_SizeFilter("SizeFilter",Int) = 2

		_sigmaS("SigmaS",Range(0.1,20)) = 8
		_sigmaL("SigmaL",Range(0.1,20)) = 8
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
				struct GS_INPUT
				{
					float4	pos		: POSITION;
					float4 color	: COLOR;
				};

				struct FS_INPUT
				{
					float4	pos		: POSITION;
					float2  tex0	: TEXCOORD0;
					float4 color	: COLOR;
				};


				// **************************************************************
				// Vars															*
				// **************************************************************

				float _Size;
				sampler2D _ColorTex;				
				sampler2D _DepthTex; 
				int _TexScale;
				float4 _Color;
				
				int _SizeFilter;
				
				float _sigmaL;
				float _sigmaS;




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
				float bilateralFilterDepth(int depth,float x, float y)
				{	
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



				// Vertex Shader ------------------------------------------------
				GS_INPUT VS_Main(appdata_full v)
				{
					GS_INPUT output = (GS_INPUT)0;

					float4 c = tex2Dlod(_ColorTex,float4(v.vertex.x,v.vertex.y,0,0));
					int dValue = textureToDepth(v.vertex.x,v.vertex.y);
					
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

					if(dValue == 0)		
						c.a = 0;
					output.pos =  pos;
					output.color = c;
					//int intpart;
					//float dColor = modf(dValue2,intpart);
					//output.color = float4(dColor,dColor,dColor,1);

					return output;
				}


			
				// Geometry Shader -----------------------------------------------------
				[maxvertexcount(4)]
				void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					
					
					//float3 look = _WorldSpaceCameraPos - p[0].pos;	
					
					float3 up =UNITY_MATRIX_IT_MV[1].xyz;
					float3 right = UNITY_MATRIX_IT_MV[0].xyz;

					//if(abs(look.y) > abs(look.x) || abs(look.y) > abs(look.z))
					//	up = float3(1,0,0);

					//look.y = 0;
					//look = normalize(look);
					up = normalize(up);
					right = normalize(right);
					//float3 right = cross(up, look);
					
					
					//float size = _TexScale * (p[0].pos.z*_Size)/351.00146192;
					float size = 0.008;
					float halfS = 0.5f * size;

					if(p[0].color.a != 0){
							
						float4 v[4];
						v[0] = float4(p[0].pos + halfS * right - halfS * up, 1.0f);
						v[1] = float4(p[0].pos + halfS * right + halfS * up, 1.0f);
						v[2] = float4(p[0].pos - halfS * right - halfS * up, 1.0f);
						v[3] = float4(p[0].pos - halfS * right + halfS * up, 1.0f);

						//float4 vp = UnityObjectToClipPos(unity_WorldToObject);
						FS_INPUT pIn;
						pIn.pos = UnityObjectToClipPos(v[0]);
						pIn.tex0 = float2(1.0f, 0.0f);
						pIn.color = p[0].color;
						triStream.Append(pIn);

						pIn.pos = UnityObjectToClipPos(v[1]);
						pIn.tex0 = float2(1.0f, 1.0f);
						pIn.color = p[0].color;
						triStream.Append(pIn);

						pIn.pos = UnityObjectToClipPos(v[2]);
						pIn.tex0 = float2(0.0f, 0.0f);
						pIn.color = p[0].color;
						triStream.Append(pIn);

						pIn.pos = UnityObjectToClipPos(v[3]);
						pIn.tex0 = float2(0.0f, 1.0f);
						pIn.color = p[0].color;
						triStream.Append(pIn);
					}
				}

				float theta1;
				float theta2;
				float theta3;

				float alph;
				bool aBuffer = true;
				float saturation;

				float _Dev;
				float _Gamma;
				float _SigmaX;
				float _SigmaY;
				float _Alpha;

				float gaussianTheta(float x, float x0, float y,float y0, float a, float sigmax, float sigmay, float gamma,float theta){
				

					float x2 = cos(theta)*(x-x0)-sin(theta)*(y-y0)+x0;
					float y2 = sin(theta)*(x-x0)+cos(theta)*(y-y0)+y0;
					float z2 = a*exp(-0.5*( pow(pow((x2-x0)/sigmax,2),gamma/2)))*exp(-0.5*( pow(pow((y2-y0)/sigmay,2),gamma/2)));
					return z2;
				}

				// Fragment Shader -----------------------------------------------
				float4 FS_Main(FS_INPUT input) : COLOR
				{
	
					
					saturation = 1.2f;
				
					float2 uv = input.tex0.xy;
	
					//------Brush stroke generation------//

					//ycenters and xcenters
					
					float alpha = gaussianTheta(uv.x, 0.5,uv.y,0.5,0.017,0.5,0.5,2,0);

					
					
					//----------------------------------//

					alpha = alpha>1? 1:alpha;
					alpha = alpha < 1? alpha*1:alpha;
					alpha = alpha < 0.01? 0:alpha;
					float4 t = float4(1.0f,1.0f,1.0f,alpha);
					float3 normal;
					if(t.a ==0 && !aBuffer){
						discard;
					}if(t.a ==0 && aBuffer){
						return float4(0.0f,0.0f,0.0f,0.0f);
					}
					t = t*input.color;
					//if(aBuffer)
				//		t.a *= _Alpha;
				//	else
						t.a = _Alpha;
					float  P=sqrt(t.r*t.r*0.299+t.g*t.g*0.587+t.b*t.b*0.114 ) ;

					//float  P=sqrt(t.r) ;

					t.r=P+((t.r)-P)*(saturation+0.3);
					t.g=P+((t.g)-P)*(saturation+0.3);
					t.b=P+((t.b)-P)*(saturation+0.3); 

					return  t;

				}

			ENDCG
		}
	} 
}
