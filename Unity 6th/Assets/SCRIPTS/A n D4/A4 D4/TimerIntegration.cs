using UnityEngine;

// ARCHIVO: TimerIntegration.cs - ACTUALIZADO PARA WAVE SYSTEM B3
// Este reemplaza tu TimerIntegration existente

namespace ShootingRange
{
    public class TimerIntegration : MonoBehaviour
    {
        [Header("Referencias de Sistemas")]
        [Tooltip("ARRASTRA AQUÍ tu LevelTimer")]
        public LevelTimer levelTimer;

        [Tooltip("ARRASTRA AQUÍ tu TimerDisplay")]
        public TimerDisplay timerDisplay;

        [Tooltip("ARRASTRA AQUÍ tu WaveSystem - NUEVO")]
        public WaveSystem waveSystem;

        [Tooltip("ARRASTRA AQUÍ tu MoneySystem")]
        public MoneySystem moneySystem;

        [Header("Configuración")]
        [Tooltip("Iniciar timer automáticamente al empezar el nivel")]
        public bool autoStartTimer = true;

        [Tooltip("Tiempo de delay antes de iniciar el timer (segundos)")]
        [Range(0f, 5f)]
        public float startDelay = 1f;

        [Header("Configuración de Wave UI")]
        [Tooltip("Mostrar mensajes de waves en consola")]
        public bool showWaveMessages = true;

        void Start()
        {
            if (autoStartTimer)
            {
                Invoke(nameof(StartLevel), startDelay);
            }

            ConnectSystems();
        }

        void ConnectSystems()
        {
            // Buscar sistemas automáticamente si no están asignados
            FindSystems();

            // Conectar eventos del timer
            if (levelTimer != null)
            {
                levelTimer.OnTimeUp += HandleTimeUp;
                levelTimer.OnWarning30Seconds += HandleWarning30;
                levelTimer.OnWarning10Seconds += HandleWarning10;
                levelTimer.OnTimerStarted += HandleTimerStarted;
            }

            // Conectar eventos del Wave System - ACTUALIZADO
            if (waveSystem != null)
            {
                waveSystem.OnWaveStarted += HandleWaveStarted;
                waveSystem.OnWaveCompleted += HandleWaveCompleted;
                waveSystem.OnAllWavesCompleted += HandleAllWavesCompleted;
                waveSystem.OnEnemySpawned += HandleEnemySpawned;
                waveSystem.OnGameStarted += HandleGameStarted;
                waveSystem.OnFinalWave += HandleFinalWave;
            }

            Debug.Log("TimerIntegration: Sistemas conectados con WaveSystem B3");
        }

        void FindSystems()
        {
            if (levelTimer == null)
            {
                levelTimer = FindObjectOfType<LevelTimer>();
            }

            if (timerDisplay == null)
            {
                timerDisplay = FindObjectOfType<TimerDisplay>();
            }

            if (waveSystem == null)
            {
                waveSystem = FindObjectOfType<WaveSystem>();
            }

            if (moneySystem == null)
            {
                moneySystem = FindObjectOfType<MoneySystem>();
            }
        }

        // MÉTODOS DE CONTROL PÚBLICO - MEJORADOS

        public void StartLevel()
        {
            if (levelTimer != null)
            {
                levelTimer.StartTimer();
            }

            // CONEXIÓN CON WAVE SYSTEM B3
            if (waveSystem != null)
            {
                waveSystem.StartWaveSystem();
            }

            Debug.Log("Nivel iniciado con WaveSystem B3");
        }

        public void PauseLevel()
        {
            if (levelTimer != null)
            {
                levelTimer.PauseTimer();
            }

            // Pausar wave system
            if (waveSystem != null)
            {
                waveSystem.PauseWaveSystem();
            }

            Debug.Log("Nivel pausado");
        }

        public void ResumeLevel()
        {
            if (levelTimer != null)
            {
                levelTimer.ResumeTimer();
            }

            // Resumir wave system
            if (waveSystem != null)
            {
                waveSystem.ResumeWaveSystem();
            }

            Debug.Log("Nivel resumido");
        }

        public void RestartLevel()
        {
            // Reset timer
            if (levelTimer != null)
            {
                levelTimer.ResetTimer();
            }

            // Reset wave system
            if (waveSystem != null)
            {
                waveSystem.ResetWaveSystem();
            }

            // Reset money de sesión
            if (moneySystem != null)
            {
                moneySystem.ResetSessionEarnings();
            }

            // Reiniciar después de un frame
            Invoke(nameof(StartLevel), 0.1f);

            Debug.Log("Nivel reiniciado con WaveSystem B3");
        }

        // MANEJADORES DE EVENTOS DEL TIMER - CONSERVADOS

        void HandleTimerStarted()
        {
            Debug.Log("TimerIntegration: Timer iniciado");
        }

        void HandleWarning30()
        {
            if (showWaveMessages)
            {
                Debug.Log("⚠️ ALERTA: 30 segundos restantes");
            }
        }

        void HandleWarning10()
        {
            if (showWaveMessages)
            {
                Debug.Log("🚨 ALERTA CRÍTICA: 10 segundos restantes");
            }
        }

        void HandleTimeUp()
        {
            Debug.Log("⏰ TimerIntegration: Tiempo agotado - Finalizando nivel");

            // DETENER WAVE SYSTEM
            if (waveSystem != null)
            {
                waveSystem.StopWaveSystem();
            }

            // Detener sistema de disparo
            TouchShootingSystem shootingSystem = FindObjectOfType<TouchShootingSystem>();
            if (shootingSystem != null)
            {
                shootingSystem.enabled = false;
            }
            if (moneySystem != null)
            {
                moneySystem.SaveMoney();
                Debug.Log($"💾 Nivel completado - Dinero guardado: ${moneySystem.GetCurrentMoney()}");
            }
            // Calcular resultados finales
            CalculateLevelResults();

            // Mostrar pantalla de resultados después de delay
            Invoke(nameof(ShowResults), 2f);
        }

        // MANEJADORES DE EVENTOS DEL WAVE SYSTEM - NUEVOS

        void HandleGameStarted()
        {
            if (showWaveMessages)
            {
                Debug.Log("🎮 ¡INICIO DE JUEGO! - Primera oleada comenzando");
            }

            // PLACEHOLDER: Efectos de UI para inicio de juego
            // UIManager.ShowMessage("¡INICIO DE JUEGO!");
        }

        void HandleWaveStarted(WaveData wave)
        {
            if (showWaveMessages)
            {
                Debug.Log($"🌊 Wave iniciada: {wave.waveName} ({waveSystem.CurrentWaveIndex + 1}/{waveSystem.TotalWaves})");
            }

            // PLACEHOLDER: Efectos visuales/auditivos para nueva wave
            // UIManager.ShowWaveMessage($"Wave: {wave.waveName}");
            // AudioManager.PlayWaveStartSound();
        }

        void HandleWaveCompleted(WaveData wave)
        {
            if (showWaveMessages)
            {
                Debug.Log($"✅ Wave completada: {wave.waveName}");
            }

            // PLACEHOLDER: Efectos de wave completada
            // AudioManager.PlayWaveCompleteSound();
            // UIManager.ShowWaveCompleteEffect();
        }

        void HandleAllWavesCompleted()
        {
            Debug.Log("🏆 ¡TODAS LAS WAVES COMPLETADAS!");

            // Opcional: Completar el nivel anticipadamente
            if (levelTimer != null && levelTimer.IsRunning)
            {
                // Podrías terminar el nivel aquí o dejarlo continuar
                Debug.Log("Waves completadas pero timer aún corriendo");
            }
        }

        void HandleEnemySpawned(int totalSpawned)
        {
            // Solo para estadísticas, no hacer nada especial aquí
            if (totalSpawned % 10 == 0 && showWaveMessages)
            {
                Debug.Log($"📊 Enemigos spawneados: {totalSpawned}");
            }
        }

        void HandleFinalWave()
        {
            if (showWaveMessages)
            {
                Debug.Log("🔥 ¡CERCA DE TERMINAR! - Oleada final comenzando");
            }

            // PLACEHOLDER: Efectos especiales para wave final
            // UIManager.ShowFinalWaveMessage();
            // AudioManager.PlayFinalWaveMusic();
        }

        void CalculateLevelResults()
        {
            Debug.Log("📊 Calculando resultados del nivel...");

            // CONEXIÓN CON MONEY SYSTEM - CONSERVADO
            if (moneySystem != null)
            {
                int sessionEarnings = moneySystem.GetSessionEarnings();
                int totalMoney = moneySystem.GetCurrentMoney();

                Debug.Log($"💰 Dinero ganado esta sesión: ${sessionEarnings}");
                Debug.Log($"💰 Dinero total: ${totalMoney}");
            }

            // CONEXIÓN CON SCORE SYSTEM - CONSERVADO
            ScoreSystem scoreSystem = FindObjectOfType<ScoreSystem>();
            if (scoreSystem != null)
            {
                int finalScore = scoreSystem.GetCurrentScore();
                float accuracy = scoreSystem.GetAccuracy();

                Debug.Log($"🎯 Puntuación final: {finalScore}");
                Debug.Log($"🎯 Precisión: {accuracy:F1}%");
            }

            // CONEXIÓN CON WAVE SYSTEM PARA ESTADÍSTICAS - NUEVO
            if (waveSystem != null)
            {
                int enemiesSpawned = waveSystem.GetTotalEnemiesSpawned();
                int currentWave = waveSystem.CurrentWaveIndex + 1;
                int totalWaves = waveSystem.TotalWaves;
                int activeEnemies = waveSystem.GetActiveEnemyCount();

                Debug.Log($"👾 Enemigos spawneados: {enemiesSpawned}");
                Debug.Log($"🌊 Waves completadas: {currentWave}/{totalWaves}");
                Debug.Log($"👾 Enemigos activos restantes: {activeEnemies}");
            }
        }

        void ShowResults()
        {
            Debug.Log("🏁 Mostrando pantalla de resultados");

            // PLACEHOLDER: Conexión con Results Screen (Lista D6)
            // ResultsScreen resultsScreen = FindObjectOfType<ResultsScreen>();
            // if (resultsScreen != null)
            // {
            //     resultsScreen.ShowResults();
            // }

            Debug.Log("=== NIVEL COMPLETADO ===");
            Debug.Log("Presiona R para reiniciar el nivel");
        }

        // MÉTODOS PÚBLICOS PARA UI - MEJORADOS

        public void OnRestartButtonClicked()
        {
            RestartLevel();
        }

        public void OnPauseButtonClicked()
        {
            if (levelTimer != null && levelTimer.IsRunning)
            {
                PauseLevel();
            }
            else
            {
                ResumeLevel();
            }
        }

        public void OnBackToMenuClicked()
        {
            Debug.Log("Volviendo al menú principal");
            // SceneManager.LoadScene("MainMenu");
        }

        // MÉTODOS ESPECÍFICOS PARA WAVE SYSTEM - NUEVOS

        public void OnSkipWaveButtonClicked()
        {
            if (waveSystem != null && waveSystem.IsRunning)
            {
                waveSystem.SkipCurrentWave();
                Debug.Log("Wave saltada manualmente");
            }
        }

        public void OnForceSpawnButtonClicked()
        {
            if (waveSystem != null && waveSystem.CanSpawnMore())
            {
                waveSystem.ForceSpawnEnemy(EnemyType.Normal);
                Debug.Log("Enemigo spawneado forzadamente");
            }
        }

        // MÉTODOS DE CONFIGURACIÓN - MEJORADOS

        public void SetLevelDuration(float seconds)
        {
            if (levelTimer != null)
            {
                levelTimer.SetDuration(seconds);
            }
        }

        public float GetRemainingTime()
        {
            return levelTimer != null ? levelTimer.CurrentTime : 0f;
        }

        public bool IsLevelActive()
        {
            return levelTimer != null && levelTimer.IsRunning;
        }

        public bool IsWaveSystemActive()
        {
            return waveSystem != null && waveSystem.IsRunning;
        }

        public int GetCurrentWave()
        {
            return waveSystem != null ? waveSystem.CurrentWaveIndex + 1 : 0;
        }

        public int GetTotalWaves()
        {
            return waveSystem != null ? waveSystem.TotalWaves : 0;
        }

        public int GetActiveEnemyCount()
        {
            return waveSystem != null ? waveSystem.GetActiveEnemyCount() : 0;
        }

        public WaveData GetCurrentWaveData()
        {
            return waveSystem != null ? waveSystem.CurrentWave : null;
        }

        // INPUT PARA TESTING - MEJORADO
        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (IsLevelActive())
                    PauseLevel();
                else
                    ResumeLevel();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartLevel();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                if (levelTimer != null)
                {
                    levelTimer.DebugAdd30Seconds();
                }
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                if (waveSystem != null && waveSystem.IsRunning)
                {
                    waveSystem.SkipCurrentWave();
                }
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (waveSystem != null)
                {
                    waveSystem.ForceSpawnEnemy(EnemyType.Normal);
                }
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (waveSystem != null)
                {
                    waveSystem.ForceSpawnEnemy(EnemyType.ZigZag);
                }
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                // Info debug
                if (waveSystem != null)
                {
                    waveSystem.DebugLogWaveInfo();
                }
            }
#endif
        }

        void OnDestroy()
        {
            // Desconectar eventos del timer
            if (levelTimer != null)
            {
                levelTimer.OnTimeUp -= HandleTimeUp;
                levelTimer.OnWarning30Seconds -= HandleWarning30;
                levelTimer.OnWarning10Seconds -= HandleWarning10;
                levelTimer.OnTimerStarted -= HandleTimerStarted;
            }

            // Desconectar eventos del wave system - NUEVO
            if (waveSystem != null)
            {
                waveSystem.OnWaveStarted -= HandleWaveStarted;
                waveSystem.OnWaveCompleted -= HandleWaveCompleted;
                waveSystem.OnAllWavesCompleted -= HandleAllWavesCompleted;
                waveSystem.OnEnemySpawned -= HandleEnemySpawned;
                waveSystem.OnGameStarted -= HandleGameStarted;
                waveSystem.OnFinalWave -= HandleFinalWave;
            }
        }
    }
}