Shader "Hidden/UToon/VolumeLight"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    #define SPOT_LIGHT_INDEX _SpotVolumeLightInfo.x
    #define SPOT_PLANES_COUNT _SpotVolumeLightInfo.y
    #define TAN_FOV_HV _CameraInfo.xy
    #define SPOT_BOUNDARY_PLANES_COUNT 5


    TEXTURE2D(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);

    //采样次数
    int _SampleCount;
    //介质吸收率
    float _Absorption;
    //摄像机参数
    float4 _CameraInfo;
    //聚光灯信息
    float4 _SpotVolumeLightInfo;
    float4 _SpotBoundaryPlanes[SPOT_BOUNDARY_PLANES_COUNT];

    struct SpotLight
    {
        float3 color;
        float3 direction;
        float3 position;
        bool isSpot;
        int lightIndex;
    };

    SpotLight GetSpotLight(int lightIndex)
    {
        SpotLight light;
        light.color = _AdditionalLightsColor[lightIndex];
        light.direction = _AdditionalLightsSpotDir[lightIndex];
        light.position = _AdditionalLightsPosition[lightIndex];
        light.isSpot = _AdditionalLightsSpotDir[lightIndex].w == 1.0;
        light.lightIndex = lightIndex;

        return light;
    }

    float IntersectPlane(float4 plane, float3 origin, float3 dir, out float intersect)
    {
        float d = dot(dir, plane.xyz);
        intersect = d;
        return -dot(float4(origin.xyz, 1), plane) / d;
    }

    void GetBoundary(float3 rayDir, out float near, out float far)
    {
        //先获取摄像机的近远平面作为边界
        float maxNear = _ProjectionParams.y;
        float minFar = _ProjectionParams.z;
        //求视线和聚光灯锥体的交点
        float intersected = 0;
        for (int i = 0; i < SPOT_LIGHT_INDEX; i++)
        {
            float t = IntersectPlane(_SpotBoundaryPlanes[i], _WorldSpaceCameraPos, rayDir, intersected);
            if (intersected < 0)
            {
                maxNear = max(maxNear, t);
            }
            if (intersected > 0)
            {
                minFar = min(minFar, t);
            }
        }

        near = maxNear;
        far = minFar;
    }

    float LinearEyeDepth(float z)
    {
        return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
    }

    float DepthToWorldDistance(float2 screenCoord, float depthValue)
    {
        float2 p = (screenCoord.xy * 2 - 1) * TAN_FOV_HV;
        float3 ray = float3(p.xy, 1);
        return LinearEyeDepth(depthValue) * length(ray);
    }

    //光在介质中传播，会被介质吸收一部分
    float BeerLambertLaw(float dis, float absorption)
    {
        return exp(-dis * absorption);
    }

    //transmittance为透射率
    float3 Scattering(SpotLight spotLight, float3 dir, float near, float far)
    {
        float totalLight = 0;
        //计算采样步长
        float stepSize = (far - near) / _SampleCount;


        for (int i = 1; i < _SampleCount; i++)
        {
            float f = stepSize;
            //计算世界空间下的采样点
            float3 pos = _WorldSpaceCameraPos + dir * (near + stepSize * i);
            //考虑光源到采样点的衰减
            float lightDistance = distance(spotLight.position, pos);
            float scat1 = BeerLambertLaw(lightDistance, _Absorption);
            f *= scat1;
            //考虑采样点到摄像机的衰减
            float cameraDistance = distance(_WorldSpaceCameraPos, pos);
            float scat2 = BeerLambertLaw(cameraDistance, _Absorption);
            f *= scat2;
            
            totalLight += f;
        }

        return totalLight * spotLight.color;
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
        //求世界空间深度
        float depthWS = DepthToWorldDistance(uv, depth);
        //求片元世界坐标
        float4 worldPos = mul(_InvViewProjMatrix, float3(uv * 2 - 1.0, depth));
        worldPos /= worldPos.w;
        //确定RayMarching方向
        float3 rayDir = worldPos - _WorldSpaceCameraPos;
        //确定RayMarching起点终点
        float near, far;
        GetBoundary(rayDir, near, far);
        far = min(far, depthWS);
        //手动深度剔除
        //如果摄像机和“圆锥体”的近交点深度 大于 片元摄像机深度，意味着近交点在实际物体前面，则Clip掉这个像素
        float3 nearWorldPos = _WorldSpaceCameraPos + rayDir * near;
        float4 p = TransformWorldToHClip(nearWorldPos);
        p /= p.w;
        //p.z - screenDepth而不是screenDepth - p.z的原因是裁剪空间z轴指向摄像机正后方，因此取反
        clip(p.z - depth);
        //RayMarching
        SpotLight spotLight = GetSpotLight(SPOT_LIGHT_INDEX);
        float3 color = Scattering(spotLight, rayDir, near, far);
        return float4(color, 1);
    }
    ENDHLSL

    SubShader
    {

        Pass
        {
            Name "UToon Volume Light"
            ZTest LESS
            Cull Front
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}