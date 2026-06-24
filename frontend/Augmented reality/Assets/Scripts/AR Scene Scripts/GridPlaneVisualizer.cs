using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

[RequireComponent(typeof(ARPlane))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridMeshPlaneVisualizer : MonoBehaviour
{
    [Header("Grid Settings")]
    public float tileSize = 0.1f;
    public float tileSpacing = 0.02f;
    public float yOffset = 0.001f;

    private ARPlane plane;
    private Mesh mesh;

    float Step => tileSize + tileSpacing;

    void Awake()
    {
        plane = GetComponent<ARPlane>();
        mesh = new Mesh();
        mesh.name = "Procedural Grid Mesh";
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void OnEnable()
    {
        plane.boundaryChanged += OnBoundaryChanged;
        GenerateGridMesh();
    }

    void OnDisable()
    {
        plane.boundaryChanged -= OnBoundaryChanged;
        mesh.Clear();
    }

    void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
    {
        GenerateGridMesh();
    }

    void GenerateGridMesh()
    {
        if (plane.boundary.Length < 3)
            return;

        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Get boundary bounds
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (var point in plane.boundary)
        {
            if (point.x < minX) minX = point.x;
            if (point.x > maxX) maxX = point.x;
            if (point.y < minZ) minZ = point.y;
            if (point.y > maxZ) maxZ = point.y;
        }

        minX = Mathf.Floor(minX / Step) * Step;
        minZ = Mathf.Floor(minZ / Step) * Step;
        maxX = Mathf.Ceil(maxX / Step) * Step;
        maxZ = Mathf.Ceil(maxZ / Step) * Step;

        int vertexIndex = 0;

        for (float x = minX; x < maxX; x += Step)
        {
            for (float z = minZ; z < maxZ; z += Step)
            {
                Vector3 center = new Vector3(
                    x + tileSize / 2f,
                    0,
                    z + tileSize / 2f
                );

                if (!IsInsidePolygon(center))
                    continue;

                float half = tileSize / 2f;

                Vector3 v0 = new Vector3(center.x - half, yOffset, center.z - half);
                Vector3 v1 = new Vector3(center.x - half, yOffset, center.z + half);
                Vector3 v2 = new Vector3(center.x + half, yOffset, center.z + half);
                Vector3 v3 = new Vector3(center.x + half, yOffset, center.z - half);

                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));

                triangles.Add(vertexIndex + 0);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);

                triangles.Add(vertexIndex + 0);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
    }

    bool IsInsidePolygon(Vector3 localPoint)
    {
        var boundary = plane.boundary;
        int crossings = 0;

        for (int i = 0; i < boundary.Length; i++)
        {
            Vector2 a = boundary[i];
            Vector2 b = boundary[(i + 1) % boundary.Length];

            if (((a.y > localPoint.z) != (b.y > localPoint.z)) &&
                (localPoint.x < (b.x - a.x) * (localPoint.z - a.y) / (b.y - a.y) + a.x))
            {
                crossings++;
            }
        }

        return (crossings % 2 == 1);
    }
}
