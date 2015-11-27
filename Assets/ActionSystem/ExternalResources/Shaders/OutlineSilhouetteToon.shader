Shader "Custom/Outlined/Silhouetted Toon" {
	Properties {
		_Color (		"Main Color", Color) = (1,1,1,1)
		_OutlineColor (	"Outline Color", Color) = (0,0,0,1)
		_Outline (		"Outline width", Range (0.0, 0.03)) = .0005
		_MainTex (		"Base (RGB)", 2D) = "white" { }
		_RampTex (		"Ramp (RGB)", 2D) = "white" { }
		_AddColor (		"Add Color", Color) = (0,0,0,1)
	}
	
	CGINCLUDE
	#include "UnityCG.cginc"
	
	half4		_Color;
	sampler2D	_MainTex;
	half4		_MainTex_ST;
	sampler2D	_RampTex;
	half4		_AddColor;
	
	struct appdata {
		half4 vertex : POSITION;
		half3 normal : NORMAL;
		half2 texcoord : TEXCOORD;
	};

	struct outline_v2f {
		half4 pos		: POSITION;
		half4 color	: COLOR;
		half4 texcoord	: TEXCOORD0;
	};
	struct base_v2f {
		half4 pos		: POSITION;
		half4 color	: COLOR;
		half4 texcoord	: TEXCOORD0;
	};

	uniform half _Outline;
	uniform half4 _OutlineColor;
	
	//global variable
	half4 _LightDir = half4(-1,0.2,-0.5,0);
	
	outline_v2f outline_vert(appdata v) {
		// just make a copy of incoming vertex data but scaled according to normal direction
		outline_v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	
		half3 norm   = mul ((half3x3)UNITY_MATRIX_IT_MV, v.normal);
		half2 offset = TransformViewToProjection(norm.xy);
	
		o.pos.xy += offset * o.pos.z * _Outline;
		o.color = _OutlineColor;
		return o;
	}
	base_v2f base_vert(appdata v) {
		// just make a copy of incoming vertex data but scaled according to normal direction
		base_v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	
		half4	mv_norm		= normalize( mul (UNITY_MATRIX_MV, half4( v.normal,0 ) ) );
		half4	m_norm		= normalize( mul (_Object2World, half4( v.normal,0 ) ) );
		half3	light_direction = normalize( _LightDir );
		o.color.r = dot( light_direction, m_norm.xyz );
		o.color.g = (m_norm.y + 1) * 0.5;
		
		o.texcoord.xy	= TRANSFORM_TEX( v.texcoord, _MainTex);
		
		return o;
	}
	ENDCG

	SubShader {
		Tags { "Queue" = "Geometry" }
		// note that a vertex shader is specified here but its using the one above
		Pass {
			Name "OUTLINE"
			Tags { "LightMode" = "Always" }
			Cull Back
			ZWrite Off
			ZTest Always
			ColorMask RGBA

			// you can choose what kind of blending mode you want for the outline
			Blend SrcAlpha OneMinusSrcAlpha // Normal
			//Blend One One // Additive
			//Blend One OneMinusDstColor // Soft Additive
			//Blend DstColor Zero // Multiplicative
			//Blend DstColor SrcColor // 2x Multiplicative

			CGPROGRAM
			#pragma vertex outline_vert
			#pragma fragment frag
			
			half4 frag(outline_v2f i) : COLOR {
				return half4( i.color.rgb, 1 );
			}
			ENDCG
		}
		Pass {
			Name "BASE"
			Tags { "LightMode" = "Always" }
			Cull Back
			ZWrite On
			ZTest LEqual
			Blend SrcAlpha OneMinusSrcAlpha
			Material {
				Diffuse [_Color]
				Ambient [_Color]
			}
			Lighting Off
			CGPROGRAM
			#pragma vertex base_vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			half4 frag( base_v2f i ) :COLOR {
				half4 albedo = tex2D( _MainTex, i.texcoord.xy );
				half lambert = tex2D( _RampTex, half2( saturate( i.color.r ), 0) );
				return half4( lerp( albedo*albedo, albedo, lambert ).rgb, albedo.a ) * _Color + _AddColor;
			}
			ENDCG
		}
	}
	
	Fallback "Diffuse"
}