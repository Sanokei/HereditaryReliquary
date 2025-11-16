Shader "Unlit (Vertex Color)"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		
		[ToggleOff] _PerVertexColor("Per-Vertex Color", Float) = 1.0
		
		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 100

		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "Always" }
			
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _ _PERVERTEXCOLOR_OFF
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			#ifndef _PERVERTEXCOLOR_OFF
				fixed4 color : COLOR;
			#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			#ifndef _PERVERTEXCOLOR_OFF
				fixed4 color : COLOR;
			#endif
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			fixed _Cutoff;

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			#ifndef _PERVERTEXCOLOR_OFF
				o.color = v.color;
			#endif
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				// Sample the texture
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				
			#ifndef _PERVERTEXCOLOR_OFF
				// Multiply by vertex color
				col *= i.color;
			#endif
				
				// Alpha test
			#ifdef _ALPHATEST_ON
				clip(col.a - _Cutoff);
			#endif
				
				// Apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				
				return col;
			}
			ENDCG
		}
		
		// Shadow caster pass
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual
			
			CGPROGRAM
			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster
			#pragma target 2.0
			
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _ _PERVERTEXCOLOR_OFF
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			
			#include "UnityCG.cginc"
			
			struct appdataShadow {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			#ifndef _PERVERTEXCOLOR_OFF
				fixed4 color : COLOR;
			#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2fShadow {
				V2F_SHADOW_CASTER;
				float2 uv : TEXCOORD1;
			#ifndef _PERVERTEXCOLOR_OFF
				fixed4 color : COLOR;
			#endif
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			fixed _Cutoff;
			
			v2fShadow vertShadowCaster(appdataShadow v)
			{
				v2fShadow o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			#ifndef _PERVERTEXCOLOR_OFF
				o.color = v.color;
			#endif
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}
			
			float4 fragShadowCaster(v2fShadow i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
			#ifndef _PERVERTEXCOLOR_OFF
				col *= i.color;
			#endif
			#ifdef _ALPHATEST_ON
				clip(col.a - _Cutoff);
			#endif
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
	
	FallBack "Unlit/Color"
	CustomEditor "StandardShaderVCGUI"
}

