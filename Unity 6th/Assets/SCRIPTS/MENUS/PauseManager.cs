using UnityEngine;
using UnityEngine.UI;

// ARCHIVO: PauseManager.cs
// Sistema de pausa para móvil que detiene el juego, timer y enemigos

namespace ShootingRange
{
    public class PauseManager : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("ARRASTRA AQUÍ tu Panel de Pausa (GameObject con Image de fondo oscuro)")]
        public GameObject pausePanel;

        [Tooltip("ARRASTRA AQUÍ el botón de Reanudar")]
        public Button resumeButton;

        [Header("Referencias de Sistemas")]
        [Tooltip("ARRASTRA AQUÍ tu TimerIntegration")]
        public TimerIntegration timerIntegration;

        [Tooltip("ARRASTRA AQUÍ tu MoneySystem")]
        public MoneySystem moneySystem;

        [Tooltip("ARRASTRA AQUÍ tu TouchShootingSystem")]
        public TouchShootingSystem touchShootingSystem;

        [Header("Configuración")]
        [Tooltip("Usar Time.timeScale para pausar (recomendado para móvil)")]
        public bool useTimeScale = true;

        [Header("Estado")]
        [SerializeField] private bool isPaused = false;

        // Eventos
        public event System.Action OnGamePaused;
        public event System.Action OnGameResumed;

        // Propiedades públicas
        public bool IsPaused => isPaused;

        void Start()
        {
            InitializePauseSystem();
        }

        void InitializePauseSystem()
        {
            // Buscar sistemas automáticamente si no están asignados
            FindSystems();

            // Ocultar panel de pausa al inicio
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            // Conectar botón de reanudar
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
            }

            // Asegurar que el juego esté corriendo
            Time.timeScale = 1f;
            isPaused = false;

            Debug.Log("PauseManager inicializado");
        }

        void FindSystems()
        {
            if (timerIntegration == null)
            {
                timerIntegration = FindObjectOfType<TimerIntegration>();
                if (timerIntegration == null)
                {
                    Debug.LogWarning("TimerIntegration no encontrado. El timer no se pausará.");
                }
            }

            if (moneySystem == null)
            {
                moneySystem = FindObjectOfType<MoneySystem>();
                if (moneySystem == null)
                {
                    Debug.LogWarning("MoneySystem no encontrado.");
                }
            }

            if (touchShootingSystem == null)
            {
                touchShootingSystem = FindObjectOfType<TouchShootingSystem>();
                if (touchShootingSystem == null)
                {
                    Debug.LogWarning("TouchShootingSystem no encontrado.");
                }
            }
        }

        // MÉTODO PRINCIPAL: Pausar el juego
        public void PauseGame()
        {
            if (isPaused) return; // Ya está pausado

            isPaused = true;

            // LIMPIAR balas activas para evitar disparos accidentales del botón de pausa
            if (touchShootingSystem != null)
            {
                ClearActiveBullets();
            }

            // Pausar el tiempo del juego (esto pausa enemigos y físicas)
            if (useTimeScale)
            {
                Time.timeScale = 0f;
            }

            // Pausar el timer y wave system
            if (timerIntegration != null)
            {
                timerIntegration.PauseLevel();
            }

            // DESHABILITAR sistema de disparo para evitar disparos accidentales
            if (touchShootingSystem != null)
            {
                touchShootingSystem.enabled = false;
                Debug.Log("🔫 Sistema de disparo deshabilitado");
            }

            // Mostrar panel de pausa
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            // Disparar evento
            OnGamePaused?.Invoke();

            Debug.Log("⏸️ Juego pausado");
        }

        // MÉTODO PRINCIPAL: Reanudar el juego
        public void ResumeGame()
        {
            if (!isPaused) return; // No está pausado

            isPaused = false;

            // Reanudar el tiempo del juego
            if (useTimeScale)
            {
                Time.timeScale = 1f;
            }

            // Reanudar el timer y wave system
            if (timerIntegration != null)
            {
                timerIntegration.ResumeLevel();
            }

            // REHABILITAR sistema de disparo
            if (touchShootingSystem != null)
            {
                touchShootingSystem.enabled = true;
                Debug.Log("🔫 Sistema de disparo habilitado");
            }

            // Ocultar panel de pausa
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            // Disparar evento
            OnGameResumed?.Invoke();

            Debug.Log("▶️ Juego reanudado");
        }

        // Toggle pause (para botón de pausa en pantalla)
        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        // IMPORTANTE: Método para salir del nivel SIN guardar dinero de sesión
        public void QuitLevelWithoutSaving()
        {
            Debug.Log("⚠️ Saliendo del nivel SIN guardar dinero de sesión");

            // CRÍTICO: Recargar el dinero guardado ANTES de entrar al nivel
            if (moneySystem != null)
            {
                int moneyBeforeLevel = PlayerPrefs.GetInt("CurrentMoney", 0);

                Debug.Log($"💰 Dinero antes del nivel: ${moneyBeforeLevel}");
                Debug.Log($"💰 Dinero actual (con ganancias): ${moneySystem.GetCurrentMoney()}");
                Debug.Log($"💰 Ganancias de sesión perdidas: ${moneySystem.GetSessionEarnings()}");

                // Restaurar el dinero que tenía ANTES de empezar el nivel
                moneySystem.LoadMoney(); // Recargar desde PlayerPrefs

                Debug.Log($"✅ Dinero restaurado a: ${moneySystem.GetCurrentMoney()}");
            }

            // Reanudar el tiempo antes de cambiar de escena
            Time.timeScale = 1f;

            // PLACEHOLDER: Aquí cargarías la escena del menú
            // SceneManager.LoadScene("MainMenu");

            Debug.Log("📍 PLACEHOLDER: Cargar escena de menú aquí");
        }

        // MÉTODO ADICIONAL: Para guardar el dinero cuando COMPLETAS el nivel
        public void CompleteLevelAndSave()
        {
            Debug.Log("✅ Nivel completado - Guardando progreso");

            if (moneySystem != null)
            {
                // Guardar el dinero actual (con las ganancias del nivel)
                moneySystem.SaveMoney();

                Debug.Log($"💾 Dinero guardado: ${moneySystem.GetCurrentMoney()}");
                Debug.Log($"💰 Ganancias de este nivel: ${moneySystem.GetSessionEarnings()}");
            }

            Time.timeScale = 1f;
        }

        // MÉTODO PÚBLICO: Para llamar desde botones de UI externos
        public void OnPauseButtonClicked()
        {
            PauseGame();
        }

        public void OnResumeButtonClicked()
        {
            ResumeGame();
        }

        public void OnQuitButtonClicked()
        {
            QuitLevelWithoutSaving();
        }

        // Input de debug (solo en editor)
        void Update()
        {
#if UNITY_EDITOR
            // Presionar ESC o P para pausar/reanudar
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }
#endif
        }

        // Limpiar al destruir
        void OnDestroy()
        {
            // Asegurar que el tiempo esté corriendo al destruir
            Time.timeScale = 1f;

            // Desconectar botón
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(ResumeGame);
            }
        }

        // Método de utilidad para verificar si el nivel está activo
        public bool IsLevelActive()
        {
            return timerIntegration != null && timerIntegration.IsLevelActive();
        }

        // NUEVO: Limpiar balas activas
        void ClearActiveBullets()
        {
            // Buscar todas las balas activas en la escena
            BulletBehavior[] bullets = FindObjectsOfType<BulletBehavior>();

            foreach (BulletBehavior bullet in bullets)
            {
                if (bullet != null && bullet.gameObject.activeInHierarchy)
                {
                    // Retornar al pool si es del sistema de disparo
                    if (touchShootingSystem != null)
                    {
                        touchShootingSystem.ReturnBulletToPool(bullet.gameObject);
                    }
                    else
                    {
                        // Si no hay sistema, simplemente desactivar
                        bullet.gameObject.SetActive(false);
                    }
                }
            }

            Debug.Log($"🧹 Limpiadas {bullets.Length} balas activas");
        }
    }
}