using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ARCHIVO: MobileCrosshair.cs
// INSTRUCCIÓN D2: Sistema de crosshair/mira móvil básico
// Mira simple que aparece en posición del toque con feedback visual

namespace ShootingRange
{
    public class MobileCrosshair : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("Imagen de la mira (debe estar en Canvas)")]
        public Image crosshairImage;
        
        [Tooltip("Canvas padre (se busca automáticamente si está vacío)")]
        public Canvas parentCanvas;
        
        [Header("Configuración Visual")]
        [Tooltip("Sprite de la mira")]
        public Sprite crosshairSprite;
        
        [Tooltip("Tamaño de la mira en píxeles")]
        [Range(32f, 128f)]
        public float crosshairSize = 64f;
        
        [Tooltip("Color de la mira")]
        public Color crosshairColor = Color.white;
        
        [Header("Configuración de Fade")]
        [Tooltip("Tiempo de aparición (fade in)")]
        [Range(0f, 0.5f)]
        public float fadeInTime = 0.1f;
        
        [Tooltip("Tiempo de desaparición (fade out)")]
        [Range(0f, 0.5f)]
        public float fadeOutTime = 0.2f;
        
        [Tooltip("Duración de la mira visible (0 = permanente)")]
        [Range(0f, 2f)]
        public float visibleDuration = 0.5f;
        
        [Header("Feedback de Disparo")]
        [Tooltip("Mostrar feedback al disparar")]
        public bool showShootFeedback = true;
        
        [Tooltip("Color al disparar")]
        public Color shootColor = Color.red;
        
        [Tooltip("Escala al disparar (multiplicador)")]
        [Range(0.5f, 2f)]
        public float shootScale = 1.3f;
        
        [Tooltip("Duración del feedback de disparo")]
        [Range(0.05f, 0.3f)]
        public float shootFeedbackDuration = 0.15f;
        
        [Header("Optimización")]
        [Tooltip("Seguir el dedo mientras se mantiene presionado")]
        public bool followTouch = true;
        
        [Tooltip("Ocultar mira automáticamente al soltar")]
        public bool hideOnRelease = true;
        
        // Variables privadas
        private RectTransform crosshairRect;
        private Vector2 originalSize;
        private Color originalColor;
        private bool isVisible = false;
        private Coroutine fadeCoroutine;
        private Coroutine feedbackCoroutine;
        private Camera gameCamera;
        
        void Start()
        {
            InitializeCrosshair();
        }
        
        void InitializeCrosshair()
        {
            gameCamera = Camera.main;
            
            // Buscar canvas si no está asignado
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas == null)
                {
                    Debug.LogError("MobileCrosshair: No se encontró Canvas padre");
                    return;
                }
            }
            
            // Configurar crosshair image
            if (crosshairImage == null)
            {
                CreateCrosshairImage();
            }
            else
            {
                crosshairRect = crosshairImage.GetComponent<RectTransform>();
            }
            
            // Configurar propiedades visuales
            if (crosshairImage != null)
            {
                if (crosshairSprite != null)
                {
                    crosshairImage.sprite = crosshairSprite;
                }
                
                crosshairImage.color = crosshairColor;
                crosshairRect.sizeDelta = new Vector2(crosshairSize, crosshairSize);
                
                originalSize = crosshairRect.sizeDelta;
                originalColor = crosshairColor;
                
                // Ocultar inicialmente
                SetCrosshairAlpha(0f);
                isVisible = false;
            }
            
            Debug.Log("MobileCrosshair inicializado");
        }
        
        void CreateCrosshairImage()
        {
            GameObject crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(parentCanvas.transform, false);
            
            crosshairImage = crosshairObj.AddComponent<Image>();
            crosshairRect = crosshairImage.GetComponent<RectTransform>();
            
            // Configurar como overlay (no bloquea raycast)
            crosshairImage.raycastTarget = false;
            
            Debug.Log("Crosshair Image creada automáticamente");
        }
        
        void Update()
        {
            // Seguir el toque si está habilitado
            if (followTouch && isVisible && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                UpdateCrosshairPosition(touch.position);
            }
            // Alternativa para mouse (testing en editor)
            else if (followTouch && isVisible && Input.GetMouseButton(0))
            {
                UpdateCrosshairPosition(Input.mousePosition);
            }
            
            // Ocultar al soltar
            if (hideOnRelease && isVisible)
            {
                if (Input.touchCount == 0 && !Input.GetMouseButton(0))
                {
                    HideCrosshair();
                }
            }
        }
        
        // ========================================
        // MÉTODOS PRINCIPALES
        // ========================================
        
        /// <summary>
        /// Muestra la mira en una posición de pantalla
        /// </summary>
        public void ShowCrosshair(Vector2 screenPosition)
        {
            if (crosshairImage == null) return;
            
            // Cancelar fade anterior
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            // Posicionar
            UpdateCrosshairPosition(screenPosition);
            
            // Resetear escala y color
            crosshairRect.sizeDelta = originalSize;
            crosshairImage.color = originalColor;
            
            // Fade in
            if (fadeInTime > 0)
            {
                fadeCoroutine = StartCoroutine(FadeIn());
            }
            else
            {
                SetCrosshairAlpha(1f);
                isVisible = true;
            }
            
            // Auto-ocultar si hay duración configurada
            if (visibleDuration > 0)
            {
                Invoke(nameof(HideCrosshair), visibleDuration);
            }
        }
        
        /// <summary>
        /// Oculta la mira
        /// </summary>
        public void HideCrosshair()
        {
            if (!isVisible) return;
            
            // Cancelar fade anterior
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            // Cancelar auto-hide
            CancelInvoke(nameof(HideCrosshair));
            
            // Fade out
            if (fadeOutTime > 0)
            {
                fadeCoroutine = StartCoroutine(FadeOut());
            }
            else
            {
                SetCrosshairAlpha(0f);
                isVisible = false;
            }
        }
        
        /// <summary>
        /// Muestra feedback visual de disparo
        /// </summary>
        public void TriggerShootFeedback()
        {
            if (!showShootFeedback || !isVisible) return;
            
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }
            
            feedbackCoroutine = StartCoroutine(ShootFeedbackCoroutine());
        }
        
        void UpdateCrosshairPosition(Vector2 screenPosition)
        {
            if (crosshairRect == null || parentCanvas == null) return;
            
            // Convertir posición de pantalla a posición de canvas
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPosition,
                parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : gameCamera,
                out localPoint
            );
            
            crosshairRect.localPosition = localPoint;
        }
        
        // ========================================
        // COROUTINES
        // ========================================
        
        IEnumerator FadeIn()
        {
            float elapsed = 0f;
            
            while (elapsed < fadeInTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInTime);
                SetCrosshairAlpha(alpha);
                yield return null;
            }
            
            SetCrosshairAlpha(1f);
            isVisible = true;
        }
        
        IEnumerator FadeOut()
        {
            float elapsed = 0f;
            float startAlpha = crosshairImage.color.a;
            
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutTime);
                SetCrosshairAlpha(alpha);
                yield return null;
            }
            
            SetCrosshairAlpha(0f);
            isVisible = false;
        }
        
        IEnumerator ShootFeedbackCoroutine()
        {
            // Guardar valores originales
            Vector2 originalSizeCached = crosshairRect.sizeDelta;
            Color originalColorCached = crosshairImage.color;
            
            // Aplicar feedback (escala y color)
            crosshairRect.sizeDelta = originalSizeCached * shootScale;
            crosshairImage.color = shootColor;
            
            // Esperar duración del feedback
            yield return new WaitForSeconds(shootFeedbackDuration);
            
            // Volver a normal
            float elapsed = 0f;
            float returnDuration = shootFeedbackDuration * 0.5f;
            
            while (elapsed < returnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / returnDuration;
                
                crosshairRect.sizeDelta = Vector2.Lerp(
                    originalSizeCached * shootScale,
                    originalSizeCached,
                    progress
                );
                
                crosshairImage.color = Color.Lerp(
                    shootColor,
                    originalColorCached,
                    progress
                );
                
                yield return null;
            }
            
            // Asegurar valores finales
            crosshairRect.sizeDelta = originalSizeCached;
            crosshairImage.color = originalColorCached;
        }
        
        void SetCrosshairAlpha(float alpha)
        {
            if (crosshairImage == null) return;
            
            Color color = crosshairImage.color;
            color.a = alpha;
            crosshairImage.color = color;
        }
        
        // ========================================
        // MÉTODOS PÚBLICOS DE CONFIGURACIÓN
        // ========================================
        
        public void SetCrosshairColor(Color newColor)
        {
            crosshairColor = newColor;
            originalColor = newColor;
            if (crosshairImage != null && !isVisible)
            {
                crosshairImage.color = newColor;
            }
        }
        
        public void SetCrosshairSize(float newSize)
        {
            crosshairSize = Mathf.Max(32f, newSize);
            originalSize = new Vector2(crosshairSize, crosshairSize);
            if (crosshairRect != null && !isVisible)
            {
                crosshairRect.sizeDelta = originalSize;
            }
        }
        
        public void SetCrosshairSprite(Sprite newSprite)
        {
            crosshairSprite = newSprite;
            if (crosshairImage != null)
            {
                crosshairImage.sprite = newSprite;
            }
        }
        
        public bool IsVisible() => isVisible;
        
        // ========================================
        // MÉTODOS DE DEBUG
        // ========================================
        
        [ContextMenu("Test Show Crosshair Center")]
        public void TestShowCenter()
        {
            Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
            ShowCrosshair(center);
        }
        
        [ContextMenu("Test Hide Crosshair")]
        public void TestHide()
        {
            HideCrosshair();
        }
        
        [ContextMenu("Test Shoot Feedback")]
        public void TestShootFeedback()
        {
            if (!isVisible)
            {
                TestShowCenter();
            }
            TriggerShootFeedback();
        }
        
        void OnValidate()
        {
            crosshairSize = Mathf.Max(32f, crosshairSize);
            fadeInTime = Mathf.Max(0f, fadeInTime);
            fadeOutTime = Mathf.Max(0f, fadeOutTime);
            visibleDuration = Mathf.Max(0f, visibleDuration);
            shootFeedbackDuration = Mathf.Max(0.05f, shootFeedbackDuration);
        }
    }
}