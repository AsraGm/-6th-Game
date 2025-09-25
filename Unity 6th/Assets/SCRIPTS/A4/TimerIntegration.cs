using UnityEngine;



namespace ShootingRange

{

    public class TimerIntegration : MonoBehaviour

    {

        [Header("Referencias de Sistemas")]

        [Tooltip("ARRASTRA AQUÃ tu LevelTimer")]

        public LevelTimer levelTimer;



        [Tooltip("ARRASTRA AQUÃ tu TimerDisplay")]

        public TimerDisplay timerDisplay;



        //[Tooltip("ARRASTRA AQUÃ tu WaveSystem (Lista B3) - opcional")]

        //public WaveSystem waveSystem;



        [Tooltip("ARRASTRA AQUÃ tu MoneySystem")]

        public MoneySystem moneySystem;



        [Header("ConfiguraciÃ³n")]

        [Tooltip("Iniciar timer automÃ¡ticamente al empezar el nivel")]

        public bool autoStartTimer = true;



        [Tooltip("Tiempo de delay antes de iniciar el timer (segundos)")]

        [Range(0f, 5f)]

        public float startDelay = 1f;



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

            // Buscar sistemas automÃ¡ticamente si no estÃ¡n asignados

            FindSystems();



            // Conectar eventos del timer

            if (levelTimer != null)

            {

                levelTimer.OnTimeUp += HandleTimeUp;

                levelTimer.OnWarning30Seconds += HandleWarning30;

                levelTimer.OnWarning10Seconds += HandleWarning10;

                levelTimer.OnTimerStarted += HandleTimerStarted;

            }



            Debug.Log("TimerIntegration: Sistemas conectados");

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



            //if (waveSystem == null)

            //{

            //    waveSystem = FindObjectOfType<WaveSystem>();

            //}



            if (moneySystem == null)

            {

                moneySystem = FindObjectOfType<MoneySystem>();

            }

        }



        // MÃ‰TODOS DE CONTROL PÃšBLICO



        public void StartLevel()

        {

            if (levelTimer != null)

            {

                levelTimer.StartTimer();

            }



            // CONEXIÃ“N CON WAVE SYSTEM (Lista B3)

            //if (waveSystem != null)

            //{

            //    waveSystem.StartWaves();

            //}



            Debug.Log("Nivel iniciado");

        }



        public void PauseLevel()

        {

            if (levelTimer != null)

            {

                levelTimer.PauseTimer();

            }



            // Pausar wave system si existe

            //if (waveSystem != null)

            //{

            //    waveSystem.PauseWaves();

            //}



            Debug.Log("Nivel pausado");

        }



        public void ResumeLevel()

        {

            if (levelTimer != null)

            {

                levelTimer.ResumeTimer();

            }



            // Resumir wave system si existe

            //if (waveSystem != null)

            //{

            //    waveSystem.ResumeWaves();

            //}



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

            //if (waveSystem != null)

            //{

            //    waveSystem.ResetWaves();

            //}



            // Reset money de sesiÃ³n (opcional)

            if (moneySystem != null)

            {

                moneySystem.ResetSessionEarnings();

            }



            // Reiniciar despuÃ©s de un frame

            Invoke(nameof(StartLevel), 0.1f);



            Debug.Log("Nivel reiniciado");

        }



        // MANEJADORES DE EVENTOS DEL TIMER



        void HandleTimerStarted()
        {
            Debug.Log("TimerIntegration: Timer iniciado");
        }



        void HandleWarning30()

        {
            Debug.Log("TimerIntegration: Warning 30 segundos");
        }



        void HandleWarning10()

        {

            Debug.Log("TimerIntegration: Warning 10 segundos");



            // CONEXIÃ“N FUTURA: Efectos mÃ¡s intensos

            // AudioManager audioManager = FindObjectOfType<AudioManager>();

            // if (audioManager != null) audioManager.PlayWarning10Sound();

        }



        void HandleTimeUp()

        {

            Debug.Log("TimerIntegration: Tiempo agotado - Finalizando nivel");



            // DETENER TODOS LOS SISTEMAS



            // 1. Detener wave system (Lista B3)

            //if (waveSystem != null)

            //{

            //    waveSystem.StopWaveSystem();

            //}



            // 2. Detener sistema de disparo

            TouchShootingSystem shootingSystem = FindObjectOfType<TouchShootingSystem>();

            if (shootingSystem != null)

            {

                shootingSystem.enabled = false; // Desactivar disparos

            }



            // 3. Calcular resultados finales

            CalculateLevelResults();



            // 4. Mostrar pantalla de resultados despuÃ©s de delay

            Invoke(nameof(ShowResults), 2f);

        }



        void CalculateLevelResults()

        {

            Debug.Log("Calculando resultados del nivel...");



            // CONEXIÃ“N CON MONEY SYSTEM

            if (moneySystem != null)

            {

                int sessionEarnings = moneySystem.GetSessionEarnings();

                int totalMoney = moneySystem.GetCurrentMoney();



                Debug.Log($"Dinero ganado esta sesiÃ³n: ${sessionEarnings}");

                Debug.Log($"Dinero total: ${totalMoney}");

            }



            // CONEXIÃ“N FUTURA CON SCORE SYSTEM

            ScoreSystem scoreSystem = FindObjectOfType<ScoreSystem>();

            if (scoreSystem != null)

            {

                int finalScore = scoreSystem.GetCurrentScore();

                float accuracy = scoreSystem.GetAccuracy();



                Debug.Log($"PuntuaciÃ³n final: {finalScore}");

                Debug.Log($"PrecisiÃ³n: {accuracy:F1}%");

            }



            // PLACEHOLDER: Guardar estadÃ­sticas del nivel (Lista G)

            // LevelStatsSystem.SaveLevelStats(sessionEarnings, finalScore, accuracy);

        }



        void ShowResults()

        {

            Debug.Log("Mostrando pantalla de resultados");



            // PLACEHOLDER: ConexiÃ³n con Results Screen (Lista D6)

            // ResultsScreen resultsScreen = FindObjectOfType<ResultsScreen>();

            // if (resultsScreen != null)

            // {

            //     resultsScreen.ShowResults();

            // }



            // Por ahora, solo log

            Debug.Log("NIVEL COMPLETADO");

            Debug.Log("=================");

            Debug.Log("Presiona R para reiniciar el nivel");

        }



        // MÃ‰TODOS PÃšBLICOS PARA UI



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

            Debug.Log("Volviendo al menÃº principal");

            // CONEXIÃ“N FUTURA: Scene management

            // SceneManager.LoadScene("MainMenu");

        }



        // MÃ‰TODOS DE CONFIGURACIÃ“N



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



        // INPUT PARA TESTING

        void Update()

        {

            // Testing controls (solo en editor)

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

                // Test: agregar 10 segundos al timer

                if (levelTimer != null)

                {

                    levelTimer.DebugAdd30Seconds();

                }

            }

#endif

        }



        void OnDestroy()

        {

            // Desconectar eventos para evitar errores

            if (levelTimer != null)

            {

                levelTimer.OnTimeUp -= HandleTimeUp;

                levelTimer.OnWarning30Seconds -= HandleWarning30;

                levelTimer.OnWarning10Seconds -= HandleWarning10;

                levelTimer.OnTimerStarted -= HandleTimerStarted;

            }

        }

    }

}