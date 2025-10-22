using System.Collections;
using TMPro;
using UnityEngine;

namespace ShootingRange
{
    public class CountdownManager: MonoBehaviour
    {
        [Header("Referencias de Sistema")]
        public WaveSystem waveSystem;
        public LevelTimer levelTimer;
        public ThemeManager themeManager;

        [Header("Configuración de Countdown")]
        [Range(0.3f, 2f)]
        public float phaseDuration = 0.8f;
        public float goDuration = 0.5f;
        public bool autoStart = true;

        [Header("Audio (Opcional)")]
        public AudioClip readySetSound;
        public AudioClip goSound;

        [Header("Global Light Control")]
        [Tooltip("Arrastra tu Global Light 2D aquí")]
        public UnityEngine.Rendering.Universal.Light2D globalLight;

        [Tooltip("Intensidad inicial (oscuro)")]
        public float startLightIntensity = 0f;

        [Tooltip("Intensidad final (normal)")]
        public float targetLightIntensity = 1.2f;

        [Tooltip("Duración de la transición de luz")]
        public float lightFadeDuration = 1.5f;

        [Header("Sistema de Luces Adicionales")]
        [Tooltip("Luces adicionales que se activarán en tiempos específicos")]
        public AdditionalLightConfig[] additionalLights;

        [Header("UIs por Tema")]
        public CountdownUITheme[] countdownUIs;

        private bool isCountdownActive = false;
        private CountdownUITheme currentUI;
        private AudioSource audioSource;
        private float countdownStartTime;

        [System.Serializable]
        public class AdditionalLightConfig
        {
            [Tooltip("Nombre identificador de esta luz")]
            public string lightName;

            [Tooltip("Componente Light 2D a controlar")]
            public UnityEngine.Rendering.Universal.Light2D light2D;

            [Header("Configuración de Fade")]
            [Tooltip("¿Activar fade para esta luz?")]
            public bool enableFade = true;

            [Tooltip("Tiempo de delay antes de iniciar el fade (desde inicio del countdown)")]
            public float fadeStartDelay = 0f;

            [Tooltip("Duración del fade")]
            public float fadeDuration = 1.5f;

            [Tooltip("Intensidad inicial")]
            public float startIntensity = 0f;

            [Tooltip("Intensidad final")]
            public float targetIntensity = 1f;

            [Header("Configuración de Activación por Tiempo")]
            [Tooltip("¿Activar/desactivar la luz en momentos específicos?")]
            public bool enableTimedToggle = false;

            [Tooltip("Tiempo para ENCENDER la luz (desde inicio del countdown)")]
            public float turnOnTime = 0f;

            [Tooltip("Tiempo para APAGAR la luz (desde inicio del countdown, 0 = no apagar)")]
            public float turnOffTime = 0f;

            [HideInInspector]
            public bool isProcessing = false;
        }

        [System.Serializable]
        public class CountdownUITheme
        {
            [Tooltip("Nombre del tema (Western, Zombie, etc.)")]
            public string themeName;

            [Tooltip("GameObject raíz que contiene todo")]
            public GameObject uiRoot;

            [Header("Luces 2D (GameObjects con Light 2D)")]
            [Tooltip("GameObject con Light 2D roja")]
            public GameObject redLight;

            [Tooltip("GameObject con Light 2D amarilla")]
            public GameObject yellowLight;

            [Tooltip("GameObject con Light 2D verde")]
            public GameObject greenLight;

            [Header("Texto del Mensaje")]
            public TextMeshProUGUI messageTextTMP;
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
            // Buscar sistemas
            if (waveSystem == null)
                waveSystem = FindObjectOfType<WaveSystem>();

            if (levelTimer == null)
                levelTimer = FindObjectOfType<LevelTimer>();

            if (themeManager == null)
                themeManager = FindObjectOfType<ThemeManager>();

            // AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Ocultar todos los UIs
            HideAllCountdownUIs();

            // Configurar Global Light en intensidad inicial
            if (globalLight != null)
            {
                globalLight.intensity = startLightIntensity;
            }

            // Inicializar luces adicionales
            InitializeAdditionalLights();

            Debug.Log("✅ CountdownManager_Enhanced inicializado");
        }

        void InitializeAdditionalLights()
        {
            if (additionalLights == null || additionalLights.Length == 0)
            {
                Debug.Log("No hay luces adicionales configuradas");
                return;
            }

            foreach (var lightConfig in additionalLights)
            {
                if (lightConfig.light2D != null)
                {
                    // Configurar intensidad inicial
                    if (lightConfig.enableFade)
                    {
                        lightConfig.light2D.intensity = lightConfig.startIntensity;
                    }

                    // Si tiene timed toggle, iniciar apagada
                    if (lightConfig.enableTimedToggle)
                    {
                        lightConfig.light2D.gameObject.SetActive(false);
                    }

                    lightConfig.isProcessing = false;
                    Debug.Log($"🔆 Luz '{lightConfig.lightName}' inicializada");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Light2D no asignado para '{lightConfig.lightName}'");
                }
            }
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
            countdownStartTime = Time.time;

            // Seleccionar UI según tema
            SelectCurrentThemeUI();

            if (currentUI == null)
            {
                Debug.LogError("No se encontró UI para el tema actual");
                CompleteCountdown();
                yield break;
            }

            // Mostrar UI Root
            currentUI.uiRoot.SetActive(true);

            // Apagar todas las luces al inicio
            TurnOffAllLights(currentUI);

            // Iniciar fade de Global Light en paralelo
            Coroutine lightFade = null;
            if (globalLight != null)
            {
                lightFade = StartCoroutine(FadeGlobalLight());
            }

            // Iniciar sistema de luces adicionales
            StartCoroutine(ManageAdditionalLights());

            // FASE 1: READY
            yield return StartCoroutine(ShowPhaseSimple("READY", currentUI.redLight));

            // FASE 2: SET
            yield return StartCoroutine(ShowPhaseSimple("SET", currentUI.yellowLight));

            // FASE 3: GO
            yield return StartCoroutine(ShowPhaseSimple("GO!", currentUI.greenLight, true));

            // ESPERAR a que el fade de luz termine antes de continuar
            if (lightFade != null)
            {
                yield return lightFade;
            }

            // Esperar a que todas las luces adicionales terminen si están configuradas
            yield return StartCoroutine(WaitForAdditionalLights());

            // Ocultar UI
            currentUI.uiRoot.SetActive(false);

            // Completar e iniciar juego
            CompleteCountdown();
        }

        IEnumerator ManageAdditionalLights()
        {
            if (additionalLights == null || additionalLights.Length == 0)
                yield break;

            foreach (var lightConfig in additionalLights)
            {
                if (lightConfig.light2D == null) continue;

                // Iniciar fade si está habilitado
                if (lightConfig.enableFade)
                {
                    StartCoroutine(FadeAdditionalLight(lightConfig));
                }

                // Iniciar toggle por tiempo si está habilitado
                if (lightConfig.enableTimedToggle)
                {
                    StartCoroutine(TimedToggleLight(lightConfig));
                }
            }
        }

        IEnumerator FadeAdditionalLight(AdditionalLightConfig config)
        {
            config.isProcessing = true;

            // Esperar el delay inicial
            if (config.fadeStartDelay > 0)
            {
                yield return new WaitForSeconds(config.fadeStartDelay);
            }

            float elapsed = 0f;
            Debug.Log($"💡 Iniciando fade de '{config.lightName}': {config.startIntensity} → {config.targetIntensity}");

            while (elapsed < config.fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / config.fadeDuration;

                // Lerp suave con curva ease-in-out
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                config.light2D.intensity = Mathf.Lerp(config.startIntensity, config.targetIntensity, smoothT);

                yield return null;
            }

            // Asegurar valor final
            config.light2D.intensity = config.targetIntensity;
            config.isProcessing = false;

            Debug.Log($"✅ Fade completo para '{config.lightName}'");
        }

        IEnumerator TimedToggleLight(AdditionalLightConfig config)
        {
            config.isProcessing = true;

            // Esperar hasta el tiempo de encendido
            float timeToWait = config.turnOnTime;
            if (timeToWait > 0)
            {
                yield return new WaitForSeconds(timeToWait);
            }

            // Encender luz
            config.light2D.gameObject.SetActive(true);
            Debug.Log($"🔆 Luz '{config.lightName}' ENCENDIDA en t={config.turnOnTime}s");

            // Si hay tiempo de apagado configurado
            if (config.turnOffTime > config.turnOnTime && config.turnOffTime > 0)
            {
                float offDelay = config.turnOffTime - config.turnOnTime;
                yield return new WaitForSeconds(offDelay);

                config.light2D.gameObject.SetActive(false);
                Debug.Log($"🔴 Luz '{config.lightName}' APAGADA en t={config.turnOffTime}s");
            }

            config.isProcessing = false;
        }

        IEnumerator WaitForAdditionalLights()
        {
            if (additionalLights == null || additionalLights.Length == 0)
                yield break;

            bool anyProcessing = true;
            while (anyProcessing)
            {
                anyProcessing = false;
                foreach (var lightConfig in additionalLights)
                {
                    if (lightConfig.isProcessing)
                    {
                        anyProcessing = true;
                        break;
                    }
                }

                if (anyProcessing)
                {
                    yield return null;
                }
            }

            Debug.Log("✅ Todas las luces adicionales completadas");
        }

        void SelectCurrentThemeUI()
        {
            currentUI = null;

            if (themeManager == null)
            {
                if (countdownUIs.Length > 0)
                {
                    currentUI = countdownUIs[0];
                    Debug.LogWarning("ThemeManager no encontrado, usando primer UI");
                }
                return;
            }

            string currentTheme = GetCurrentThemeName();

            foreach (var ui in countdownUIs)
            {
                if (ui.themeName.Equals(currentTheme, System.StringComparison.OrdinalIgnoreCase))
                {
                    currentUI = ui;
                    Debug.Log($"🎨 UI de countdown: {ui.themeName}");
                    return;
                }
            }

            if (countdownUIs.Length > 0)
            {
                currentUI = countdownUIs[0];
                Debug.LogWarning($"No se encontró UI para '{currentTheme}', usando fallback");
            }
        }

        string GetCurrentThemeName()
        {
            if (themeManager != null)
            {
                return themeManager.GetCurrentThemeName();
            }

            return "Default";
        }

        IEnumerator ShowPhaseSimple(string message, GameObject light, bool isGoPhase = false)
        {
            Debug.Log($"🏁 Fase: {message} | Luz: {(light != null ? light.name : "NULL")}");

            SetMessage(message);

            if (light != null)
            {
                light.SetActive(true);
                Debug.Log($"✅ Luz {light.name} activada");
            }
            else
            {
                Debug.LogError($"❌ Luz es NULL para fase {message}");
            }

            PlaySound(isGoPhase ? goSound : readySetSound);

            float duration = isGoPhase ? goDuration : phaseDuration;
            yield return new WaitForSeconds(duration);

            if (light != null)
            {
                light.SetActive(false);
                Debug.Log($"🔴 Luz {light.name} desactivada");
            }
        }

        void TurnOffAllLights(CountdownUITheme ui)
        {
            if (ui.redLight != null)
                ui.redLight.SetActive(false);

            if (ui.yellowLight != null)
                ui.yellowLight.SetActive(false);

            if (ui.greenLight != null)
                ui.greenLight.SetActive(false);
        }

        void SetMessage(string message)
        {
            if (currentUI.messageTextTMP != null)
            {
                currentUI.messageTextTMP.text = message;
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

            if (waveSystem != null)
            {
                waveSystem.StartWaveSystem();
            }

            if (levelTimer != null)
            {
                levelTimer.StartTimer();
            }

            Debug.Log("✅ Countdown completado - Juego iniciado");
        }

        IEnumerator FadeGlobalLight()
        {
            if (globalLight == null) yield break;

            float elapsed = 0f;

            Debug.Log($"💡 Iniciando fade global: {startLightIntensity} → {targetLightIntensity} en {lightFadeDuration}s");

            while (elapsed < lightFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lightFadeDuration;

                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                globalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, smoothT);

                yield return null;
            }

            globalLight.intensity = targetLightIntensity;

            Debug.Log($"💡 Global Light fade completo: {targetLightIntensity}");
        }

        public bool IsCountdownActive() => isCountdownActive;

        public float GetCountdownElapsedTime()
        {
            return isCountdownActive ? Time.time - countdownStartTime : 0f;
        }

        [ContextMenu("Test Countdown")]
        public void TestCountdown()
        {
            StartCountdown();
        }
    }
}