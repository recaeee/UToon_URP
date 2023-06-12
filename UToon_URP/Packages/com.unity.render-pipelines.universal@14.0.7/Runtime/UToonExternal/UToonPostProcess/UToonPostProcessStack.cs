using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class UToonPostProcessStack
    {
        private ScriptableRenderContext context;
        private RenderingData renderingData;
        private Material uberMat;
        private UToonVolumeLightRenderPass UToonVolumeLightRenderPass;

        public UToonPostProcessStack()
        {
            UToonVolumeLightRenderPass = new UToonVolumeLightRenderPass();
        }

        public bool Setup(Material uberMat, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            this.context = context;
            this.renderingData = renderingData;
            this.uberMat = uberMat;
            return true;
        }

        public void RenderVolumeLight()
        {
            if (UToonVolumeLightRenderPass.Setup(uberMat,renderingData))
            {
                UToonVolumeLightRenderPass.Execute(context, ref renderingData);
            }
            
        }
    }
    
    public static class UToonConstants
    {
        public static readonly int volumeLightTex = Shader.PropertyToID("_VolumeLightTex");
    }
}


