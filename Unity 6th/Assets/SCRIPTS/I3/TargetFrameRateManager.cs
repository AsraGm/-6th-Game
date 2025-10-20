using UnityEngine;
using System.Collections.Generic;

public class TargetFrameRateManager : MonoBehaviour
{
    [System.Serializable]
    public class DeviceProfile
    {
        public string deviceName;
        public int targetFPS;
        public int targetFrameMs;
        public bool isLowEnd;
    }

    public static TargetFrameRateManager Instance { get; private set; }

    [SerializeField] private int lowEndTargetFPS = 30;
    [SerializeField] private int moderateEndTargetFPS = 60;

    private int currentTargetFPS;
    private DeviceProfile currentProfile;
    private float thermalHeadroom = 0.65f; // 35% idle time para thermal throttling

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFrameRateTarget();
    }

    private void InitializeFrameRateTarget()
    {
        // Detectar capacidades del dispositivo
        bool isLowEnd = DetectLowEndDevice();
        currentTargetFPS = isLowEnd ? lowEndTargetFPS : moderateEndTargetFPS;

        // Aplicar target frame rate
        Application.targetFrameRate = currentTargetFPS;

        // Calcular presupuesto real (con thermal headroom)
        int targetFrameMs = Mathf.RoundToInt(1000f / currentTargetFPS);
        int realBudgetMs = Mathf.RoundToInt(targetFrameMs * thermalHeadroom);

        currentProfile = new DeviceProfile
        {
            deviceName = SystemInfo.deviceModel,
            targetFPS = currentTargetFPS,
            targetFrameMs = realBudgetMs,
            isLowEnd = isLowEnd
        };

        Debug.Log($"[Performance] Device: {currentProfile.deviceName} | Target: {currentTargetFPS}FPS | Budget: {currentProfile.targetFrameMs}ms");
    }

    private bool DetectLowEndDevice()
    {
        // Detectar dispositivos de gama baja por RAM y procesador
        int systemMemoryMB = SystemInfo.systemMemorySize;
        int processorCount = SystemInfo.processorCount;

        // Dispositivos con menos de 3GB RAM o 2 cores se consideran low-end
        return systemMemoryMB < 3000 || processorCount <= 2;
    }

    public int GetTargetFPS() => currentTargetFPS;
    public int GetFrameBudgetMs() => currentProfile.targetFrameMs;
    public bool IsLowEndDevice() => currentProfile.isLowEnd;
    public string GetDeviceInfo() => currentProfile.deviceName;
}