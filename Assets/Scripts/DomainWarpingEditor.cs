using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DomainWarping))]
public class BetterMountainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the normal inspector (all the fields)
        DrawDefaultInspector();

        // Reference to the target component
        DomainWarping gen = (DomainWarping)target;

        GUILayout.Space(10);

        // Generate button
        if (GUILayout.Button("Generate"))
        {
            // If you only want this to work in play mode, you can guard with:
            // if (Application.isPlaying)
            gen.GenerateAndApply();

            // Mark scene dirty so changes are saved
            EditorUtility.SetDirty(gen);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    gen.gameObject.scene
                );
            }

            // Repaint scene view so you see changes immediately
            SceneView.RepaintAll();
        }
    }
}
