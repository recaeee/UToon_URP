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
        private Material uberMat;
        private RenderTargetIdentifier source;
        private RenderTextureDescriptor sourceDesc;
        private RenderTexture volumeLightTex;

        private Mesh volumeMesh;
        
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
            this.uberMat = uberMat;

            return true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            var cmd = CommandBufferPool.Get("UToon-VolumeLight");
            using (new ProfilingScope(cmd, profilingSampler))
            {
                mat.SetFloat(ShaderConstants.sampleCount, UToonVolumeLight.SampleCount.value);
                mat.SetFloat(ShaderConstants.absorption, UToonVolumeLight.Absorption.value);
                var tanFov = Mathf.Tan(camera.fieldOfView / 2 * Mathf.Deg2Rad);
                var tanFovWidth = tanFov * camera.aspect;
                mat.SetVector(ShaderConstants.cameraInfo, new Vector4(tanFovWidth, tanFov, 0.0f, 0.0f));
                cmd.SetRenderTarget(volumeLightTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(true, true, Color.clear);
                DrawSpotVolumeLights(cmd, renderingData.lightData);
                // Blitter.BlitTexture(cmd, source, volumeLightTex, mat, 0);
                uberMat.SetTexture(ShaderConstants.uToonVolumeLightTex, volumeLightTex);
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

        private void DrawSpotVolumeLights(CommandBuffer cmd, LightData lightData)
        {
            int spotLightIndex = 0;
            for (int i = 0; i < lightData.visibleLights.Length; i++)
            {
                VisibleLight visibleLight = lightData.visibleLights[i];
                if (visibleLight.lightType != LightType.Spot)
                {
                    continue;
                }
                //聚光灯的包围锥体
                Vector4[] spotBoundaryPlanes = GetSpotLightVolumeBoundary(visibleLight.light);
                mat.SetVectorArray(ShaderConstants.spotBoundaryPlanes, spotBoundaryPlanes);
                mat.SetVector(ShaderConstants.spotVolumeLightInfo,
                    new Vector4(spotLightIndex++, spotBoundaryPlanes.Length, 0.0f, 0.0f));
                SetSpotVolomeLightMesh(visibleLight.light);
                cmd.DrawMesh(volumeMesh, visibleLight.localToWorldMatrix, mat, 0);
            }
        }

        private Vector4[] GetSpotLightVolumeBoundary(Light light)
        {
            if (light.type != LightType.Spot)
            {
                return null;
            }

            Vector4[] planes = new Vector4[5];
        
            Matrix4x4 viewProjection = Matrix4x4.identity;
            viewProjection = Matrix4x4.Perspective(light.spotAngle, 1, 0.03f, light.range)
                             * Matrix4x4.Scale(new Vector3(1, 1, -1)) * light.transform.worldToLocalMatrix;
            var m0 = viewProjection.GetRow(0);
            var m1 = viewProjection.GetRow(1);
            var m2 = viewProjection.GetRow(2);
            var m3 = viewProjection.GetRow(3);
            planes[0] = -(m3 + m0);
            planes[1] = -(m3 - m0);
            planes[2] = -(m3 + m1);
            planes[3] = -(m3 - m1);
            //ignore near
            planes[4] = -(m3 - m2);
        
            return planes;
        }

        private void SetSpotVolomeLightMesh(Light light)
        {
            if (light.type != LightType.Spot)
            {
                return;
            }

            if (volumeMesh == null)
            {
                volumeMesh = new Mesh();
            }
            
            var tanFOV = Mathf.Tan(light.spotAngle / 2 * Mathf.Deg2Rad);
            float range = light.range;
            var verts = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-tanFOV, -tanFOV, 1) * range,
                new Vector3(-tanFOV,  tanFOV, 1) * range,
                new Vector3( tanFOV,  tanFOV, 1) * range,
                new Vector3( tanFOV, -tanFOV, 1) * range,
            };
            volumeMesh.Clear();
            volumeMesh.vertices = verts;
            volumeMesh.triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 1,
                1, 4, 3,
                1, 3, 2,
            };
            volumeMesh.RecalculateNormals();
        }

        static class ShaderConstants
        {
            public static readonly int uToonVolumeLightTex = Shader.PropertyToID("_VolumeLightTex");
            public static readonly int sampleCount = Shader.PropertyToID("_SampleCount");
            
            public static readonly int spotBoundaryPlanes = Shader.PropertyToID("_SpotBoundaryPlanes");
            public static readonly int spotVolumeLightInfo = Shader.PropertyToID("_SpotVolumeLightInfo");
            public static readonly int absorption = Shader.PropertyToID("_Absorption");
            public static readonly int cameraInfo = Shader.PropertyToID("_CameraInfo");

        }
    }
}