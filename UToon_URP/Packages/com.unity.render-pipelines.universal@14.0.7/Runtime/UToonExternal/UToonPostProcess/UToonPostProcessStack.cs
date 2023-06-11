using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class UToonPostProcessStack
    {
        private ScriptableRenderContext context;
        private RenderingData renderingData;
        private UToonVolumeLightRenderPass UToonVolumeLightRenderPass;

        public UToonPostProcessStack()
        {
            UToonVolumeLightRenderPass = new UToonVolumeLightRenderPass();
        }

        public bool Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            this.context = context;
            this.renderingData = renderingData;
            return true;
        }

        public void RenderVolumeLight()
        {
            UToonVolumeLightRenderPass.Execute(context, ref renderingData);
        }
    }
}


