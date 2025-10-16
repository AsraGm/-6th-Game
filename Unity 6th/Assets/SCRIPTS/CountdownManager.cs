using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ARCHIVO: CountdownManager.cs
// Sistema de cuenta regresiva Ready-Set-Go con soporte multi-tema
// CONEXIÓN: ThemeManager, WaveSystem, LevelTimer

namespace ShootingRange
{
    public class CountdownManager : MonoBehaviour
    {
        [Header("Referencias de Sistema")]
        [Tooltip("Sistema de temas para detectar skin actual")]
        public ThemeManager themeManager;

        [Tooltip("Sistema de oleadas para iniciar después del countdown")]
        public WaveSystem waveSystem;

        [Tooltip("Timer del nivel para iniciar después del countdown")]
        public LevelTimer levelTimer;

        [Header("Configuración de Countdown")]
        [Tooltip("Duración de cada fase (Ready, Set, Go)")]
        [Range(0.3f, 2f)]
        public float phaseDuration = 0.8f;

        [Tooltip("Duración del mensaje GO antes de iniciar")]
        [Range(0.1f, 1f)]
        public float goDuration = 0.5f;

        [Tooltip("Iniciar automáticamente en Start")]
        public bool autoStart = true;

        [Header("Configuración de Audio")]
        [Tooltip("Sonido para Ready/Set")]
        public AudioClip readySetSound;

        [Tooltip("Sonido para GO")]
        public AudioClip goSound;

        [Header("UIs por Tema")]
        [Tooltip("ARRASTRA AQUÍ los GameObjects de UI de cada tema")]
        public CountdownUITheme[] countdownUIs;

        // Estado del sistema
        private bool isCountdownActive = false;
        private CountdownUITheme currentUI;
        private AudioSource audioSource;

        // Eventos
        public event System.Action OnCountdownStarted;
        public event System.Action OnCountdownCompleted;
        public event System.Action<CountdownPhase> OnPhaseChanged;

        public enum CountdownPhase
        {
            Ready,
            Set,
            Go
        }

        [System.Serializable]
        public class CountdownUITheme
        {
            [Tooltip("Nombre del tema (debe coincidir con ThemeManager)")]
            public string themeName;

            [Tooltip("GameObject raíz del UI de countdown para este tema")]
            public GameObject uiRoot;

            [Tooltip("Imagen de la luz roja (Ready)")]
            public Image redLight;

            [Tooltip("Imagen de la luz amarilla (Set)")]
            public Image yellowLight;

            [Tooltip("Imagen de la luz verde (Go)")]
            public Image greenLight;

            [Tooltip("Texto del mensaje (Ready/Set/Go)")]
            public Text messageText; // O TextMeshProUGUI si usas TMPro

            [Header("Colores de Luces")]
            public Color redLightColor = Color.red;
            public Color yellowLightColor = Color.yellow;
            public Color greenLightColor = Color.green;
            public Color lightOffColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            [Header("Configuración Visual (Opcional)")]
            [Tooltip("Escala de animación de las luces")]
            public float lightPulseScale = 1.2f;

            [Tooltip("Usar animación de pulso en luces")]
            public bool usePulseAnimation = true;
        }

        void Start()
        {
            Initialize();

            if (autoStart)
            {
                StartCountdown();
            }
        }

        void Initialize()
        {
            // Buscar sistemas si no están asignados
            if (themeManager == null)
                themeManager = FindObjectOfType<ThemeManager>();

            if (waveSystem == null)
                waveSystem = FindObjectOfType<WaveSystem>();

            if (levelTimer == null)
                levelTimer = FindObjectOfType<LevelTimer>();

            // Configurar AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Ocultar todos los UIs al inicio
            HideAllCountdownUIs();

            Debug.Log("✅ CountdownManager inicializado");
        }

        void HideAllCountdownUIs()
        {
            foreach (var ui in countdownUIs)
            {
                if (ui.uiRoot != null)
                {
                    ui.uiRoot.SetActive(false);
                }
            }
        }

        // MÉTODO PRINCIPAL: Iniciar countdown
        public void StartCountdown()
        {
            if (isCountdownActive)
            {
                Debug.LogWarning("Countdown ya está activo");
                return;
            }

            StartCoroutine(CountdownSequence());
        }

        IEnumerator CountdownSequence()
        {
            isCountdownActive = true;
            OnCountdownStarted?.Invoke();

            // Seleccionar UI según tema actual
            SelectCurrentThemeUI();

            if (currentUI == null)
            {
                Debug.LogError("No se encontró UI para el tema actual");
                CompleteCountdown();
                yield break;
            }

            // Mostrar UI
            currentUI.uiRoot.SetActive(true);

            // Apagar todas las luces al inicio
            ResetLights(currentUI);

            // FASE 1: READY
            yield return StartCoroutine(ShowPhase(CountdownPhase.Ready, currentUI));

            // FASE 2: SET
            yield return StartCoroutine(ShowPhase(CountdownPhase.Set, currentUI));

            // FASE 3: GO
            yield return StartCoroutine(ShowPhase(CountdownPhase.Go, currentUI));

            // Ocultar UI
            currentUI.uiRoot.SetActive(false);

            // Completar countdown e iniciar juego
            CompleteCountdown();
        }

        void SelectCurrentThemeUI()
        {
            currentUI = null;

            // Si no hay ThemeManager, usar el primer UI disponible
            if (themeManager == null)
            {
                if (countdownUIs.Length > 0)
                {
                    currentUI = countdownUIs[0];
                    Debug.LogWarning("ThemeManager no encontrado, usando primer UI disponible");
                }
                return;
            }

            // Obtener tema actual
            string currentTheme = themeManager.GetCurrentThemeName();

            // Buscar UI correspondiente
            foreach (var ui in countdownUIs)
            {
                if (ui.themeName.Equals(currentTheme, System.StringComparison.OrdinalIgnoreCase))
                {
                    currentUI = ui;
                    Debug.Log($"🎨 UI de countdown seleccionado: {ui.themeName}");
                    return;
                }
            }

            // Fallback: usar primer UI si no se encontró match
            if (countdownUIs.Length > 0)
            {
                currentUI = countdownUIs[0];
                Debug.LogWarning($"No se encontró UI para tema '{currentTheme}', usando fallback");
            }
        }

        IEnumerator ShowPhase(CountdownPhase phase, CountdownUITheme ui)
        {
            OnPhaseChanged?.Invoke(phase);

            // Configurar según fase
            switch (phase)
            {
                case CountdownPhase.Ready:
                    ActivateLight(ui.redLight, ui.redLightColor, ui);
                    SetMessage(ui, "READY");
                    PlaySound(readySetSound);
                    break;

                case CountdownPhase.Set:
                    ActivateLight(ui.yellowLight, ui.yellowLightColor, ui);
                    SetMessage(ui, "SET");
                    PlaySound(readySetSound);
                    break;

                case CountdownPhase.Go:
                    ActivateLight(ui.greenLight, ui.greenLightColor, ui);
                    SetMessage(ui, "GO!");
                    PlaySound(goSound);
                    break;
            }

            // Esperar duración de la fase
            float duration = (phase == CountdownPhase.Go) ? goDuration : phaseDuration;
            yield return new WaitForSeconds(duration);
        }

        void ActivateLight(Image light, Color activeColor, CountdownUITheme ui)
        {
            if (light == null) return;

            // Apagar todas las luces primero
            ResetLights(ui);

            // Encender luz actual (alpha 1 = visible)
            Color visibleColor = activeColor;
            visibleColor.a = 1f; // Hacer visible
            light.color = visibleColor;

            // Animación de pulso (opcional)
            if (ui.usePulseAnimation)
            {
                StartCoroutine(PulseLightAnimation(light, ui.lightPulseScale));
            }
        }

        IEnumerator PulseLightAnimation(Image light, float scale)
        {
            if (light == null) yield break;

            Vector3 originalScale = light.transform.localScale;
            Vector3 targetScale = originalScale * scale;

            float duration = 0.2f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                light.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            // Scale back
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                light.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            light.transform.localScale = originalScale;
        }

        void ResetLights(CountdownUITheme ui)
        {
            // Apagar luces (alpha 0 = invisible)
            if (ui.redLight != null)
            {
                Color offColor = ui.lightOffColor;
                offColor.a = 0f; // Hacer invisible
                ui.redLight.color = offColor;
            }

            if (ui.yellowLight != null)
            {
                Color offColor = ui.lightOffColor;
                offColor.a = 0f;
                ui.yellowLight.color = offColor;
            }

            if (ui.greenLight != null)
            {
                Color offColor = ui.lightOffColor;
                offColor.a = 0f;
                ui.greenLight.color = offColor;
            }
        }

        void SetMessage(CountdownUITheme ui, string message)
        {
            if (ui.messageText != null)
            {
                ui.messageText.text = message;
            }
        }

        void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        void CompleteCountdown()
        {
            isCountdownActive = false;
            OnCountdownCompleted?.Invoke();

            // Iniciar sistemas del juego
            StartGameSystems();

            Debug.Log("✅ Countdown completado - Juego iniciado");
        }

        void StartGameSystems()
        {
            // Iniciar WaveSystem
            if (waveSystem != null)
            {
                waveSystem.StartWaveSystem();
            }

            // Iniciar LevelTimer
            if (levelTimer != null)
            {
                levelTimer.StartTimer();
            }
        }

        // MÉTODOS PÚBLICOS
        public bool IsCountdownActive() => isCountdownActive;

        public void ForceStopCountdown()
        {
            StopAllCoroutines();
            HideAllCountdownUIs();
            isCountdownActive = false;
        }

        // MÉTODO DE TESTING
        [ContextMenu("Test Countdown")]
        public void TestCountdown()
        {
            StartCountdown();
        }
    }
}