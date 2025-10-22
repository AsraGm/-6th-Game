using UnityEngine;

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

        [Tooltip("ARRASTRA AQUÍ tu ResultsScreen")]
        public ResultsScreen resultsScreen;

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
            FindSystems();

            if (levelTimer != null)
            {
                levelTimer.OnTimeUp += HandleTimeUp;
                levelTimer.OnWarning30Seconds += HandleWarning30;
                levelTimer.OnWarning10Seconds += HandleWarning10;
                levelTimer.OnTimerStarted += HandleTimerStarted;
            }

            if (waveSystem != null)
            {
                waveSystem.OnWaveStarted += HandleWaveStarted;
                waveSystem.OnWaveCompleted += HandleWaveCompleted;
                waveSystem.OnAllWavesCompleted += HandleAllWavesCompleted;
                waveSystem.OnEnemySpawned += HandleEnemySpawned;
                waveSystem.OnGameStarted += HandleGameStarted;
                waveSystem.OnFinalWave += HandleFinalWave;
            }
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
            if (resultsScreen == null)
            {
                resultsScreen = FindObjectOfType<ResultsScreen>();
            }
        }

        public void StartLevel()
        {
            if (StatsTracker.Instance != null)
            {
                string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                StatsTracker.Instance.StartLevelSession(currentSceneName);
            }

            if (levelTimer != null)
            {
                levelTimer.StartTimer();
            }

            if (waveSystem != null)
            {
                waveSystem.StartWaveSystem();
            }
        }

        public void PauseLevel()
        {
            if (levelTimer != null)
            {
                levelTimer.PauseTimer();
            }

            if (waveSystem != null)
            {
                waveSystem.PauseWaveSystem();
            }
        }

        public void ResumeLevel()
        {
            if (levelTimer != null)
            {
                levelTimer.ResumeTimer();
            }

            if (waveSystem != null)
            {
                waveSystem.ResumeWaveSystem();
            }
        }

        public void RestartLevel()
        {
            if (levelTimer != null)
            {
                levelTimer.ResetTimer();
            }

            if (waveSystem != null)
            {
                waveSystem.ResetWaveSystem();
            }

            if (moneySystem != null)
            {
                moneySystem.ResetSessionEarnings();
            }
            Invoke(nameof(StartLevel), 0.1f);
        }

        void HandleTimerStarted()
        {
        }

        void HandleWarning30()
        {
            if (showWaveMessages)
            {
            }
        }

        void HandleWarning10()
        {
            if (showWaveMessages)
            {
            }
        }

        void HandleTimeUp()
        {
            if (waveSystem != null)
            {
                waveSystem.StopWaveSystem();
            }

            TouchShootingSystem shootingSystem = FindObjectOfType<TouchShootingSystem>();
            if (shootingSystem != null)
            {
                shootingSystem.enabled = false;
            }
            if (moneySystem != null)
            {
                moneySystem.SaveMoney();
            }
            CalculateLevelResults();

            Invoke(nameof(ShowResults), 2f);
        }

        void HandleGameStarted()
        {
            if (showWaveMessages)
            {
            }
            // PLACEHOLDER: Efectos de UI para inicio de juego
            // UIManager.ShowMessage("¡INICIO DE JUEGO!");
        }

        void HandleWaveStarted(WaveData wave)
        {
            if (showWaveMessages)
            {
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
            if (levelTimer != null && levelTimer.IsRunning)
            {
            }
        }

        void HandleEnemySpawned(int totalSpawned)
        {
            if (totalSpawned % 10 == 0 && showWaveMessages)
            {
            }
        }

        void HandleFinalWave()
        {
            if (showWaveMessages)
            {
            }
            // PLACEHOLDER: Efectos especiales para wave final
            // UIManager.ShowFinalWaveMessage();
            // AudioManager.PlayFinalWaveMusic();
        }

        void CalculateLevelResults()
        {
            if (moneySystem != null)
            {
                int sessionEarnings = moneySystem.GetSessionEarnings();
                int totalMoney = moneySystem.GetCurrentMoney();
            }

            ScoreSystem scoreSystem = FindObjectOfType<ScoreSystem>();
            if (scoreSystem != null)
            {
                int finalScore = scoreSystem.GetCurrentScore();
                float accuracy = scoreSystem.GetAccuracy();
            }

            if (waveSystem != null)
            {
                int enemiesSpawned = waveSystem.GetTotalEnemiesSpawned();
                int currentWave = waveSystem.CurrentWaveIndex + 1;
                int totalWaves = waveSystem.TotalWaves;
                int activeEnemies = waveSystem.GetActiveEnemyCount();
            }
        }

        void ShowResults()
        {
            if (resultsScreen != null)
            {
                resultsScreen.ShowResults();
            }
            else
            {
            }
        }

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

        public void OnSkipWaveButtonClicked()
        {
            if (waveSystem != null && waveSystem.IsRunning)
            {
                waveSystem.SkipCurrentWave();
            }
        }

        public void OnForceSpawnButtonClicked()
        {
            if (waveSystem != null && waveSystem.CanSpawnMore())
            {
                waveSystem.ForceSpawnEnemy(EnemyType.Normal);
            }
        }
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

        void Update()
        {
            if (levelTimer != null && levelTimer.IsRunning && StatsTracker.Instance != null)
            {
                StatsTracker.Instance.UpdateSessionTime(levelTimer.LevelDuration - levelTimer.CurrentTime);
            }
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
                if (waveSystem != null)
                {
                    waveSystem.DebugLogWaveInfo();
                }
            }
#endif
        }

        void OnDestroy()
        {
            if (levelTimer != null)
            {
                levelTimer.OnTimeUp -= HandleTimeUp;
                levelTimer.OnWarning30Seconds -= HandleWarning30;
                levelTimer.OnWarning10Seconds -= HandleWarning10;
                levelTimer.OnTimerStarted -= HandleTimerStarted;
            }

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