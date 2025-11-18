using UnityEngine;
using UnityEngine.Rendering; // <-- add this

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    public Vector2Int size = new Vector2Int(128, 128);
    public Vector2 worldSize = new Vector2(100f, 100f);
    public float[,] heightMap;
    public float heightMultiplier = 5f;

    void Awake()
    {
        mesh = new Mesh();

        // enable 32-bit indices when needed (or always — it's fine)
        int vertCountEstimate = (size.x + 1) * (size.y + 1);
        if (vertCountEstimate > 65000)
            mesh.indexFormat = IndexFormat.UInt32;
        else
            mesh.indexFormat = IndexFormat.UInt16;

        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void CreateShape()
    {
        if (heightMap == null) return;

        int width = Mathf.Max(1, size.x);
        int height = Mathf.Max(1, size.y);

        // If resolution changed after Awake, re-check the index format:
        int vertCount = (width + 1) * (height + 1);
        mesh.indexFormat = (vertCount > 65000) ? IndexFormat.UInt32 : IndexFormat.UInt16;

        float stepX = worldSize.x / width;
        float stepZ = worldSize.y / height;

        vertices = new Vector3[(width + 1) * (height + 1)];
        uvs = new Vector2[(width + 1) * (height + 1)];

        int i = 0;
        for (int z = 0; z <= height; z++)
        {
            float v = (float)z / height;
            for (int x = 0; x <= width; x++)
            {
                float u = (float)x / width;
                float y = SampleHeight(heightMap, u, v) * heightMultiplier;

                vertices[i] = new Vector3(x * stepX, y, z * stepZ);
                uvs[i] = new Vector2(u, v);
                i++;
            }
        }

        triangles = new int[width * height * 6];
        int vert = 0, tris = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        UpdateMesh();
    }

    public void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    static float SampleHeight(float[,] map, float u, float v)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        float x = Mathf.Clamp01(u) * (w - 1);
        float y = Mathf.Clamp01(v) * (h - 1);

        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = Mathf.Min(x0 + 1, w - 1);
        int y1 = Mathf.Min(y0 + 1, h - 1);

        float tx = x - x0;
        float ty = y - y0;

        float h00 = map[x0, y0];
        float h10 = map[x1, y0];
        float h01 = map[x0, y1];
        float h11 = map[x1, y1];

        float a = Mathf.Lerp(h00, h10, tx);
        float b = Mathf.Lerp(h01, h11, tx);
        return Mathf.Lerp(a, b, ty);
    }
}