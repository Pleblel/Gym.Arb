using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class CustomProfiler : MonoBehaviour
{
    float deltaTime;
    GUIStyle style;

    void Start()
    {
        style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        float fps = 1.0f / deltaTime;   

        string text =
            $"FPS: {fps:0.}\n" +
            $"Draw Calls: {UnityStats.drawCalls}\n" +
            $"Batches: {UnityStats.batches}\n" +
            $"Triangles: {UnityStats.triangles}\n" +
            $"Vertices: {UnityStats.vertices}\n" +
            $"Used Memory: {Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MB\n" +
            $"Reserved Memory: {Profiler.GetTotalReservedMemoryLong() / (1024 * 1024)} MB\n" +
            $"Mono Heap: {Profiler.GetMonoUsedSizeLong() / (1024 * 1024)} MB\n" +
            $"CPU: {SystemInfo.processorType}\n" +
            $"GPU: {SystemInfo.graphicsDeviceName}\n" +
            $"VRAM: {SystemInfo.graphicsMemorySize} MB";

        GUI.Label(new Rect(10, 10, 500, 500), text, style);
    }
}