// Outline Shader for black/white stencil masks via Laplacian Edge Detection
// First pass can be run multiple times to increase outline thickness (expensive if too thick, slightly janky on corners).
// Second pass composites the accumulated outline to the given _SceneTex in the given _LineColor.

Shader "kaycodes13/EdgeDetection" {
	Properties {
		[PerRendererData]
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "black"{}
		// ^ silhouette texture for detecting edges (black background, white shapes)
		
		[PerRendererData]
		[NoScaleOffset]
		_SceneTex("Scene Texture", 2D) = "black"{}
		// ^ the original scene to composite on top of
		
		[PerRendererData]
		[NoScaleOffset]
		_SubtractTex("Subtract Texture", 2D) = "black"{}
		// ^ silhouette texture to subtract from the final edge detection
		
		[PerRendererData]
		_LineColor ("Color", Color) = (1, 1, 1, 1)
	}
	
	SubShader {
		Cull Off
		ZWrite Off
		ZTest Always
		Tags { "Queue"="Transparent" }
		
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
