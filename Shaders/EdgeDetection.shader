// Outline Shader via Laplacian Edge Detection

// Pass 0 renders pure black/white masks of limited sets of objects
// Pass 1 runs edge detection on the results of Pass 0, can be looped to increase outline thickness
// Pass 2 composites the outline with the full scene, and an optional second silhouette to subtract from the outline

Shader "kaycodes13/EdgeDetection" {
	Properties {
		[PerRendererData]
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "black"{}
		// ^ (pass 0) isolated render of objects to silhouette
		// ^ (pass 1/2) silhouette texture for detecting edges
		
		[PerRendererData]
		_AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.4
		// ^ (pass 0) minimum alpha for a pixel to be included in the output
		
		[PerRendererData]
		[NoScaleOffset]
		_SceneTex("Scene Texture", 2D) = "black"{}
		// ^ (pass 2) the original scene to composite on top of
		
		[PerRendererData]
		[NoScaleOffset]
		_SubtractTex("Subtract Texture", 2D) = "black"{}
		// ^ (pass 2) silhouette texture to subtract from the final edge detection
		
		[PerRendererData]
		_LineColor ("Color", Color) = (1, 1, 1, 1)
		// ^ (pass 2) colour of the final edge detection lines
	}
	
	SubShader {
		Cull Off
		ZWrite Off
		ZTest Always
		Tags { "Queue"="Transparent" }
		
		Pass {
			Name "SILHOUETTE"
			ZClip On
			Lighting Off
			Blend One One
			
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
		
		
		Pass {
			Name "DETECT"
		
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
				half2 uv[5] : TEXCOORD0;
			};

			sampler2D _MainTex;
			half4 _MainTex_TexelSize; // float4(1/width, 1/height, width, height)

			half laplace(v2f i) {
				const half G[5] = {
					     1,
					 1, -4,  1,
					     1
				};
				half texColor;
				half edge = 0;
				for (int index = 0; index < 5; index++) {
					texColor = tex2D(_MainTex, i.uv[index]).r;
					edge += texColor * G[index];
				}
				return 1 - abs(edge);
			}

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv[0] = v.uv + _MainTex_TexelSize.xy * half2(0, -1);
				o.uv[1] = v.uv + _MainTex_TexelSize.xy * half2(-1, 0);
				o.uv[2] = v.uv;
				o.uv[3] = v.uv + _MainTex_TexelSize.xy * half2(1, 0);
				o.uv[4] = v.uv + _MainTex_TexelSize.xy * half2(0, 1);
				return o;
			}

			half4 frag(v2f i) : SV_Target {
				half4 stencilCol = tex2D(_MainTex, i.uv[2]);
				
				if (stencilCol.r > 0 || laplace(i) >= 0.5)
					return stencilCol;
				
				return half4(1, 0, 0, 1);
			}
			ENDCG
		}
		
		Pass {
			Name "COMPOSITE"
		
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
			sampler2D _SceneTex;
			sampler2D _SubtractTex;
			float4 _LineColor;

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag(v2f i) : SV_TARGET {				
				if (tex2D(_SubtractTex, i.uv).r > 0.1)
					return tex2D(_SceneTex, i.uv);
			
				float4 stencilCol = tex2D(_MainTex, i.uv);
				
				if (stencilCol.r < 0.1 || stencilCol.g > 0.1)
					return tex2D(_SceneTex, i.uv);
				
				return _LineColor;
			}
			ENDCG
		}

	}
}
