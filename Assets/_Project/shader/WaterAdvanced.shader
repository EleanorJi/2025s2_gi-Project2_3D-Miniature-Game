Shader "Custom/WaterAdvanced" {
    Properties {
        _MainColor ("Water Color", Color) = (0.2, 0.6, 0.8, 0.6)
        _WaveSpeed ("Wave Speed", Float) = 1.5
        _WaveFrequency ("Wave Frequency", Float) = 2.0
        _WaveHeight ("Wave Height", Float) = 0.15
        _WaveSharpness ("Wave Sharpness", Range(0.1, 2.0)) = 0.8
        _NoiseScale ("Noise Scale", Float) = 3.0
        _NoiseStrength ("Noise Strength", Float) = 0.2
    }
    
    SubShader {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 200
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float _WaveSpeed;
            float _WaveFrequency;
            float _WaveHeight;
            float _WaveSharpness;
            float _NoiseScale;
            float _NoiseStrength;

            // A simple noise function for generating more natural waves
            float noise(float2 uv) {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Smooth noise function
            float smoothNoise(float2 uv) {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                // Bilinear interpolation
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + 
                      (c - a) * u.y * (1.0 - u.x) + 
                      (d - b) * u.x * u.y;
            }

            v2f vert (appdata v) {
                v2f o;
                
                // Obtain world coordinates
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // wave calculation: Use the x and z coordinates of the world as input, and output the influence on the y coordinate.
                float wave1 = sin(worldPos.x * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveHeight;
                float wave2 = cos(worldPos.z * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3) * _WaveHeight * 0.8;
                
                // Noise disturbance
                float2 noiseUV = worldPos.xz * _NoiseScale;
                float noiseValue = smoothNoise(noiseUV + _Time.y * _WaveSpeed * 0.5) * _NoiseStrength;
                
                // Combined wave effect
                float combinedWave = (wave1 + wave2) * 0.5 + noiseValue;
                
                // Add the calculated wave value to the y-component of the world coordinates
                worldPos.y += combinedWave * _WaveSharpness;
                
                // Convert the modified world coordinates back to the clipping space
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Simple Fresnel effect based on perspective and normal
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 2.0);
                
                // Base color combined with Fresnel effect
                fixed4 col = _MainColor;
                col.a = _MainColor.a * (0.7 + fresnel * 0.3);
                
                // Add some UV-based ripple details
                float ripple = sin(i.uv.x * 15 + _Time.y * 2) * 0.02 + 
                              sin(i.uv.y * 12 + _Time.y * 1.7) * 0.02;
                col.rgb += ripple;
                
                return col;
            }
            ENDCG
        }
    }
}