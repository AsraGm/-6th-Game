using System.Collections;
using TMPro;
using UnityEngine;



// ARCHIVO: CountdownManager_Simple.cs

// VersiÃ³n SUPER SIMPLE con GameObjects activados/desactivados



namespace ShootingRange

{

    public class CountdownManager_Simple : MonoBehaviour

    {

        [Header("Referencias de Sistema")]

        public WaveSystem waveSystem;

        public LevelTimer levelTimer;

        public ThemeManager themeManager;



        [Header("ConfiguraciÃ³n de Countdown")]

        [Range(0.3f, 2f)]

        public float phaseDuration = 0.8f;



        public float goDuration = 0.5f;



        public bool autoStart = true;



        [Header("Audio (Opcional)")]

        public AudioClip readySetSound;

        public AudioClip goSound;



        [Header("Global Light Control (Opcional)")]

        [Tooltip("Arrastra tu Global Light 2D aquÃ­")]

        public UnityEngine.Rendering.Universal.Light2D globalLight;



        [Tooltip("Intensidad inicial (oscuro)")]

        public float startLightIntensity = 0f;



        [Tooltip("Intensidad final (normal)")]

        public float targetLightIntensity = 1.2f;



        [Tooltip("DuraciÃ³n de la transiciÃ³n de luz")]

        public float lightFadeDuration = 1.5f;



        [Header("UIs por Tema")]

        public CountdownUITheme[] countdownUIs;



        private bool isCountdownActive = false;

        private CountdownUITheme currentUI;

        private AudioSource audioSource;



        [System.Serializable]

        public class CountdownUITheme

        {

            [Tooltip("Nombre del tema (Western, Zombie, etc.)")]

            public string themeName;



            [Tooltip("GameObject raÃ­z que contiene todo")]

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



            Debug.Log("âœ… CountdownManager_Simple inicializado");

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

                Debug.LogWarning("Countdown ya estÃ¡ activo");

                return;

            }



            StartCoroutine(CountdownSequence());

        }



        IEnumerator CountdownSequence()

        {

            isCountdownActive = true;



            // Seleccionar UI segÃºn tema

            SelectCurrentThemeUI();



            if (currentUI == null)

            {

                Debug.LogError("No se encontrÃ³ UI para el tema actual");

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



            // Ocultar UI

            currentUI.uiRoot.SetActive(false);



            // Completar e iniciar juego

            CompleteCountdown();

        }



        void SelectCurrentThemeUI()

        {

            currentUI = null;



            // Si no hay ThemeManager, usar el primero

            if (themeManager == null)

            {

                if (countdownUIs.Length > 0)

                {

                    currentUI = countdownUIs[0];

                    Debug.LogWarning("ThemeManager no encontrado, usando primer UI");

                }

                return;

            }



            // Obtener tema actual

            string currentTheme = GetCurrentThemeName();



            // Buscar UI correspondiente

            foreach (var ui in countdownUIs)

            {

                if (ui.themeName.Equals(currentTheme, System.StringComparison.OrdinalIgnoreCase))

                {

                    currentUI = ui;

                    Debug.Log($"ðŸŽ¨ UI de countdown: {ui.themeName}");

                    return;

                }

            }



            // Fallback

            if (countdownUIs.Length > 0)

            {

                currentUI = countdownUIs[0];

                Debug.LogWarning($"No se encontrÃ³ UI para '{currentTheme}', usando fallback");

            }

        }



        string GetCurrentThemeName()

        {

            // Usar el mÃ©todo pÃºblico del ThemeManager

            if (themeManager != null)

            {

                return themeManager.GetCurrentThemeName();

            }



            return "Default"; // Fallback si no hay ThemeManager

        }



        IEnumerator ShowPhaseSimple(string message, GameObject light, bool isGoPhase = false)

        {

            Debug.Log($"ðŸ”¦ Fase: {message} | Luz: {(light != null ? light.name : "NULL")}");



            // Actualizar texto

            SetMessage(message);



            // Encender luz

            if (light != null)

            {

                light.SetActive(true);

                Debug.Log($"âœ… Luz {light.name} activada");

            }

            else

            {

                Debug.LogError($"âŒ Luz es NULL para fase {message}");

            }



            // Reproducir sonido

            PlaySound(isGoPhase ? goSound : readySetSound);



            // Esperar

            float duration = isGoPhase ? goDuration : phaseDuration;

            yield return new WaitForSeconds(duration);



            // Apagar luz

            if (light != null)

            {

                light.SetActive(false);

                Debug.Log($"ðŸ”´ Luz {light.name} desactivada");

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



            // Iniciar sistemas del juego

            if (waveSystem != null)

            {

                waveSystem.StartWaveSystem();

            }



            if (levelTimer != null)

            {

                levelTimer.StartTimer();

            }



            Debug.Log("âœ… Countdown completado - Juego iniciado");

        }



        IEnumerator FadeGlobalLight()

        {

            if (globalLight == null) yield break;



            float elapsed = 0f;



            Debug.Log($"ðŸ’¡ Iniciando fade: {startLightIntensity} â†’ {targetLightIntensity} en {lightFadeDuration}s");



            while (elapsed < lightFadeDuration)

            {

                elapsed += Time.deltaTime; // Cambiado de unscaledDeltaTime a deltaTime

                float t = elapsed / lightFadeDuration;



                // Lerp suave con curva ease-in-out

                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                globalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, smoothT);



                Debug.Log($"Fade progress: {t:F2} | Intensity: {globalLight.intensity:F2}");



                yield return null;

            }



            // Asegurar valor final

            globalLight.intensity = targetLightIntensity;



            Debug.Log($"ðŸ’¡ Global Light fade completo: {targetLightIntensity}");

        }



        public bool IsCountdownActive() => isCountdownActive;



        [ContextMenu("Test Countdown")]

        public void TestCountdown()

        {

            StartCountdown();

        }

    }

}