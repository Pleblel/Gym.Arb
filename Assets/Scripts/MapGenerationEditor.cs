using static UnityEngine.GraphicsBuffer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    static System.Random prng = new System.Random();

    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        if (DrawDefaultInspector())
        {
            if (mapGen.autoUpdate)
            {
                mapGen.GenerateMap(100001);
            }
        }

        if (GUILayout.Button("Generate"))
        {
            mapGen.GenerateMap(100001);
        }

        if (GUILayout.Button("Random seed generate"))
        {
            int randomSeed = prng.Next(0, 100000);
            mapGen.seed = randomSeed;
            mapGen.GenerateMap(randomSeed);
        }
    }
}
