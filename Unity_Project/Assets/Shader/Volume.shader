Shader "Ray Marching/Volume"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		[HideInInspector] _Volume ("Volume", 3D) = "" {}
		[NoScaleOffset] _TFTex("Transfer Function Texture (Generated)", 2D) = "" {}
		_NoiseTex("Noise Texture (Generated)", 2D) = "white" {}
		_Intensity ("Intensity", Range(0.0, 5.0)) = 1.3
		_Threshold ("Render Threshold", Range(0,1)) = 0.5
		_RaySteps ("Raycasting Steps", Range(1,1000)) = 64
		_WinWidth("Window Width", Range(1, 2500)) = 500
		_WinCenter("Window Center", Range(-200, 2000)) = 120
		[HideInInspector]_HoundLow ("Lower Houndsfield", Float) = 50
		[HideInInspector]_HoundMax ("Upper Houndsfield", Float) = 500
		_SliceMin ("Slice min", Vector) = (0.0, 0.0, 0.0, -1.0)
		_SliceMax ("Slice max", Vector) = (1.0, 1.0, 1.0, -1.0)		
		_IntMove("Intensity Movement",Range(-2000,2000)) = 0
		_LightDir ("Light Direction", Vector) = (0.0, 0.0, 0.0, 1.0)
		_IsoMin("Min Isovalue", Range(0,1)) = 0.2
		_IsoMax("Max Isovalue", Range(0,1)) = 0.8
		_Debug("Debug", Range(0,1)) = 0.0
	}
	
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile MODE_DVR MODE_MIP MODE_SURF
			#pragma multi_compile LIGHT_ON LIGHT_OFF
			#pragma enable_d3d11_debug_symbols
			half4 _Color;
			float _RaySteps;
			sampler3D _Volume;
			sampler2D _NoiseTex;
			sampler2D _TFTex;
			float _RenderMode,_Debug, _WinWidth, _WinCenter, _IntMove, _Intensity, _Threshold, _HoundLow, _HoundMax, _IsoMin, _IsoMax;
			float3 _SliceMin, _SliceMax,_LightDir;
			float stepSize = sqrt(3) / 500; //Basis Wert

			struct appdata {
				float4 vertex : POSITION;
			  	float4 normal : NORMAL;
			  	float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float3 vertexLocal : TEXCOORD1;
                float3 normal : NORMAL;
			};

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vertexLocal = v.vertex;
				o.normal = UnityObjectToWorldNormal(v.normal);
				return o;
			}

			float4 getTF1DColour(float density) {
				return tex2Dlod(_TFTex, float4(density, 0.0f, 0.0f, 0.0f));
			}

			float map(float value, float fromLow, float fromHigh, float toLow, float toHigh) {
				return ((value - fromLow) / (fromHigh - fromLow)) * (toHigh - toLow) + toLow;
			}

			float getWindow(float intens) {
				float yMin = _HoundLow;
				float yMax = _HoundMax;
				float targetInt = 0;
				if (intens <= _WinCenter - 0.5 - (_WinWidth - 1) / 2) {
					targetInt = yMin;
				} else if (intens > _WinCenter - 0.5 + (_WinWidth - 1) / 2) {
					targetInt = yMax;
				} else {
					targetInt = (float)((((intens - (_WinCenter - 0.5f)) / (_WinWidth - 1) + 0.5f)) * ((yMax - yMin) + yMin));
				}
				return map(targetInt, yMin, yMax, 0.0f, 1.0f);
			}

			float get_data(float3 pos) {
				float alpha = 1.0f;
				float4 data4 = tex3Dlod(_Volume, float4(pos, 0));
				float voxel = data4.r; // [0-1]
				float fl2h = map(voxel, 0.0f, 1.0f, _HoundLow, _HoundMax); //[-1024-- 2048]
				fl2h = fl2h + _IntMove;
				fl2h = getWindow(fl2h);
				fl2h *= step(_SliceMin.x, pos.x);
				fl2h *= step(pos.x,_SliceMax.x);
				fl2h *= step(_SliceMin.y, pos.y);
				fl2h *= step(pos.y, _SliceMax.y);
				fl2h *= step(_SliceMin.z, pos.z);
				fl2h *= step(pos.z, _SliceMax.z);
				return fl2h; // [0-1]
			}

			float getDataGr(float x1, float y1, float z1, float x2, float y2, float z2) {
				return get_data(float3(x1, y1, z1)) - get_data(float3(x2, y2, z2));
			}

			float3 getGradient(float3 pos) {
				float x = pos[0];
				float y = pos[1];
				float z = pos[2];

				float xDiff = getDataGr(x-0.0001,y, z , x+0.0001,y,z);
				float yDiff = getDataGr(x, y-0.0001, z, x, y+0.0001, z);
				float zDiff = getDataGr(x, y, z-0.0001, x, y, z+0.0001);

				return normalize(float3(xDiff, yDiff, zDiff));
			}

			float4 frag(v2f i) : SV_Target {
				stepSize = sqrt(3) / _RaySteps; //Longest distance in cube (worst case)
				float3 rayStartPos = i.vertexLocal + float3(0.5f, 0.5f, 0.5f);
                float3 rayDir = normalize(ObjSpaceViewDir(float4(i.vertexLocal, 0.0f)));
				#if MODE_SURF
					rayStartPos += rayDir * stepSize * _RaySteps;
					float3 lightDir = rayDir; //Licht aus Blickrichtung
					// float3 lightDir = normalize(_LightDir); //Lickt aus Lichvektor
					rayDir = -rayDir;
					rayStartPos = rayStartPos + (2.0f * rayDir / _RaySteps) * tex2D(_NoiseTex, float2(i.uv.x, i.uv.y)).r; //Zufälliger Offset
				#endif
				float4	ray_col = float4(0.0f, 0.0f, 0.0f, 0.0f);
				for (int step = 1; step <= _RaySteps; step++) {
					const float dist = step * stepSize;
                    const float3 ray_pos = rayStartPos + rayDir * dist;
					if (ray_pos.x < 0.0f || ray_pos.x >= 1.0f || ray_pos.y < 0.0f || ray_pos.y > 1.0f || ray_pos.z < 0.0f || ray_pos.z > 1.0f){
						continue; 
					}
					float voxel_dens = get_data(ray_pos);
					float4 voxel_col = getTF1DColour(voxel_dens);
					if (voxel_dens <= _Threshold) {
						voxel_col.a = 0.0f;
					}
					//////////////////////////////////// Conditional-Compiling 
					#if MODE_MIP 
						ray_col.a=max(ray_col.a,voxel_dens);
						ray_col.rgb = _Color.rgb;
					////////////////////////////////////
					#elif MODE_DVR
						ray_col.rgb =  voxel_col.a * voxel_col.rgb + (1 - voxel_col.a) * ray_col.rgb; //Back to front
						ray_col.a = voxel_col.a  + (1 - voxel_col.a) * ray_col.a;
						if (ray_col.a > 1.0f)
                        	break;
					////////////////////////////////////					
					#elif MODE_SURF
						if (voxel_dens >= _IsoMin && voxel_dens <= _IsoMax ) {
							float3 normal = normalize(getGradient(ray_pos));
							float lightReflection = dot(normal, lightDir);
							lightReflection = max(lerp(0.0f, 1.5f, lightReflection), 0.5f);
							#if LIGHT_ON
								ray_col.rgb = lightReflection * _Color;
								// if(0.5>_Debug){
								//  	ray_col.rgb= float3(lightReflection,lightReflection,lightReflection);
								// }
							#elif LIGHT_OFF
								ray_col.rgb = _Color;
							#endif
							ray_col.a = 1.0f;
							break; // Break, First Voxel has been found
						}
					#endif
				}
				ray_col *= _Intensity;
				return ray_col;
			}
			ENDCG
		}
	}
	FallBack Off
}