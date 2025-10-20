using UnityEngine;
using System.Collections.Generic;

namespace ShootingRange
{
    public class LevelManager : MonoBehaviour
    {
        [Header("Level Configuration")]
        [SerializeField] private List<SOLevelData> allLevels = new List<SOLevelData>();
        [SerializeField] private int currentLevelIndex = 0;

        [Header("System References - CONECTA CON TUS SISTEMAS")]
        [Tooltip("Se conectará automáticamente si no se asigna")]
        public MoneySystem moneySystem;

        [Tooltip("Se conectará automáticamente si no se asigna")]
        public ScoreSystem scoreSystem;

        [Tooltip("Se conectará automáticamente si no se asigna")]
        public TargetDetectionSystem targetDetectionSystem;

        // Referencias
        private SOLevelData currentLevelData;
        private bool isLevelActive = false;

        // Events para comunicación con otros sistemas
        public static System.Action<SOLevelData> OnLevelLoaded;
        public static System.Action<int> OnLevelChanged;
        public static System.Action OnAllLevelsCompleted;

        // Events específicos para conectar con tus sistemas
        public static System.Action<float> OnLevelTimeChanged; // Para A4 Timer
        public static System.Action<List<EnemyType>, float> OnSpawnConfigChanged; // Para B3 Wave System

        // Properties públicas
        public SOLevelData CurrentLevel => currentLevelData;
        public int CurrentLevelIndex => currentLevelIndex;
        public int TotalLevels => allLevels.Count;
        public bool HasNextLevel => currentLevelIndex < allLevels.Count - 1;
        public bool HasPreviousLevel => currentLevelIndex > 0;
        public bool IsLevelActive => isLevelActive;

        private void Awake()
        {
            // Encontrar sistemas existentes
            FindExistingSystems();

            // Cargar el primer nivel al iniciar
            if (allLevels.Count > 0)
            {
                LoadLevel(0);
            }
            else
            {
                Debug.LogError("[LevelManager] No levels configured in LevelManager!");
            }
        }

        void FindExistingSystems()
        {
            if (moneySystem == null)
                moneySystem = FindObjectOfType<MoneySystem>();

            if (scoreSystem == null)
                scoreSystem = FindObjectOfType<ScoreSystem>();

            if (targetDetectionSystem == null)
                targetDetectionSystem = FindObjectOfType<TargetDetectionSystem>();
        }

        /// <summary>
        /// Carga un nivel específico por índice
        /// </summary>
        public void LoadLevel(int levelIndex)
        {
            // Validación de índice
            if (levelIndex < 0 || levelIndex >= allLevels.Count)
            {
                Debug.LogError($"[LevelManager] Level index {levelIndex} is out of range!");
                return;
            }

            currentLevelIndex = levelIndex;
            currentLevelData = allLevels[levelIndex];

            Debug.Log($"[LevelManager] Loading Level {currentLevelData.levelNumber}: {currentLevelData.levelName} (ID: {currentLevelData.levelID})");

            // NUEVO: Inicializar StatsTracker con el levelID del ScriptableObject
            InitializeStatsTracking();

            // Configurar sistemas existentes con los datos del nivel
            ConfigureSystemsForLevel(currentLevelData);

            // Marcar nivel como activo
            isLevelActive = true;

            // Notificar a otros sistemas
            OnLevelLoaded?.Invoke(currentLevelData);
            OnLevelChanged?.Invoke(currentLevelIndex);
            OnLevelTimeChanged?.Invoke(currentLevelData.levelDuration);
            OnSpawnConfigChanged?.Invoke(currentLevelData.allowedSpawnTypes, currentLevelData.baseSpawnRate);
        }

        /// <summary>
        /// NUEVO: Inicializa el tracking de estadísticas para el nivel actual
        /// Conexión con G2 (StatsTracker)
        /// </summary>
        private void InitializeStatsTracking()
        {
            if (StatsTracker.Instance == null)
            {
                Debug.LogWarning("[LevelManager] StatsTracker no encontrado en la escena!");
                return;
            }

            if (currentLevelData == null)
            {
                Debug.LogError("[LevelManager] No hay levelData actual para inicializar tracking!");
                return;
            }

            // Validar que el levelID no esté vacío
            if (string.IsNullOrEmpty(currentLevelData.levelID))
            {
                Debug.LogError($"[LevelManager] El nivel '{currentLevelData.levelName}' no tiene levelID asignado!");
                return;
            }

            // Iniciar sesión de tracking
            StatsTracker.Instance.StartLevelSession(currentLevelData.levelID);

            Debug.Log($"[LevelManager] StatsTracker inicializado para nivel: {currentLevelData.levelID}");
        }

        void ConfigureSystemsForLevel(SOLevelData levelData)
        {
            // Resetear sistemas para el nuevo nivel
            if (scoreSystem != null)
            {
                scoreSystem.ResetScore();
                Debug.Log($"[LevelManager] ScoreSystem configurado para nivel {levelData.levelNumber}");
            }

            if (moneySystem != null)
            {
                moneySystem.ResetSessionEarnings();
                Debug.Log($"[LevelManager] MoneySystem configurado para nivel {levelData.levelNumber}");
            }

            // El timer y wave system se configurarán via eventos
            Debug.Log($"[LevelManager] Level {levelData.levelNumber} configured - Duration: {levelData.levelDuration}s, Spawn Types: {levelData.allowedSpawnTypes.Count}");
        }

        /// <summary>
        /// NUEVO: Completa el nivel actual y obtiene las estadísticas
        /// Llamar cuando el timer llegue a 0 o se complete el nivel
        /// Conexión con G2 (StatsTracker) y D6 (ResultsScreen)
        /// </summary>
        public LevelStats CompleteLevel()
        {
            if (!isLevelActive)
            {
                Debug.LogWarning("[LevelManager] No hay nivel activo para completar!");
                return null;
            }

            if (StatsTracker.Instance == null)
            {
                Debug.LogError("[LevelManager] No se puede completar el nivel: StatsTracker no encontrado!");
                return null;
            }

            // Marcar nivel como no activo
            isLevelActive = false;

            // Obtener estadísticas del nivel completado
            LevelStats stats = StatsTracker.Instance.CompleteLevelAndSave();

            if (stats != null)
            {
                Debug.Log($"[LevelManager] Nivel completado exitosamente:");
                Debug.Log($"  - Level: {stats.levelID}");
                Debug.Log($"  - Money: ${stats.moneyEarned}");
                Debug.Log($"  - Enemies: {stats.enemiesKilled}");
                Debug.Log($"  - Time: {stats.timeSpent:F1}s");
                Debug.Log($"  - Score: {stats.finalScore}");
                Debug.Log($"  - New Record: {stats.isNewBestScore}");

                // Aquí puedes mostrar la pantalla de resultados (D6)
                // ShowResultsScreen(stats);
            }

            return stats;
        }

        /// <summary>
        /// Avanza al siguiente nivel
        /// </summary>
        public void NextLevel()
        {
            if (HasNextLevel)
            {
                LoadLevel(currentLevelIndex + 1);
            }
            else
            {
                Debug.Log("[LevelManager] All levels completed!");
                OnAllLevelsCompleted?.Invoke();
                HandleAllLevelsCompleted();
            }
        }

        void HandleAllLevelsCompleted()
        {
            // PLACEHOLDER: Conectar con sistema de menús/navegación
            // Podrías cargar la escena de victoria o volver al menú principal

            MAINMENU mainMenu = FindObjectOfType<MAINMENU>();
            if (mainMenu != null)
            {
                // Volver al menú principal después de un delay
                Invoke(nameof(ReturnToMainMenu), 2f);
            }
        }

        void ReturnToMainMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MAIN MENU");
        }

        /// <summary>
        /// Retrocede al nivel anterior
        /// </summary>
        public void PreviousLevel()
        {
            if (HasPreviousLevel)
            {
                LoadLevel(currentLevelIndex - 1);
            }
        }

        /// <summary>
        /// Reinicia el nivel actual
        /// </summary>
        public void RestartCurrentLevel()
        {
            LoadLevel(currentLevelIndex);
        }

        /// <summary>
        /// Carga un nivel específico por su número
        /// </summary>
        public void LoadLevelByNumber(int levelNumber)
        {
            for (int i = 0; i < allLevels.Count; i++)
            {
                if (allLevels[i].levelNumber == levelNumber)
                {
                    LoadLevel(i);
                    return;
                }
            }
            Debug.LogError($"[LevelManager] Level number {levelNumber} not found!");
        }

        /// <summary>
        /// Obtiene información de un nivel sin cargarlo
        /// </summary>
        public SOLevelData GetLevelData(int levelIndex)
        {
            if (levelIndex >= 0 && levelIndex < allLevels.Count)
            {
                return allLevels[levelIndex];
            }
            return null;
        }

        /// <summary>
        /// Obtiene todos los datos de niveles
        /// </summary>
        public List<SOLevelData> GetAllLevels()
        {
            return new List<SOLevelData>(allLevels);
        }

        // Métodos para que otros sistemas obtengan datos del nivel actual
        public float GetCurrentLevelDuration() => currentLevelData?.levelDuration ?? 60f;
        public List<EnemyType> GetCurrentLevelSpawnTypes() => currentLevelData?.allowedSpawnTypes ?? new List<EnemyType>();
        public float GetCurrentLevelSpawnRate() => currentLevelData?.baseSpawnRate ?? 2f;
        public float GetCurrentLevelDifficulty() => currentLevelData?.difficultyMultiplier ?? 1f;

        // Métodos para debugging/testing
#if UNITY_EDITOR
        [ContextMenu("Load Next Level")]
        private void DebugNextLevel()
        {
            NextLevel();
        }
        
        [ContextMenu("Load Previous Level")]
        private void DebugPreviousLevel()
        {
            PreviousLevel();
        }
        
        [ContextMenu("Restart Level")]
        private void DebugRestartLevel()
        {
            RestartCurrentLevel();
        }
        
        [ContextMenu("Log Current Level Info")]
        private void DebugLogLevelInfo()
        {
            if (currentLevelData != null)
            {
                Debug.Log($"[LevelManager] Current Level: {currentLevelData.levelName} (ID: {currentLevelData.levelID}) | Duration: {currentLevelData.levelDuration}s | Spawn Types: {currentLevelData.allowedSpawnTypes.Count}");
            }
        }
        
        [ContextMenu("Complete Current Level (Test)")]
        private void DebugCompleteLevel()
        {
            LevelStats stats = CompleteLevel();
            if (stats != null)
            {
                Debug.Log($"[LevelManager] Test completion: {stats.ToString()}");
            }
        }
#endif
    }

    // Clase helper para facilitar acceso desde otros scripts - MEJORADA
    public static class LevelHelper
    {
        private static LevelManager _instance;

        public static LevelManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Object.FindObjectOfType<LevelManager>();
                return _instance;
            }
        }

        public static SOLevelData CurrentLevel => Instance?.CurrentLevel;
        public static int CurrentLevelIndex => Instance?.CurrentLevelIndex ?? 0;
        public static float CurrentLevelDuration => Instance?.GetCurrentLevelDuration() ?? 60f;
        public static List<EnemyType> CurrentSpawnTypes => Instance?.GetCurrentLevelSpawnTypes() ?? new List<EnemyType>();
        public static float CurrentSpawnRate => Instance?.GetCurrentLevelSpawnRate() ?? 2f;
    }
}