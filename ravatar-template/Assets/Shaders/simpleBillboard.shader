// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/Simple Billboard"
{
	Properties 
	{
		_Size ("Size", Range(0, 3)) = 0.03 //patch size

		_Dev ("Dev", Range(-5, 5)) = 0
		_Gamma ("Gamma", Range(0, 6)) =3.41
		_SigmaX ("SigmaX", Range(-3, 3)) = 0.10
		_SigmaY ("SigmaY", Range(-3, 3)) = 0.10
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

					output.pos =  v.vertex;
					output.normal = v.normal;
					output.tex0 = float2(0, 0);
					output.color = v.color;

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
					
					
					//float size = (p[0].pos.z*_Size)/351.00146192  ;
					float size = 0.014;
					float halfS = 0.5f * size;

							
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
