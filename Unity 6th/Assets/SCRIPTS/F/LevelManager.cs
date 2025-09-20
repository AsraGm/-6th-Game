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
                Debug.LogError("No levels configured in LevelManager!");
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
                Debug.LogError($"Level index {levelIndex} is out of range!");
                return;
            }
            
            currentLevelIndex = levelIndex;
            currentLevelData = allLevels[levelIndex];
            
            Debug.Log($"Loading Level {currentLevelData.levelNumber}: {currentLevelData.levelName}");
            
            // Configurar sistemas existentes con los datos del nivel
            ConfigureSystemsForLevel(currentLevelData);
            
            // Notificar a otros sistemas
            OnLevelLoaded?.Invoke(currentLevelData);
            OnLevelChanged?.Invoke(currentLevelIndex);
            OnLevelTimeChanged?.Invoke(currentLevelData.levelDuration);
            OnSpawnConfigChanged?.Invoke(currentLevelData.allowedSpawnTypes, currentLevelData.baseSpawnRate);
        }
        
        void ConfigureSystemsForLevel(SOLevelData levelData)
        {
            // Resetear sistemas para el nuevo nivel
            if (scoreSystem != null)
            {
                scoreSystem.ResetScore();
                Debug.Log($"ScoreSystem configurado para nivel {levelData.levelNumber}");
            }
            
            if (moneySystem != null)
            {
                moneySystem.ResetSessionEarnings();
                Debug.Log($"MoneySystem configurado para nivel {levelData.levelNumber}");
            }
            
            // El timer y wave system se configurarán via eventos
            Debug.Log($"Level {levelData.levelNumber} configured - Duration: {levelData.levelDuration}s, Spawn Types: {levelData.allowedSpawnTypes.Count}");
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
                Debug.Log("All levels completed!");
                OnAllLevelsCompleted?.Invoke();
                // Podríamos volver al menú principal o mostrar pantalla de victoria
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
            Debug.LogError($"Level number {levelNumber} not found!");
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
                Debug.Log($"Current Level: {currentLevelData.levelName} | Duration: {currentLevelData.levelDuration}s | Spawn Types: {currentLevelData.allowedSpawnTypes.Count}");
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
