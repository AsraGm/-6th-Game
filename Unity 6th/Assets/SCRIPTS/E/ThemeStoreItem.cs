using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        private SOGameTheme theme;
        private ThemeStoreUI storeUI;
        private ThemeStoreManager storeManager;

        public void Initialize(SOGameTheme themeData, ThemeStoreUI ui)
        {
            theme = themeData;
            storeUI = ui;
            storeManager = ThemeStoreManager.Instance;

            if (itemButton != null)
            {
                itemButton.onClick.AddListener(OnItemClicked);
            }

            UpdateVisuals();
            UpdateState();
        }

        void UpdateVisuals()
        {
            if (theme == null) return;

            if (themeNameText != null)
            {
                themeNameText.text = theme.themeName;
            }

            if (themeIconImage != null && theme.themeIcon != null)
            {
                themeIconImage.sprite = theme.themeIcon;
            }
            else if (themeIconImage != null && theme.backgroundSprite != null)
            {
                themeIconImage.sprite = theme.backgroundSprite;
            }

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
            if (lockedIndicator != null)
                lockedIndicator.SetActive(true);

            if (equippedIndicator != null)
                equippedIndicator.SetActive(false);

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(true);
                lockOverlay.color = new Color(0, 0, 0, 0.5f); 
            }

            if (themeIconImage != null)
            {
                themeIconImage.color = lockedColor;
            }

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
                themePriceText.color = Color.white;
            }
        }

        void SetUnlockedState()
        {
            gameObject.SetActive(true);

            if (lockedIndicator != null)
                lockedIndicator.SetActive(false);

            if (equippedIndicator != null)
                equippedIndicator.SetActive(false);

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(false);
            }

            if (themeIconImage != null)
            {
                themeIconImage.color = unlockedColor;
                themeIconImage.gameObject.SetActive(true);
            }

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

            if (itemButton != null)
            {
                itemButton.interactable = true;
            }
        }

        void SetEquippedState()
        {
            if (lockedIndicator != null)
                lockedIndicator.SetActive(false);

            if (equippedIndicator != null)
                equippedIndicator.SetActive(true);

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(false);
            }

            if (themeIconImage != null)
            {
                themeIconImage.color = equippedColor;
            }

            if (themePriceText != null)
            {
                themePriceText.gameObject.SetActive(true);
                themePriceText.text = "✓ EQUIPADO";
                themePriceText.color = equippedColor; 
            }
        }

        void OnItemClicked()
        {
            if (theme == null || storeUI == null) return;

            storeUI.ShowPreview(theme);

            Debug.Log($"Item de tema clickeado: {theme.themeName}");
        }

        void OnDestroy()
        {
            if (itemButton != null)
            {
                itemButton.onClick.RemoveListener(OnItemClicked);
            }
        }
    }
}