Shader "Hidden/UToon/VolumeLight"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    
    TEXTURE2D(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);

    //采样次数
    int _SampleCount;
    //最远步进距离
    float _MaxRayLength;
    //介质密度
    float _Density;
    //介质散射性质
    float _MieG;

    float GetLightAttenuation(float3 worldPos)
    {
        float atten = 1;

        return atten;
    }

    //HG公式，参考https://zhuanlan.zhihu.com/p/21425792中的散射函数
    float Phase(float cos)
    {
        float mieG2 = pow(_MieG, 2);
        return rcp(4 * PI) * rcp(1 - mieG2) / pow(1 + mieG2 - 2 * _MieG * cos, 1.5);
    }

    float4 Scatter(float3 accumulatedLightEnergy, float accumulatedTransmittance, float sliceLight, float sliceDensity)
    {
        sliceDensity = max(sliceDensity, 0.000001);
        float sliceTransmittance = exp(-sliceDensity / _SampleCount);

        float3 sliceLightIntegral = sliceLight * (1.0 - sliceTransmittance) / sliceDensity;
        accumulatedLightEnergy += sliceLightIntegral * accumulatedTransmittance;
        accumulatedTransmittance *= sliceTransmittance;

        return float4(accumulatedLightEnergy, accumulatedTransmittance);
    }

    float4 RayMarching(Light light, float2 screenPos, float3 rayStart, float3 rayEnd, float3 rayDir, float rate)
    {
        float4 lightEnergy = float4(0, 0, 0, 1);
        rate *= _Density;
        //计算每次步进距离
        float3 step = rcp(_SampleCount);
        //计算光线步进方向与光源的夹角
        float cos = dot(light.direction, -rayDir);

        [loop]
        for (float i = step.x; i < 1; i += step.x)
        {
            //确定当前采样点
            float lerpValue = i;
            float3 curPos = lerp(rayStart, rayEnd, i);
            //是否考虑阴影
            float atten = GetLightAttenuation(curPos);
            //考虑方向光的Phase Function(由于光线散射到各方向的能量衰减)
            atten *= Phase(cos);
            //计算累计光线能量
            lightEnergy = Scatter(lightEnergy.rgb, lightEnergy.a, atten, rate);
        }

        lightEnergy.rgb *= light.color;
        lightEnergy.a = saturate(lightEnergy.a);
        return lightEnergy;
    }

    float4 Frag(Varyings input):SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        #if defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
            uv = RemapFoveatedRenderingResolve(uv);
        #endif

        //采样深度
        float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
        //求片元世界坐标
        float4 worldPos = mul(_InvViewProjMatrix, float3(uv * 2 - 1.0, depth));
        worldPos /= worldPos.w;
        //确定RayMarching方向
        float3 startPos = _WorldSpaceCameraPos;
        float3 rayDir = worldPos - startPos;
        //确定RayMarching距离
        float rayLength = length(rayDir);
        rayDir /= rayLength;
        rayLength = min(rayLength, _MaxRayLength);
        //确定RayMarching终点
        float3 endPos = _WorldSpaceCameraPos + rayDir * rayLength;
        //计算主方向光的RayMarching
        float4 color = RayMarching(GetMainLight(), uv, startPos, endPos, rayDir, rayLength / _MaxRayLength);
        return color;
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