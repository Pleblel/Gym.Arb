using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, TextureMap};
    public DrawMode drawMode;

    public int size;
    public int worldSize;
    public float noiseScale;
    public int octaves;
    [Range(0, 1)] public float persistance;
    public float lacunarity;
    public int seed;

    public Vector2 offset;
    public int tiling = 32;
    public int resolution;

    public bool autoUpdate;

    public TerrainType[] regions;

    public MeshGenerator meshGenerator;
    public Erosion erosion;
    [HideInInspector] public float[,] currentHeightMap;

    public void GenerateMap(int randSeed)
    {
        if (randSeed != 100001)
            seed = randSeed;

        int baseSize = size;

        float[,] noiseMap = Noise.GenerateNoiseMap(
            baseSize, baseSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        currentHeightMap = noiseMap;

        if (erosion != null)
        {
            erosion.heightMap = currentHeightMap;
            erosion.meshGenerator = meshGenerator;
        }

        RegenerateTexturesFromHeightMap(currentHeightMap);

        if (meshGenerator != null)
        {
            meshGenerator.size = new Vector2Int(baseSize, baseSize);
            meshGenerator.heightMap = currentHeightMap;
            meshGenerator.CreateShape();
        }
    }

    public void RegenerateTexturesFromHeightMap(float[,] sourceMap)
    {
        if (sourceMap == null)
        {
            Debug.LogWarning("RegenerateTexturesFromHeightMap: sourceMap is null.");
            return;
        }

        int baseSize = sourceMap.GetLength(0);
        int colorMapSize = baseSize * resolution;
        Color[] colorMap = new Color[colorMapSize * colorMapSize];

        for (int y = 0; y < colorMapSize; y++)
        {
            for (int x = 0; x < colorMapSize; x++)
            {
                float u = x / (float)(colorMapSize - 1);
                float v = y / (float)(colorMapSize - 1);
                float fx = u * (baseSize - 1);
                float fy = v * (baseSize - 1);
                int x0 = Mathf.FloorToInt(fx);
                int y0 = Mathf.FloorToInt(fy);
                int x1 = Mathf.Min(x0 + 1, baseSize - 1);
                int y1 = Mathf.Min(y0 + 1, baseSize - 1);
                float tx = fx - x0;
                float ty = fy - y0;

                float n00 = sourceMap[x0, y0];
                float n10 = sourceMap[x1, y0];
                float n01 = sourceMap[x0, y1];
                float n11 = sourceMap[x1, y1];
                float heightValue = Mathf.Lerp(
                    Mathf.Lerp(n00, n10, tx),
                    Mathf.Lerp(n01, n11, tx),
                    ty);


                Color finalColor = Color.black;
                for (int i = 0; i < regions.Length; i++)
                {
                    if (heightValue <= regions[i].height)
                    {
                        float texU = (u * tiling) % 1f;
                        float texV = (v * tiling) % 1f;
                        regions[i].texture.wrapMode = TextureWrapMode.Repeat;

                        if (i == 0)
                        {
                            finalColor = regions[i].texture.GetPixelBilinear(texU, texV);
                        }
                        else
                        {
                            float lowerHeight = regions[i - 1].height;
                            float upperHeight = regions[i].height;
                            float t = Mathf.InverseLerp(lowerHeight, upperHeight, heightValue);
                            Color lower = regions[i - 1].texture.GetPixelBilinear(texU, texV);
                            Color upper = regions[i].texture.GetPixelBilinear(texU, texV);
                            finalColor = Color.Lerp(lower, upper, t);
                        }
                        break;
                    }
                }

                colorMap[y * colorMapSize + x] = finalColor;
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (display != null)
        {
            if (drawMode == DrawMode.NoiseMap)
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(sourceMap));
            else if (drawMode == DrawMode.TextureMap)
                display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, colorMapSize, colorMapSize));
        }
    }

    private void OnValidate()
    {
        if (size < 1) size = 1;
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Texture2D texture;
}
