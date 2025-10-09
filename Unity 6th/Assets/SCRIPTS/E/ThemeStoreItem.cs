using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ARCHIVO: ThemeStoreItem.cs
// INSTRUCCIÓN E3: Componente para cada item de tema en la lista de la tienda
// Se adjunta al prefab de cada botón de tema

namespace ShootingRange
{
    public class ThemeStoreItem : MonoBehaviour
    {
        [Header("Referencias UI del Item")]
        [Tooltip("Botón principal del item")]
        public Button itemButton;
        
        [Tooltip("Icono/preview del tema")]
        public Image themeIconImage;
        
        [Tooltip("Nombre del tema")]
        public TextMeshProUGUI themeNameText;
        
        [Tooltip("Precio del tema")]
        public TextMeshProUGUI themePriceText;
        
        [Tooltip("Indicador de estado (Locked/Unlocked/Equipped)")]
        public GameObject lockedIndicator;
        
        [Tooltip("Indicador de tema equipado")]
        public GameObject equippedIndicator;
        
        [Tooltip("Overlay para temas bloqueados")]
        public Image lockOverlay;
        
        [Header("Configuración Visual")]
        [Tooltip("Color cuando está bloqueado")]
        public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        [Tooltip("Color cuando está desbloqueado")]
        public Color unlockedColor = Color.white;
        
        [Tooltip("Color cuando está equipado")]
        public Color equippedColor = new Color(0.3f, 1f, 0.3f, 1f);
        
        [Tooltip("Prefijo del precio")]
        public string pricePrefix = "$";
        
        // Referencias privadas
        private SOGameTheme theme;
        private ThemeStoreUI storeUI;
        private ThemeStoreManager storeManager;
        
        /// <summary>
        /// Inicializa el item con un tema específico
        /// </summary>
        public void Initialize(SOGameTheme themeData, ThemeStoreUI ui)
        {
            theme = themeData;
            storeUI = ui;
            storeManager = ThemeStoreManager.Instance;
            
            // Configurar botón
            if (itemButton != null)
            {
                itemButton.onClick.AddListener(OnItemClicked);
            }
            
            // Actualizar visual
            UpdateVisuals();
            UpdateState();
        }
        
        /// <summary>
        /// Actualiza los visuales del item (nombre, icono, precio)
        /// </summary>
        void UpdateVisuals()
        {
            if (theme == null) return;
            
            // Nombre del tema
            if (themeNameText != null)
            {
                themeNameText.text = theme.themeName;
            }
            
            // Icono del tema
            if (themeIconImage != null && theme.themeIcon != null)
            {
                themeIconImage.sprite = theme.themeIcon;
            }
            else if (themeIconImage != null && theme.backgroundSprite != null)
            {
                // Usar background como fallback si no hay icono
                themeIconImage.sprite = theme.backgroundSprite;
            }
            
            // Precio del tema
            if (themePriceText != null)
            {
                if (theme.themeCost > 0)
                {
                    themePriceText.text = $"{pricePrefix}{theme.themeCost}";
                }
                else
                {
                    themePriceText.text = "GRATIS";
                }
            }
        }
        
        /// <summary>
        /// Actualiza el estado visual del item (bloqueado/desbloqueado/equipado)
        /// </summary>
        public void UpdateState()
        {
            if (theme == null || storeManager == null) return;
            
            ThemeState state = storeManager.GetThemeState(theme.themeID);
            
            switch (state)
            {
                case ThemeState.Locked:
                    SetLockedState();
                    break;
                
                case ThemeState.Unlocked:
                    SetUnlockedState();
                    break;
                
                case ThemeState.Equipped:
                    SetEquippedState();
                    break;
            }
        }
        
        void SetLockedState()
        {
            // Mostrar indicador de bloqueado
            if (lockedIndicator != null)
                lockedIndicator.SetActive(true);
            
            if (equippedIndicator != null)
                equippedIndicator.SetActive(false);
            
            // Aplicar overlay de bloqueado
            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(true);
                lockOverlay.color = new Color(0, 0, 0, 0.5f); // Semi-transparente negro
            }
            
            // Color del icono
            if (themeIconImage != null)
            {
                themeIconImage.color = lockedColor;
            }
            
            // Mostrar precio
            if (themePriceText != null)
            {
                themePriceText.gameObject.SetActive(true);
            }
        }
        
        void SetUnlockedState()
        {
            // Ocultar indicadores
            if (lockedIndicator != null)
                lockedIndicator.SetActive(false);
            
            if (equippedIndicator != null)
                equippedIndicator.SetActive(false);
            
            // Quitar overlay
            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(false);
            }
            
            // Color del icono
            if (themeIconImage != null)
            {
                themeIconImage.color = unlockedColor;
            }
            
            // Ocultar precio (ya está comprado)
            if (themePriceText != null)
            {
                themePriceText.gameObject.SetActive(false);
            }
        }
        
        void SetEquippedState()
        {
            // Ocultar locked, mostrar equipped
            if (lockedIndicator != null)
                lockedIndicator.SetActive(false);
            
            if (equippedIndicator != null)
                equippedIndicator.SetActive(true);
            
            // Quitar overlay
            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(false);
            }
            
            // Color destacado del icono
            if (themeIconImage != null)
            {
                themeIconImage.color = equippedColor;
            }
            
            // Ocultar precio
            if (themePriceText != null)
            {
                themePriceText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Callback cuando se hace clic en el item
        /// </summary>
        void OnItemClicked()
        {
            if (theme == null || storeUI == null) return;
            
            // Mostrar preview del tema en el panel principal
            storeUI.ShowPreview(theme);
            
            Debug.Log($"Item de tema clickeado: {theme.themeName}");
        }
        
        void OnDestroy()
        {
            // Limpiar listener
            if (itemButton != null)
            {
                itemButton.onClick.RemoveListener(OnItemClicked);
            }
        }
    }
}