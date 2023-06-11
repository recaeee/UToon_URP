using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class UToonPostProcessStack
    {
        private UToonVolumeLightRenderPass UToonVolumeLightRenderPass;

        public UToonPostProcessStack()
        {
            UToonVolumeLightRenderPass = new UToonVolumeLightRenderPass();
        }

        public void RenderVolumeLight()
        {
            
        }
    }
}


