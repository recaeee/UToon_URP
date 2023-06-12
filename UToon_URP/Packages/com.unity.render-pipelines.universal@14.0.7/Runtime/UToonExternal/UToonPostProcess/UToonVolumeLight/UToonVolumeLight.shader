Shader "Hidden/UToon/VolumeLight"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core@14.0.7/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal@14.0.7/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    
    float4 Frag(Varyings input):SV_Target
    {
        return 0;
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