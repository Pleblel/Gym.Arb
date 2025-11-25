using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(DomainWarping))]
public class DomainWarpingEditor : Editor
{
    // optional: deterministic random for seeds
    static System.Random prng = new System.Random();

    public override void OnInspectorGUI()
    {
        DomainWarping gen = (DomainWarping)target;

        // Draw all fields. If something changed in the inspector, this returns true.
        if (DrawDefaultInspector())
        {
            // Auto update when values change
            if (gen.autoUpdate)
            {
                DoGenerate(gen);
            }
        }

        GUILayout.Space(10);

        // Normal Generate button
        if (GUILayout.Button("Generate"))
        {
            DoGenerate(gen);
        }

        // Random seed + Generate button
        if (GUILayout.Button("Random Seed Generate"))
        {
            Undo.RecordObject(gen, "Randomize DomainWarping Seed");

            // pick a random seed
            gen.seed = prng.Next(0, 1000000);

            DoGenerate(gen);
        }
    }

    private void DoGenerate(DomainWarping gen)
    {
        gen.GenerateAndApply();

        EditorUtility.SetDirty(gen);
        if (gen.meshGenerator != null)
            EditorUtility.SetDirty(gen.meshGenerator);

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gen.gameObject.scene);
        }

        SceneView.RepaintAll();
    }
}
