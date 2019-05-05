// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Snow"{
Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Bump ("Bump", 2D) = "bump" {}
		_Cutoff("Alpha Cutout",Range(0.01,1)) = 0.3
        _Snow ("Snow Level", Range(0,1) ) = 0
        _SnowColor ("Snow Color", Color) = (1.0,1.0,1.0,1.0)
        _SnowDirection ("Snow Direction", Vector) = (0,1,0)
        _SnowDepth ("Snow Depth", Range(0,0.2)) = 0.1
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
		Cull off
		

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow dithercrossfade
 
        sampler2D _MainTex;
        sampler2D _Bump;
		float _Cutoff;
        float _Snow;
        float4 _SnowColor;
        float4 _SnowDirection;
        float _SnowDepth;
 
        struct Input {
            float2 uv_MainTex;
            float2 uv_Bump;
            float3 worldNormal;
            INTERNAL_DATA
        };
 
        void vert (inout appdata_full v) {
            //Convert the normal to world coortinates
            float3 snormal = normalize(_SnowDirection.xyz);
            float3 sn = mul((float3x3)unity_WorldToObject, snormal).xyz;
 
            if(dot(v.normal, sn) >= lerp(1,-1, (_Snow*2)/3))
            {
               v.vertex.xyz += normalize(sn + v.normal) * _SnowDepth * _Snow;
            }
        }
 
        void surf (Input IN, inout SurfaceOutput o) {
            half4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Normal = UnpackNormal (tex2D (_Bump, IN.uv_Bump));
            if(dot(WorldNormalVector(IN, o.Normal), _SnowDirection.xyz)>=lerp(1,-1,_Snow))
            {
                o.Albedo = _SnowColor.rgb;
            }
            else {
                o.Albedo = c.rgb;
            }
            o.Alpha = c.a;
			clip(c.a - _Cutoff);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
