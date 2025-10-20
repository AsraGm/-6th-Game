using UnityEngine;
using System.Collections.Generic;

namespace ShootingRange
{
    /// <summary>
    /// Lista G2: Tracking básico de estadísticas
    /// Solo tracking de dinero total y mejor puntaje por nivel
    /// Sin analytics complejos - datos mínimos para funcionamiento básico
    /// Conexión con D6 (Results display)
    /// </summary>
    public class StatsTracker : MonoBehaviour
    {
        public static StatsTracker Instance { get; private set; }

        [Header("Estadísticas de Sesión Actual")]
        [SerializeField] private int sessionMoneyEarned = 0;
        [SerializeField] private int sessionEnemiesKilled = 0;
        [SerializeField] private float sessionTimeSpent = 0f;

        [Header("Debug Info")]
        [SerializeField] private string currentLevelID = "";
        [SerializeField] private bool isTrackingSession = false;

        // Eventos para notificar cambios
        public System.Action<int> OnMoneyEarnedChanged;
        public System.Action<int> OnEnemiesKilledChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        #region Session Tracking

        /// <summary>
        /// Inicia el tracking de una nueva sesión de nivel
        /// Llamar al inicio del nivel
        /// </summary>
        public void StartLevelSession(string levelID)
        {
            currentLevelID = levelID;
            sessionMoneyEarned = 0;
            sessionEnemiesKilled = 0;
            sessionTimeSpent = 0f;
            isTrackingSession = true;

            Debug.Log($"[StatsTracker] Sesión iniciada para nivel: {levelID}");
        }

        /// <summary>
        /// Detiene el tracking de la sesión actual
        /// </summary>
        public void EndLevelSession()
        {
            isTrackingSession = false;
            Debug.Log($"[StatsTracker] Sesión terminada para nivel: {currentLevelID}");
        }

        /// <summary>
        /// Registra dinero ganado en la sesión actual
        /// </summary>
        public void AddSessionMoney(int amount)
        {
            if (!isTrackingSession) return;

            sessionMoneyEarned += amount;
            OnMoneyEarnedChanged?.Invoke(sessionMoneyEarned);
        }

        /// <summary>
        /// Registra enemigo eliminado en la sesión actual
        /// </summary>
        public void AddEnemyKilled()
        {
            if (!isTrackingSession) return;

            sessionEnemiesKilled++;
            OnEnemiesKilledChanged?.Invoke(sessionEnemiesKilled);
        }

        /// <summary>
        /// Actualiza el tiempo jugado (llamar desde LevelTimer)
        /// </summary>
        public void UpdateSessionTime(float timeSpent)
        {
            if (!isTrackingSession) return;
            sessionTimeSpent = timeSpent;
        }

        #endregion

        #region Level Completion

        /// <summary>
        /// Completa el nivel y guarda las estadísticas
        /// CONEXIÓN G1: Usa SaveSystem para persistencia
        /// CONEXIÓN D6: Provee datos para ResultsScreen
        /// </summary>
        public LevelStats CompleteLevelAndSave()
        {
            if (string.IsNullOrEmpty(currentLevelID))
            {
                Debug.LogWarning("[StatsTracker] No hay nivel activo para completar");
                return null;
            }

            // Calcular score final (en este sistema simple, score = dinero)
            int finalScore = sessionMoneyEarned;

            // Crear objeto de estadísticas
            LevelStats stats = new LevelStats
            {
                levelID = currentLevelID,
                moneyEarned = sessionMoneyEarned,
                enemiesKilled = sessionEnemiesKilled,
                timeSpent = sessionTimeSpent,
                finalScore = finalScore
            };

            // Guardar en SaveSystem (G1)
            SaveSystem.Instance.SaveLevelCompletion(
                currentLevelID,
                sessionMoneyEarned,
                finalScore
            );

            // Verificar si es nuevo récord
            bool isNewRecord = SaveSystem.Instance.IsNewBestScore(currentLevelID, finalScore);
            stats.isNewBestScore = isNewRecord;

            Debug.Log($"[StatsTracker] Nivel completado - Money: {sessionMoneyEarned}, Score: {finalScore}, Récord: {isNewRecord}");

            // Terminar sesión
            EndLevelSession();

            return stats;
        }

        #endregion

        #region Get Session Data (para D6 - Results Display)

        /// <summary>
        /// Obtiene el dinero ganado en la sesión actual
        /// </summary>
        public int GetSessionMoney()
        {
            return sessionMoneyEarned;
        }

        /// <summary>
        /// Obtiene enemigos eliminados en la sesión actual
        /// </summary>
        public int GetSessionEnemiesKilled()
        {
            return sessionEnemiesKilled;
        }

        /// <summary>
        /// Obtiene el tiempo de la sesión actual
        /// </summary>
        public float GetSessionTime()
        {
            return sessionTimeSpent;
        }

        /// <summary>
        /// Obtiene el ID del nivel actual
        /// </summary>
        public string GetCurrentLevelID()
        {
            return currentLevelID;
        }

        #endregion

        #region Best Score Queries

        /// <summary>
        /// Obtiene el mejor score de un nivel específico
        /// </summary>
        public int GetBestScore(string levelID)
        {
            return SaveSystem.Instance.LoadLevelBestScore(levelID);
        }

        /// <summary>
        /// Obtiene el mejor score del nivel actual
        /// </summary>
        public int GetCurrentLevelBestScore()
        {
            if (string.IsNullOrEmpty(currentLevelID))
                return 0;

            return GetBestScore(currentLevelID);
        }

        #endregion

        #region Global Stats

        /// <summary>
        /// Obtiene el dinero total acumulado
        /// </summary>
        public int GetTotalMoney()
        {
            return SaveSystem.Instance.LoadTotalMoney();
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current Session")]
        public void LogCurrentSession()
        {
            Debug.Log("=== SESIÓN ACTUAL ===");
            Debug.Log($"Level: {currentLevelID}");
            Debug.Log($"Money Earned: ${sessionMoneyEarned}");
            Debug.Log($"Enemies Killed: {sessionEnemiesKilled}");
            Debug.Log($"Time Spent: {sessionTimeSpent:F1}s");
            Debug.Log($"Tracking: {isTrackingSession}");
            Debug.Log("==================");
        }

        [ContextMenu("Simulate Level Completion")]
        public void DebugSimulateLevelCompletion()
        {
            if (!isTrackingSession)
            {
                Debug.LogWarning("No hay sesión activa. Iniciando sesión de prueba...");
                StartLevelSession("Level_Debug");
                sessionMoneyEarned = Random.Range(100, 500);
                sessionEnemiesKilled = Random.Range(10, 30);
                sessionTimeSpent = Random.Range(30f, 120f);
            }

            LevelStats stats = CompleteLevelAndSave();
            if (stats != null)
            {
                Debug.Log($"Nivel completado exitosamente: {stats.ToString()}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Clase simple para almacenar estadísticas de un nivel completado
    /// Usada para pasar datos a ResultsScreen (D6)
    /// </summary>
    [System.Serializable]
    public class LevelStats
    {
        public string levelID;
        public int moneyEarned;
        public int enemiesKilled;
        public float timeSpent;
        public int finalScore;
        public bool isNewBestScore;

        public override string ToString()
        {
            return $"Level: {levelID}, Money: ${moneyEarned}, Enemies: {enemiesKilled}, Time: {timeSpent:F1}s, Score: {finalScore}, New Record: {isNewBestScore}";
        }
    }
}