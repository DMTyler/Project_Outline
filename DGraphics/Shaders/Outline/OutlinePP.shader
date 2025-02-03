Shader "Hidden/DGraphics/OutlinePP"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
        }
        
        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            int _Width;
            int _Height;

            StructuredBuffer<float4> _OutlineColors;
            StructuredBuffer<float4> _OutlineParams;
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_OutlineInfo);
            SAMPLER(sampler_OutlineInfo);

            struct VertIn
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertOut
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VertOut vert(VertIn i)
            {
                VertOut o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }
            
            inline float greyScale(float3 color)
            {
                return 0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b;
            }

            float4 frag(VertOut i) : SV_Target
            {
                const float sobelX[9] = {
                    -1, 0, 1,
                    -2, 0, 2,
                    -1, 0, 1
                };

                const float sobelY[9] = {
                    -1, -2, -1,
                    0, 0, 0,
                    1, 2, 1
                };

                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 outlineInfo = SAMPLE_TEXTURE2D(_OutlineInfo, sampler_OutlineInfo, i.uv);

                int index = (int)outlineInfo.a;
                if (index == 0) return color;
                
                float depth = outlineInfo.b;
                float linearDepth = Linear01Depth(depth, _ProjectionParams);
                
                index -= 1;
                
                float4 outlineColor = _OutlineColors[index];
                float4 outlineParams = _OutlineParams[index];
                
                float outlineWidth = outlineParams.x;
                // outlineWidth *= linearDepth;
                
                float threshold = outlineParams.y;
                float normalStrength = outlineParams.z;
                float depthStrength = outlineParams.w;
                
                float2 uvs[9];
                float uStep = 1.0 / _Width;
                float vStep = 1.0 / _Height;
                
                uvs[0] = i.uv + float2(-uStep, -vStep) * outlineWidth;
                uvs[1] = i.uv + float2(0, -vStep) * outlineWidth;
                uvs[2] = i.uv + float2(uStep, -vStep) * outlineWidth;
                uvs[3] = i.uv + float2(-uStep, 0) * outlineWidth;
                uvs[4] = i.uv;
                uvs[5] = i.uv + float2(uStep, 0) * outlineWidth;
                uvs[6] = i.uv + float2(-uStep, vStep) * outlineWidth;
                uvs[7] = i.uv + float2(0, vStep) * outlineWidth;
                uvs[8] = i.uv + float2(uStep, vStep) * outlineWidth;

                float Gx = 0;
                float Gy = 0;
                bool greater = false;
                bool less = false;
                for (int x = 0; x < 9; x++)
                {
                    int otherIndex = (int)SAMPLE_TEXTURE2D(_OutlineInfo, sampler_OutlineInfo, uvs[x]).a;
                    otherIndex -= 1;
                    greater = (otherIndex < index);
                    less = (otherIndex > index);
                    if (greater || less) break;
                }

                if (greater) return outlineColor;
                if (less) return color;
                
                float3 normals[9];
                for (int x = 0; x < 9; x++)
                {
                    float4 info = SAMPLE_TEXTURE2D(_OutlineInfo, sampler_OutlineInfo, uvs[x]);
                    float2 normalXY = info.rg;
                    float normalZ = sqrt(1 - info.r * info.r - info.g * info.g);
                    
                    normals[x] = float3(normalXY, normalZ);
                    if ((int)info.a < 0) normals[x].z *= -1;
                }
                
                for (int x = 0; x < 9; x++)
                {
                    float3 currentNormal = normals[x];
                    float angleX = dot(normals[4], currentNormal);
                    float angleY = dot(normals[4], currentNormal);

                    Gx += (1 - angleX) * sobelX[x] * normalStrength;
                    Gy += (1 - angleY) * sobelY[x] * normalStrength;
                }
                
                if (sqrt(Gx * Gx + Gy * Gy) > threshold)
                {
                    return outlineColor;
                }
                
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
