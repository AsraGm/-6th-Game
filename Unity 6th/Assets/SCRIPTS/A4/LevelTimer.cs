using UnityEngine;
using System.Collections;

// ARCHIVO: LevelTimer.cs
// Sistema de temporización de niveles simplificado (A4)

namespace ShootingRange
{
    public class LevelTimer : MonoBehaviour
    {
        [Header("Configuración del Timer")]
        [Tooltip("Duración del nivel en segundos")]
        [Range(30f, 600f)]
        public float levelDuration = 120f; // 2 minutos por defecto
        
        [Tooltip("Precisión del timer (0.1 = décimas de segundo)")]
        [Range(0.01f, 1f)]
        public float timerPrecision = 0.1f;
        
        [Header("Referencias UI")]
        [Tooltip("ARRASTRA AQUÍ tu TimerDisplay para mostrar el countdown")]
        public TimerDisplay timerDisplay;
        
        [Header("Estado del Timer")]
        [Tooltip("Tiempo restante actual (se actualiza automáticamente)")]
        [SerializeField] private float currentTime = 0f;
        
        [Tooltip("¿Está el timer corriendo actualmente?")]
        [SerializeField] private bool isRunning = false;
        
        [Tooltip("¿Se acabó el tiempo?")]
        [SerializeField] private bool isTimeUp = false;
        
        // Eventos para notificar cambios
        public event System.Action<float> OnTimeChanged;
        public event System.Action OnTimeUp;
        public event System.Action OnTimerStarted;
        public event System.Action OnTimerPaused;
        public event System.Action OnTimerResumed;
        
        // Eventos para warnings de tiempo (CONEXIÓN CON D4)
        public event System.Action OnWarning30Seconds; // Amarillo
        public event System.Action OnWarning10Seconds; // Rojo
        
        // Variables de control
        private Coroutine timerCoroutine;
        private bool warning30Triggered = false;
        private bool warning10Triggered = false;
        
        // Propiedades públicas
        public float CurrentTime => currentTime;
        public float LevelDuration => levelDuration;
        public bool IsRunning => isRunning;
        public bool IsTimeUp => isTimeUp;
        public float TimeProgress => 1f - (currentTime / levelDuration); // 0-1 para progress bars
        
        void Start()
        {
            InitializeTimer();
        }
        
        void InitializeTimer()
        {
            // Configurar tiempo inicial
            currentTime = levelDuration;
            isTimeUp = false;
            isRunning = false;
            
            // Buscar UI si no está asignada
            if (timerDisplay == null)
            {
                timerDisplay = FindObjectOfType<TimerDisplay>();
            }
            
            // Configurar UI inicial
            if (timerDisplay != null)
            {
                timerDisplay.SetTime(currentTime, false); // Sin animación inicial
            }
            
            // Notificar estado inicial
            OnTimeChanged?.Invoke(currentTime);
            
            Debug.Log($"LevelTimer inicializado - Duración: {levelDuration}s");
        }
        
        // MÉTODOS PRINCIPALES DE CONTROL
        
        public void StartTimer()
        {
            if (!isRunning && !isTimeUp)
            {
                isRunning = true;
                warning30Triggered = false;
                warning10Triggered = false;
                
                // Usar Corrutina para mejor rendimiento móvil
                if (timerCoroutine != null)
                {
                    StopCoroutine(timerCoroutine);
                }
                timerCoroutine = StartCoroutine(TimerCoroutine());
                
                OnTimerStarted?.Invoke();
                Debug.Log("Timer iniciado");
            }
        }
        
        public void PauseTimer()
        {
            if (isRunning)
            {
                isRunning = false;
                
                if (timerCoroutine != null)
                {
                    StopCoroutine(timerCoroutine);
                    timerCoroutine = null;
                }
                
                OnTimerPaused?.Invoke();
                Debug.Log("Timer pausado");
            }
        }
        
        public void ResumeTimer()
        {
            if (!isRunning && !isTimeUp && currentTime > 0)
            {
                isRunning = true;
                
                if (timerCoroutine != null)
                {
                    StopCoroutine(timerCoroutine);
                }
                timerCoroutine = StartCoroutine(TimerCoroutine());
                
                OnTimerResumed?.Invoke();
                Debug.Log("Timer resumido");
            }
        }
        
        public void ResetTimer()
        {
            // Parar timer actual
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
            
            // Reset variables
            currentTime = levelDuration;
            isRunning = false;
            isTimeUp = false;
            warning30Triggered = false;
            warning10Triggered = false;
            
            // Actualizar UI
            if (timerDisplay != null)
            {
                timerDisplay.SetTime(currentTime, false);
                timerDisplay.ResetWarnings();
            }
            
            OnTimeChanged?.Invoke(currentTime);
            Debug.Log("Timer reseteado");
        }
        
        public void SetDuration(float newDuration)
        {
            levelDuration = newDuration;
            ResetTimer();
        }
        
        // CORRUTINA PRINCIPAL DEL TIMER (Optimización móvil)
        IEnumerator TimerCoroutine()
        {
            WaitForSeconds waitTime = new WaitForSeconds(timerPrecision);
            
            while (isRunning && currentTime > 0)
            {
                yield return waitTime;
                
                // Decrementar tiempo
                currentTime -= timerPrecision;
                currentTime = Mathf.Max(0f, currentTime); // No bajar de 0
                
                // Actualizar UI
                if (timerDisplay != null)
                {
                    timerDisplay.SetTime(currentTime, true);
                }
                
                // Notificar cambio
                OnTimeChanged?.Invoke(currentTime);
                
                // Verificar warnings (CONEXIÓN CON D4)
                CheckTimeWarnings();
                
                // Verificar si se acabó el tiempo
                if (currentTime <= 0)
                {
                    TimeUp();
                    break;
                }
            }
        }
        
        // Verificar warnings de tiempo para UI
        void CheckTimeWarnings()
        {
            // Warning a los 30 segundos
            if (!warning30Triggered && currentTime <= 30f && currentTime > 10f)
            {
                warning30Triggered = true;
                OnWarning30Seconds?.Invoke();
                
                if (timerDisplay != null)
                {
                    timerDisplay.SetWarningState(TimerWarningState.Warning30);
                }
                
                Debug.Log("Warning: 30 segundos restantes");
            }
            
            // Warning a los 10 segundos
            if (!warning10Triggered && currentTime <= 10f)
            {
                warning10Triggered = true;
                OnWarning10Seconds?.Invoke();
                
                if (timerDisplay != null)
                {
                    timerDisplay.SetWarningState(TimerWarningState.Warning10);
                }
                
                Debug.Log("Warning: 10 segundos restantes");
            }
        }
        
        // Manejar fin del tiempo
        void TimeUp()
        {
            isRunning = false;
            isTimeUp = true;
            currentTime = 0f;
            
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
            
            // Actualizar UI final
            if (timerDisplay != null)
            {
                timerDisplay.SetTime(0f, true);
                timerDisplay.SetWarningState(TimerWarningState.TimeUp);
            }
            
            // Notificar que se acabó el tiempo
            OnTimeUp?.Invoke();
            
            Debug.Log("¡Tiempo agotado!");
            
            // CONEXIÓN CON WAVE SYSTEM (Lista B3)
            // Detener spawn de enemigos
            //WaveSystem waveSystem = FindObjectOfType<WaveSystem>();
            //if (waveSystem != null)
            //{
            //    waveSystem.StopWaveSystem();
            //}
            
            // CONEXIÓN CON GAME MANAGER (futuro)
            // Activar pantalla de resultados después de un delay
            StartCoroutine(ShowResultsAfterDelay());
        }
        
        // Mostrar resultados después del tiempo
        IEnumerator ShowResultsAfterDelay()
        {
            yield return new WaitForSeconds(2f); // Delay para que el jugador procese
            
            // PLACEHOLDER: Conexión con sistema de resultados (Lista D6)
            Debug.Log("Mostrar pantalla de resultados");
            
            // Aquí se conectará con el sistema de resultados
            // ResultsScreen resultsScreen = FindObjectOfType<ResultsScreen>();
            // if (resultsScreen != null) resultsScreen.ShowResults();
        }
        
        // MÉTODOS DE UTILIDAD
        
        public string GetFormattedTime()
        {
            return FormatTime(currentTime);
        }
        
        public static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
        
        public float GetTimePercentage()
        {
            return currentTime / levelDuration;
        }
        
        // MÉTODOS DE DEBUG
        [ContextMenu("Start Timer")]
        public void DebugStartTimer()
        {
            StartTimer();
        }
        
        [ContextMenu("Pause Timer")]
        public void DebugPauseTimer()
        {
            PauseTimer();
        }
        
        [ContextMenu("Add 30 Seconds")]
        public void DebugAdd30Seconds()
        {
            currentTime += 30f;
            currentTime = Mathf.Min(currentTime, levelDuration);
            
            if (timerDisplay != null)
            {
                timerDisplay.SetTime(currentTime, true);
            }
            
            OnTimeChanged?.Invoke(currentTime);
        }
        
        [ContextMenu("Set 10 Seconds Left")]
        public void DebugSet10SecondsLeft()
        {
            currentTime = 10f;
            warning30Triggered = true; // Para que no se dispare el warning de 30
            
            if (timerDisplay != null)
            {
                timerDisplay.SetTime(currentTime, true);
            }
            
            OnTimeChanged?.Invoke(currentTime);
        }
        
        void OnDestroy()
        {
            // Limpiar corrutinas
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
            }
        }
    }
    
    // ENUM para estados de warning (CONEXIÓN CON D4)
    public enum TimerWarningState
    {
        Normal,      // Color normal
        Warning30,   // Amarillo (30 segundos)
        Warning10,   // Rojo (10 segundos)
        TimeUp       // Tiempo agotado
    }
}