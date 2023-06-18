using System;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("UToon/UToonVolumeLight")]
    public sealed class UToonVolumeLight : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter Enabled = new BoolParameter(true, true);

        public ClampedIntParameter SampleCount = new ClampedIntParameter(5, 0, 100, true);
        
        public ClampedFloatParameter MaxRayLength = new ClampedFloatParameter(100f, 0f, 10000f, true);
        
        public ClampedFloatParameter Density = new ClampedFloatParameter(0.5f, 0f, 1f, true);
        
        public ClampedFloatParameter MieG = new ClampedFloatParameter(0.5f, 0f, 1f, true);
        public bool IsActive() => Enabled.value;

        public bool IsTileCompatible() => true;
    }
}

