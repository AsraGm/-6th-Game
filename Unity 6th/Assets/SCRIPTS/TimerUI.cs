using UnityEngine;
using TMPro;

// ARCHIVO: TimerUI.cs
// INSTRUCCIÓN D4: Timer UI básico
// Countdown simple con formato MM:SS y cambio de color en últimos segundos

namespace ShootingRange
{
    public class TimerUI : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("Texto del timer")]
        public TextMeshProUGUI timerText;
        
        [Header("Configuración de Formato")]
        [Tooltip("Formato del tiempo (MM:SS, SS, o personalizado)")]
        public TimerFormat timeFormat = TimerFormat.MinutesSeconds;
        
        [Tooltip("Mostrar décimas de segundo")]
        public bool showDecimals = false;
        
        [Tooltip("Texto cuando el tiempo llega a 0")]
        public string timeUpText = "00:00";
        
        [Header("Configuración de Colores")]
        [Tooltip("Color normal del timer")]
        public Color normalColor = Color.white;
        
        [Tooltip("Color de advertencia (últimos 30s)")]
        public Color warningColor = Color.yellow;
        
        [Tooltip("Tiempo para activar color amarillo (segundos)")]
        [Range(10f, 60f)]
        public float warningThreshold = 30f;
        
        [Tooltip("Color crítico (últimos 10s)")]
        public Color criticalColor = Color.red;
        
        [Tooltip("Tiempo para activar color rojo (segundos)")]
        [Range(1f, 30f)]
        public float criticalThreshold = 10f;
        
        [Header("Configuración de Animación")]
        [Tooltip("Pulsar cuando está en crítico")]
        public bool pulseWhenCritical = false;
        
        [Tooltip("Velocidad de pulsación")]
        [Range(0.5f, 3f)]
        public float pulseSpeed = 1.5f;
        
        [Tooltip("Escala de pulsación")]
        [Range(1f, 1.5f)]
        public float pulseScale = 1.2f;
        
        [Header("Referencias de Sistema")]
        [Tooltip("Sistema de timer (opcional, se puede actualizar manualmente)")]
        public MonoBehaviour timerSystem;
        
        // Variables privadas
        private float currentTime = 0f;
        private bool isRunning = false;
        private bool isCritical = false;
        private bool isWarning = false;
        private Vector3 originalScale;
        private float pulseTimer = 0f;
        
        // Enum para formato
        public enum TimerFormat
        {
            MinutesSeconds,    // MM:SS
            SecondsOnly,       // SS
            HoursMinutesSeconds // HH:MM:SS
        }
        
        void Start()
        {
            InitializeTimer();
        }
        
        void InitializeTimer()
        {
            if (timerText != null)
            {
                timerText.color = normalColor;
                originalScale = timerText.transform.localScale;
                UpdateTimerDisplay(0f);
            }
            
            Debug.Log("TimerUI inicializado");
        }
        
        void Update()
        {
            if (!isRunning) return;
            
            // Actualizar tiempo
            currentTime -= Time.deltaTime;
            if (currentTime < 0f)
            {
                currentTime = 0f;
                OnTimeUp();
            }
            
            // Actualizar display
            UpdateTimerDisplay(currentTime);
            
            // Actualizar colores
            UpdateTimerColor();
            
            // Animación de pulso si está crítico
            if (pulseWhenCritical && isCritical)
            {
                UpdatePulseAnimation();
            }
        }
        
        // ========================================
        // ACTUALIZACIÓN DE DISPLAY
        // ========================================
        
        void UpdateTimerDisplay(float timeInSeconds)
        {
            if (timerText == null) return;
            
            if (timeInSeconds <= 0f)
            {
                timerText.text = timeUpText;
                return;
            }
            
            string formattedTime = FormatTime(timeInSeconds);
            timerText.text = formattedTime;
        }
        
        string FormatTime(float timeInSeconds)
        {
            int totalSeconds = Mathf.FloorToInt(timeInSeconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            float decimals = (timeInSeconds - totalSeconds) * 10f;
            int deciseconds = Mathf.FloorToInt(decimals);
            
            switch (timeFormat)
            {
                case TimerFormat.MinutesSeconds:
                    if (showDecimals)
                        return $"{minutes:00}:{seconds:00}.{deciseconds:0}";
                    else
                        return $"{minutes:00}:{seconds:00}";
                    
                case TimerFormat.SecondsOnly:
                    if (showDecimals)
                        return $"{totalSeconds}.{deciseconds:0}";
                    else
                        return $"{totalSeconds}";
                    
                case TimerFormat.HoursMinutesSeconds:
                    if (showDecimals)
                        return $"{hours:00}:{minutes:00}:{seconds:00}.{deciseconds:0}";
                    else
                        return $"{hours:00}:{minutes:00}:{seconds:00}";
                    
                default:
                    return $"{minutes:00}:{seconds:00}";
            }
        }
        
        // ========================================
        // SISTEMA DE COLORES
        // ========================================
        
        void UpdateTimerColor()
        {
            if (timerText == null) return;
            
            // Determinar estado
            bool wasCritical = isCritical;
            bool wasWarning = isWarning;
            
            if (currentTime <= criticalThreshold)
            {
                isCritical = true;
                isWarning = false;
                
                if (!wasCritical)
                {
                    OnEnterCritical();
                }
            }
            else if (currentTime <= warningThreshold)
            {
                isCritical = false;
                isWarning = true;
                
                if (!wasWarning)
                {
                    OnEnterWarning();
                }
            }
            else
            {
                isCritical = false;
                isWarning = false;
            }
            
            // Aplicar color
            if (isCritical)
            {
                timerText.color = criticalColor;
            }
            else if (isWarning)
            {
                timerText.color = warningColor;
            }
            else
            {
                timerText.color = normalColor;
            }
        }
        
        void OnEnterWarning()
        {
            Debug.Log($"Timer entró en advertencia: {currentTime:F1}s restantes");
        }
        
        void OnEnterCritical()
        {
            Debug.Log($"Timer entró en crítico: {currentTime:F1}s restantes");
            
            // Resetear escala para pulso
            if (pulseWhenCritical && timerText != null)
            {
                timerText.transform.localScale = originalScale;
                pulseTimer = 0f;
            }
        }
        
        void OnTimeUp()
        {
            isRunning = false;
            isCritical = false;
            isWarning = false;
            
            if (timerText != null)
            {
                timerText.transform.localScale = originalScale;
            }
            
            Debug.Log("¡Tiempo agotado!");
        }
        
        // ========================================
        // ANIMACIÓN DE PULSO
        // ========================================
        
        void UpdatePulseAnimation()
        {
            if (timerText == null) return;
            
            pulseTimer += Time.deltaTime * pulseSpeed;
            
            // Calcular escala usando función seno
            float scale = 1f + (Mathf.Sin(pulseTimer * Mathf.PI * 2f) * (pulseScale - 1f));
            
            timerText.transform.localScale = originalScale * scale;
        }
        
        // ========================================
        // MÉTODOS PÚBLICOS DE CONTROL
        // ========================================
        
        /// <summary>
        /// Inicia el timer con un tiempo específico
        /// </summary>
        public void StartTimer(float timeInSeconds)
        {
            currentTime = timeInSeconds;
            isRunning = true;
            isCritical = false;
            isWarning = false;
            
            if (timerText != null)
            {
                timerText.color = normalColor;
                timerText.transform.localScale = originalScale;
            }
            
            Debug.Log($"Timer iniciado: {timeInSeconds}s");
        }
        
        /// <summary>
        /// Pausa el timer
        /// </summary>
        public void PauseTimer()
        {
            isRunning = false;
            Debug.Log("Timer pausado");
        }
        
        /// <summary>
        /// Reanuda el timer
        /// </summary>
        public void ResumeTimer()
        {
            isRunning = true;
            Debug.Log("Timer reanudado");
        }
        
        /// <summary>
        /// Detiene el timer completamente
        /// </summary>
        public void StopTimer()
        {
            isRunning = false;
            currentTime = 0f;
            isCritical = false;
            isWarning = false;
            
            if (timerText != null)
            {
                timerText.transform.localScale = originalScale;
            }
            
            UpdateTimerDisplay(0f);
            Debug.Log("Timer detenido");
        }
        
        /// <summary>
        /// Agrega tiempo al timer
        /// </summary>
        public void AddTime(float secondsToAdd)
        {
            currentTime += secondsToAdd;
            Debug.Log($"Tiempo agregado: +{secondsToAdd}s (Total: {currentTime:F1}s)");
        }
        
        /// <summary>
        /// Establece el tiempo actual sin reiniciar
        /// </summary>
        public void SetTime(float timeInSeconds)
        {
            currentTime = timeInSeconds;
            UpdateTimerDisplay(currentTime);
        }
        
        /// <summary>
        /// Obtiene el tiempo restante actual
        /// </summary>
        public float GetTimeRemaining()
        {
            return currentTime;
        }
        
        /// <summary>
        /// Verifica si el timer está corriendo
        /// </summary>
        public bool IsRunning()
        {
            return isRunning;
        }
        
        /// <summary>
        /// Verifica si está en estado crítico
        /// </summary>
        public bool IsCritical()
        {
            return isCritical;
        }
        
        /// <summary>
        /// Verifica si está en estado de advertencia
        /// </summary>
        public bool IsWarning()
        {
            return isWarning;
        }
        
        // ========================================
        // MÉTODOS DE DEBUG
        // ========================================
        
        [ContextMenu("Test Start 60s")]
        public void TestStart60()
        {
            StartTimer(60f);
        }
        
        [ContextMenu("Test Start 30s (Warning)")]
        public void TestStart30()
        {
            StartTimer(30f);
        }
        
        [ContextMenu("Test Start 10s (Critical)")]
        public void TestStart10()
        {
            StartTimer(10f);
        }
        
        [ContextMenu("Test Add 15s")]
        public void TestAddTime()
        {
            AddTime(15f);
        }
        
        [ContextMenu("Test Pause")]
        public void TestPause()
        {
            PauseTimer();
        }
        
        [ContextMenu("Test Resume")]
        public void TestResume()
        {
            ResumeTimer();
        }
        
        [ContextMenu("Test Stop")]
        public void TestStop()
        {
            StopTimer();
        }
        
        void OnValidate()
        {
            warningThreshold = Mathf.Max(criticalThreshold + 1f, warningThreshold);
            criticalThreshold = Mathf.Max(1f, criticalThreshold);
            pulseSpeed = Mathf.Max(0.5f, pulseSpeed);
            pulseScale = Mathf.Max(1f, pulseScale);
        }
    }
}