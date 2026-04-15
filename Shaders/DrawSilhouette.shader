// Creates pure black/white masks of a limited set of objects, for use with the edge detection shader

Shader "kaycodes13/DrawSilhouette" {
	Properties {
		[PerRendererData]
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "black"{}
		// ^ isolated render of objects to render in silhouette
		
		[PerRendererData]
		_AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.4
		// ^ minimum alpha for a pixel to be included in the output
	}
	SubShader {
		Cull Off
		ZWrite Off
		ZTest Always
		ZClip On
		Lighting Off
		Blend One One
		Tags { "Queue"="Transparent" }
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float _AlphaThreshold;

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half4 frag(v2f i) : COLOR0 {
				if (tex2D(_MainTex, i.uv).a >= _AlphaThreshold)
					return half4(1,1,1,1);
				else
					return half4(0,0,0,0);
			}
			ENDCG
		}
	}
}
