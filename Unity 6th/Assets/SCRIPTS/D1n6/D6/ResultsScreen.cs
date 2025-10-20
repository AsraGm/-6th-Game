using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace ShootingRange
{
    /// <summary>
    /// Sistema de pantalla de resultados (D6)
    /// INTEGRADO con G2 (StatsTracker) y G1 (SaveSystem)
    /// Muestra estadísticas del nivel completado
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        [Header("Referencias UI - Textos")]
        [Tooltip("ARRASTRA AQUÍ el texto de 'Level Complete' o título")]
        public TextMeshProUGUI titleText;

        [Tooltip("ARRASTRA AQUÍ el texto del dinero ganado en este nivel")]
        public TextMeshProUGUI sessionMoneyText;

        [Tooltip("ARRASTRA AQUÍ el texto del dinero total acumulado")]
        public TextMeshProUGUI totalMoneyText;

        [Tooltip("ARRASTRA AQUÍ el texto del tiempo del nivel")]
        public TextMeshProUGUI levelTimeText;

        [Tooltip("ARRASTRA AQUÍ el texto de enemigos eliminados")]
        public TextMeshProUGUI enemiesKilledText;

        [Tooltip("(Opcional) Texto para mostrar mejor puntaje")]
        public TextMeshProUGUI bestScoreText;

        [Tooltip("(Opcional) Indicador de nuevo récord")]
        public GameObject newRecordIndicator;

        [Header("Referencias UI - Botones")]
        [Tooltip("ARRASTRA AQUÍ el botón para volver a Level Selection")]
        public Button backToLevelSelectionButton;

        [Tooltip("ARRASTRA AQUÍ el botón para reintentar el nivel (opcional)")]
        public Button retryButton;

        [Header("Referencias UI - Panel")]
        [Tooltip("ARRASTRA AQUÍ el GameObject del panel completo de resultados")]
        public GameObject resultsPanel;

        [Header("Configuración de Animación")]
        [Tooltip("Tiempo de delay antes de mostrar resultados")]
        [Range(0f, 3f)]
        public float showDelay = 1.5f;

        [Tooltip("Duración de la animación de aparición")]
        [Range(0.1f, 2f)]
        public float fadeInDuration = 0.5f;

        [Header("Nombre de Escena")]
        [Tooltip("Nombre exacto de tu escena de Level Selection")]
        public string levelSelectionSceneName = "LEVEL SELECTION";

        [Header("Colores")]
        public Color positiveColor = Color.green;
        public Color normalColor = Color.white;
        public Color newRecordColor = Color.yellow;

        // Datos del nivel (ahora vienen de StatsTracker)
        private LevelStats currentLevelStats;

        void Start()
        {
            InitializeResultsScreen();
        }

        void InitializeResultsScreen()
        {
            // Ocultar panel al inicio
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }

            // Ocultar indicador de nuevo récord
            if (newRecordIndicator != null)
            {
                newRecordIndicator.SetActive(false);
            }

            // Conectar botones
            if (backToLevelSelectionButton != null)
            {
                backToLevelSelectionButton.onClick.AddListener(BackToLevelSelection);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(RetryLevel);
            }

            Debug.Log("✅ ResultsScreen inicializado");
        }

        #region Show Results

        /// <summary>
        /// MÉTODO PRINCIPAL: Mostrar resultados del nivel
        /// Usa StatsTracker (G2) para obtener y guardar datos
        /// </summary>
        public void ShowResults()
        {
            StartCoroutine(ShowResultsCoroutine());
        }

        IEnumerator ShowResultsCoroutine()
        {
            // Esperar el delay configurado
            yield return new WaitForSeconds(showDelay);

            // CONEXIÓN G2: Completar nivel y obtener estadísticas
            currentLevelStats = StatsTracker.Instance.CompleteLevelAndSave();

            if (currentLevelStats == null)
            {
                Debug.LogError("No se pudieron obtener estadísticas del nivel");
                yield break;
            }

            // Mostrar panel
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
            }

            // Actualizar UI con los datos
            UpdateResultsUI();

            // Animación de fade in
            yield return StartCoroutine(FadeInAnimation());

            Debug.Log("📊 Resultados mostrados");
        }

        #endregion

        #region Update UI

        /// <summary>
        /// Actualizar UI con las estadísticas del nivel
        /// </summary>
        void UpdateResultsUI()
        {
            if (currentLevelStats == null) return;

            // Título
            if (titleText != null)
            {
                string title = currentLevelStats.isNewBestScore ?
                    "¡NUEVO RÉCORD!" : "¡NIVEL COMPLETADO!";
                titleText.text = title;

                if (currentLevelStats.isNewBestScore)
                {
                    titleText.color = newRecordColor;
                }
            }

            // Dinero de sesión
            if (sessionMoneyText != null)
            {
                sessionMoneyText.text = $"Dinero Ganado: ${currentLevelStats.moneyEarned}";
                sessionMoneyText.color = currentLevelStats.moneyEarned > 0 ? positiveColor : normalColor;
            }

            // Dinero total (CONEXIÓN G1 vía StatsTracker)
            if (totalMoneyText != null)
            {
                int totalMoney = StatsTracker.Instance.GetTotalMoney();
                totalMoneyText.text = $"Dinero Total: ${totalMoney}";
            }

            // Tiempo del nivel
            if (levelTimeText != null)
            {
                levelTimeText.text = $"Tiempo: {FormatTime(currentLevelStats.timeSpent)}";
            }

            // Enemigos eliminados
            if (enemiesKilledText != null)
            {
                enemiesKilledText.text = $"Enemigos Eliminados: {currentLevelStats.enemiesKilled}";
            }

            // Mejor puntaje (opcional)
            if (bestScoreText != null)
            {
                int bestScore = StatsTracker.Instance.GetBestScore(currentLevelStats.levelID);
                bestScoreText.text = $"Mejor Score: {bestScore}";
            }

            // Indicador de nuevo récord (opcional)
            if (newRecordIndicator != null)
            {
                newRecordIndicator.SetActive(currentLevelStats.isNewBestScore);
            }

            Debug.Log($"UI actualizada con stats: {currentLevelStats.ToString()}");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Formatear tiempo en formato MM:SS
        /// </summary>
        string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }

        /// <summary>
        /// Animación simple de fade in
        /// </summary>
        IEnumerator FadeInAnimation()
        {
            CanvasGroup canvasGroup = resultsPanel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = resultsPanel.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        #endregion

        #region Button Callbacks

        /// <summary>
        /// BOTÓN: Volver a Level Selection
        /// </summary>
        public void BackToLevelSelection()
        {
            Debug.Log($"🔙 Volviendo a {levelSelectionSceneName}");

            // Asegurar que el tiempo esté corriendo
            Time.timeScale = 1f;

            // Cargar escena de Level Selection
            SceneManager.LoadScene(levelSelectionSceneName);
        }

        /// <summary>
        /// BOTÓN: Reintentar nivel
        /// </summary>
        public void RetryLevel()
        {
            Debug.Log("🔄 Reintentando nivel");

            // Asegurar que el tiempo esté corriendo
            Time.timeScale = 1f;

            // Recargar la escena actual
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        #endregion

        #region Testing Methods

        /// <summary>
        /// Método para testing - Mostrar resultados con datos de prueba
        /// </summary>
        [ContextMenu("Test Show Results")]
        public void TestShowResults()
        {
            // Crear datos de prueba
            currentLevelStats = new LevelStats
            {
                levelID = "Level_Test",
                moneyEarned = 250,
                enemiesKilled = 15,
                timeSpent = 120f,
                finalScore = 250,
                isNewBestScore = true
            };

            StartCoroutine(ShowResultsCoroutine());
        }

        #endregion

        void OnDestroy()
        {
            // Desconectar botones
            if (backToLevelSelectionButton != null)
            {
                backToLevelSelectionButton.onClick.RemoveListener(BackToLevelSelection);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(RetryLevel);
            }
        }
    }
}