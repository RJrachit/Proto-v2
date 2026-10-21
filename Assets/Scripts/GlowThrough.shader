// A soft light that always shows, even behind walls, roofs and trees (the depth test is switched off, or left on by the _ZTest setting).
// Added together with what is behind it, so it reads as light. Used for the glow round a thrown parcel (ParcelGlow.cs).
// Added to a phone build by Editor/AddShadersToBuild.
Shader "PolyParcel/GlowThrough"
{
    Properties
    {
        [MainTexture] _MainTex ("Glow", 2D) = "white" {}
        [MainColor] _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "GlowThrough"
            ZTest [_ZTest]
            ZWrite Off
            Cull Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return half4(_BaseColor.rgb, t.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
