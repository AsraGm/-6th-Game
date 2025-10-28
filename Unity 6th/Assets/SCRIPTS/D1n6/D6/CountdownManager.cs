using System.Collections;
using TMPro;
using UnityEngine;

namespace ShootingRange
{
    /// <summary>
    /// Sistema híbrido SIMPLIFICADO con overlay negro INDEPENDIENTE del tema:
    /// - Overlay negro permanece igual siempre (no afectado por temas)
    /// - Durante countdown: Luces 2D + overlay negro visible
    /// - Al GO!: Luz baja a 0 (oscuro)
    /// - Desactivar overlay (invisible porque está oscuro)
    /// - Luz sube revelando el escenario gradualmente
    /// </summary>
    public class HybridCountdownManager : MonoBehaviour
    {
        [Header("Referencias de Sistema")]
        public WaveSystem waveSystem;
        public LevelTimer levelTimer;

        [Header("⚠️ IMPORTANTE: Theme Manager")]
        [Tooltip("Usa CanvasThemeManager SI usas Canvas UI, o ThemeManager normal si usas SpriteRenderer")]
        public MonoBehaviour themeManagerReference;

        [Header("Configuración de Countdown")]
        [Range(0.3f, 2f)]
        public float phaseDuration = 0.8f;
        public float goDuration = 0.5f;
        public bool autoStart = true;

        [Header("Audio (Opcional)")]
        public AudioClip readySetSound;
        public AudioClip goSound;

        [Header("🆕 OVERLAY NEGRO (Independiente del tema)")]
        [Tooltip("ARRASTRA AQUÍ el SpriteRenderer del overlay negro - ESTE SPRITE NUNCA CAMBIA")]
        public SpriteRenderer blackOverlay;

        [Header("🆕 Configuración de Transición SIMPLIFICADA")]
        [Tooltip("Duración de la transición de luz (revelar escenario)")]
        [Range(0.5f, 3f)]
        public float transitionDuration = 1.5f;

        [Tooltip("¿Ocultar escenario (Canvas o SpriteRenderer) al inicio?")]
        public bool hideSceneryAtStart = true;

        [Header("Global Light Control")]
        [Tooltip("Arrastra tu Global Light 2D aquí")]
        public UnityEngine.Rendering.Universal.Light2D globalLight;

        [Tooltip("Intensidad durante countdown (para ver las luces)")]
        public float countdownLightIntensity = 0.3f;

        [Tooltip("Intensidad final del juego")]
        public float gameLightIntensity = 1.2f;

        [Header("Sistema de Luces Adicionales")]
        [Tooltip("Luces adicionales")]
        public AdditionalLightConfig[] additionalLights;

        [Header("UIs por Tema")]
        public CountdownUITheme[] countdownUIs;

        private bool isCountdownActive = false;
        private CountdownUITheme currentUI;
        private AudioSource audioSource;
        private float countdownStartTime;

        // Referencias dinámicas
        private CanvasThemeManager canvasThemeManager;
        private ThemeManager spriteThemeManager;

        [System.Serializable]
        public class AdditionalLightConfig
        {
            [Tooltip("Nombre identificador")]
            public string lightName;

            [Tooltip("Componente Light 2D")]
            public UnityEngine.Rendering.Universal.Light2D light2D;

            [Header("Configuración de Fade")]
            [Tooltip("¿Activar fade?")]
            public bool enableFade = true;

            [Tooltip("Delay antes del fade")]
            public float fadeStartDelay = 0f;

            [Tooltip("Duración del fade")]
            public float fadeDuration = 1.5f;

            [Tooltip("Intensidad inicial")]
            public float startIntensity = 0f;

            [Tooltip("Intensidad final")]
            public float targetIntensity = 1f;

            [Header("Activación por Tiempo")]
            [Tooltip("¿Activar/desactivar en momentos específicos?")]
            public bool enableTimedToggle = false;

            [Tooltip("Tiempo para ENCENDER")]
            public float turnOnTime = 0f;

            [Tooltip("Tiempo para APAGAR (0 = no apagar)")]
            public float turnOffTime = 0f;

            [HideInInspector]
            public bool isProcessing = false;
        }

        [System.Serializable]
        public class CountdownUITheme
        {
            [Tooltip("Nombre del tema")]
            public string themeName;

            [Tooltip("GameObject raíz del UI")]
            public GameObject uiRoot;

            [Header("Luces 2D del Semáforo")]
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

            // Detectar qué tipo de ThemeManager está en uso
            DetectThemeManager();

            // AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Ocultar todos los UIs
            HideAllCountdownUIs();

            // Ocultar escenario al inicio
            if (hideSceneryAtStart)
            {
                HideSceneryElements();
            }

            // 🆕 CONFIGURAR OVERLAY NEGRO (INDEPENDIENTE DEL TEMA)
            if (blackOverlay != null)
            {
                // Asegurar que el overlay esté configurado correctamente
                Color c = blackOverlay.color;
                c.a = 1f; // Opaco
                blackOverlay.color = c;
                blackOverlay.gameObject.SetActive(true);

                // Sorting order alto para estar adelante
                blackOverlay.sortingLayerName = "Default";
                blackOverlay.sortingOrder = 9999;

                Debug.Log("🖤 Overlay negro configurado (independiente del tema)");
            }
            else
            {
                Debug.LogWarning("⚠️ Black Overlay no asignado");
            }

            // Configurar luz inicial para countdown
            if (globalLight != null)
            {
                globalLight.intensity = countdownLightIntensity;
                Debug.Log($"💡 Luz inicial: {countdownLightIntensity}");
            }

            // Inicializar luces adicionales
            InitializeAdditionalLights();

            Debug.Log("✅ HybridCountdownManager inicializado");
            LogAvailableThemes();
        }

        void DetectThemeManager()
        {
            // Intentar obtener CanvasThemeManager
            canvasThemeManager = CanvasThemeManager.Instance;
            if (canvasThemeManager != null)
            {
                Debug.Log("✅ Usando CanvasThemeManager (Canvas UI)");
                return;
            }

            // Intentar obtener ThemeManager normal
            spriteThemeManager = ThemeManager.Instance;
            if (spriteThemeManager != null)
            {
                Debug.Log("✅ Usando ThemeManager (SpriteRenderer)");
                return;
            }

            Debug.LogWarning("⚠️ No se encontró ningún ThemeManager");
        }

        void HideSceneryElements()
        {
            if (canvasThemeManager != null)
            {
                // Ocultar Canvas
                if (canvasThemeManager.backgroundCanvas != null)
                {
                    canvasThemeManager.backgroundCanvas.gameObject.SetActive(false);
                    Debug.Log("🙈 Background Canvas ocultado");
                }

                if (canvasThemeManager.curtainCanvas != null)
                {
                    canvasThemeManager.curtainCanvas.gameObject.SetActive(false);
                    Debug.Log("🙈 Curtain Canvas ocultado");
                }
            }
            else if (spriteThemeManager != null)
            {
                // Ocultar SpriteRenderers
                if (spriteThemeManager.backgroundRenderer != null)
                {
                    spriteThemeManager.backgroundRenderer.gameObject.SetActive(false);
                    Debug.Log("🙈 Background SpriteRenderer ocultado");
                }

                if (spriteThemeManager.curtainRenderer != null)
                {
                    spriteThemeManager.curtainRenderer.gameObject.SetActive(false);
                    Debug.Log("🙈 Curtain SpriteRenderer ocultado");
                }
            }
        }

        void RevealSceneryElements()
        {
            if (canvasThemeManager != null)
            {
                // Activar Canvas
                if (canvasThemeManager.backgroundCanvas != null)
                {
                    canvasThemeManager.backgroundCanvas.gameObject.SetActive(true);
                    Debug.Log("👁️ Background Canvas activado");
                }

                if (canvasThemeManager.curtainCanvas != null)
                {
                    canvasThemeManager.curtainCanvas.gameObject.SetActive(true);
                    Debug.Log("👁️ Curtain Canvas activado");
                }

                // Aplicar tema actual
                canvasThemeManager.ApplyCurrentTheme();
                Debug.Log("🎨 Tema Canvas aplicado");
            }
            else if (spriteThemeManager != null)
            {
                // Activar SpriteRenderers
                if (spriteThemeManager.backgroundRenderer != null)
                {
                    spriteThemeManager.backgroundRenderer.gameObject.SetActive(true);
                    Debug.Log("👁️ Background SpriteRenderer activado");
                }

                if (spriteThemeManager.curtainRenderer != null)
                {
                    spriteThemeManager.curtainRenderer.gameObject.SetActive(true);
                    Debug.Log("👁️ Curtain SpriteRenderer activado");
                }

                // Aplicar tema actual
                spriteThemeManager.ApplyCurrentTheme();
                Debug.Log("🎨 Tema SpriteRenderer aplicado");
            }
        }

        void InitializeAdditionalLights()
        {
            if (additionalLights == null || additionalLights.Length == 0)
            {
                Debug.Log("No hay luces adicionales");
                return;
            }

            foreach (var lightConfig in additionalLights)
            {
                if (lightConfig.light2D != null)
                {
                    if (lightConfig.enableFade)
                    {
                        lightConfig.light2D.intensity = lightConfig.startIntensity;
                    }

                    if (lightConfig.enableTimedToggle)
                    {
                        lightConfig.light2D.gameObject.SetActive(false);
                    }

                    lightConfig.isProcessing = false;
                    Debug.Log($"💡 Luz '{lightConfig.lightName}' inicializada");
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
                Debug.LogError("❌ No se encontró UI");
                CompleteCountdown();
                yield break;
            }

            currentUI.uiRoot.SetActive(true);
            TurnOffAllLights(currentUI);

            // Iniciar luces adicionales
            StartCoroutine(ManageAdditionalLights());

            // ==========================================
            // COUNTDOWN CON LUCES
            // ==========================================

            // FASE 1: READY
            yield return StartCoroutine(ShowPhaseSimple("READY", currentUI.redLight));

            // FASE 2: SET
            yield return StartCoroutine(ShowPhaseSimple("SET", currentUI.yellowLight));

            // FASE 3: GO!
            yield return StartCoroutine(ShowPhaseSimple("GO!", currentUI.greenLight, true));

            // ==========================================
            // 🆕 TRANSICIÓN SIMPLIFICADA
            // ==========================================

            Debug.Log("🎬 Iniciando transición...");

            // 1. Bajar luz a 0 (oscuro)
            if (globalLight != null)
            {
                globalLight.intensity = 0f;
                Debug.Log("💡 Luz apagada (oscuro)");
            }

            // 2. Pequeño delay
            yield return new WaitForSeconds(0.1f);

            // 3. Activar escenario y aplicar tema (invisible porque luz = 0)
            RevealSceneryElements();

            // 4. Desactivar overlay (invisible porque está oscuro)
            if (blackOverlay != null)
            {
                blackOverlay.gameObject.SetActive(false);
                Debug.Log("🖤 Overlay desactivado");
            }

            // 5. Subir luz gradualmente revelando el escenario
            yield return StartCoroutine(FadeInGameLight());

            // Esperar luces adicionales
            yield return StartCoroutine(WaitForAdditionalLights());

            // Ocultar UI del countdown
            currentUI.uiRoot.SetActive(false);

            // Completar
            CompleteCountdown();
        }

        IEnumerator FadeInGameLight()
        {
            if (globalLight == null) yield break;

            float elapsed = 0f;
            float startIntensity = 0f;
            float targetIntensity = gameLightIntensity;

            Debug.Log($"💡 Fade in luz: 0 → {targetIntensity} en {transitionDuration}s");

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);

                yield return null;
            }

            globalLight.intensity = targetIntensity;
            Debug.Log($"✅ Luz final: {targetIntensity}");
        }

        IEnumerator ManageAdditionalLights()
        {
            if (additionalLights == null || additionalLights.Length == 0)
                yield break;

            foreach (var lightConfig in additionalLights)
            {
                if (lightConfig.light2D == null) continue;

                if (lightConfig.enableFade)
                {
                    StartCoroutine(FadeAdditionalLight(lightConfig));
                }

                if (lightConfig.enableTimedToggle)
                {
                    StartCoroutine(TimedToggleLight(lightConfig));
                }
            }
        }

        IEnumerator FadeAdditionalLight(AdditionalLightConfig config)
        {
            config.isProcessing = true;

            if (config.fadeStartDelay > 0)
            {
                yield return new WaitForSeconds(config.fadeStartDelay);
            }

            float elapsed = 0f;

            while (elapsed < config.fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / config.fadeDuration;
                config.light2D.intensity = Mathf.Lerp(config.startIntensity, config.targetIntensity, t);

                yield return null;
            }

            config.light2D.intensity = config.targetIntensity;
            config.isProcessing = false;
        }

        IEnumerator TimedToggleLight(AdditionalLightConfig config)
        {
            config.isProcessing = true;

            if (config.turnOnTime > 0)
            {
                yield return new WaitForSeconds(config.turnOnTime);
            }

            config.light2D.gameObject.SetActive(true);

            if (config.turnOffTime > config.turnOnTime && config.turnOffTime > 0)
            {
                float offDelay = config.turnOffTime - config.turnOnTime;
                yield return new WaitForSeconds(offDelay);
                config.light2D.gameObject.SetActive(false);
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
                    yield return null;
            }
        }

        void SelectCurrentThemeUI()
        {
            currentUI = null;

            string currentThemeName = GetCurrentThemeName();

            if (string.IsNullOrEmpty(currentThemeName))
            {
                if (countdownUIs.Length > 0)
                {
                    currentUI = countdownUIs[0];
                    Debug.LogWarning($"⚠️ Usando primer UI: {currentUI.themeName}");
                }
                return;
            }

            Debug.Log($"🔍 Buscando UI para: '{currentThemeName}'");

            foreach (var ui in countdownUIs)
            {
                if (ui.themeName.Equals(currentThemeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    currentUI = ui;
                    Debug.Log($"✅ UI encontrado: '{ui.themeName}'");
                    return;
                }
            }

            if (countdownUIs.Length > 0)
            {
                currentUI = countdownUIs[0];
                Debug.LogWarning($"⚠️ Usando fallback: '{currentUI.themeName}'");
            }
        }

        string GetCurrentThemeName()
        {
            if (canvasThemeManager != null && canvasThemeManager.CurrentTheme != null)
            {
                return canvasThemeManager.CurrentTheme.themeName;
            }
            else if (spriteThemeManager != null && spriteThemeManager.CurrentTheme != null)
            {
                return spriteThemeManager.CurrentTheme.themeName;
            }

            return "";
        }

        IEnumerator ShowPhaseSimple(string message, GameObject light, bool isGoPhase = false)
        {
            SetMessage(message);

            if (light != null)
            {
                light.SetActive(true);
            }

            PlaySound(isGoPhase ? goSound : readySetSound);

            float duration = isGoPhase ? goDuration : phaseDuration;
            yield return new WaitForSeconds(duration);

            if (light != null)
            {
                light.SetActive(false);
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

        public bool IsCountdownActive() => isCountdownActive;

        public float GetCountdownElapsedTime()
        {
            return isCountdownActive ? Time.time - countdownStartTime : 0f;
        }

        [ContextMenu("🔍 Log System Info")]
        public void LogAvailableThemes()
        {
            Debug.Log("=== 🎨 COUNTDOWN SYSTEM DEBUG ===");

            if (canvasThemeManager != null)
            {
                Debug.Log($"✅ Usando: CanvasThemeManager");
                if (canvasThemeManager.CurrentTheme != null)
                    Debug.Log($"📌 Tema: '{canvasThemeManager.CurrentTheme.themeName}'");
            }
            else if (spriteThemeManager != null)
            {
                Debug.Log($"✅ Usando: ThemeManager (SpriteRenderer)");
                if (spriteThemeManager.CurrentTheme != null)
                    Debug.Log($"📌 Tema: '{spriteThemeManager.CurrentTheme.themeName}'");
            }
            else
            {
                Debug.LogWarning("⚠️ No se detectó ThemeManager");
            }

            Debug.Log($"\n🖤 Overlay negro: {(blackOverlay != null ? "✅ Asignado (independiente del tema)" : "❌ NO asignado")}");
            Debug.Log($"\n📋 Countdown UIs: {countdownUIs.Length}");
        }

        [ContextMenu("Test Countdown")]
        public void TestCountdown()
        {
            StartCountdown();
        }
    }
}