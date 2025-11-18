using System;
using System.Collections;
using UnityEngine;

public class ErosionCoroutine : MonoBehaviour
{
    [Header("Targets")]
    public MeshGenerator meshGenerator;
    public float[,] heightMap;

    [Header("Run")]
    public int droplets = 20000;
    public int seed = 12345;
    public float delaySeconds = 0.01f;
    public int meshUpdateInterval = 20;
    public int bakeIterations = 1;        // how many times to run droplet loop in BakeErosionOnce

    [Header("Hydraulic Params")]
    public int maxLifetime = 30;
    public float inertia = 0.05f;
    public float sedimentCapacityFactor = 4f;
    public float minSedimentCapacity = 0.01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;
    public float evaporateSpeed = 0.01f;
    public float gravity = 4f;
    public float initialWater = 1f;
    public float initialSpeed = 1f;
    public int brushRadius = 3;

    [Header("Spike / Smoothing Controls")]
    public float maxStepAmount = 0.02f;   // max height change per cell *per droplet step*
    public float minHeightFloor = 0f;
    public int slopeRelaxIterations = 0;
    public float talus = 0.015f;

    // NEW: hard slope clamp – after this runs, spikes cannot exist
    public float hardMaxNeighbourDiff = 0.02f; // max allowed diff vs neighbour avg
    public int hardClampIterations = 4;        // more iterations = smoother / safer

    int mapW = -1, mapH = -1;
    int[][] brushIndices;
    float[][] brushWeights;

    Coroutine running;

    public void StartErosion()
    {
        if (running != null) StopCoroutine(running);
        if (heightMap == null && meshGenerator != null) heightMap = meshGenerator.heightMap;
        if (heightMap == null)
        {
            Debug.LogWarning("ErosionCoroutine: heightMap is null.");
            return;
        }
        EnsureBrush(heightMap);
        running = StartCoroutine(Run());
    }

    public void StopErosion()
    {
        if (running != null) StopCoroutine(running);
        running = null;
    }

    IEnumerator Run()
    {
        var rng = new System.Random(seed);
        for (int i = 0; i < droplets; i++)
        {
            ApplyDroplet(heightMap, rng);

            if (meshGenerator != null && (i % meshUpdateInterval == 0))
            {
                meshGenerator.heightMap = heightMap;
                meshGenerator.CreateShape();
            }

            if (delaySeconds > 0f) yield return new WaitForSeconds(delaySeconds);
            else yield return null;
        }

        FinalizeMap(heightMap);

        if (meshGenerator != null)
        {
            meshGenerator.heightMap = heightMap;
            meshGenerator.CreateShape();
        }

        running = null;
    }

    public void TestDigCenterPit()
    {
        if (heightMap == null && meshGenerator != null)
            heightMap = meshGenerator.heightMap;

        if (heightMap == null)
        {
            Debug.LogWarning("TestDigCenterPit: heightMap is null.");
            return;
        }

        int sx = heightMap.GetLength(0);
        int sy = heightMap.GetLength(1);
        int cx = sx / 2;
        int cy = sy / 2;
        int radius = 5;

        for (int x = cx - radius; x <= cx + radius; x++)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                if (x >= 0 && x < sx && y >= 0 && y < sy)
                {
                    heightMap[x, y] = 0f;
                }
            }
        }

        if (meshGenerator != null)
        {
            meshGenerator.heightMap = heightMap;
            meshGenerator.CreateShape();
        }

        Debug.Log("TestDigCenterPit: crater applied and mesh updated.");
    }

    public void BakeErosionOnce()
    {
        if (heightMap == null && meshGenerator != null)
            heightMap = meshGenerator.heightMap;

        if (heightMap == null)
        {
            Debug.LogWarning("BakeErosionOnce: heightMap is null.");
            return;
        }

        EnsureBrush(heightMap);

        var rng = new System.Random(seed);

        for (int iter = 0; iter < bakeIterations; iter++)
        {
            for (int i = 0; i < droplets; i++)
            {
                ApplyDroplet(heightMap, rng);
            }
        }

        FinalizeMap(heightMap);

        if (meshGenerator != null)
        {
            meshGenerator.heightMap = heightMap;
            meshGenerator.CreateShape();
        }

        Debug.Log($"BakeErosionOnce: erosion baked with {bakeIterations} iterations and mesh updated.");
    }

    void EnsureBrush(float[,] map)
    {
        int w = map.GetLength(0), h = map.GetLength(1);
        if (w <= 1 || h <= 1) return;
        if (w != mapW || h != mapH || brushIndices == null) BuildBrush(w, h);
    }

    void ApplyDroplet(float[,] map, System.Random rng)
    {
        int sizeX = map.GetLength(0);
        int sizeY = map.GetLength(1);
        if (sizeX <= 1 || sizeY <= 1) return;

        float posX = (float)rng.NextDouble() * (sizeX - 1);
        float posY = (float)rng.NextDouble() * (sizeY - 1);
        float dirX = 0f, dirY = 0f;
        float speed = initialSpeed;
        float water = initialWater;
        float sediment = 0f;

        for (int life = 0; life < maxLifetime; life++)
        {
            int cx = (int)posX;
            int cy = (int)posY;
            float cellX = posX - cx;
            float cellY = posY - cy;

            float h00 = map[cx, cy];
            float h10 = map[Math.Min(cx + 1, sizeX - 1), cy];
            float h01 = map[cx, Math.Min(cy + 1, sizeY - 1)];
            float h11 = map[Math.Min(cx + 1, sizeX - 1), Math.Min(cy + 1, sizeY - 1)];
            float height = Bilinear(h00, h10, h01, h11, cellX, cellY);

            float gradX = ((h10 - h00) * (1f - cellY)) + ((h11 - h01) * cellY);
            float gradY = ((h01 - h00) * (1f - cellX)) + ((h11 - h10) * cellX);

            dirX = dirX * inertia - gradX * (1f - inertia);
            dirY = dirY * inertia - gradY * (1f - inertia);

            float len = Sqrt(dirX * dirX + dirY * dirY);
            if (len != 0f) { dirX /= len; dirY /= len; }

            posX += dirX;
            posY += dirY;

            if (posX < 0f || posX >= sizeX - 1 || posY < 0f || posY >= sizeY - 1) break;

            int nx = (int)posX;
            int ny = (int)posY;
            float ncellX = posX - nx;
            float ncellY = posY - ny;

            float nh00 = map[nx, ny];
            float nh10 = map[Math.Min(nx + 1, sizeX - 1), ny];
            float nh01 = map[nx, Math.Min(ny + 1, sizeY - 1)];
            float nh11 = map[Math.Min(nx + 1, sizeX - 1), Math.Min(ny + 1, sizeY - 1)];
            float newHeight = Bilinear(nh00, nh10, nh01, nh11, ncellX, ncellY);

            float deltaH = newHeight - height;
            float capacity = Max(-deltaH * speed * water * sedimentCapacityFactor, minSedimentCapacity);

            if (sediment > capacity || deltaH > 0f)
            {
                float toDeposit = (deltaH > 0f)
                    ? Min(deltaH, sediment)
                    : (sediment - capacity) * depositSpeed;

                if (toDeposit > 0f)
                {
                    // limit total deposit this step (spread across 4 cells)
                    float maxDeposit = maxStepAmount * 4f;
                    if (toDeposit > maxDeposit) toDeposit = maxDeposit;

                    Deposit(map, nx, ny, ncellX, ncellY, toDeposit);
                    sediment -= toDeposit;
                }
            }
            else
            {
                float toErode = Min((capacity - sediment) * erodeSpeed, -deltaH);
                if (toErode > 0f)
                    sediment += ErodeAt(map, nx, ny, toErode);
            }

            speed = Sqrt(Max(0f, speed * speed + deltaH * gravity));
            water *= (1f - evaporateSpeed);
            if (water <= 0.001f) break;

            height = newHeight;
        }
    }

    void FinalizeMap(float[,] map)
    {
        // Optional thermal smoothing
        if (slopeRelaxIterations > 0)
            ThermalRelax(map, slopeRelaxIterations, talus);

        // Light despike (handles small single-cell pops)
        Despike(map, spikeThreshold: 0.02f, iterations: 1);

        // HARD clamp: after this, no cell can differ from its neighbours' avg
        // by more than hardMaxNeighbourDiff
        if (hardClampIterations > 0 && hardMaxNeighbourDiff > 0f)
            HardClampSlopes(map, hardMaxNeighbourDiff, hardClampIterations);

        int sizeX = map.GetLength(0), sizeY = map.GetLength(1);
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                float h = map[x, y];

                if (h < minHeightFloor) h = minHeightFloor;

                map[x, y] = Clamp01(h);
            }
        }
    }

    void BuildBrush(int sizeX, int sizeY)
    {
        mapW = sizeX; mapH = sizeY;
        int radius = Math.Max(1, brushRadius);
        int diameter = radius * 2 + 1;

        brushIndices = new int[sizeX * sizeY][];
        brushWeights = new float[sizeX * sizeY][];

        (int ox, int oy, float w)[] templ = new (int, int, float)[diameter * diameter];
        int tCount = 0; float tSum = 0f;

        for (int oy = -radius; oy <= radius; oy++)
            for (int ox = -radius; ox <= radius; ox++)
            {
                float dist = Sqrt(ox * ox + oy * oy);
                if (dist <= radius + 1e-4f)
                {
                    float w = 1f - (dist / (radius + 1e-4f));
                    templ[tCount++] = (ox, oy, w);
                    tSum += w;
                }
            }
        for (int i = 0; i < tCount; i++) templ[i].w /= tSum;

        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
            {
                int idx = y * sizeX + x;
                int valid = 0;
                for (int i = 0; i < tCount; i++)
                {
                    int px = x + templ[i].ox, py = y + templ[i].oy;
                    if (px >= 0 && px < sizeX && py >= 0 && py < sizeY) valid++;
                }

                var indices = new int[valid];
                var weights = new float[valid];
                float sum = 0f; int wri = 0;

                for (int i = 0; i < tCount; i++)
                {
                    int px = x + templ[i].ox, py = y + templ[i].oy;
                    if (px < 0 || px >= sizeX || py < 0 || py >= sizeY) continue;
                    indices[wri] = py * sizeX + px;
                    float w = templ[i].w;
                    weights[wri] = w;
                    sum += w;
                    wri++;
                }
                for (int i = 0; i < weights.Length; i++) weights[i] /= sum;
                brushIndices[idx] = indices;
                brushWeights[idx] = weights;
            }
    }

    float ErodeAt(float[,] map, int x, int y, float amount)
    {
        int sizeX = map.GetLength(0);
        int brushIndex = y * sizeX + x;

        var indices = brushIndices[brushIndex];
        var weights = brushWeights[brushIndex];

        float removed = 0f;
        float maxPerCell = maxStepAmount;

        for (int i = 0; i < indices.Length; i++)
        {
            int flat = indices[i];
            int px = flat % sizeX;
            int py = flat / sizeX;

            float h = map[px, py];
            float weightedAmount = amount * weights[i];

            float delta = weightedAmount;
            if (delta > maxPerCell) delta = maxPerCell;
            if (delta > h) delta = h;
            if (delta <= 0f) continue;

            map[px, py] = h - delta;
            removed += delta;
        }

        return removed;
    }

    void Deposit(float[,] map, int x, int y, float fx, float fy, float amount)
    {
        int sizeX = map.GetLength(0);
        int sizeY = map.GetLength(1);

        float posX = x + fx;
        float posY = y + fy;

        int cx = (int)posX;
        int cy = (int)posY;

        if (cx < 0 || cx >= sizeX - 1 || cy < 0 || cy >= sizeY - 1)
            return;

        float cellX = posX - cx;
        float cellY = posY - cy;

        float w00 = (1f - cellX) * (1f - cellY);
        float w10 = cellX * (1f - cellY);
        float w01 = (1f - cellX) * cellY;
        float w11 = cellX * cellY;

        float maxPerCell = maxStepAmount;

        void AddClamped(int px, int py, float w)
        {
            float d = amount * w;
            if (d > maxPerCell) d = maxPerCell;
            if (d < -maxPerCell) d = -maxPerCell;
            map[px, py] += d;
        }

        AddClamped(cx, cy, w00);
        AddClamped(cx + 1, cy, w10);
        AddClamped(cx, cy + 1, w01);
        AddClamped(cx + 1, cy + 1, w11);
    }

    void ThermalRelax(float[,] map, int iterations, float talusVal)
    {
        int w = map.GetLength(0), h = map.GetLength(1);
        for (int it = 0; it < iterations; it++)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (x + 1 < w) RelaxPair(map, x, y, x + 1, y, talusVal);
                    if (y + 1 < h) RelaxPair(map, x, y, x, y + 1, talusVal);
                }
    }

    void RelaxPair(float[,] m, int x0, int y0, int x1, int y1, float talusVal)
    {
        float h0 = m[x0, y0];
        float h1 = m[x1, y1];
        float diff = h0 - h1;
        if (diff > talusVal)
        {
            float move = 0.25f * (diff - talusVal);
            move = Min(move, maxStepAmount * 0.25f);
            m[x0, y0] = h0 - move;
            m[x1, y1] = h1 + move;
        }
        else if (-diff > talusVal)
        {
            float move = 0.25f * (-diff - talusVal);
            move = Min(move, maxStepAmount * 0.25f);
            m[x0, y0] = h0 + move;
            m[x1, y1] = h1 - move;
        }
    }

    void Despike(float[,] map, float spikeThreshold, int iterations)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        for (int it = 0; it < iterations; it++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    float hC = map[x, y];

                    float hL = map[x - 1, y];
                    float hR = map[x + 1, y];
                    float hD = map[x, y - 1];
                    float hU = map[x, y + 1];

                    float avg = (hL + hR + hD + hU) * 0.25f;
                    float diff = hC - avg;

                    if (diff > spikeThreshold)
                    {
                        map[x, y] = avg + spikeThreshold;
                    }
                    else if (diff < -spikeThreshold)
                    {
                        map[x, y] = avg - spikeThreshold;
                    }
                }
            }
        }
    }

    // NEW: hard slope clamp using 8-neighbour average
    void HardClampSlopes(float[,] map, float maxDiff, int iterations)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        for (int it = 0; it < iterations; it++)
        {
            float[,] src = (float[,])map.Clone();

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    float hC = src[x, y];

                    float sum = 0f;
                    int count = 0;

                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            sum += src[x + ox, y + oy];
                            count++;
                        }
                    }

                    float avg = sum / count;
                    float diff = hC - avg;

                    if (diff > maxDiff)
                        map[x, y] = avg + maxDiff;
                    else if (diff < -maxDiff)
                        map[x, y] = avg - maxDiff;
                    else
                        map[x, y] = hC;
                }
            }
        }
    }

    float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    float Lerp(float a, float b, float t) => a + (b - a) * t;
    float Max(float a, float b) => a > b ? a : b;
    float Min(float a, float b) => a < b ? a : b;
    float Sqrt(float v) => (float)Math.Sqrt(v);

    float Bilinear(float h00, float h10, float h01, float h11, float tx, float ty)
    {
        float a = Lerp(h00, h10, tx);
        float b = Lerp(h01, h11, tx);
        return Lerp(a, b, ty);
    }
}
