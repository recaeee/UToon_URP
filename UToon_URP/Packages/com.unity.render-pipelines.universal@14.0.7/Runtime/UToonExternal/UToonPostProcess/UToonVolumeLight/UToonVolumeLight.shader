Shader "Hidden/UToon/VolumeLight"
{
    HLSLINCLUDE
    // #include "Packages/com.unity.render-pipelines.core@14.0.7/ShaderLibrary/Common.hlsl"
    // #include "Packages/com.unity.render-pipelines.universal@14.0.7/ShaderLibrary/Core.hlsl"
    // #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

    TEXTURE2D(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);

    float4 Frag(Varyings input):SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        #if defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
            uv = RemapFoveatedRenderingResolve(uv);
        #endif
        
        return float4(uv,0.0,1.0);
        // return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord);
    }
    ENDHLSL

    SubShader
    {

        Pass
        {
            Name "UToon Volume Light"
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}