// Soft unlit floss for the sugar thread. The LineRenderer tiles the texture along the
// thread in world units; the texture drifts from the kart toward the candy over time.
// Texture alpha shapes the floss, and texture red lifts its fibres from the vertex colour to white.
Shader "CottonCircuit/SugarFloss"
{
    Properties
    {
        [MainTexture] _MainTex ("Floss", 2D) = "white" {}
        _FlowSpeed ("Flow speed (tiles per second)", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SugarFloss"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _FlowSpeed;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                // U grows from the kart toward the candy, so subtracting time moves the pattern that way.
                output.uv = TRANSFORM_TEX(input.uv, _MainTex) - float2(frac(_Time.y * _FlowSpeed), 0);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 floss = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return half4(lerp(input.color.rgb, 1, floss.r), floss.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
