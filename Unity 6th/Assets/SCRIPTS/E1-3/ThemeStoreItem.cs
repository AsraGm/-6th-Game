using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ARCHIVO: ThemeStoreItem.cs - VERSIÓN CORREGIDA
// Los items ahora se mantienen visibles siempre, solo cambian su estado visual

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

            // Precio del tema (SIEMPRE visible, cambia el texto según estado)
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

            // CAMBIO: Mostrar precio con formato de bloqueado
            if (themePriceText != null)
            {
                themePriceText.gameObject.SetActive(true);
                if (theme.themeCost > 0)
                {
                    themePriceText.text = $"{pricePrefix}{theme.themeCost}";
                }
                else
                {
                    themePriceText.text = "GRATIS";
                }
                themePriceText.color = Color.white; // Color normal
            }
        }

        void SetUnlockedState()
        {
            // FORZAR que el item principal esté activo
            gameObject.SetActive(true);

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

            // Color del icono - ASEGURAR QUE SEA VISIBLE
            if (themeIconImage != null)
            {
                themeIconImage.color = unlockedColor;
                themeIconImage.gameObject.SetActive(true); // FORZAR visible
            }

            // MANTENER todo visible
            if (themeNameText != null)
            {
                themeNameText.gameObject.SetActive(true);
            }

            if (themePriceText != null)
            {
                themePriceText.gameObject.SetActive(true);
                themePriceText.text = "DESBLOQUEADO";
                themePriceText.color = new Color(0.5f, 1f, 0.5f, 1f);
            }

            // ASEGURAR que el botón esté interactuable
            if (itemButton != null)
            {
                itemButton.interactable = true;
            }

            Debug.Log($"[ThemeStoreItem] '{theme.themeName}' configurado como DESBLOQUEADO y VISIBLE");
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

            // CAMBIO: Mantener visible pero cambiar texto a "EQUIPADO"
            if (themePriceText != null)
            {
                themePriceText.gameObject.SetActive(true);
                themePriceText.text = "✓ EQUIPADO";
                themePriceText.color = equippedColor; // Color verde equipado
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