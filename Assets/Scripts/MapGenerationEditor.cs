using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    System.Random prng = new System.Random(0);
    public override void OnInspectorGUI()
    {
        //Creates editor of mapgenerator
        MapGenerator mapGen = (MapGenerator)target;

        //Tells mapgenerator if it should auto update
        if (DrawDefaultInspector())
        {
            if (mapGen.autoUpdate)
            {
                mapGen.GenerateMap(100001);
            }
        }

        //Tells mapgenerator to create current map
        if (GUILayout.Button("Generate"))
        {
            mapGen.GenerateMap(100001);
        }

        //Tells mapgenerator to generate random map
        if (GUILayout.Button("Random seed generate"))
        {
            mapGen.GenerateMap(prng.Next(-100000, 100000));
        }
    }
}