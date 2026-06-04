using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tạo lưới mesh 3D hiển thị vùng nhìn của địch trên mặt đất.
/// Vùng nhìn sẽ bị cắt bởi vật cản (obstacleMask) tạo cảm giác thực tế.
/// Đổi sang màu đỏ khi phát hiện người chơi.
/// </summary>
[RequireComponent(typeof(EnemyVision))]
public class EnemyVisionCone : MonoBehaviour
{
    [SerializeField] private float viewRadius = 9f;
    [SerializeField] private float viewAngle = 100f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float meshResolution = 1.5f; 
    [SerializeField] private int edgeResolveIterations = 3;
    [SerializeField] private float edgeDstThreshold = 0.5f;

    private MeshFilter viewMeshFilter;
    private Mesh viewMesh;
    private EnemyVision enemyVision;

    private void Start()
    {
        enemyVision = GetComponent<EnemyVision>();
        
        // Tạo GameObject con chứa Mesh
        GameObject coneObj = new GameObject("VisionConeMesh");
        coneObj.transform.SetParent(transform);
        coneObj.transform.localPosition = new Vector3(0, 0.1f, 0); // Cách mặt đất 0.1m
        coneObj.transform.localRotation = Quaternion.identity;

        viewMeshFilter = coneObj.AddComponent<MeshFilter>();
        MeshRenderer renderer = coneObj.AddComponent<MeshRenderer>();
        
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(1f, 1f, 0f, 0.35f);
        // Set transparent
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;
        renderer.material = mat;

        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;

        // Tự động gán layer mask nếu quên
        obstacleMask = LayerMask.GetMask("Default");
    }

    private void LateUpdate()
    {
        DrawFieldOfView();
    }

    private void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero) viewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) viewPoints.Add(edge.pointB);
                }
            }
            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            // Chuyển đổi global point thành local point so với Transform của Mesh
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
            vertices[i + 1].y = 0; // Ép phẳng sát mặt đất

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();

        // Cập nhật màu sắc nếu phát hiện
        bool detected = false;
        if (enemyVision != null) detected = enemyVision.CanSeePlayer();
        
        Color targetColor = detected ? new Color(1f, 0.1f, 0.1f, 0.5f) : new Color(1f, 0.9f, 0f, 0.35f);
        viewMeshFilter.GetComponent<MeshRenderer>().material.color = targetColor;
    }

    private ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        Vector3 eyePos = transform.position + Vector3.up * 1.6f;

        if (Physics.Raycast(eyePos, dir, out RaycastHit hit, viewRadius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, eyePos + dir * viewRadius, viewRadius, globalAngle);
        }
    }

    private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    private struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;
        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit; point = _point; dst = _dst; angle = _angle;
        }
    }

    private struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA; pointB = _pointB;
        }
    }
}
