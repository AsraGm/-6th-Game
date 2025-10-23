using UnityEngine;
using UnityEngine.UI;

// ARCHIVO: MobileHUD.cs
// INSTRUCCIÓN D1: HUD optimizado para pantallas móviles
// Maneja Safe Area, responsive layout y tamaños táctiles

namespace ShootingRange
{
    public class MobileHUD : MonoBehaviour
    {
        [Header("Referencias de Canvas")]
        [Tooltip("Canvas principal del HUD")]
        public Canvas hudCanvas;
        
        [Tooltip("Panel contenedor principal")]
        public RectTransform mainPanel;
        
        [Header("Safe Area Configuration")]
        [Tooltip("Aplicar Safe Area automáticamente (para notch)")]
        public bool applySafeArea = true;
        
        [Tooltip("Panel que se ajustará al Safe Area")]
        public RectTransform safeAreaPanel;
        
        [Header("Responsive Settings")]
        [Tooltip("Tamaño mínimo táctil en píxeles (recomendado 44px)")]
        [Range(32f, 64f)]
        public float minTouchSize = 44f;
        
        [Tooltip("Escala automática para diferentes aspectos")]
        public bool autoScaleForAspect = true;
        
        [Header("HUD Elements")]
        [Tooltip("Referencias opcionales para validar tamaños")]
        public Button[] hudButtons;
        
        [Tooltip("Elementos UI críticos")]
        public RectTransform[] criticalUIElements;
        
        [Header("Performance")]
        [Tooltip("Modo de render del Canvas")]
        public RenderMode canvasRenderMode = RenderMode.ScreenSpaceCamera;
        
        [Tooltip("Cámara para el Canvas (si usa Camera mode)")]
        public Camera uiCamera;
        
        [Header("Debug Info")]
        [SerializeField] private Vector2 screenSize;
        [SerializeField] private float screenAspect;
        [SerializeField] private Rect safeArea;
        [SerializeField] private bool hasNotch;
        
        // Variables privadas
        private Rect lastSafeArea;
        private Vector2 lastScreenSize;
        
        void Start()
        {
            InitializeHUD();
        }
        
        void InitializeHUD()
        {
            SetupCanvas();
            
            if (applySafeArea)
            {
                ApplySafeAreaToPanel();
            }
            
            if (autoScaleForAspect)
            {
                AdjustForScreenAspect();
            }
            
            ValidateButtonSizes();
            
            UpdateDebugInfo();
            
            Debug.Log($"MobileHUD inicializado - Resolución: {Screen.width}x{Screen.height}, Aspect: {screenAspect:F2}");
        }
        
        void SetupCanvas()
        {
            if (hudCanvas == null)
            {
                hudCanvas = GetComponent<Canvas>();
            }
            
            if (hudCanvas != null)
            {
                // Configurar modo de render
                hudCanvas.renderMode = canvasRenderMode;
                
                if (canvasRenderMode == RenderMode.ScreenSpaceCamera)
                {
                    if (uiCamera == null)
                    {
                        uiCamera = Camera.main;
                    }
                    hudCanvas.worldCamera = uiCamera;
                    hudCanvas.planeDistance = 10f;
                }
                
                // Configurar CanvasScaler para responsive
                CanvasScaler scaler = hudCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920); // Resolución base móvil vertical
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f; // Balance entre width y height
                }
            }
        }
        
        void ApplySafeAreaToPanel()
        {
            if (safeAreaPanel == null)
            {
                Debug.LogWarning("MobileHUD: No se asignó SafeAreaPanel");
                return;
            }
            
            Rect safe = Screen.safeArea;
            
            // Convertir safe area a anchors
            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            
            // Aplicar
            safeAreaPanel.anchorMin = anchorMin;
            safeAreaPanel.anchorMax = anchorMax;
            
            lastSafeArea = safe;
            
            // Detectar si hay notch
            hasNotch = (safe.width < Screen.width) || (safe.height < Screen.height);
            
            if (hasNotch)
            {
                Debug.Log($"Notch detectado - Safe Area: {safe}");
            }
        }
        
        void AdjustForScreenAspect()
        {
            float aspect = (float)Screen.width / Screen.height;
            
            // Ajustar escala para aspectos extremos
            if (aspect > 2f) // Pantallas muy anchas (ej: tablets horizontales)
            {
                if (mainPanel != null)
                {
                    mainPanel.localScale = Vector3.one * 1.2f;
                }
            }
            else if (aspect < 0.5f) // Pantallas muy altas
            {
                if (mainPanel != null)
                {
                    mainPanel.localScale = Vector3.one * 0.9f;
                }
            }
        }
        
        void ValidateButtonSizes()
        {
            if (hudButtons == null || hudButtons.Length == 0)
                return;
            
            foreach (Button button in hudButtons)
            {
                if (button == null) continue;
                
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (buttonRect == null) continue;
                
                // Calcular tamaño en píxeles
                Vector2 size = buttonRect.rect.size;
                float canvasScale = hudCanvas.scaleFactor;
                float actualWidth = size.x * canvasScale;
                float actualHeight = size.y * canvasScale;
                
                // Validar tamaño mínimo táctil
                if (actualWidth < minTouchSize || actualHeight < minTouchSize)
                {
                    Debug.LogWarning($"Botón '{button.name}' es muy pequeño: {actualWidth:F0}x{actualHeight:F0}px (mínimo: {minTouchSize}px)");
                    
                    // Auto-ajustar si es posible
                    float scaleFactor = minTouchSize / Mathf.Min(actualWidth, actualHeight);
                    buttonRect.localScale *= scaleFactor;
                    
                    Debug.Log($"Botón '{button.name}' auto-escalado a: {size.x * scaleFactor:F0}x{size.y * scaleFactor:F0}");
                }
            }
        }
        
        void Update()
        {
            // Detectar cambios en resolución o safe area
            if (Screen.safeArea != lastSafeArea || 
                new Vector2(Screen.width, Screen.height) != lastScreenSize)
            {
                OnScreenChanged();
            }
        }
        
        void OnScreenChanged()
        {
            Debug.Log("Cambio de pantalla detectado, reajustando HUD...");
            
            if (applySafeArea)
            {
                ApplySafeAreaToPanel();
            }
            
            if (autoScaleForAspect)
            {
                AdjustForScreenAspect();
            }
            
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            UpdateDebugInfo();
        }
        
        void UpdateDebugInfo()
        {
            screenSize = new Vector2(Screen.width, Screen.height);
            screenAspect = (float)Screen.width / Screen.height;
            safeArea = Screen.safeArea;
        }
        
        // ========================================
        // MÉTODOS PÚBLICOS
        // ========================================
        
        /// <summary>
        /// Muestra el HUD
        /// </summary>
        public void ShowHUD()
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Oculta el HUD
        /// </summary>
        public void HideHUD()
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Fuerza recalcular safe area
        /// </summary>
        public void ForceRefresh()
        {
            OnScreenChanged();
        }
        
        /// <summary>
        /// Obtiene el factor de escala actual del canvas
        /// </summary>
        public float GetCanvasScale()
        {
            return hudCanvas != null ? hudCanvas.scaleFactor : 1f;
        }
        
        /// <summary>
        /// Convierte tamaño de píxeles a unidades de canvas
        /// </summary>
        public float PixelsToCanvasUnits(float pixels)
        {
            return pixels / GetCanvasScale();
        }
        
        /// <summary>
        /// Valida si un elemento UI cumple con tamaño táctil mínimo
        /// </summary>
        public bool ValidateTouchSize(RectTransform element)
        {
            if (element == null) return false;
            
            Vector2 size = element.rect.size;
            float canvasScale = GetCanvasScale();
            float actualWidth = size.x * canvasScale;
            float actualHeight = size.y * canvasScale;
            
            return actualWidth >= minTouchSize && actualHeight >= minTouchSize;
        }
        
        // ========================================
        // MÉTODOS DE DEBUG
        // ========================================
        
        [ContextMenu("Log HUD Info")]
        public void LogHUDInfo()
        {
            Debug.Log("=== MOBILE HUD INFO ===");
            Debug.Log($"Screen Size: {screenSize}");
            Debug.Log($"Screen Aspect: {screenAspect:F2}");
            Debug.Log($"Safe Area: {safeArea}");
            Debug.Log($"Has Notch: {hasNotch}");
            Debug.Log($"Canvas Scale: {GetCanvasScale():F2}");
            Debug.Log($"Min Touch Size: {minTouchSize}px");
        }
        
        [ContextMenu("Force Refresh HUD")]
        public void DebugForceRefresh()
        {
            ForceRefresh();
        }
        
        [ContextMenu("Validate All Buttons")]
        public void DebugValidateButtons()
        {
            ValidateButtonSizes();
        }
        
        void OnValidate()
        {
            minTouchSize = Mathf.Max(32f, minTouchSize);
        }
    }
}