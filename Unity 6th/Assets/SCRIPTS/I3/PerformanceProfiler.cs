using UnityEngine;
using System.Collections.Generic;

public class PerformanceProfiler : MonoBehaviour
{
    public static PerformanceProfiler Instance { get; private set; }

    private class ProfilePoint
    {
        public string name;
        public float startTime;
        public float duration;
        public float maxDuration;
    }

    [SerializeField] private bool enableProfiling = true;
    [SerializeField] private int sampleFrames = 60;
    [SerializeField] private bool showDebugUI = true;

    private Dictionary<string, ProfilePoint> profilePoints = new();
    private float frameStartTime;
    private float currentFrameTime;
    private Queue<float> frameTimeHistory = new();
    private float averageFrameTime;
    private float maxFrameTime;
    private int frameBudgetMs;
    private bool isFrameOverBudget;

    private GUIStyle debugStyle;
    private Rect debugRect;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        frameBudgetMs = TargetFrameRateManager.Instance.GetFrameBudgetMs();
    }

    private void OnEnable()
    {
        debugStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };
        debugRect = new Rect(10, 10, 350, 300);
    }

    private void Update()
    {
        if (!enableProfiling) return;

        frameStartTime = Time.realtimeSinceStartup;
    }

    private void LateUpdate()
    {
        if (!enableProfiling) return;

        // Calcular tiempo del frame
        currentFrameTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f; // en ms

        // Mantener historial
        frameTimeHistory.Enqueue(currentFrameTime);
        if (frameTimeHistory.Count > sampleFrames)
            frameTimeHistory.Dequeue();

        // Calcular promedios
        CalculateFrameStats();

        // Detectar si frame excede budget
        isFrameOverBudget = currentFrameTime > frameBudgetMs;
    }

    private void CalculateFrameStats()
    {
        float sum = 0;
        maxFrameTime = 0;

        foreach (float time in frameTimeHistory)
        {
            sum += time;
            if (time > maxFrameTime)
                maxFrameTime = time;
        }

        averageFrameTime = sum / frameTimeHistory.Count;
    }

    /// <summary>
    /// Marcar inicio de un profile point
    /// </summary>
    public void BeginProfilePoint(string name)
    {
        if (!enableProfiling) return;

        if (!profilePoints.ContainsKey(name))
            profilePoints[name] = new ProfilePoint { name = name, maxDuration = 0 };

        profilePoints[name].startTime = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// Marcar final de un profile point
    /// </summary>
    public void EndProfilePoint(string name)
    {
        if (!enableProfiling || !profilePoints.ContainsKey(name)) return;

        float duration = (Time.realtimeSinceStartup - profilePoints[name].startTime) * 1000f;
        profilePoints[name].duration = duration;

        if (duration > profilePoints[name].maxDuration)
            profilePoints[name].maxDuration = duration;
    }

    public float GetCurrentFrameTime() => currentFrameTime;
    public float GetAverageFrameTime() => averageFrameTime;
    public float GetMaxFrameTime() => maxFrameTime;
    public int GetFrameBudgetMs() => frameBudgetMs;
    public bool IsFrameOverBudget() => isFrameOverBudget;
    public float GetCurrentFPS() => frameTimeHistory.Count > 0 ? 1000f / averageFrameTime : 0;

    private void OnGUI()
    {
        if (!showDebugUI || !enableProfiling) return;

        try
        {
            GUI.Box(debugRect, "Performance Monitor");

            GUILayout.BeginArea(new Rect(debugRect.x + 5, debugRect.y + 20, debugRect.width - 10, debugRect.height - 25));

            DrawFrameStats();
            GUILayout.Space(10);
            DrawProfilePoints();

            GUILayout.EndArea();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"GUI Error (ignorado): {ex.Message}");
        }
    }

    private void DrawFrameStats()
    {
        string frameColor = isFrameOverBudget ? "red" : "lime";
        string budgetStatus = isFrameOverBudget ? "⚠ OVER BUDGET" : "✓ OK";

        GUILayout.Label($"<color=yellow>FPS: {GetCurrentFPS():F1}</color>", debugStyle);
        GUILayout.Label($"Frame Time: {currentFrameTime:F2}ms (Avg: {averageFrameTime:F2}ms)", debugStyle);
        GUILayout.Label($"Budget: {frameBudgetMs}ms | <color={frameColor}>{budgetStatus}</color>", debugStyle);
        GUILayout.Label($"Max Frame: {maxFrameTime:F2}ms", debugStyle);

        string device = TargetFrameRateManager.Instance.IsLowEndDevice() ? "Low-End" : "Modern";
        GUILayout.Label($"Device: {device} | Target: {TargetFrameRateManager.Instance.GetTargetFPS()}FPS", debugStyle);
    }

    private void DrawProfilePoints()
    {
        if (profilePoints.Count == 0) return;

        GUILayout.Label("<color=lime>Profile Points:</color>", debugStyle);

        foreach (var pp in profilePoints.Values)
        {
            string color = pp.duration > frameBudgetMs * 0.5f ? "yellow" : "white";
            GUILayout.Label($"  {pp.name}: <color={color}>{pp.duration:F2}ms</color> (Max: {pp.maxDuration:F2}ms)", debugStyle);
        }
    }

    public void ResetStats()
    {
        frameTimeHistory.Clear();
        profilePoints.Clear();
        averageFrameTime = 0;
        maxFrameTime = 0;
    }
}
public class PerformanceTestExample : MonoBehaviour
{
    private void Update()
    {
        // Perfilar un sistema específico
        PerformanceProfiler.Instance.BeginProfilePoint("EnemyUpdate");
        // ... código de enemigos ...
        PerformanceProfiler.Instance.EndProfilePoint("EnemyUpdate");

        PerformanceProfiler.Instance.BeginProfilePoint("Rendering");
        // ... código de rendering ...
        PerformanceProfiler.Instance.EndProfilePoint("Rendering");
    }
}