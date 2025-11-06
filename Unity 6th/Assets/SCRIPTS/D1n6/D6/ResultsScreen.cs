using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace ShootingRange
{
    /// <summary>
    /// ResultsScreen.cs - CORREGIDO para guardar estrellas correctamente
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

        // Datos del nivel
        private LevelStats currentLevelStats;
        private int starsEarned = 0;

        void Start()
        {
            InitializeResultsScreen();
        }

        void InitializeResultsScreen()
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }

            if (newRecordIndicator != null)
            {
                newRecordIndicator.SetActive(false);
            }

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

        public void ShowResults()
        {
            StartCoroutine(ShowResultsCoroutine());
        }

        IEnumerator ShowResultsCoroutine()
        {
            yield return new WaitForSeconds(showDelay);

            // OBTENER ESTADÍSTICAS DEL NIVEL
            currentLevelStats = StatsTracker.Instance.CompleteLevelAndSave();

            if (currentLevelStats == null)
            {
                Debug.LogError("❌ No se pudieron obtener estadísticas del nivel");
                yield break;
            }

            // ⭐ CALCULAR Y GUARDAR ESTRELLAS
            CalculateAndSaveStars();

            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
            }

            UpdateResultsUI();
            yield return StartCoroutine(FadeInAnimation());
            yield return StartCoroutine(AnimateStars());

            Debug.Log($"📊 Resultados mostrados - Estrellas: {starsEarned}/3");
        }

        #endregion

        #region ⭐ Star Calculation & Saving

        /// <summary>
        /// 🔥 MÉTODO CRÍTICO: Calcular y GUARDAR estrellas correctamente
        /// </summary>
        void CalculateAndSaveStars()
        {
            // Obtener el nivel actual desde LevelHelper
            SOLevelData currentLevel = LevelHelper.CurrentLevel;

            if (currentLevel == null)
            {
                Debug.LogError("❌ LevelHelper.CurrentLevel es NULL - No se pueden calcular estrellas");
                starsEarned = 0;
                return;
            }

            if (currentLevel.starThresholds == null)
            {
                Debug.LogError("❌ StarThresholds no configurado en el LevelData");
                starsEarned = 0;
                return;
            }

            // 🔥 USAR EL levelID DEL SOLevelData (el mismo que usa LevelButton)
            string levelID = currentLevel.levelID;

            // Calcular estrellas según el dinero ganado
            starsEarned = currentLevel.starThresholds.CalculateStars(currentLevelStats.moneyEarned);

            Debug.Log($"💰 Dinero ganado: ${currentLevelStats.moneyEarned}");
            Debug.Log($"⭐ Estrellas calculadas: {starsEarned}/3");
            Debug.Log($"🆔 Guardando con levelID: '{levelID}'");

            // Obtener mejor puntuación anterior
            int previousBestStars = SaveSystem.Instance.LoadLevelStars(levelID);
            int previousBestMoney = SaveSystem.Instance.LoadLevelBestScore(levelID);

            Debug.Log($"📜 Mejor anterior - Estrellas: {previousBestStars}, Dinero: ${previousBestMoney}");

            // 🔥 GUARDAR LAS ESTRELLAS
            // Nota: SaveLevelStars solo guarda si es mejor que antes
            SaveSystem.Instance.SaveLevelStars(levelID, starsEarned);

            if (starsEarned > previousBestStars)
            {
                Debug.Log($"🌟 ¡NUEVO RÉCORD DE ESTRELLAS! {starsEarned}/3 (Anterior: {previousBestStars})");
            }
            else if (starsEarned == previousBestStars)
            {
                Debug.Log($"⭐ Estrellas mantenidas: {starsEarned}/3");
            }
            else
            {
                Debug.Log($"⭐ Estrellas obtenidas: {starsEarned}/3 (Mejor: {previousBestStars}/3)");
            }

            // 🔥 GUARDAR EL DINERO
            // Nota: SaveLevelBestScore solo guarda si es mejor que antes
            SaveSystem.Instance.SaveLevelBestScore(levelID, currentLevelStats.moneyEarned);

            if (currentLevelStats.moneyEarned > previousBestMoney)
            {
                Debug.Log($"💰 ¡Nuevo récord de dinero! ${currentLevelStats.moneyEarned} (Anterior: ${previousBestMoney})");
            }
            else
            {
                Debug.Log($"💵 Dinero ganado: ${currentLevelStats.moneyEarned} (Mejor: ${previousBestMoney})");
            }

            Debug.Log("💾 ✅ Datos guardados automáticamente por SaveSystem");
        }

        #endregion

        #region ⭐ Star Animation

        IEnumerator AnimateStars()
        {
            // Inicializar todas las estrellas como inactivas
            for (int i = 0; i < starObjects.Length; i++)
            {
                if (starObjects[i] != null)
                {
                    starObjects[i].transform.localScale = Vector3.zero;

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
                    if (starImages != null && i < starImages.Length && starImages[i] != null)
                    {
                        starImages[i].color = activeStarColor;
                    }

                    StartCoroutine(AnimateSingleStar(starObjects[i].transform));

                    // Vibración en móvil (opcional)
                    TryVibrate();
                }
            }

            // Actualizar texto de estrellas
            if (starCountText != null)
            {
                starCountText.text = $"{starsEarned}/3";
            }
        }

        IEnumerator AnimateSingleStar(Transform star)
        {
            float elapsed = 0f;
            float overshootScale = 1.3f;

            // Scale up con overshoot
            while (elapsed < starAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / starAnimationDuration;
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
                float scale = Mathf.Lerp(overshootScale, 2f, t);
                star.localScale = Vector3.one * scale;
                yield return null;
            }

            star.localScale = Vector3.one;
        }

        void TryVibrate()
        {
#if UNITY_ANDROID
            if (Application.isMobilePlatform)
            {
                try
                {
                    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                    {
                        vibrator.Call("vibrate", 50L);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"No se pudo vibrar: {e.Message}");
                }
            }
#endif
        }

        #endregion

        #region Update UI

        void UpdateResultsUI()
        {
            if (currentLevelStats == null) return;

            // Título
            if (titleText != null)
            {
                string title = currentLevelStats.isNewBestScore ? "¡NEW RECORD!" : "¡LEVEL COMPLETED!";
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

            // Dinero total
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

            // Mejor puntaje
            if (bestScoreText != null)
            {
                int bestScore = StatsTracker.Instance.GetBestScore(currentLevelStats.levelID);
                bestScoreText.text = $"High Score: {bestScore}";
            }

            // Indicador de nuevo récord
            if (newRecordIndicator != null)
            {
                newRecordIndicator.SetActive(currentLevelStats.isNewBestScore);
            }
        }

        #endregion

        #region Utility Methods

        string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }

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

        public void BackToLevelSelection()
        {
            Debug.Log($"🔙 Volviendo a {levelSelectionSceneName}");
            Time.timeScale = 1f;
            SceneManager.LoadScene(levelSelectionSceneName);
        }

        public void RetryLevel()
        {
            Debug.Log("🔄 Reintentando nivel");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        #endregion

        #region Testing Methods

        [ContextMenu("🧪 Test - 3 Estrellas")]
        public void TestShowResults3Stars()
        {
            currentLevelStats = new LevelStats
            {
                levelID = "Level_Test",
                moneyEarned = 500,
                enemiesKilled = 20,
                timeSpent = 120f,
                finalScore = 500,
                isNewBestScore = true
            };

            StartCoroutine(ShowResultsCoroutine());
        }

        [ContextMenu("🧪 Test - 1 Estrella")]
        public void TestShowResults1Star()
        {
            currentLevelStats = new LevelStats
            {
                levelID = "Level_Test",
                moneyEarned = 100,
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