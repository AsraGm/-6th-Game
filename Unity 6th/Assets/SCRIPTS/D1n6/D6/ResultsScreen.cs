using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace ShootingRange
{
    /// <summary>
    /// ResultsScreen.cs - MODIFICADO CON SISTEMA DE ESTRELLAS
    /// Sistema de pantalla de resultados (D6)
    /// INTEGRADO con G2 (StatsTracker), G1 (SaveSystem) y NUEVO: Star Rating System
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

        [Header("⭐ NUEVO - Star Rating UI")]
        [Tooltip("ARRASTRA AQUÍ los 3 GameObjects de las estrellas (Star1, Star2, Star3)")]
        public GameObject[] starObjects = new GameObject[3];

        [Tooltip("(Opcional) Si tus estrellas son Images, arrástralas aquí también")]
        public Image[] starImages = new Image[3];

        [Tooltip("ARRASTRA AQUÍ el texto que muestra '★★★ 3/3' o similar (opcional)")]
        public TextMeshProUGUI starCountText;

        [Header("⭐ NUEVO - Star Colors & Settings")]
        [SerializeField] private Color activeStarColor = Color.yellow;
        [SerializeField] private Color inactiveStarColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        [Tooltip("Delay entre cada estrella al animar")]
        [Range(0.1f, 1f)]
        public float delayBetweenStars = 0.3f;

        [Tooltip("Duración de la animación de cada estrella")]
        [Range(0.1f, 0.5f)]
        public float starAnimationDuration = 0.3f;

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
        private int starsEarned = 0;

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

            Debug.Log("✅ ResultsScreen inicializado (CON ESTRELLAS)");
        }

        #region Show Results

        /// <summary>
        /// MÉTODO PRINCIPAL: Mostrar resultados del nivel CON ESTRELLAS
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

            // ⭐ NUEVO: Calcular estrellas basado en dinero ganado
            CalculateStars();

            // Mostrar panel
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
            }

            // Actualizar UI con los datos
            UpdateResultsUI();

            // Animación de fade in
            yield return StartCoroutine(FadeInAnimation());

            // ⭐ NUEVO: Animar estrellas después del fade in
            yield return StartCoroutine(AnimateStars());

            Debug.Log($"📊 Resultados mostrados - Estrellas: {starsEarned}/3");
        }

        #endregion

        #region ⭐ NUEVO - Star Calculation & Animation

        /// <summary>
        /// Calcular estrellas según el dinero ganado
        /// </summary>
        void CalculateStars()
        {
            // Obtener datos del nivel actual
            SOLevelData currentLevel = LevelHelper.CurrentLevel;

            if (currentLevel == null || currentLevel.starThresholds == null)
            {
                Debug.LogWarning("No se puede calcular estrellas: LevelData o StarThresholds no disponibles");
                starsEarned = 0;
                return;
            }

            // Calcular estrellas basado en dinero de sesión
            starsEarned = currentLevel.starThresholds.CalculateStars(currentLevelStats.moneyEarned);

            // Guardar las estrellas en SaveSystem
            SaveSystem.Instance.SaveLevelStars(currentLevelStats.levelID, starsEarned);

            // Verificar si es nuevo récord de estrellas
            int previousBestStars = SaveSystem.Instance.LoadLevelStars(currentLevelStats.levelID);
            bool isNewStarRecord = starsEarned > previousBestStars;

            Debug.Log($"⭐ Estrellas ganadas: {starsEarned}/3 (Dinero: ${currentLevelStats.moneyEarned})");
            if (isNewStarRecord)
            {
                Debug.Log($"🌟 ¡NUEVO RÉCORD DE ESTRELLAS! Anterior: {previousBestStars}");
            }
        }

        /// <summary>
        /// Animar las estrellas una por una
        /// </summary>
        IEnumerator AnimateStars()
        {
            // Inicializar todas las estrellas como inactivas/apagadas
            for (int i = 0; i < starObjects.Length; i++)
            {
                if (starObjects[i] != null)
                {
                    starObjects[i].transform.localScale = Vector3.zero;

                    // Si tiene Image component, cambiar color
                    if (starImages != null && i < starImages.Length && starImages[i] != null)
                    {
                        starImages[i].color = inactiveStarColor;
                    }
                }
            }

            // Animar cada estrella ganada
            for (int i = 0; i < starsEarned && i < starObjects.Length; i++)
            {
                yield return new WaitForSeconds(delayBetweenStars);

                if (starObjects[i] != null)
                {
                    // Cambiar color a activo
                    if (starImages != null && i < starImages.Length && starImages[i] != null)
                    {
                        starImages[i].color = activeStarColor;
                    }

                    // Animar escala con efecto de "pop"
                    StartCoroutine(AnimateSingleStar(starObjects[i].transform));

                    // Vibración háptica (solo en móviles)
                    if (Application.isMobilePlatform)
                    {
                        Handheld.Vibrate();
                    }
                }
            }

            // Actualizar texto de estrellas si existe
            if (starCountText != null)
            {
                starCountText.text = $"{starsEarned}/3";
            }
        }

        /// <summary>
        /// Animar una sola estrella con efecto "pop"
        /// </summary>
        IEnumerator AnimateSingleStar(Transform star)
        {
            float elapsed = 0f;
            float overshootScale = 1.3f;

            // Scale up con overshoot
            while (elapsed < starAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / starAnimationDuration;

                // Ease out back (efecto de rebote)
                float scale = Mathf.Lerp(0f, overshootScale, t);
                star.localScale = Vector3.one * scale;

                yield return null;
            }

            // Volver a escala normal
            elapsed = 0f;
            float returnDuration = starAnimationDuration * 0.5f;

            while (elapsed < returnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / returnDuration;

                float scale = Mathf.Lerp(overshootScale, 1f, t);
                star.localScale = Vector3.one * scale;

                yield return null;
            }

            star.localScale = Vector3.one;
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
                    "¡NEW RECORD!" : "¡LEVEL COMPLETED!";
                titleText.text = title;

                if (currentLevelStats.isNewBestScore)
                {
                    titleText.color = newRecordColor;
                }
            }

            // Dinero de sesión
            if (sessionMoneyText != null)
            {
                sessionMoneyText.text = $"Money Level: ${currentLevelStats.moneyEarned}";
                sessionMoneyText.color = currentLevelStats.moneyEarned > 0 ? positiveColor : normalColor;
            }

            // Dinero total (CONEXIÓN G1 vía StatsTracker)
            if (totalMoneyText != null)
            {
                int totalMoney = StatsTracker.Instance.GetTotalMoney();
                totalMoneyText.text = $"Total Money: ${totalMoney}";
            }

            // Tiempo del nivel
            if (levelTimeText != null)
            {
                levelTimeText.text = $"Time: {FormatTime(currentLevelStats.timeSpent)}";
            }

            // Enemigos eliminados
            if (enemiesKilledText != null)
            {
                enemiesKilledText.text = $"Enemies Eliminated: {currentLevelStats.enemiesKilled}";
            }

            // Mejor puntaje (opcional)
            if (bestScoreText != null)
            {
                int bestScore = StatsTracker.Instance.GetBestScore(currentLevelStats.levelID);
                bestScoreText.text = $"High Score: {bestScore}";
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
        [ContextMenu("Test Show Results - 3 Stars")]
        public void TestShowResults3Stars()
        {
            // Crear datos de prueba para 3 estrellas
            currentLevelStats = new LevelStats
            {
                levelID = "Level_Test",
                moneyEarned = 500, // Suficiente para 3 estrellas
                enemiesKilled = 20,
                timeSpent = 120f,
                finalScore = 500,
                isNewBestScore = true
            };

            StartCoroutine(ShowResultsCoroutine());
        }

        [ContextMenu("Test Show Results - 1 Star")]
        public void TestShowResults1Star()
        {
            // Crear datos de prueba para 1 estrella
            currentLevelStats = new LevelStats
            {
                levelID = "Level_Test",
                moneyEarned = 100, // Solo 1 estrella
                enemiesKilled = 8,
                timeSpent = 120f,
                finalScore = 100,
                isNewBestScore = false
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