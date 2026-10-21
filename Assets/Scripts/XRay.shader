// The hoop rings' "x-ray" pass: draws the ring only where something stands in front of it (the depth test is reversed),
// in a dimmer colour, so the ring can be seen through walls, roofs and trees. The ring's normal bright pass draws it where it is visible.
// Added to a phone build by Editor/AddShadersToBuild. Used by Prim.GlowOrange.
Shader "PolyParcel/XRay"
{
    Properties
    {
        [MainColor] _BaseColor ("Colour", Color) = (1, 0.45, 0.05, 0.4)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "XRay"
            ZTest Greater
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
