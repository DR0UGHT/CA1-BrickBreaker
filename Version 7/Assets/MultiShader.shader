Shader "Custom/InstancedCircleURP"
{
    Properties
    {
        [MainColor] _InstColor("Color", Color) = (1,1,1,1)
        _AlphaMask("Alpha Mask", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "CirclePass"
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float _AlphaMask;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float4 worldPos = mul(UNITY_MATRIX_M, float4(v.positionOS, 1.0));
                o.positionCS = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = v.uv;
                return o;
            }

            float circleMask(float2 uv)
            {
                float2 center = uv - 0.5;
                return smoothstep(0.5, 0.48, length(center));
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _InstColor);
                bool isWhite = all(abs(color.rgb - float3(1.0, 1.0, 1.0)) < 0.01);
                float alpha = !isWhite ? circleMask(i.uv) : 1.0;
                return float4(color.rgb, color.a * alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
