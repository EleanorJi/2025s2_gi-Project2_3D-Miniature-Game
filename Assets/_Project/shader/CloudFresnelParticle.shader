Shader "Particles/CloudFresnel"
{
    Properties
    {
        _BaseMap     ("Base (RGBA)", 2D) = "white" {}
        _BaseColor   ("Base Color", Color) = (1,1,1,1)

        _CenterColor ("Center Color", Color)  = (0.85,0.85,0.85,1)
        _FresnelColor("Fresnel (Rim) Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 3.0
        _RimIntensity("Rim Intensity", Range(0, 2)) = 1.0

        [Toggle(_ALPHAPREMULTIPLY_ON)] _Premul ("Premultiply Alpha", Float) = 1
        _Cutoff ("Alpha Cutoff (optional)", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _ALPHAPREMULTIPLY_ON
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;

            float4 _BaseColor;
            float4 _CenterColor;
            float4 _FresnelColor;
            float  _FresnelPower;
            float  _RimIntensity;
            float  _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float3 nrmWS  : TEXCOORD1;
                float3 posWS  : TEXCOORD2;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.nrmWS = UnityObjectToWorldNormal(v.normal);
                o.uv    = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 采样贴图 × 顶点色 × 基色
                fixed4 baseSample = tex2D(_BaseMap, i.uv);
                fixed4 baseCol = baseSample * i.color * _BaseColor;

                // Fresnel（世界空间）
                float3 N = normalize(i.nrmWS);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.posWS);
                float ndotv = saturate(dot(N, V));
                float fresnel = pow(saturate(1.0 - ndotv), _FresnelPower);
                fresnel *= _RimIntensity;

                // 从中心色到边缘色插值
                fixed4 rimBlend = lerp(_CenterColor, _FresnelColor, saturate(fresnel));

                fixed4 col = baseCol * rimBlend;

                // 可选裁剪
                if (_Cutoff > 0 && col.a < _Cutoff) discard;

                #if _ALPHAPREMULTIPLY_ON
                    col.rgb *= col.a;
                #endif
                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
