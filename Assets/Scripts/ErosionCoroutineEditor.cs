using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ErosionCoroutine))]
public class ErosionCoroutineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ErosionCoroutine erosion = (ErosionCoroutine)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Erosion Controls", EditorStyles.boldLabel);

        // Works both in edit and play mode: instant modifications
        if (GUILayout.Button("Test Dig Center Pit"))
        {
            erosion.TestDigCenterPit();
        }

        if (GUILayout.Button("Bake Erosion Once (No Coroutine)"))
        {
            erosion.BakeErosionOnce();
        }

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Coroutine Controls (Play Mode)", EditorStyles.miniBoldLabel);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button("Start Erosion (Coroutine)"))
        {
            erosion.StartErosion();
        }

        if (GUILayout.Button("Stop Erosion (Coroutine)"))
        {
            erosion.StopErosion();
        }

        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Start/Stop Erosion (Coroutine) only works in Play Mode.\n" +
                "Test Dig Center Pit and Bake Erosion Once can be used in Edit Mode too.",
                MessageType.Info
            );
        }
    }
}