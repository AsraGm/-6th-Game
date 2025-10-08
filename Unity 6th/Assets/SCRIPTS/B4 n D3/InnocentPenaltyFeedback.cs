using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ARCHIVO: InnocentPenaltyFeedback.cs
// INSTRUCCIÓN B4: Sistema simplificado de inocentes - Feedback visual inmediato
// Conexión con Lista D5 (Penalty feedback)

namespace ShootingRange
{
    public class InnocentPenaltyFeedback : MonoBehaviour
    {
        [Header("Referencias UI - IMPORTANTE")]
        [Tooltip("ARRASTRA AQUÍ un Image que cubra toda la pantalla (Alpha 0 inicial)")]
        public Image screenFlashImage;
        
        [Tooltip("Text para mostrar penalización (opcional)")]
        public TMPro.TextMeshProUGUI penaltyText;
        
        [Header("Configuración de Flash")]
        [Tooltip("Color del flash de pantalla al disparar inocente")]
        public Color flashColor = new Color(1f, 0f, 0f, 0.5f); // Rojo semi-transparente
        
        [Tooltip("Duración del flash en segundos")]
        [Range(0.1f, 1f)]
        public float flashDuration = 0.3f;
        
        [Header("Configuración de Texto")]
        [Tooltip("Mostrar cantidad de dinero perdido")]
        public bool showPenaltyAmount = true;
        
        [Tooltip("Duración del texto de penalización")]
        [Range(0.5f, 3f)]
        public float textDuration = 1.5f;
        
        [Tooltip("Distancia que se mueve el texto hacia arriba")]
        [Range(10f, 100f)]
        public float textMoveDistance = 50f;
        
        [Header("Efectos de Sonido")]
        [Tooltip("Sonido al disparar inocente (error/penalización)")]
        public AudioClip penaltySound;
        
        [Tooltip("Volumen del sonido de penalización")]
        [Range(0f, 1f)]
        public float soundVolume = 0.7f;
        
        [Header("Vibración")]
        [Tooltip("Usar vibración más fuerte para inocentes")]
        public bool useStrongVibration = true;
        
        [Tooltip("Duración de vibración en milisegundos (solo Android)")]
        public long vibrationDuration = 200;
        
        // Variables privadas
        private AudioSource audioSource;
        private bool isFlashing = false;
        private Vector3 penaltyTextOriginalPosition;
        
        void Start()
        {
            InitializeFeedbackSystem();
        }
        
        void InitializeFeedbackSystem()
        {
            // Configurar audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            
            // Configurar flash image
            if (screenFlashImage != null)
            {
                Color transparent = flashColor;
                transparent.a = 0f;
                screenFlashImage.color = transparent;
                screenFlashImage.raycastTarget = false; // No bloquear inputs
            }
            else
            {
                Debug.LogWarning("InnocentPenaltyFeedback: No se asignó screenFlashImage. Crear un UI Image que cubra toda la pantalla.");
            }
            
            // Configurar penalty text
            if (penaltyText != null)
            {
                penaltyTextOriginalPosition = penaltyText.transform.position;
                penaltyText.gameObject.SetActive(false);
            }
            
            Debug.Log("Sistema de feedback de penalización inicializado");
        }
        
        // MÉTODO PRINCIPAL: Activar feedback completo
        public void TriggerInnocentPenalty(int penaltyAmount)
        {
            if (isFlashing)
            {
                // Si ya hay un flash activo, no apilar otro
                return;
            }
            
            // Flash de pantalla
            StartCoroutine(ScreenFlashCoroutine());
            
            // Mostrar texto de penalización
            if (showPenaltyAmount && penaltyText != null)
            {
                StartCoroutine(ShowPenaltyTextCoroutine(penaltyAmount));
            }
            
            // Sonido de error
            PlayPenaltySound();
            
            // Vibración fuerte
            if (useStrongVibration)
            {
                TriggerStrongVibration();
            }
            
            Debug.Log($"Penalización de inocente activada: -{penaltyAmount}");
        }
        
        // Flash rojo de pantalla completa
        IEnumerator ScreenFlashCoroutine()
        {
            if (screenFlashImage == null) yield break;
            
            isFlashing = true;
            
            // Fade in rápido
            float elapsed = 0f;
            float fadeInTime = flashDuration * 0.2f; // 20% del tiempo para aparecer
            
            while (elapsed < fadeInTime)
            {
                elapsed += Time.unscaledDeltaTime; // Usar unscaled para que funcione aunque el juego esté pausado
                float progress = elapsed / fadeInTime;
                
                Color currentColor = flashColor;
                currentColor.a = Mathf.Lerp(0f, flashColor.a, progress);
                screenFlashImage.color = currentColor;
                
                yield return null;
            }
            
            // Mantener un momento
            screenFlashImage.color = flashColor;
            yield return new WaitForSecondsRealtime(flashDuration * 0.3f); // 30% mantener
            
            // Fade out
            elapsed = 0f;
            float fadeOutTime = flashDuration * 0.5f; // 50% del tiempo para desaparecer
            
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / fadeOutTime;
                
                Color currentColor = flashColor;
                currentColor.a = Mathf.Lerp(flashColor.a, 0f, progress);
                screenFlashImage.color = currentColor;
                
                yield return null;
            }
            
            // Asegurar que quede transparente
            Color transparent = flashColor;
            transparent.a = 0f;
            screenFlashImage.color = transparent;
            
            isFlashing = false;
        }
        
        // Mostrar texto de penalización que sube y desaparece
        IEnumerator ShowPenaltyTextCoroutine(int amount)
        {
            if (penaltyText == null) yield break;
            
            // Configurar texto
            penaltyText.text = $"-${Mathf.Abs(amount)}";
            penaltyText.color = Color.red;
            penaltyText.transform.position = penaltyTextOriginalPosition;
            penaltyText.gameObject.SetActive(true);
            
            float elapsed = 0f;
            Vector3 startPos = penaltyTextOriginalPosition;
            Vector3 endPos = startPos + new Vector3(0, textMoveDistance, 0);
            
            while (elapsed < textDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / textDuration;
                
                // Mover hacia arriba
                penaltyText.transform.position = Vector3.Lerp(startPos, endPos, progress);
                
                // Fade out
                Color textColor = Color.red;
                textColor.a = Mathf.Lerp(1f, 0f, progress);
                penaltyText.color = textColor;
                
                yield return null;
            }
            
            // Ocultar texto
            penaltyText.gameObject.SetActive(false);
        }
        
        // Reproducir sonido de penalización
        void PlayPenaltySound()
        {
            if (audioSource != null && penaltySound != null)
            {
                audioSource.PlayOneShot(penaltySound, soundVolume);
            }
        }
        
        // Vibración fuerte para móvil
        void TriggerStrongVibration()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Vibración usando AndroidJavaClass para más control
            try 
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                vibrator.Call("vibrate", vibrationDuration);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"No se pudo activar vibración: {e.Message}");
                Handheld.Vibrate(); // Fallback
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
        
        // MÉTODOS PÚBLICOS PARA CONFIGURACIÓN
        
        public void SetFlashColor(Color newColor)
        {
            flashColor = newColor;
        }
        
        public void SetFlashDuration(float duration)
        {
            flashDuration = Mathf.Max(0.1f, duration);
        }
        
        public void SetPenaltySound(AudioClip clip)
        {
            penaltySound = clip;
        }
        
        // Método simple para activar solo el flash (sin cantidad)
        public void TriggerSimpleFlash()
        {
            if (!isFlashing)
            {
                StartCoroutine(ScreenFlashCoroutine());
            }
        }
        
        // MÉTODOS DE DEBUG PARA TESTING
        
        [ContextMenu("Test Penalty Feedback")]
        public void TestPenaltyFeedback()
        {
            TriggerInnocentPenalty(15); // Simular pérdida de $15
        }
        
        [ContextMenu("Test Flash Only")]
        public void TestFlashOnly()
        {
            TriggerSimpleFlash();
        }
        
        [ContextMenu("Test Vibration")]
        public void TestVibration()
        {
            TriggerStrongVibration();
        }
        
        void OnValidate()
        {
            // Validar valores en el inspector
            flashDuration = Mathf.Max(0.1f, flashDuration);
            textDuration = Mathf.Max(0.5f, textDuration);
            textMoveDistance = Mathf.Max(10f, textMoveDistance);
            soundVolume = Mathf.Clamp01(soundVolume);
            
            if (vibrationDuration < 50)
                vibrationDuration = 50;
        }
    }
}