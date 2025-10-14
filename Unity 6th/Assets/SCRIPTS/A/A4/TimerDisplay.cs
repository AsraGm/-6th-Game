using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ARCHIVO: TimerDisplay.cs
// UI del timer con formato MM:SS y cambios de color (CONEXIÓN CON D4)

namespace ShootingRange
{
    public class TimerDisplay : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("ARRASTRA AQUÍ tu Text/TextMeshPro para mostrar el tiempo")]
        public TextMeshProUGUI timerText;
        
        [Tooltip("ARRASTRA AQUÍ un Image de fondo para el timer (opcional)")]
        public Image backgroundImage;
        
        [Tooltip("ARRASTRA AQUÍ un Image para mostrar progress bar (opcional)")]
        public Image progressBarImage;
        
        [Header("Configuración de Colores")]
        [Tooltip("Color normal del timer")]
        public Color normalColor = Color.white;
        
        [Tooltip("Color de warning a los 30 segundos")]
        public Color warning30Color = Color.yellow;
        
        [Tooltip("Color de warning a los 10 segundos")]
        public Color warning10Color = Color.red;
        
        [Tooltip("Color cuando se acaba el tiempo")]
        public Color timeUpColor = Color.red;
        
        [Header("Configuración Visual")]
        [Tooltip("Formato de tiempo: MM:SS o solo segundos")]
        public TimerFormat timeFormat = TimerFormat.MinutesSeconds;
        
        [Tooltip("Mostrar décimas de segundo en los últimos 10 segundos")]
        public bool showDecimalsAtEnd = true;
        
        [Tooltip("Tamaño de texto aumentado durante warnings")]
        [Range(1.0f, 2.0f)]
        public float warningTextScale = 1.2f;
        
        [Header("Efectos Visuales")]
        [Tooltip("Usar efecto de parpadeo en los últimos 10 segundos")]
        public bool useBlinkEffect = false;
        
        [Tooltip("Velocidad del parpadeo (parpadeos por segundo)")]
        [Range(0.5f, 5f)]
        public float blinkRate = 2f;
        
        // Variables privadas
        private float originalFontSize;
        private Vector3 originalScale;
        private TimerWarningState currentWarningState = TimerWarningState.Normal;
        private Coroutine blinkCoroutine;
        private bool isBlinking = false;
        
        // Cache para optimización
        private string cachedTimeString = "";
        private float lastDisplayedTime = -1f;
        
        void Start()
        {
            InitializeDisplay();
        }
        
        void InitializeDisplay()
        {
            // Configurar referencias por defecto
            if (timerText == null)
            {
                timerText = GetComponent<TextMeshProUGUI>();
                if (timerText == null)
                {
                    timerText = GetComponentInChildren<TextMeshProUGUI>();
                }
            }
            
            if (timerText != null)
            {
                originalFontSize = timerText.fontSize;
                originalScale = timerText.transform.localScale;
                timerText.color = normalColor;
            }
            
            // Configurar progress bar
            if (progressBarImage != null)
            {
                progressBarImage.fillMethod = Image.FillMethod.Horizontal;
                progressBarImage.type = Image.Type.Filled;
                progressBarImage.fillAmount = 1f;
            }
            
            Debug.Log("TimerDisplay inicializado");
        }
        
        // MÉTODO PRINCIPAL: Actualizar tiempo mostrado
        public void SetTime(float timeInSeconds, bool animate = false)
        {
            // Optimización: solo actualizar si el tiempo cambió significativamente
            if (Mathf.Abs(timeInSeconds - lastDisplayedTime) < 0.05f && !animate)
                return;
                
            lastDisplayedTime = timeInSeconds;
            
            // Formatear tiempo según configuración
            string timeString = FormatTime(timeInSeconds);
            
            // Optimización: solo actualizar UI si el string cambió
            if (timeString != cachedTimeString)
            {
                cachedTimeString = timeString;
                
                if (timerText != null)
                {
                    timerText.text = timeString;
                }
            }
            
            // Actualizar progress bar si existe
            UpdateProgressBar(timeInSeconds);
        }
        
        // Formatear tiempo según configuración
        string FormatTime(float seconds)
        {
            switch (timeFormat)
            {
                case TimerFormat.MinutesSeconds:
                    if (showDecimalsAtEnd && seconds <= 10f)
                    {
                        return $"{seconds:0.0}s";
                    }
                    else
                    {
                        int minutes = Mathf.FloorToInt(seconds / 60f);
                        int secs = Mathf.FloorToInt(seconds % 60f);
                        return $"{minutes:00}:{secs:00}";
                    }
                    
                case TimerFormat.SecondsOnly:
                    if (showDecimalsAtEnd && seconds <= 10f)
                    {
                        return $"{seconds:0.0}";
                    }
                    else
                    {
                        return $"{seconds:0}";
                    }
                    
                case TimerFormat.MinutesSecondsDecimals:
                    int mins = Mathf.FloorToInt(seconds / 60f);
                    float remainingSeconds = seconds % 60f;
                    return $"{mins:00}:{remainingSeconds:00.0}";
                    
                default:
                    return $"{seconds:0.0}";
            }
        }
        
        // Actualizar barra de progreso
        void UpdateProgressBar(float currentTime)
        {
            if (progressBarImage != null)
            {
                // Obtener duración total del timer
                LevelTimer levelTimer = FindObjectOfType<LevelTimer>();
                if (levelTimer != null)
                {
                    float progress = currentTime / levelTimer.LevelDuration;
                    progressBarImage.fillAmount = progress;
                    
                    // Cambiar color de la barra según el warning state
                    switch (currentWarningState)
                    {
                        case TimerWarningState.Normal:
                            progressBarImage.color = Color.green;
                            break;
                        case TimerWarningState.Warning30:
                            progressBarImage.color = warning30Color;
                            break;
                        case TimerWarningState.Warning10:
                            progressBarImage.color = warning10Color;
                            break;
                        case TimerWarningState.TimeUp:
                            progressBarImage.color = timeUpColor;
                            break;
                    }
                }
            }
        }
        
        // CONFIGURAR ESTADO DE WARNING (llamado desde LevelTimer)
        public void SetWarningState(TimerWarningState newState)
        {
            if (currentWarningState == newState) return;
            
            currentWarningState = newState;
            
            // Aplicar cambios visuales según el estado
            switch (newState)
            {
                case TimerWarningState.Normal:
                    SetNormalState();
                    break;
                    
                case TimerWarningState.Warning30:
                    SetWarning30State();
                    break;
                    
                case TimerWarningState.Warning10:
                    SetWarning10State();
                    break;
                    
                case TimerWarningState.TimeUp:
                    SetTimeUpState();
                    break;
            }
        }
        
        void SetNormalState()
        {
            if (timerText != null)
            {
                timerText.color = normalColor;
                timerText.fontSize = originalFontSize;
                timerText.transform.localScale = originalScale;
            }
            
            StopBlinking();
        }
        
        void SetWarning30State()
        {
            if (timerText != null)
            {
                timerText.color = warning30Color;
                timerText.fontSize = originalFontSize * warningTextScale;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = warning30Color;
            }
            
            Debug.Log("Timer: Warning 30 segundos");
        }
        
        void SetWarning10State()
        {
            if (timerText != null)
            {
                timerText.color = warning10Color;
                timerText.fontSize = originalFontSize * warningTextScale;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = warning10Color;
            }
            
            // Activar parpadeo si está habilitado
            if (useBlinkEffect)
            {
                StartBlinking();
            }
            
            Debug.Log("Timer: Warning 10 segundos");
        }
        
        void SetTimeUpState()
        {
            if (timerText != null)
            {
                timerText.color = timeUpColor;
                timerText.text = "00:00";
                timerText.fontSize = originalFontSize * warningTextScale;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = timeUpColor;
            }
            
            if (progressBarImage != null)
            {
                progressBarImage.fillAmount = 0f;
                progressBarImage.color = timeUpColor;
            }
            
            StopBlinking();
            Debug.Log("Timer: Tiempo agotado");
        }
        
        // EFECTOS DE PARPADEO
        void StartBlinking()
        {
            if (!isBlinking && useBlinkEffect)
            {
                isBlinking = true;
                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                }
                blinkCoroutine = StartCoroutine(BlinkCoroutine());
            }
        }
        
        void StopBlinking()
        {
            isBlinking = false;
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            
            // Asegurar que el texto esté visible
            if (timerText != null)
            {
                Color color = timerText.color;
                color.a = 1f;
                timerText.color = color;
            }
        }
        
        IEnumerator BlinkCoroutine()
        {
            float blinkInterval = 1f / (blinkRate * 2f); // *2 porque parpadea (on/off)
            WaitForSeconds wait = new WaitForSeconds(blinkInterval);
            
            while (isBlinking)
            {
                // Fade out
                if (timerText != null)
                {
                    Color color = timerText.color;
                    color.a = 0.3f;
                    timerText.color = color;
                }
                
                yield return wait;
                
                // Fade in
                if (timerText != null)
                {
                    Color color = timerText.color;
                    color.a = 1f;
                    timerText.color = color;
                }
                
                yield return wait;
            }
        }
        
        // MÉTODOS PÚBLICOS DE UTILIDAD
        
        public void ResetWarnings()
        {
            SetWarningState(TimerWarningState.Normal);
        }
        
        public void SetColors(Color normal, Color warning30, Color warning10, Color timeUp)
        {
            normalColor = normal;
            warning30Color = warning30;
            warning10Color = warning10;
            timeUpColor = timeUp;
            
            // Aplicar color actual
            SetWarningState(currentWarningState);
        }
        
        public void SetTimeFormat(TimerFormat newFormat)
        {
            timeFormat = newFormat;
            cachedTimeString = ""; // Forzar actualización
        }
        
        // MÉTODOS DE DEBUG
        [ContextMenu("Test Warning 30")]
        public void TestWarning30()
        {
            SetWarningState(TimerWarningState.Warning30);
            SetTime(30f, true);
        }
        
        [ContextMenu("Test Warning 10")]
        public void TestWarning10()
        {
            SetWarningState(TimerWarningState.Warning10);
            SetTime(10f, true);
        }
        
        [ContextMenu("Test Time Up")]
        public void TestTimeUp()
        {
            SetWarningState(TimerWarningState.TimeUp);
            SetTime(0f, true);
        }
        
        void OnDestroy()
        {
            // Limpiar corrutinas
            StopBlinking();
        }
    }
    
    // ENUM para formato de tiempo
    public enum TimerFormat
    {
        MinutesSeconds,        // 02:30
        SecondsOnly,          // 150
        MinutesSecondsDecimals // 02:30.5
    }
}