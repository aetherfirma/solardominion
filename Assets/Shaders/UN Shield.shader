// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/UNShield" 
{
    Properties {
         _Radius ("Radius", Float) = 0.5
    }
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Opaque" "DisableBatching" = "True"}
		ZWrite On Lighting Off Cull Off Fog { Mode Off } Blend One Zero

		GrabPass { "_GrabTexture" }
		
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _GrabTexture;
			float _Radius;

			struct vin_vct
			{
				float4 vertex : POSITION;
				float4 tangent : tangent;
			};

			struct v2f_vct
			{
				float4 vertex : POSITION;
				float4 uvgrab : TEXCOORD1;
				float4 tangent: tangent;
			};

			// Vertex function 
			v2f_vct vert (vin_vct v)
			{
				v2f_vct o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvgrab = ComputeGrabScreenPos(o.vertex);
				o.tangent = v.tangent;
				return o;
			}

			// Fragment function
			half4 frag (v2f_vct i) : COLOR
			{
			    fixed4 col;
			    if (dot(i.tangent, float4(0,0,0,0) > _Radius))
				col = tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(i.uvgrab));
				else
				col = half4(1,1,1,1);
				return col;
			}
		
			ENDCG
		} 
	}
}