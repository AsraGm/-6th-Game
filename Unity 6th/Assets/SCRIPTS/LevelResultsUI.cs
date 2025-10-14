using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

// ARCHIVO: LevelResultsUI.cs
// INSTRUCCIÓN D6: Canvas de resultados de nivel
// Pantalla básica de "Level Complete" con estadísticas simples

namespace ShootingRange
{
    public class LevelResultsUI : MonoBehaviour
    {
        [Header("Referencias del Panel")]
        [Tooltip("Panel principal de resultados")]
        public GameObject resultsPanel;
        
        [Tooltip("Panel de fondo oscuro (opcional)")]
        public GameObject backgroundOverlay;
        
        [Header("Textos de Resultados")]
        [Tooltip("Título principal (Level Complete / Level Failed)")]
        public TextMeshProUGUI titleText;
        
        [Tooltip("Texto de dinero ganado total")]
        public TextMeshProUGUI totalMoneyText;
        
        [Tooltip("Texto de tiempo completado")]
        public TextMeshProUGUI completionTimeText;
        
        [Tooltip("Texto de puntaje final (opcional)")]
        public TextMeshProUGUI finalScoreText;
        
        [Tooltip("Texto de nuevo récord (opcional)")]
        public TextMeshProUGUI newRecordText;
        
        [Header("Botones")]
        [Tooltip("Botón para regresar a selección de niveles")]
        public Button returnToMenuButton;
        
        [Tooltip("Botón para reintentar nivel (opcional)")]
        public Button retryLevelButton;
        
        [Tooltip("Botón para siguiente nivel (opcional)")]
        public Button nextLevelButton;
        
        [Header("Configuración de Texto")]
        [Tooltip("Texto cuando se completa el nivel")]
        public string completedTitle = "¡NIVEL COMPLETADO!";
        
        [Tooltip("Texto cuando se falla el nivel")]
        public string failedTitle = "NIVEL FALLIDO";
        
        [Tooltip("Prefijo de dinero")]
        public string moneyPrefix = "$";
        
        [Tooltip("Formato de tiempo")]
        public bool showTimeInMinutes = true;
        
        [Header("Configuración de Animación")]
        [Tooltip("Animar entrada del panel")]
        public bool animateEntry = true;
        
        [Tooltip("Duración de la animación de entrada")]
        [Range(0.1f, 1f)]
        public float entryAnimationDuration = 0.5f;
        
        [Tooltip("Delay antes de mostrar resultados")]
        [Range(0f, 2f)]
        public float showDelay = 0.5f;
        
        [Header("Navegación de Escenas")]
        [Tooltip("Nombre de la escena de selección de niveles")]
        public string levelSelectionSceneName = "LevelSelection";
        
        [Tooltip("Nombre de la escena del nivel actual (para retry)")]
        public string currentLevelSceneName = "";
        
        // Variables privadas
        private int totalMoneyEarned = 0;
        private float completionTime = 0f;
        private int finalScore = 0;
        private bool isNewRecord = false;
        private bool isLevelCompleted = false;
        private CanvasGroup canvasGroup;
        
        void Start()
        {
            InitializeResultsUI();
        }
        
        void InitializeResultsUI()
        {
            // Configurar CanvasGroup para animaciones
            canvasGroup = resultsPanel?.GetComponent<CanvasGroup>();
            if (canvasGroup == null && resultsPanel != null)
            {
                canvasGroup = resultsPanel.AddComponent<CanvasGroup>();
            }
            
            // Configurar botones
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(OnReturnToMenu);
            }
            
            if (retryLevelButton != null)
            {
                retryLevelButton.onClick.AddListener(OnRetryLevel);
            }
            
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(OnNextLevel);
            }
            
            // Ocultar inicialmente
            HideResults();
            
            Debug.Log("LevelResultsUI inicializado");
        }
        
        // ========================================
        // MÉTODO PRINCIPAL: Mostrar Resultados
        // ========================================
        
        /// <summary>
        /// Muestra los resultados del nivel
        /// </summary>
        public void ShowResults(int moneyEarned, float timeInSeconds, bool completed)
        {
            totalMoneyEarned = moneyEarned;
            completionTime = timeInSeconds;
            isLevelCompleted = completed;
            
            // Calcular score simple (dinero ganado)
            finalScore = moneyEarned;
            
            // TODO: Verificar si es nuevo récord (requiere sistema de guardado)
            isNewRecord = false;
            
            // Mostrar panel con delay
            StartCoroutine(ShowResultsCoroutine());
        }
        
        IEnumerator ShowResultsCoroutine()
        {
            // Delay inicial
            yield return new WaitForSeconds(showDelay);
            
            // Activar panel
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
            }
            
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(true);
            }
            
            // Actualizar textos
            UpdateResultTexts();
            
            // Animación de entrada
            if (animateEntry && canvasGroup != null)
            {
                yield return StartCoroutine(AnimateEntry());
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
        
        // ========================================
        // ACTUALIZACIÓN DE TEXTOS
        // ========================================
        
        void UpdateResultTexts()
        {
            // Título
            if (titleText != null)
            {
                titleText.text = isLevelCompleted ? completedTitle : failedTitle;
                titleText.color = isLevelCompleted ? Color.green : Color.red;
            }
            
            // Dinero total
            if (totalMoneyText != null)
            {
                totalMoneyText.text = $"Dinero Ganado: {moneyPrefix}{totalMoneyEarned}";
            }
            
            // Tiempo de completado
            if (completionTimeText != null)
            {
                string timeString = FormatTime(completionTime);
                completionTimeText.text = $"Tiempo: {timeString}";
            }
            
            // Score final
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Puntaje: {finalScore}";
            }
            
            // Nuevo récord
            if (newRecordText != null)
            {
                newRecordText.gameObject.SetActive(isNewRecord);
                if (isNewRecord)
                {
                    newRecordText.text = "¡NUEVO RÉCORD!";
                }
            }
            
            // Configurar botones según estado
            ConfigureButtons();
        }
        
        string FormatTime(float timeInSeconds)
        {
            if (showTimeInMinutes)
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
                int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
                return $"{minutes:00}:{seconds:00}";
            }
            else
            {
                return $"{timeInSeconds:F1}s";
            }
        }
        
        void ConfigureButtons()
        {
            // Botón de retry solo si el nivel falló
            if (retryLevelButton != null)
            {
                retryLevelButton.gameObject.SetActive(!isLevelCompleted);
            }
            
            // Botón de siguiente nivel solo si completó
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(isLevelCompleted);
            }
            
            // Botón de menú siempre visible
            if (returnToMenuButton != null)
            {
                returnToMenuButton.gameObject.SetActive(true);
            }
        }
        
        // ========================================
        // ANIMACIÓN DE ENTRADA
        // ========================================
        
        IEnumerator AnimateEntry()
        {
            canvasGroup.alpha = 0f;
            
            // Opcional: escala inicial
            Vector3 originalScale = resultsPanel.transform.localScale;
            resultsPanel.transform.localScale = originalScale * 0.8f;
            
            float elapsed = 0f;
            
            while (elapsed < entryAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / entryAnimationDuration;
                
                // Fade in
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                
                // Scale up
                resultsPanel.transform.localScale = Vector3.Lerp(
                    originalScale * 0.8f,
                    originalScale,
                    progress
                );
                
                yield return null;
            }
            
            // Asegurar valores finales
            canvasGroup.alpha = 1f;
            resultsPanel.transform.localScale = originalScale;
        }
        
        // ========================================
        // CALLBACKS DE BOTONES
        // ========================================
        
        void OnReturnToMenu()
        {
            Debug.Log("Regresando a selección de niveles...");
            
            // TODO: Guardar progreso antes de cambiar escena
            
            if (!string.IsNullOrEmpty(levelSelectionSceneName))
            {
                SceneManager.LoadScene(levelSelectionSceneName);
            }
            else
            {
                Debug.LogWarning("Nombre de escena de selección no configurado");
            }
        }
        
        void OnRetryLevel()
        {
            Debug.Log("Reintentando nivel...");
            
            if (!string.IsNullOrEmpty(currentLevelSceneName))
            {
                SceneManager.LoadScene(currentLevelSceneName);
            }
            else
            {
                // Recargar escena actual
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        
        void OnNextLevel()
        {
            Debug.Log("Cargando siguiente nivel...");
            
            // TODO: Implementar lógica de siguiente nivel
            // Por ahora, regresar a menú
            OnReturnToMenu();
        }
        
        // ========================================
        // MÉTODOS PÚBLICOS
        // ========================================
        
        /// <summary>
        /// Muestra resultados con parámetros completos
        /// </summary>
        public void ShowResults(int moneyEarned, float timeInSeconds, bool completed, int score)
        {
            totalMoneyEarned = moneyEarned;
            completionTime = timeInSeconds;
            isLevelCompleted = completed;
            finalScore = score;
            
            StartCoroutine(ShowResultsCoroutine());
        }
        
        /// <summary>
        /// Oculta el panel de resultados
        /// </summary>
        public void HideResults()
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }
            
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(false);
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
        
        /// <summary>
        /// Establece si es un nuevo récord
        /// </summary>
        public void SetNewRecord(bool isRecord)
        {
            isNewRecord = isRecord;
            
            if (newRecordText != null)
            {
                newRecordText.gameObject.SetActive(isRecord);
            }
        }
        
        /// <summary>
        /// Configura el nombre de la escena actual para retry
        /// </summary>
        public void SetCurrentLevelScene(string sceneName)
        {
            currentLevelSceneName = sceneName;
        }
        
        // ========================================
        // INTEGRACIÓN CON SISTEMAS
        // ========================================
        
        /// <summary>
        /// Método de integración con MoneySystem y otros sistemas
        /// </summary>
        public void ShowResultsFromSystems()
        {
            // Buscar sistemas en la escena
            MoneySystem moneySystem = FindObjectOfType<MoneySystem>();
            TimerUI timerUI = FindObjectOfType<TimerUI>();
            
            int money = moneySystem != null ? moneySystem.CurrentMoney : 0;
            float time = timerUI != null ? timerUI.GetTimeRemaining() : 0f;
            
            // Por defecto, asumir que completó si tiene tiempo restante
            bool completed = time > 0f;
            
            ShowResults(money, time, completed);
        }
        
        // ========================================
        // MÉTODOS DE DEBUG
        // ========================================
        
        [ContextMenu("Test Show Success Results")]
        public void TestShowSuccess()
        {
            ShowResults(500, 45.5f, true);
        }
        
        [ContextMenu("Test Show Failed Results")]
        public void TestShowFailed()
        {
            ShowResults(150, 0f, false);
        }
        
        [ContextMenu("Test Show With New Record")]
        public void TestShowWithRecord()
        {
            ShowResults(1000, 30f, true);
            SetNewRecord(true);
        }
        
        [ContextMenu("Test Hide Results")]
        public void TestHide()
        {
            HideResults();
        }
        
        void OnDestroy()
        {
            // Limpiar listeners de botones
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(OnReturnToMenu);
            }
            
            if (retryLevelButton != null)
            {
                retryLevelButton.onClick.RemoveListener(OnRetryLevel);
            }
            
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(OnNextLevel);
            }
        }
        
        void OnValidate()
        {
            entryAnimationDuration = Mathf.Max(0.1f, entryAnimationDuration);
            showDelay = Mathf.Max(0f, showDelay);
        }
    }
}