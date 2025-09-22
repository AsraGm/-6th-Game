using UnityEngine;

// ARCHIVO: WaveIntegration.cs
// Conecta WaveSystem con TimerIntegration y otros sistemas (B3)
// COMPLETA LAS LÍNEAS COMENTADAS EN TimerIntegration

namespace ShootingRange
{
    public class WaveIntegration : MonoBehaviour
    {
        [Header("Referencias de Sistemas")]
        [Tooltip("ARRASTRA AQUÍ tu WaveSystem")]
        public WaveSystem waveSystem;

        [Tooltip("ARRASTRA AQUÍ tu TimerIntegration")]
        public TimerIntegration timerIntegration;

        [Tooltip("ARRASTRA AQUÍ tu MoneySystem")]
        public MoneySystem moneySystem;

        [Tooltip("ARRASTRA AQUÍ tu ScoreSystem")]
        public ScoreSystem scoreSystem;

        [Header("Configuración")]
        [Tooltip("Conectar automáticamente al inicializar")]
        public bool autoConnect = true;

        [Tooltip("Bonus de dinero por completar oleada")]
        [Range(0, 100)]
        public int waveCompletionBonus = 50;

        [Tooltip("Bonus de puntos por completar oleada")]
        [Range(0, 500)]
        public int waveCompletionScore = 100;

        void Start()
        {
            if (autoConnect)
            {
                ConnectSystems();
            }
        }

        void ConnectSystems()
        {
            FindSystems();
            ConnectWaveEvents();

            Debug.Log("WaveIntegration: Sistemas conectados");
        }

        void FindSystems()
        {
            if (waveSystem == null)
                waveSystem = FindObjectOfType<WaveSystem>();

            if (timerIntegration == null)
                timerIntegration = FindObjectOfType<TimerIntegration>();

            if (moneySystem == null)
                moneySystem = FindObjectOfType<MoneySystem>();

            if (scoreSystem == null)
                scoreSystem = FindObjectOfType<ScoreSystem>();
        }

        void ConnectWaveEvents()
        {
            if (waveSystem != null)
            {
                waveSystem.OnWaveStarted += HandleWaveStarted;
                waveSystem.OnWaveCompleted += HandleWaveCompleted;
                waveSystem.OnAllWavesCompleted += HandleAllWavesCompleted;
                waveSystem.OnEnemySpawned += HandleEnemySpawned;
            }
        }

        // MANEJADORES DE EVENTOS DE OLEADAS

        void HandleWaveStarted(SOWaveData waveData)
        {
            Debug.Log($"WaveIntegration: Oleada iniciada - {waveData.waveName}");

            // Aplicar multiplicadores de la oleada
            if (moneySystem != null && waveData.moneyMultiplier != 1f)
            {
                // Nota: Necesitarías agregar un método para multiplicadores temporales en MoneySystem
                Debug.Log($"Aplicado multiplicador de dinero: x{waveData.moneyMultiplier}");
            }

            if (scoreSystem != null && waveData.scoreMultiplier != 1f)
            {
                // Nota: Necesitarías agregar un método para multiplicadores temporales en ScoreSystem  
                Debug.Log($"Aplicado multiplicador de puntuación: x{waveData.scoreMultiplier}");
            }

            // PLACEHOLDER: Efectos visuales/sonoros de inicio de oleada
            // AudioManager.PlayWaveStartSound();
            // UIManager.ShowWaveStartNotification(waveData.waveName);
        }

        void HandleWaveCompleted(SOWaveData waveData)
        {
            Debug.Log($"WaveIntegration: Oleada completada - {waveData.waveName}");

            // Dar bonificaciones por completar oleada
            if (moneySystem != null && waveCompletionBonus > 0)
            {
                int bonus = Mathf.RoundToInt(waveCompletionBonus * waveData.moneyMultiplier);
                moneySystem.AddMoney(bonus, true);
                Debug.Log($"Bonus de dinero por oleada: +{bonus}");
            }

            if (scoreSystem != null && waveCompletionScore > 0)
            {
                int bonus = Mathf.RoundToInt(waveCompletionScore * waveData.scoreMultiplier);
                scoreSystem.AddScore(bonus, ObjectType.Enemy, EnemyType.Normal);
                Debug.Log($"Bonus de puntuación por oleada: +{bonus}");
            }

            // PLACEHOLDER: Efectos de oleada completada
            // AudioManager.PlayWaveCompleteSound();
            // UIManager.ShowWaveCompleteNotification();
            // ParticleManager.PlayWaveCompleteEffect();
        }

        void HandleAllWavesCompleted()
        {
            Debug.Log("WaveIntegration: ¡Todas las oleadas completadas!");

            // Bonus especial por completar todas las oleadas
            if (moneySystem != null)
            {
                int bigBonus = waveCompletionBonus * 3;
                moneySystem.AddMoney(bigBonus, true);
                Debug.Log($"GRAN BONUS por completar todas las oleadas: +{bigBonus}");
            }

            if (scoreSystem != null)
            {
                int bigBonus = waveCompletionScore * 3;
                scoreSystem.AddScore(bigBonus, ObjectType.Enemy, EnemyType.Valuable);
                Debug.Log($"GRAN BONUS de puntuación: +{bigBonus}");
            }

            // Notificar al TimerIntegration que el nivel está realmente completo
            if (timerIntegration != null)
            {
                // El timer podría seguir corriendo, pero las oleadas están completas
                Debug.Log("Nivel completado por oleadas - Timer puede seguir corriendo");
            }

            // PLACEHOLDER: Efectos épicos de victoria
            // AudioManager.PlayVictoryMusic();
            // UIManager.ShowAllWavesCompleteScreen();
            // ParticleManager.PlayVictoryExplosion();
        }

        void HandleEnemySpawned(EnemyType enemyType, Vector3 position)
        {
            Debug.Log($"WaveIntegration: Enemigo spawneado - {enemyType} en {position}");

            // PLACEHOLDER: Efectos de spawn
            // ParticleManager.PlaySpawnEffect(position);
            // AudioManager.PlaySpawnSound(enemyType);
        }

        // MÉTODOS PÚBLICOS PARA CONTROL EXTERNO

        public void ForceStartWaves()
        {
            if (waveSystem != null)
            {
                waveSystem.StartWaves();
            }
        }

        public void ForceStopWaves()
        {
            if (waveSystem != null)
            {
                waveSystem.StopWaveSystem();
            }
        }

        public void SetWaveCompletionBonuses(int moneyBonus, int scoreBonus)
        {
            waveCompletionBonus = moneyBonus;
            waveCompletionScore = scoreBonus;
        }

        // INFORMACIÓN PARA UI

        public int GetCurrentWaveIndex()
        {
            return waveSystem != null ? waveSystem.GetCurrentWaveIndex() : -1;
        }

        public int GetTotalWaveCount()
        {
            return waveSystem != null ? waveSystem.GetTotalWaveCount() : 0;
        }

        public float GetCurrentWaveProgress()
        {
            return waveSystem != null ? waveSystem.GetWaveProgress() : 0f;
        }

        public string GetCurrentWaveName()
        {
            SOWaveData currentWave = waveSystem?.GetCurrentWave();
            return currentWave != null ? currentWave.waveName : "Sin oleada";
        }

        public int GetActiveEnemyCount()
        {
            return waveSystem != null ? waveSystem.GetActiveEnemyCount() : 0;
        }

        public bool AreWavesRunning()
        {
            return waveSystem != null && waveSystem.IsWaveRunning();
        }

        // MÉTODOS DE DEBUG
        [ContextMenu("Force Start Waves")]
        public void DebugStartWaves()
        {
            ForceStartWaves();
        }

        [ContextMenu("Force Stop Waves")]
        public void DebugStopWaves()
        {
            ForceStopWaves();
        }

        [ContextMenu("Log Wave Status")]
        public void DebugLogWaveStatus()
        {
            Debug.Log($"Estado de oleadas:");
            Debug.Log($"- Oleada actual: {GetCurrentWaveIndex() + 1}/{GetTotalWaveCount()}");
            Debug.Log($"- Nombre: {GetCurrentWaveName()}");
            Debug.Log($"- Progreso: {GetCurrentWaveProgress():F2}");
            Debug.Log($"- Enemigos activos: {GetActiveEnemyCount()}");
            Debug.Log($"- Corriendo: {AreWavesRunning()}");

            // NUEVO: Info de prefabs configurados
            if (waveSystem != null)
            {
                var configuredTypes = waveSystem.GetConfiguredEnemyTypes();
                Debug.Log($"- Tipos de enemigos disponibles: {string.Join(", ", configuredTypes)}");
            }
        }

        [ContextMenu("Validate Wave System Prefabs")]
        public void DebugValidateWavePrefabs()
        {
            if (waveSystem != null)
            {
                waveSystem.DebugValidatePrefabs();
            }
            else
            {
                Debug.LogWarning("WaveSystem no está asignado");
            }
        }

        void OnDestroy()
        {
            // Desconectar eventos para evitar errores
            if (waveSystem != null)
            {
                waveSystem.OnWaveStarted -= HandleWaveStarted;
                waveSystem.OnWaveCompleted -= HandleWaveCompleted;
                waveSystem.OnAllWavesCompleted -= HandleAllWavesCompleted;
                waveSystem.OnEnemySpawned -= HandleEnemySpawned;
            }
        }
    }
}