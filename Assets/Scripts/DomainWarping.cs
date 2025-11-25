using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshGenerator))]
public class DomainWarping : MonoBehaviour
{
    public MeshGenerator meshGenerator;

    [Header("Resolution")]
    public bool useMeshSize = true;
    public Vector2Int customSize = new Vector2Int(256, 256);

    [Header("Seed")]
    public int seed = 1;

    [Header("Warp Noise (for warp_x, warp_y)")]
    [Range(1, 8)] public int warpOctaves = 3;
    public float warpScale = 80f;      // like warpScale in your formula
    public float warpStrength = 6f;    // how far to push coords in grid units

    [Header("Base Noise (height field)")]
    [Range(1, 10)] public int baseOctaves = 5;
    public float baseScale = 40f;      // like baseScale in your formula

    [Header("Height Output")]
    public float heightMultiplier = 25f;

    [HideInInspector] public float[,] latestHeightMap;

    void OnValidate()
    {
        if (meshGenerator == null)
            meshGenerator = GetComponent<MeshGenerator>();
    }

    /// <summary>
    /// Called by editor button. Generates heightmap, assigns it to MeshGenerator, rebuilds mesh.
    /// </summary>
    public void GenerateAndApply()
    {
        if (meshGenerator == null)
            meshGenerator = GetComponent<MeshGenerator>();

        if (meshGenerator == null)
        {
            Debug.LogError("DomainWarpMountainGenerator: No MeshGenerator found.");
            return;
        }

        int width, height;
        if (useMeshSize)
        {
            width = Mathf.Max(1, meshGenerator.size.x);
            height = Mathf.Max(1, meshGenerator.size.y);
        }
        else
        {
            width = Mathf.Max(1, customSize.x);
            height = Mathf.Max(1, customSize.y);
        }

        latestHeightMap = GenerateHeightMap(width, height);

        meshGenerator.heightMap = latestHeightMap;
        meshGenerator.heightMultiplier = heightMultiplier;
        meshGenerator.CreateShape();
    }

    float[,] GenerateHeightMap(int width, int height)
    {
        float[,] map = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // --- 1. warp_x, warp_y ---
                float warp_x = FBMPerlin(x, y, warpOctaves, warpScale, seed + 1000);
                float warp_y = FBMPerlin(x, y, warpOctaves, warpScale, seed + 2000);

                // Map from [0,1] to [-1,1] so warp can go both directions
                warp_x = warp_x * 2f - 1f;
                warp_y = warp_y * 2f - 1f;

                // warped_x = x + warp_x * warpStrength
                // warped_y = y + warp_y * warpStrength
                float warped_x = x + warp_x * warpStrength;
                float warped_y = y + warp_y * warpStrength;

                // --- 2. height from base fBm with warped coords ---
                float h = FBMPerlin(warped_x, warped_y, baseOctaves, baseScale, seed);

                map[x, y] = h;  // in [0,1] (roughly)
            }
        }

        return map;
    }

    /// <summary>
    /// fBm Perlin implementation:
    /// Σ perlin(x*2^i/scale, y*2^i/scale, seedOffset) * (1/2^i)
    /// We approximate the "seed" parameter by offsetting the input coords.
    /// </summary>
    float FBMPerlin(float x, float y, int octaves, float scale, int seedOffset)
    {
        float value = 0f;
        float amplitudeSum = 0f;

        float invScale = 1f / Mathf.Max(scale, 0.0001f);

        for (int i = 0; i < octaves; i++)
        {
            float freq = Mathf.Pow(2f, i);            // 2^i
            float amp = 1f / Mathf.Pow(2f, i);        // 1/2^i

            float nx = x * freq * invScale;
            float ny = y * freq * invScale;

            // emulate "seed" argument by shifting coords
            float s = (seed + seedOffset) * 0.01f;
            float n = Mathf.PerlinNoise(nx + s, ny + s);   // in [0,1]

            value += n * amp;
            amplitudeSum += amp;
        }

        // normalize so result stays roughly [0,1]
        return value / amplitudeSum;
    }
}
