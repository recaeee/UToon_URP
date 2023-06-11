using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UToonVolumeLightHelper))]
class UToonVolumeLightHelperEditor : Editor
{
    private void OnSceneGUI()
    {
        Camera camera = (target as UToonVolumeLightHelper).GetComponent<Camera>();
        CalculateCameraFrustumVertex(camera, out Vector3[] nearPlaneVertex, out Vector3[] farPlaneVertex);
        Handles.color = Color.cyan;

        float gap = 15f;
        
        for (int i = 0; i < 4; i++)
        {
            Handles.DrawDottedLine(nearPlaneVertex[i], nearPlaneVertex[(i + 1) % 4], gap);
        }
        
        for (int i = 0; i < 4; i++)
        {
            Handles.DrawDottedLine(farPlaneVertex[i], farPlaneVertex[(i + 1) % 4], gap);
        }
        
        for (int i = 0; i < 4; i++)
        {
            Handles.DrawDottedLine(farPlaneVertex[i], farPlaneVertex[(i + 1) % 4], gap);
        }

        for (int i = 0; i < 4; i++)
        {
            Handles.DrawDottedLine(nearPlaneVertex[i], farPlaneVertex[i], gap);
        }
    }

    //获取摄像机近裁面矩形、远裁面8个顶点的世界坐标
    //参考：https://gwb.tencent.com/community/detail/122057
    private void CalculateCameraFrustumVertex(Camera camera, out Vector3[] nearPlaneVertex,
        out Vector3[] farPlaneVertex)
    {
        nearPlaneVertex = new Vector3[4];
        farPlaneVertex = new Vector3[4];

        float fov = camera.fieldOfView, aspect = camera.aspect;
        float verticalTang = Mathf.Tan(fov / 2f * Mathf.Deg2Rad);
        float horizontalTang = verticalTang * aspect;
        float nearPlaneDistance = camera.nearClipPlane, farPlaneDistance = camera.farClipPlane;

        //求视锥体四条"棱柱"的方向向量，顺序为左上、左下、右下、右上
        //构建模型空间下向量，再根据摄像机M矩阵变换到世界空间（不是单位向量，在摄像机z轴上投影为1）
        Transform transform = camera.transform;
        Matrix4x4 viewMat = transform.localToWorldMatrix;
        Vector3 cameraPosWS = transform.position;
        Vector3[] dir = new Vector3[4]
        {
            viewMat * new Vector3(-horizontalTang, verticalTang, 1),
            viewMat * new Vector3(-horizontalTang, -verticalTang, 1),
            viewMat * new Vector3(horizontalTang, -verticalTang, 1),
            viewMat * new Vector3(horizontalTang, verticalTang, 1),
        };
        //求近、远平面顶点的世界坐标
        for (int i = 0; i < 4; i++)
        {
            nearPlaneVertex[i] = cameraPosWS + nearPlaneDistance * dir[i];
            farPlaneVertex[i] = cameraPosWS + farPlaneDistance * dir[i];
        }
    }
}