Shader "Particles/CloudFresnelSimple"
{
    Properties
    {
        _CenterColor  ("Center Color", Color)  = (0.85,0.85,0.85,1)
        _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower ("Fresnel Power", Range(0.1,8)) = 3
        _RimIntensity ("Rim Intensity", Range(0,2)) = 1
        [Toggle(_ALPHAPREMULTIPLY_ON)] _Premul ("Premultiply Alpha", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
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

            float4 _CenterColor, _FresnelColor;
            float  _FresnelPower, _RimIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color  : COLOR;   // Particle vertex color
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float3 nrmWS : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.nrmWS = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Fresnel: use power of 1 - dot(N,V) as interpolation factor
                float3 N = normalize(i.nrmWS);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.posWS);
                float  t = pow(saturate(1.0 - saturate(dot(N, V))), _FresnelPower);
                t *= _RimIntensity;

                // Lerp(CenterColor, FresnelColor, t)
                fixed4 col = lerp(_CenterColor, _FresnelColor, saturate(t));

                // Multiply by particle vertex color (connects to particle system Start/Over Lifetime color and transparency)
                col *= i.color;

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
