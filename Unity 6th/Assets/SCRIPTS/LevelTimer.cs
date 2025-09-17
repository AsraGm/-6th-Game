using UnityEngine;
using System.Collections;

// A4: Sistema de temporización de niveles simplificado
public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }
    
    [Header("Timer Settings")]
    [SerializeField] private float levelDuration = 60f; // Duración del nivel en segundos
    [SerializeField] private float timerPrecision = 0.1f; // Precisión de 0.1 segundos
    
    [Header("UI References")]
    [SerializeField] private TMPro.TextMeshProUGUI timerText;
    
    // Events
    public System.Action OnTimerFinished;
    public System.Action<float> OnTimerUpdated;
    
    private float currentTime;
    private bool isTimerActive = false;
    private Coroutine timerCoroutine;
    
    public float CurrentTime => currentTime;
    public float TimeRemaining => currentTime;
    public bool IsActive => isTimerActive;
    public float Duration => levelDuration;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartTimer(float duration = -1f)
    {
        // Si se especifica una duración, usarla. Si no, usar la por defecto
        if (duration > 0)
            levelDuration = duration;
            
        currentTime = levelDuration;
        isTimerActive = true;
        
        // Detener timer anterior si existe
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        // Iniciar nuevo timer usando Coroutine optimizada
        timerCoroutine = StartCoroutine(TimerCountdown());
        
        // Actualizar UI inicial
        UpdateTimerUI();
    }
    
    public void StopTimer()
    {
        isTimerActive = false;
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }
    
    public void PauseTimer()
    {
        isTimerActive = false;
    }
    
    public void ResumeTimer()
    {
        if (currentTime > 0)
        {
            isTimerActive = true;
            timerCoroutine = StartCoroutine(TimerCountdown());
        }
    }
    
    private IEnumerator TimerCountdown()
    {
        WaitForSeconds wait = new WaitForSeconds(timerPrecision);
        
        while (currentTime > 0 && isTimerActive)
        {
            yield return wait;
            
            currentTime -= timerPrecision;
            
            // Asegurar que no sea negativo
            if (currentTime < 0)
                currentTime = 0;
            
            UpdateTimerUI();
            OnTimerUpdated?.Invoke(currentTime);
        }
        
        // Timer terminado
        if (currentTime <= 0)
        {
            isTimerActive = false;
            OnTimerFinished?.Invoke();
        }
    }
    
    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        
        // Formatear tiempo como MM:SS
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        
        // Cambiar color basado en tiempo restante
        if (currentTime <= 10f)
        {
            timerText.color = Color.red; // Últimos 10 segundos
        }
        else if (currentTime <= 30f)
        {
            timerText.color = Color.yellow; // Últimos 30 segundos
        }
        else
        {
            timerText.color = Color.white; // Color normal
        }
    }
    
    public void AddTime(float timeToAdd)
    {
        currentTime += timeToAdd;
        OnTimerUpdated?.Invoke(currentTime);
    }
    
    public void SetTime(float newTime)
    {
        currentTime = newTime;
        OnTimerUpdated?.Invoke(currentTime);
    }
    
    public float GetTimePercentage()
    {
        return currentTime / levelDuration;
    }
    
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    private void OnDestroy()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
    }
}