//参考：https://zhuanlan.zhihu.com/p/37624886
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class UToonVolumeLightRenderPass : ScriptableRenderPass
    {
        public UToonVolumeLightRenderPass()
        {
            profilingSampler = new ProfilingSampler("UToon-VolumeLight");
        }

        public UToonVolumeLight UToonVolumeLight;
        private Shader shader;
        private Material mat;
        private RenderTargetIdentifier source;
        private RenderTextureDescriptor sourceDesc;
        private RenderTexture volumeLightTex;
        
        public bool Setup(Material uberMat, RenderingData renderingData)
        {
            FetchVolumeComponent();

            if (UToonVolumeLight == null || !UToonVolumeLight.IsActive())
            {
                return false;
            }
            
            if (shader == null)
            {
                shader = Shader.Find("Hidden/UToon/VolumeLight");
            }

            if (shader == null)
            {
                return false;
            }
            if (mat == null && shader != null)
            {
                mat = CoreUtils.CreateEngineMaterial(shader);
            }

            if (mat == null)
            {
                return false;
            }
            
            

            source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            sourceDesc = renderingData.cameraData.cameraTargetDescriptor;
            
            if(volumeLightTex != null && (volumeLightTex.width != sourceDesc.width || volumeLightTex.height != sourceDesc.height))
            {
                RenderTexture.ReleaseTemporary(volumeLightTex);
                volumeLightTex = null;
            }
            
            if (volumeLightTex == null)
            {
                volumeLightTex = RenderTexture.GetTemporary(sourceDesc);
                volumeLightTex.name = "_VolumeLightTex";
                volumeLightTex.filterMode = FilterMode.Bilinear;
                volumeLightTex.wrapMode = TextureWrapMode.Clamp;
            }

            uberMat.SetTexture(UToonConstants.volumeLightTex, volumeLightTex);

            return true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            var cmd = CommandBufferPool.Get("UToon-VolumeLight");
            using (new ProfilingScope(cmd, profilingSampler))
            {
                CalculateCameraFrustumVertex(camera, out Vector4[] nearPlaneVertex, out Vector4[] farPlaneVertex);
                mat.SetVectorArray(ShaderConstants.nearPlaneVertex, nearPlaneVertex);
                mat.SetVectorArray(ShaderConstants.farPlaneVertex, farPlaneVertex);

                mat.SetFloat(ShaderConstants.sampleCount, UToonVolumeLight.SampleCount.value);
                mat.SetFloat(ShaderConstants.maxRayLength, UToonVolumeLight.MaxRayLength.value);
                mat.SetFloat(ShaderConstants.density, UToonVolumeLight.Density.value);
                mat.SetFloat(ShaderConstants.mieG, UToonVolumeLight.MieG.value);
                
                cmd.SetRenderTarget(volumeLightTex);
                cmd.ClearRenderTarget(true, true, Color.clear);
                Blitter.BlitTexture(cmd, source, volumeLightTex, mat, 0);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }

        //获取摄像机近裁面矩形、远裁面8个顶点的世界坐标
        //参考：https://gwb.tencent.com/community/detail/122057
        private void CalculateCameraFrustumVertex(Camera camera, out Vector4[] nearPlaneVertex,
            out Vector4[] farPlaneVertex)
        {
            nearPlaneVertex = new Vector4[4];
            farPlaneVertex = new Vector4[4];

            float fov = camera.fieldOfView, aspect = camera.aspect;
            float verticalTang = Mathf.Tan(fov / 2f * Mathf.Deg2Rad);
            float horizontalTang = verticalTang * aspect;
            float nearPlaneDistance = camera.nearClipPlane, farPlaneDistance = camera.farClipPlane;

            //求视锥体四条"棱柱"的方向向量，顺序为左上、左下、右下、右上
            //构建模型空间下向量，再根据摄像机M矩阵变换到世界空间（不是单位向量，在摄像机z轴上投影为1）
            Transform transform = camera.transform;
            Matrix4x4 viewMat = transform.localToWorldMatrix;
            Vector4 cameraPosWS = transform.position;
            Vector4[] dir = new Vector4[4]
            {
                viewMat * new Vector4(-horizontalTang, verticalTang, 1f,0f),
                viewMat * new Vector4(-horizontalTang, -verticalTang, 1f,0f),
                viewMat * new Vector4(horizontalTang, -verticalTang, 1f,0f),
                viewMat * new Vector4(horizontalTang, verticalTang, 1f,0f),
            };
            //求近、远平面顶点的世界坐标
            for (int i = 0; i < 4; i++)
            {
                nearPlaneVertex[i] = cameraPosWS + nearPlaneDistance * dir[i];
                farPlaneVertex[i] = cameraPosWS + farPlaneDistance * dir[i];
            }
        }

        private void FetchVolumeComponent()
        {
            if (UToonVolumeLight == null)
            {
                UToonVolumeLight = VolumeManager.instance.stack.GetComponent<UToonVolumeLight>();
            }
        }

        static class ShaderConstants
        {
            public static readonly int UToonVolumeLightTex = Shader.PropertyToID("_UToonVolumeLightTex");
            public static readonly int nearPlaneVertex = Shader.PropertyToID("nearPlaneVertex");
            public static readonly int farPlaneVertex = Shader.PropertyToID("farPlaneVertex");
            public static readonly int sampleCount = Shader.PropertyToID("_SampleCount");
            public static readonly int maxRayLength = Shader.PropertyToID("_MaxRayLength");
            public static readonly int density = Shader.PropertyToID("_Density");
            public static readonly int mieG = Shader.PropertyToID("_MieG");
        }
    }
}