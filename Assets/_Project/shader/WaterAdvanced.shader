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

            // 简单的噪声函数，用于生成更自然的波浪:cite[5]
            float noise(float2 uv) {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 平滑的噪声函数
            float smoothNoise(float2 uv) {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                // 双线性插值
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
                
                // 获取世界坐标
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // 正确的波浪计算：使用世界坐标的x和z作为输入，输出影响y坐标
                float wave1 = sin(worldPos.x * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveHeight;
                float wave2 = cos(worldPos.z * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3) * _WaveHeight * 0.8;
                
                // 噪声扰动
                float2 noiseUV = worldPos.xz * _NoiseScale;
                float noiseValue = smoothNoise(noiseUV + _Time.y * _WaveSpeed * 0.5) * _NoiseStrength;
                
                // 组合波浪效果
                float combinedWave = (wave1 + wave2) * 0.5 + noiseValue;
                
                // 关键：将计算出的波浪值加到世界坐标的y分量上
                worldPos.y += combinedWave * _WaveSharpness;
                
                // 将修改后的世界坐标转换回裁剪空间
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 基于视角和法线的简单菲涅尔效应:cite[6]
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 2.0);
                
                // 基础颜色加上菲涅尔效果
                fixed4 col = _MainColor;
                col.a = _MainColor.a * (0.7 + fresnel * 0.3);
                
                // 添加一些基于UV的波纹细节:cite[3]
                float ripple = sin(i.uv.x * 15 + _Time.y * 2) * 0.02 + 
                              sin(i.uv.y * 12 + _Time.y * 1.7) * 0.02;
                col.rgb += ripple;
                
                return col;
            }
            ENDCG
        }
    }
}