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

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            //获取摄像机近裁面矩形四个顶点的世界坐标
            CalculateCameraFrustumVertex(camera, out Vector3[] nearPlaneVertex, out Vector3[] farPlaneVertex);
        }

        private void CalculateCameraFrustumVertex(Camera camera, out Vector3[] nearPlaneVertex,
            out Vector3[] farPlaneVertex)
        {
            //参考：https://gwb.tencent.com/community/detail/122057
            //修改：感觉取Camera.Transform效率太差了，我这里改用模型空间->世界空间的方法求视锥体8个顶点，但其实CPU端矩阵运算效率也不高，不知道性能会不会好点
            nearPlaneVertex = new Vector3[4];
            farPlaneVertex = new Vector3[4];

            float fov = camera.fieldOfView, aspect = camera.aspect;
            float verticalTang = Mathf.Tan(fov / 2f * Mathf.Deg2Rad);
            // float horizontalTang = verticalTang * aspect;
            float nearPlaneDistance = camera.nearClipPlane, farPlaneDistance = camera.farClipPlane;
            float nearVerticalBias = nearPlaneDistance * verticalTang,
                nearHorizontalBias = nearVerticalBias * aspect,
                farVerticalBias = farPlaneDistance * verticalTang,
                farHorizontalBias = farVerticalBias * aspect;

            //求视锥体近平面左上、左下、右下、右上4个顶点的“模型空间”坐标
            nearPlaneVertex = new Vector3[4]
            {
                new Vector3(-nearPlaneDistance, nearVerticalBias, -nearHorizontalBias),
                new Vector3(-nearPlaneDistance, -nearVerticalBias, -nearHorizontalBias),
                new Vector3(-nearPlaneDistance, -nearVerticalBias, nearHorizontalBias),
                new Vector3(-nearPlaneDistance, nearVerticalBias, nearHorizontalBias),
            };
            
            //同理远平面
            farPlaneVertex = new Vector3[4]
            {
                new Vector3(-farPlaneDistance, farVerticalBias, -farHorizontalBias),
                new Vector3(-farPlaneDistance, -farVerticalBias, -farHorizontalBias),
                new Vector3(-farPlaneDistance, -farVerticalBias, farHorizontalBias),
                new Vector3(-farPlaneDistance, farVerticalBias, farHorizontalBias),
            };
            
            //根据摄像机V矩阵变换到世界空间
            Matrix4x4 viewMat = camera.transform.localToWorldMatrix;
            for (int i = 0; i < 4; i++)
            {
                // nearPlaneVertex[i] = 
            }
        }
    }
}