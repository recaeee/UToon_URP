#ifndef UTOON_UBER_EXTERNAL_INCLUDED
#define UTOON_UBER_EXTERNAL_INCLUDED

TEXTURE2D(_VolumeLightTex);

float3 DebugVolumeLight(float2 uv)
{
    return SAMPLE_TEXTURE2D(_VolumeLightTex, sampler_LinearClamp, uv);
}

#endif
