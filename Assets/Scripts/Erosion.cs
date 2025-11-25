using UnityEngine;

public class Erosion : MonoBehaviour
{
    [Header("References")]
    public MeshGenerator meshGenerator;
    public MapGenerator mapGenerator;
    public DomainWarping domainWarping;   // last generator (domain warp)

    [HideInInspector] public float[,] heightMap;
    [Tooltip("How many droplets per bake")]
    public int numIterations = 50000;

    [Header("Erosion Settings")]
    public int seed;
    [Range(2, 8)] public int erosionRadius = 3;
    [Range(0, 1)] public float inertia = .05f;
    public float sedimentCapacityFactor = 4;
    public float minSedimentCapacity = .01f;
    [Range(0, 1)] public float erodeSpeed = .3f;
    [Range(0, 1)] public float depositSpeed = .3f;
    [Range(0, 1)] public float evaporateSpeed = .01f;
    public float gravity = 4;
    public int maxDropletLifetime = 30;
    public float initialWaterVolume = 1;
    public float initialSpeed = 1;

    int[][] erosionBrushIndices;
    float[][] erosionBrushWeights;
    System.Random prng;

    int currentSeed;
    int currentErosionRadius;
    int currentMapSize;

    void OnValidate()
    {
        // convenience: auto-hook references on same GameObject if missing
        if (meshGenerator == null)
            meshGenerator = GetComponent<MeshGenerator>();
        if (domainWarping == null)
            domainWarping = GetComponent<DomainWarping>();
        if (mapGenerator == null)
            mapGenerator = GetComponent<MapGenerator>();
    }

    public void ErosionBake()
    {
        // ONLY take from MeshGenerator
        if (meshGenerator == null || meshGenerator.heightMap == null)
        {
            Debug.LogWarning("Erosion: MeshGenerator or its heightMap is null, cannot bake.");
            return;
        }

        // Always sync from current mesh heightmap
        heightMap = meshGenerator.heightMap;

        int mapSize = heightMap.GetLength(0);
        if (mapSize != heightMap.GetLength(1))
        {
            Debug.LogWarning("Erosion: heightMap must be square.");
            return;
        }

        // Convert 2D map to 1D
        float[] map1D = new float[mapSize * mapSize];
        for (int y = 0; y < mapSize; y++)
            for (int x = 0; x < mapSize; x++)
                map1D[y * mapSize + x] = heightMap[x, y];

        // Run erosion
        Erode(map1D, mapSize, numIterations, true);

        // Convert back to 2D
        for (int y = 0; y < mapSize; y++)
            for (int x = 0; x < mapSize; x++)
                heightMap[x, y] = map1D[y * mapSize + x];

        // Push result back into other systems (optional but useful)
        if (domainWarping != null)
        {
            domainWarping.latestHeightMap = heightMap;
        }

        meshGenerator.heightMap = heightMap;
        meshGenerator.CreateShape();

        if (mapGenerator != null)
        {
            mapGenerator.currentHeightMap = heightMap;
            mapGenerator.RegenerateTexturesFromHeightMap(heightMap);
        }
    }


    void Initialize(int mapSize, bool resetSeed)
    {
        if (resetSeed || prng == null || currentSeed != seed)
        {
            prng = new System.Random(seed);
            currentSeed = seed;
        }

        if (erosionBrushIndices == null || currentErosionRadius != erosionRadius || currentMapSize != mapSize)
        {
            InitializeBrushIndices(mapSize, erosionRadius);
            currentErosionRadius = erosionRadius;
            currentMapSize = mapSize;
        }
    }

    public void Erode(float[] map, int mapSize, int numIterations = 1, bool resetSeed = false)
    {
        Initialize(mapSize, resetSeed);

        for (int iteration = 0; iteration < numIterations; iteration++)
        {
            float posX = prng.Next(0, mapSize - 1);
            float posY = prng.Next(0, mapSize - 1);
            float dirX = 0;
            float dirY = 0;
            float speed = initialSpeed;
            float water = initialWaterVolume;
            float sediment = 0;

            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {
                int nodeX = (int)posX;
                int nodeY = (int)posY;
                int dropletIndex = nodeY * mapSize + nodeX;
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                HeightAndGradient heightAndGradient = CalculateHeightAndGradient(map, mapSize, posX, posY);

                dirX = (dirX * inertia - heightAndGradient.gradientX * (1 - inertia));
                dirY = (dirY * inertia - heightAndGradient.gradientY * (1 - inertia));
                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len != 0)
                {
                    dirX /= len;
                    dirY /= len;
                }
                posX += dirX;
                posY += dirY;

                if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= mapSize - 1 || posY < 0 || posY >= mapSize - 1)
                {
                    break;
                }

                float newHeight = CalculateHeightAndGradient(map, mapSize, posX, posY).height;
                float deltaHeight = newHeight - heightAndGradient.height;

                float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

                if (sediment > sedimentCapacity || deltaHeight > 0)
                {
                    float amountToDeposit = (deltaHeight > 0) ?
                        Mathf.Min(deltaHeight, sediment) :
                        (sediment - sedimentCapacity) * depositSpeed;
                    sediment -= amountToDeposit;

                    map[dropletIndex] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    map[dropletIndex + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    map[dropletIndex + mapSize] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    map[dropletIndex + mapSize + 1] += amountToDeposit * cellOffsetX * cellOffsetY;
                }
                else
                {
                    float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);

                    for (int brushPointIndex = 0; brushPointIndex < erosionBrushIndices[dropletIndex].Length; brushPointIndex++)
                    {
                        int nodeIndex = erosionBrushIndices[dropletIndex][brushPointIndex];
                        float weighedErodeAmount = amountToErode * erosionBrushWeights[dropletIndex][brushPointIndex];
                        float deltaSediment = (map[nodeIndex] < weighedErodeAmount) ? map[nodeIndex] : weighedErodeAmount;
                        map[nodeIndex] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }

                speed = Mathf.Sqrt(speed * speed + deltaHeight * gravity);
                water *= (1 - evaporateSpeed);
            }
        }
    }

    HeightAndGradient CalculateHeightAndGradient(float[] nodes, int mapSize, float posX, float posY)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        float x = posX - coordX;
        float y = posY - coordY;

        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = nodes[nodeIndexNW];
        float heightNE = nodes[nodeIndexNW + 1];
        float heightSW = nodes[nodeIndexNW + mapSize];
        float heightSE = nodes[nodeIndexNW + mapSize + 1];

        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        float height = heightNW * (1 - x) * (1 - y) +
                       heightNE * x * (1 - y) +
                       heightSW * (1 - x) * y +
                       heightSE * x * y;

        return new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
    }

    void InitializeBrushIndices(int mapSize, int radius)
    {
        erosionBrushIndices = new int[mapSize * mapSize][];
        erosionBrushWeights = new float[mapSize * mapSize][];

        int[] xOffsets = new int[radius * radius * 4];
        int[] yOffsets = new int[radius * radius * 4];
        float[] weights = new float[radius * radius * 4];
        float weightSum = 0;
        int addIndex = 0;

        for (int i = 0; i < erosionBrushIndices.GetLength(0); i++)
        {
            int centreX = i % mapSize;
            int centreY = i / mapSize;

            weightSum = 0;
            addIndex = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    float sqrDst = x * x + y * y;
                    if (sqrDst < radius * radius)
                    {
                        int coordX = centreX + x;
                        int coordY = centreY + y;

                        if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize)
                        {
                            float weight = 1 - Mathf.Sqrt(sqrDst) / radius;
                            weightSum += weight;
                            weights[addIndex] = weight;
                            xOffsets[addIndex] = x;
                            yOffsets[addIndex] = y;
                            addIndex++;
                        }
                    }
                }
            }

            int numEntries = addIndex;
            erosionBrushIndices[i] = new int[numEntries];
            erosionBrushWeights[i] = new float[numEntries];

            for (int j = 0; j < numEntries; j++)
            {
                erosionBrushIndices[i][j] =
                    (yOffsets[j] + centreY) * mapSize +
                    xOffsets[j] + centreX;
                erosionBrushWeights[i][j] = weights[j] / weightSum;
            }
        }
    }

    struct HeightAndGradient
    {
        public float height;
        public float gradientX;
        public float gradientY;
    }
}
