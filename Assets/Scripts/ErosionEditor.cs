using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Erosion))]
public class ErosionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Erosion erosion = (Erosion)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Erosion Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Bake Erosion"))
        {
            erosion.ErosionBake();
        }
    }
}