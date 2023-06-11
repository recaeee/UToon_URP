using System;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("UToon/UToonVolumeLight")]
    public sealed class UToonVolumeLight : VolumeComponent, IPostProcessComponent
    {
        [HideInInspector] public BoolParameter Enabled = new BoolParameter(false, true);

        public bool IsActive() => Enabled.value;

        public bool IsTileCompatible() => true;
    }
}

