using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

// ARCHIVO: ResultsScreen.cs
// Sistema de pantalla de resultados (D6) - Conectado con A3, A4, G1, F2
// Muestra estadísticas del nivel completado

namespace ShootingRange
{
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

        // Referencias a sistemas
        private MoneySystem moneySystem;
        private LevelTimer levelTimer;
        private ScoreSystem scoreSystem;
        private WaveSystem waveSystem;

        // Datos del nivel
        private int sessionMoney;
        private int totalMoney;
        private float levelTime;
        private int enemiesKilled;

        void Start()
        {
            InitializeResultsScreen();
        }

        void InitializeResultsScreen()
        {
            // Buscar sistemas
            FindSystems();

            // Ocultar panel al inicio
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
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

        void FindSystems()
        {
            if (moneySystem == null)
                moneySystem = FindObjectOfType<MoneySystem>();

            if (levelTimer == null)
                levelTimer = FindObjectOfType<LevelTimer>();

            if (scoreSystem == null)
                scoreSystem = FindObjectOfType<ScoreSystem>();

            if (waveSystem == null)
                waveSystem = FindObjectOfType<WaveSystem>();
        }

        // MÉTODO PRINCIPAL: Mostrar resultados
        public void ShowResults()
        {
            StartCoroutine(ShowResultsCoroutine());
        }

        IEnumerator ShowResultsCoroutine()
        {
            // Esperar el delay configurado
            yield return new WaitForSeconds(showDelay);

            // Recopilar datos
            CollectLevelData();

            // Guardar progreso (CONEXIÓN G1)
            SaveProgress();

            // Mostrar panel
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
            }

            // Actualizar UI con los datos
            UpdateResultsUI();

            // Animación de fade in (simple)
            yield return StartCoroutine(FadeInAnimation());

            Debug.Log("📊 Resultados mostrados");
        }

        // Recopilar datos del nivel completado
        void CollectLevelData()
        {
            // CONEXIÓN A3: Dinero
            if (moneySystem != null)
            {
                sessionMoney = moneySystem.GetSessionEarnings();
                totalMoney = moneySystem.GetCurrentMoney();
            }

            // CONEXIÓN A4: Tiempo
            if (levelTimer != null)
            {
                levelTime = levelTimer.LevelDuration - levelTimer.CurrentTime;
            }

            // CONEXIÓN con ScoreSystem: Enemigos eliminados
            if (scoreSystem != null)
            {
                enemiesKilled = scoreSystem.GetEnemiesHit();
            }
            else
            {
                // Fallback: usar WaveSystem si no hay ScoreSystem
                if (waveSystem != null)
                {
                    enemiesKilled = waveSystem.GetTotalEnemiesSpawned();
                }
                else
                {
                    // Si no hay ninguno, poner 0
                    enemiesKilled = 0;
                }
            }

            Debug.Log($"📊 Datos recopilados - Dinero: ${sessionMoney}, Tiempo: {levelTime:F1}s, Enemigos: {enemiesKilled}");
        }

        // Actualizar UI con los datos recopilados
        void UpdateResultsUI()
        {
            // Título
            if (titleText != null)
            {
                titleText.text = "¡NIVEL COMPLETADO!";
            }

            // Dinero de sesión
            if (sessionMoneyText != null)
            {
                sessionMoneyText.text = $"Dinero Ganado: ${sessionMoney}";
                sessionMoneyText.color = sessionMoney > 0 ? positiveColor : normalColor;
            }

            // Dinero total
            if (totalMoneyText != null)
            {
                totalMoneyText.text = $"Dinero Total: ${totalMoney}";
            }

            // Tiempo del nivel
            if (levelTimeText != null)
            {
                levelTimeText.text = $"Tiempo: {FormatTime(levelTime)}";
            }

            // Enemigos eliminados
            if (enemiesKilledText != null)
            {
                enemiesKilledText.text = $"Enemigos Eliminados: {enemiesKilled}";
            }
        }

        // Guardar progreso (CONEXIÓN G1)
        void SaveProgress()
        {
            if (moneySystem != null)
            {
                moneySystem.SaveMoney();
                Debug.Log("💾 Progreso guardado");
            }

            // PLACEHOLDER: Aquí podrías guardar best score, etc.
            // PlayerPrefs.SetInt($"BestScore_{currentLevel}", bestScore);
        }

        // Formatear tiempo
        string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }

        // Animación simple de fade in
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
                elapsed += Time.unscaledDeltaTime; // unscaledDeltaTime porque el juego puede estar pausado
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        // BOTÓN: Volver a Level Selection (CONEXIÓN F2)
        public void BackToLevelSelection()
        {
            Debug.Log($"🔙 Volviendo a {levelSelectionSceneName}");

            // Asegurar que el tiempo esté corriendo
            Time.timeScale = 1f;

            // Cargar escena de Level Selection
            SceneManager.LoadScene(levelSelectionSceneName);
        }

        // BOTÓN: Reintentar nivel
        public void RetryLevel()
        {
            Debug.Log("🔄 Reintentando nivel");

            // Asegurar que el tiempo esté corriendo
            Time.timeScale = 1f;

            // Recargar la escena actual
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // MÉTODO PÚBLICO: Mostrar resultados con datos específicos (uso avanzado)
        public void ShowResultsWithData(int money, int total, float time, int enemies)
        {
            sessionMoney = money;
            totalMoney = total;
            levelTime = time;
            enemiesKilled = enemies;

            ShowResults();
        }

        // MÉTODO PARA TESTING
        [ContextMenu("Test Show Results")]
        public void TestShowResults()
        {
            ShowResultsWithData(150, 1000, 120f, 25);
        }

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