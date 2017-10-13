// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/GS Billboard" 
{
	Properties 
	{
		_SpriteTex ("Base (RGB)", 2D) = "white" {}
		_Size ("Size", Range(0, 3)) = 0.03 //patch size
		_Color ("Color", Color) = (1, 1, 1, 0.2) 

		_Dev ("Dev", Range(-5, 5)) = 0.15
		_Gamma ("Gamma", Range(0, 6)) =3.41
		_SigmaX ("SigmaX", Range(-3, 3)) = 0.16
		_SigmaY ("SigmaY", Range(-3, 3)) = 0.08
		_Alpha ("Alpha", Range(0,1)) = 0.015

	}

	SubShader 
	{
		Pass
		{
			Tags { "RenderType"="Transparent" }
			LOD 200
			
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
					float3	normal	: NORMAL;
					float2  tex0	: TEXCOORD0;
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
				float4x4 _VP;
				Texture2D _SpriteTex;
				SamplerState sampler_SpriteTex;
				float4 _Color; 

				// **************************************************************
				// Shader Programs												*
				// **************************************************************

				// Vertex Shader ------------------------------------------------
				GS_INPUT VS_Main(appdata_full v)
				{
					GS_INPUT output = (GS_INPUT)0;

					output.pos =  mul(unity_ObjectToWorld, v.vertex);
					output.normal = v.normal;
					output.tex0 = float2(0, 0);
					output.color = v.color;

					return output;
				}



				// Geometry Shader -----------------------------------------------------
				[maxvertexcount(4)]
				void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					/*float nx = p[0].normal.x;
					float ny = p[0].normal.y;
					float nz = p[0].normal.z;
					float n = sqrt(pow(nx,2) + pow(ny,2) + pow(nz,2));
					
					float h1 = max( nx - n , nx + n );
					float h2 = ny;
					float h3 = nz;
					float h = sqrt(pow(h1,2) + pow(h2,2) + pow(h3,2));
					float3 right = float3(-2*h1*h2/pow(h,2), 1 - 2*pow(h2,2)/pow(h,2), -2*h2*h3/pow(h,2));
					float3 up = float3(-2*h1*h3/pow(h,2), -2*h2*h3/pow(h,2), 1 - 2*pow(h3,2)/pow(h,2));
					*/
					
					float3 up = float3(0, 1, 0);
					float3 look = _WorldSpaceCameraPos - p[0].pos;
					look.y = 0;
					look = normalize(look);
					float3 right = cross(up, look);
					
					
					
					float halfS = 0.5f * _Size;
							
					float4 v[4];
					v[0] = float4(p[0].pos + halfS * right - halfS * up, 1.0f);
					v[1] = float4(p[0].pos + halfS * right + halfS * up, 1.0f);
					v[2] = float4(p[0].pos - halfS * right - halfS * up, 1.0f);
					v[3] = float4(p[0].pos - halfS * right + halfS * up, 1.0f);

					float4x4 vp = UnityObjectToClipPos(unity_WorldToObject);
					FS_INPUT pIn;
					pIn.pos = mul(vp, v[0]);
					pIn.tex0 = float2(1.0f, 0.0f);
					pIn.color = p[0].color;
					triStream.Append(pIn);

					pIn.pos =  mul(vp, v[1]);
					pIn.tex0 = float2(1.0f, 1.0f);
					pIn.color = p[0].color;
					triStream.Append(pIn);

					pIn.pos =  mul(vp, v[2]);
					pIn.tex0 = float2(0.0f, 0.0f);
					pIn.color = p[0].color;
					triStream.Append(pIn);

					pIn.pos =  mul(vp, v[3]);
					pIn.tex0 = float2(0.0f, 1.0f);
					pIn.color = p[0].color;
					triStream.Append(pIn);
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
					//float a1 = pow(cos(theta),2)/(2*pow(sigmax,2)) + pow(sin(theta),2)/(2*pow(sigmay,2));
					//float b1 = pow(sin(2*theta),2)/(4*pow(sigmax,2)) + pow(sin(2*theta),2)/(4*pow(sigmay,2));
					//float c1 = pow(sin(theta),2)/(2*pow(sigmax,2)) + pow(cos(theta),2)/(2*pow(sigmay,2));

					//return  a * exp(- (a1*pow(x-x0,2) - (2*b1*(x-x0)*(y-y0)) + c1*pow(y-y0,2))) ;

					float x2 = cos(theta)*(x-x0)-sin(theta)*(y-y0)+x0;
					float y2 = sin(theta)*(x-x0)+cos(theta)*(y-y0)+y0;
					float z2 = a*exp(-0.5*( pow(pow((x2-x0)/sigmax,2),gamma/2)))*exp(-0.5*( pow(pow((y2-y0)/sigmay,2),gamma/2)));
					return z2;
				}

				// Fragment Shader -----------------------------------------------
				float4 FS_Main(FS_INPUT input) : COLOR
				{
	
					//float4 texColor = _SpriteTex.Sample(sampler_SpriteTex, input.tex0);
					//if(texColor.a < 0.3)
						//discard;
					
					//return texColor * (input.color*0.70 + _Color*0.30);

					saturation = 1.2f;
				
					float2 uv = input.tex0.xy;
	
					//------Brush stroke generation------//

					//ycenters and xcenters
					float xc[9]= {0.25f,0.5f,0.75f,0.25f,0.5f,0.75f,0.25f,0.5f,0.75f};
					float yc[9]= {0.25f,0.25f,0.25f,0.5f,0.5f,0.5f,0.75f,0.75f,0.75f};
					//float a =1.0f;
					//float sigmax = 0.15f; float sigmay = 0.12f;
					//float gamma =4;
					//float dev = 0.12;
					float alpha = 0;

					float a1 =gaussianTheta(uv.x, xc[0],uv.y,yc[0]+_Dev-theta1/6,_Alpha,_SigmaX,_SigmaY,_Gamma,theta1);
					float a2 =gaussianTheta(uv.x, xc[1],uv.y,yc[1]+_Dev-theta2/6,_Alpha*0.9,_SigmaX,_SigmaY,_Gamma,theta2); 
					float a3 =gaussianTheta(uv.x, xc[2],uv.y,yc[2]+_Dev-theta3/6,_Alpha*0.8,_SigmaX,_SigmaY,_Gamma,theta3); 
					float a4 =gaussianTheta(uv.x, xc[3],uv.y,yc[3]-theta1/6,_Alpha,_SigmaX,_SigmaY,_Gamma,theta1);
					float a5 =gaussianTheta(uv.x, xc[4],uv.y,yc[4]-theta2/6,_Alpha*0.9,_SigmaX,_SigmaY,_Gamma,theta2);
					float a6 =gaussianTheta(uv.x, xc[5],uv.y,yc[5]-theta3/6,_Alpha*0.8,_SigmaX,_SigmaY,_Gamma,theta3);
					float a7 =gaussianTheta(uv.x, xc[6],uv.y,yc[6]-_Dev-theta1/6,_Alpha,_SigmaX,_SigmaY,_Gamma,theta1);
					float a8 =gaussianTheta(uv.x, xc[7],uv.y,yc[7]-_Dev-theta2/6,_Alpha*0.9,_SigmaX,_SigmaY,_Gamma,theta2);
					float a9 =gaussianTheta(uv.x, xc[8],uv.y,yc[8]-_Dev-theta3/6,_Alpha*0.8,_SigmaX,_SigmaY,_Gamma,theta3);

					//alpha = max(max(max(max(max(max(max(max(a1,a2),a3),a4), a5),a6),a7),a8), a9);
					alpha= a1+a2+a3+a4+a5+a6+a7+a8+a9;
				//	alpha = gaussianTheta(uv.x, xc[4],uv.y,yc[4],a,_SigmaX,_SigmaY,Gamma,0.7853981634);
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
