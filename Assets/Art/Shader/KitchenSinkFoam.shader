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

            // noise function
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
                
                // Create a foam noise pattern
                float2 foamUV1 = uv * _NoiseScale + _Time.x * _FoamSpeed;
                float2 foamUV2 = uv * _NoiseScale * 1.7 + _Time.x * _FoamSpeed * 1.3;
                
                float foamNoise1 = fbm(foamUV1);
                float foamNoise2 = fbm(foamUV2);
                float combinedFoam = (foamNoise1 + foamNoise2) * 0.5;
                
                // Edge foam - Foaming occurs at the Quad edge
                float edgeFoam = 1.0 - smoothstep(0.0, 0.2, min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
                edgeFoam *= _EdgeFoam;
                
                // Ripple effect
                float2 rippleUV = uv * _RippleFrequency;
                float ripple = sin(rippleUV.x + _Time.y * _RippleSpeed) * 
                              sin(rippleUV.y + _Time.y * _RippleSpeed * 1.3) * 0.1;
                
                // Foam mask
                float foamMask = combinedFoam * _FoamDensity + edgeFoam + ripple;
                foamMask = saturate(foamMask);
                
                // Control of foam thickness
                float foamArea = step(1.0 - _FoamThickness, foamMask);
                
                // Color blending
                fixed4 col = _WaterColor;
                
                // Bubble color (use bubble color in the bubble area)
                col = lerp(col, _FoamColor, foamArea);
                
                // Translucent foam effect
                float foamAlpha = foamMask * _FoamColor.a;
                col.a = max(_WaterColor.a, foamAlpha);
                
                // Add some details of foam to enhance the variation.
                col.rgb += (combinedFoam - 0.5) * 0.1 * foamArea;
                
                return col;
            }
            ENDCG
        }
    }
}