Shader "Custom/KitchenSinkFoam" {
    Properties {
        _WaterColor ("Water Color", Color) = (0.1, 0.4, 0.6, 0.8)
        _FoamColor ("Foam Color", Color) = (0.9, 0.95, 1.0, 0.9)
        _FoamThickness ("Foam Thickness", Range(0, 0.3)) = 0.1
        _FoamDensity ("Foam Density", Range(0, 2)) = 1.0
        _FoamSpeed ("Foam Speed", Float) = 0.5
        _RippleSpeed ("Ripple Speed", Float) = 1.0
        _RippleFrequency ("Ripple Frequency", Float) = 10.0
        _NoiseScale ("Noise Scale", Float) = 5.0
        _EdgeFoam ("Edge Foam", Range(0, 1)) = 0.5
    }
    
    SubShader {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
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
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float4 _WaterColor;
            float4 _FoamColor;
            float _FoamThickness;
            float _FoamDensity;
            float _FoamSpeed;
            float _RippleSpeed;
            float _RippleFrequency;
            float _NoiseScale;
            float _EdgeFoam;

            // 噪声函数
            float noise(float2 uv) {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float smoothNoise(float2 uv) {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 uv) {
                float value = 0.0;
                float amplitude = 0.5;
                
                for(int i = 0; i < 4; i++) {
                    value += amplitude * smoothNoise(uv);
                    amplitude *= 0.5;
                    uv *= 2.0;
                }
                return value;
            }

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                
                // 创建泡沫噪声图案
                float2 foamUV1 = uv * _NoiseScale + _Time.x * _FoamSpeed;
                float2 foamUV2 = uv * _NoiseScale * 1.7 + _Time.x * _FoamSpeed * 1.3;
                
                float foamNoise1 = fbm(foamUV1);
                float foamNoise2 = fbm(foamUV2);
                float combinedFoam = (foamNoise1 + foamNoise2) * 0.5;
                
                // 边缘泡沫 - 在Quad边缘产生泡沫
                float edgeFoam = 1.0 - smoothstep(0.0, 0.2, min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
                edgeFoam *= _EdgeFoam;
                
                // 涟漪效果
                float2 rippleUV = uv * _RippleFrequency;
                float ripple = sin(rippleUV.x + _Time.y * _RippleSpeed) * 
                              sin(rippleUV.y + _Time.y * _RippleSpeed * 1.3) * 0.1;
                
                // 泡沫遮罩
                float foamMask = combinedFoam * _FoamDensity + edgeFoam + ripple;
                foamMask = saturate(foamMask);
                
                // 泡沫厚度控制
                float foamArea = step(1.0 - _FoamThickness, foamMask);
                
                // 颜色混合
                fixed4 col = _WaterColor;
                
                // 泡沫颜色（在泡沫区域使用泡沫颜色）
                col = lerp(col, _FoamColor, foamArea);
                
                // 半透明泡沫效果
                float foamAlpha = foamMask * _FoamColor.a;
                col.a = max(_WaterColor.a, foamAlpha);
                
                // 添加一些泡沫细节变化
                col.rgb += (combinedFoam - 0.5) * 0.1 * foamArea;
                
                return col;
            }
            ENDCG
        }
    }
}