﻿Shader "AVProVideo/VR/InsideSphere (unlit)"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

		[KeywordEnum(None, Top_Bottom, Left_Right)] Stereo ("Stereo Mode", Float) = 0
		[Toggle(STEREO_DEBUG)] _StereoDebug ("Stereo Debug Tinting", Float) = 0
		[Toggle(HIGH_QUALITY)] _HighQuality ("High Quality", Float) = 0
    }
    SubShader
    {
		Tags { "RenderType"="Opaque" "IgnoreProjector" = "True" "Queue" = "Background" }
		ZWrite On
		//ZTest Always
		Cull Front
		Lighting Off

        Pass
        {
            CGPROGRAM
			#include "UnityCG.cginc"
			#include "AVProVideo.cginc"
			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

			//#define STEREO_DEBUG 1
			//#define HIGH_QUALITY 1

			#pragma multi_compile_fog
			#pragma multi_compile MONOSCOPIC STEREO_TOP_BOTTOM STEREO_LEFT_RIGHT
			#pragma multi_compile STEREO_DEBUG_OFF STEREO_DEBUG
			#pragma multi_compile HIGH_QUALITY_OFF HIGH_QUALITY

            struct appdata
            {
                float4 vertex : POSITION; // vertex position
#if HIGH_QUALITY
				float3 normal : NORMAL;
#else
                float2 uv : TEXCOORD0; // texture coordinate			
#endif
				
            };

            struct v2f
            {
                float4 vertex : SV_POSITION; // clip space position
#if HIGH_QUALITY
				float3 normal : TEXCOORD0;
				
#if STEREO_TOP_BOTTOM | STEREO_LEFT_RIGHT
				float4 scaleOffset : TEXCOORD1; // texture coordinate
				UNITY_FOG_COORDS(2)
#else
				UNITY_FOG_COORDS(1)
#endif
#else
                float2 uv : TEXCOORD0; // texture coordinate
				UNITY_FOG_COORDS(1)
#endif

#if STEREO_DEBUG
				float4 tint : COLOR;
#endif
            };

            uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float3 _cameraPosition;

			#if STEREO_DEBUG
			float4 GetStereoDebugTint(bool isLeftEye)
			{
				float4 tint = 1;

				#if STEREO_TOP_BOTTOM | STEREO_LEFT_RIGHT
				float4 leftEyeColor = float4(0, 1, 0, 1);		// green
				float4 rightEyeColor = float4(1, 0, 0, 1);		// red

				if (isLeftEye)
				{
					tint = leftEyeColor;
				}
				else
				{
					tint = rightEyeColor;
				}
				#endif

				#if UNITY_UV_STARTS_AT_TOP
				tint.b = 0.5;
				#endif

				return tint;
			}
			#endif

		#if STEREO_TOP_BOTTOM | STEREO_LEFT_RIGHT
			float4 GetStereoScaleOffset(bool isLeftEye)
			{
				float2 scale = 1;
				float2 offset = 0;

			// Top-Bottom
			#if STEREO_TOP_BOTTOM

				scale.y = 0.5;
				offset.y = 0.0;

				if (!isLeftEye)
				{
					offset.y = 0.5;
				}

				// UNITY_UV_STARTS_AT_TOP is for directx
				#if !UNITY_UV_STARTS_AT_TOP
				offset.y = 0.5 - offset.y;
				#endif

			// Left-Right 
			#elif STEREO_LEFT_RIGHT

				scale.x = 0.5;
				offset.x = 0.0;
				if (!isLeftEye)
				{
					offset.x = 0.5;
				}

			#endif

				return float4(scale, offset);
            }
		#endif

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
#if !HIGH_QUALITY
				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.xy = float2(1-o.uv.x, o.uv.y);
#endif

#if STEREO_TOP_BOTTOM | STEREO_LEFT_RIGHT
				float4 scaleOffset = GetStereoScaleOffset(IsStereoEyeLeft(_cameraPosition));

				#if !HIGH_QUALITY
				o.uv.xy *= scaleOffset.xy;
				o.uv.xy += scaleOffset.zw;
				#else
				o.scaleOffset = scaleOffset;
				#endif
#endif

#if HIGH_QUALITY
				o.normal = v.normal;
#endif

				#if STEREO_DEBUG
				o.tint = GetStereoDebugTint(IsStereoEyeLeft(_cameraPosition));
				#endif

				UNITY_TRANSFER_FOG(o, o.vertex);

                return o;
			}

            
            fixed4 frag (v2f i) : SV_Target
            {
				float2 uv;

#if HIGH_QUALITY
				float3 n = normalize(i.normal);

				float M_1_PI = 1.0 / 3.1415926535897932384626433832795;
				float M_1_2PI = 1.0 / 6.283185307179586476925286766559;
				uv.x = 0.5 - atan2(n.z, n.x) * M_1_2PI;
				uv.y = 0.5 - asin(-n.y) * M_1_PI;
				uv.x += 0.75;
				uv.x = uv.x % 1.0;
				uv.xy = TRANSFORM_TEX(uv, _MainTex);
				#if STEREO_TOP_BOTTOM | STEREO_LEFT_RIGHT
				uv.xy *= i.scaleOffset.xy;
				uv.xy += i.scaleOffset.zw;
				#endif
#else
				uv = i.uv;
#endif

                fixed4 col = tex2D(_MainTex, uv);
#if STEREO_DEBUG
				col *= i.tint;
#endif
				UNITY_APPLY_FOG(i.fogCoord, col);

                return fixed4(col.rgb, 1.0);
            }
            ENDCG
        }
    }
}