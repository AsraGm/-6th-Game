using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ShootingRange
{
    public class ThemeStoreUI : MonoBehaviour
    {
        [Header("Referencias UI Principal")]
        [Tooltip("Panel principal de la tienda")]
        public GameObject storePanel;

        [Tooltip("ARRASTRA AQUÍ el ScrollView o contenedor donde irán los items")]
        public Transform itemsContainer;

        [Tooltip("ARRASTRA AQUÍ el prefab de ThemeStoreItem")]
        public GameObject themeItemPrefab;

        [Header("Preview del Tema")]
        [Tooltip("Panel de preview del tema seleccionado")]
        public GameObject previewPanel;

        [Tooltip("Imagen para mostrar preview del background")]
        public Image previewBackgroundImage;

        [Tooltip("Imagen para mostrar preview de la cortina")]
        public Image previewCurtainImage;

        [Tooltip("Nombre del tema en preview")]
        public TextMeshProUGUI previewThemeNameText;

        [Tooltip("Descripción del tema en preview")]
        public TextMeshProUGUI previewDescriptionText;

        [Tooltip("Precio del tema en preview")]
        public TextMeshProUGUI previewPriceText;

        [Header("Botones de Preview")]
        [Tooltip("Botón para comprar tema")]
        public Button buyButton;

        [Tooltip("Botón para equipar tema")]
        public Button equipButton;

        [Tooltip("Texto del botón de compra")]
        public TextMeshProUGUI buyButtonText;

        [Header("Display de Dinero")]
        [Tooltip("Texto que muestra el dinero actual del jugador")]
        public TextMeshProUGUI currentMoneyText;

        [Tooltip("Prefijo del dinero")]
        public string moneyPrefix = "$";

        [Header("Feedback")]
        [Tooltip("Panel de mensaje (para errores/confirmaciones)")]
        public GameObject messagePanel;

        [Tooltip("Texto del mensaje")]
        public TextMeshProUGUI messageText;

        [Tooltip("Duración del mensaje en segundos")]
        public float messageDuration = 2f;

        [Header("Referencias de Sistemas")]
        [Tooltip("ARRASTRA AQUÍ tu ThemeStoreManager")]
        public ThemeStoreManager storeManager;

        [Tooltip("ARRASTRA AQUÍ tu MoneySystem")]
        public MoneySystem moneySystem;

        private List<ThemeStoreItem> spawnedItems = new List<ThemeStoreItem>();
        private SOGameTheme currentPreviewTheme;
        private Coroutine messageCoroutine;

        void Start()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            FindSystems();

            if (messagePanel != null)
                messagePanel.SetActive(false);

            if (previewPanel != null)
                previewPanel.SetActive(false);

            SetupButtons();

            if (storeManager != null)
            {
                storeManager.OnStoreUpdated += RefreshStoreUI;
                storeManager.OnThemePurchased += OnThemePurchased;
                storeManager.OnThemeEquipped += OnThemeEquipped;
                storeManager.OnPurchaseFailed += ShowMessage;
            }

            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged += UpdateMoneyDisplay;
                UpdateMoneyDisplay(moneySystem.CurrentMoney);
            }
            PopulateStore();
        }

        void FindSystems()
        {
            if (storeManager == null)
            {
                storeManager = ThemeStoreManager.Instance;
                if (storeManager == null)
                {
                }
            }

            if (moneySystem == null)
            {
                moneySystem = FindObjectOfType<MoneySystem>();
            }
        }

        void SetupButtons()
        {
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(OnBuyButtonClicked);
            }

            if (equipButton != null)
            {
                equipButton.onClick.AddListener(OnEquipButtonClicked);
            }
        }

        void PopulateStore()
        {
            if (storeManager == null || itemsContainer == null || themeItemPrefab == null)
            {
                return;
            }

            ClearStoreItems();

            foreach (var theme in storeManager.availableThemes)
            {
                CreateThemeItem(theme);
            }
            VerifyAllItemsVisible();
        }

        void CreateThemeItem(SOGameTheme theme)
        {
            GameObject itemObj = Instantiate(themeItemPrefab, itemsContainer);

            itemObj.SetActive(true);

            ThemeStoreItem item = itemObj.GetComponent<ThemeStoreItem>();

            if (item != null)
            {
                item.Initialize(theme, this);
                spawnedItems.Add(item);
            }
            else
            {
                Destroy(itemObj);
            }
        }

        void ClearStoreItems()
        {
            foreach (var item in spawnedItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            spawnedItems.Clear();
        }

        void RefreshStoreUI()
        {
            Debug.Log($"[ThemeStoreUI] Refrescando UI - Total items: {spawnedItems.Count}");

            int visibleCount = 0;
            foreach (var item in spawnedItems)
            {
                if (item != null)
                {
                    item.gameObject.SetActive(true);
                    item.UpdateState();

                    if (item.gameObject.activeInHierarchy)
                        visibleCount++;
                }
            }

            if (currentPreviewTheme != null)
            {
                ShowPreview(currentPreviewTheme);
            }
        }

        void VerifyAllItemsVisible()
        {
            int activeCount = 0;
            foreach (var item in spawnedItems)
            {
                if (item != null && item.gameObject.activeInHierarchy)
                    activeCount++;
            }

            Debug.Log($"[ThemeStoreUI] Verificación de visibilidad: {activeCount}/{spawnedItems.Count} items activos");

            if (activeCount < spawnedItems.Count)
            {
            }
        }

        public void ShowPreview(SOGameTheme theme)
        {
            if (theme == null || previewPanel == null)
                return;

            currentPreviewTheme = theme;
            previewPanel.SetActive(true);

            if (previewBackgroundImage != null && theme.backgroundSprite != null)
            {
                previewBackgroundImage.sprite = theme.backgroundSprite;
                previewBackgroundImage.gameObject.SetActive(true);
            }

            if (previewCurtainImage != null && theme.curtainSprite != null)
            {
                previewCurtainImage.sprite = theme.curtainSprite;
                previewCurtainImage.gameObject.SetActive(true);
            }

            if (previewThemeNameText != null)
                previewThemeNameText.text = theme.themeName;

            if (previewDescriptionText != null)
                previewDescriptionText.text = theme.themeDescription;

            if (previewPriceText != null)
                previewPriceText.text = $"{moneyPrefix}{theme.themeCost}";

            UpdatePreviewButtons(theme);
        }

        void UpdatePreviewButtons(SOGameTheme theme)
        {
            if (storeManager == null) return;

            ThemeState state = storeManager.GetThemeState(theme.themeID);

            switch (state)
            {
                case ThemeState.Locked:
                    if (buyButton != null)
                    {
                        buyButton.gameObject.SetActive(true);
                        bool canAfford = moneySystem != null && moneySystem.CanAfford(theme.themeCost);
                        buyButton.interactable = canAfford;

                        if (buyButtonText != null)
                            buyButtonText.text = canAfford ? "BUY" : "PLAY MORE!!";
                    }

                    if (equipButton != null)
                        equipButton.gameObject.SetActive(false);
                    break;

                case ThemeState.Unlocked:
                    if (buyButton != null)
                        buyButton.gameObject.SetActive(false);

                    if (equipButton != null)
                    {
                        equipButton.gameObject.SetActive(true);
                        equipButton.interactable = true;
                    }
                    break;

                case ThemeState.Equipped:
                    if (buyButton != null)
                        buyButton.gameObject.SetActive(false);

                    if (equipButton != null)
                    {
                        equipButton.gameObject.SetActive(true);
                        equipButton.interactable = false;

                        var equipText = equipButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (equipText != null)
                            equipText.text = "EQUIPADO";
                    }
                    break;
            }
        }

        public void HidePreview()
        {
            if (previewPanel != null)
                previewPanel.SetActive(false);

            currentPreviewTheme = null;
        }

        void OnBuyButtonClicked()
        {
            if (currentPreviewTheme == null || storeManager == null)
                return;

            if (storeManager.TryPurchaseTheme(currentPreviewTheme.themeID))
            {
                ShowMessage($"¡{currentPreviewTheme.themeName} comprado!");

                StartCoroutine(RefreshAfterPurchase());
            }
        }

        System.Collections.IEnumerator RefreshAfterPurchase()
        {
            yield return null; 

            if (currentPreviewTheme != null)
            {
                ShowPreview(currentPreviewTheme);
            }

            VerifyAllItemsVisible();
        }

        void OnEquipButtonClicked()
        {
            if (currentPreviewTheme == null || storeManager == null)
                return;

            if (storeManager.EquipTheme(currentPreviewTheme.themeID))
            {
                ShowMessage($"¡{currentPreviewTheme.themeName} equipado!");
            }
        }

        void OnThemePurchased(SOGameTheme theme)
        {
            Debug.Log($"[ThemeStoreUI] Evento: Tema comprado - {theme.themeName}");
        }

        void OnThemeEquipped(SOGameTheme theme)
        {
            Debug.Log($"[ThemeStoreUI] Evento: Tema equipado - {theme.themeName}");
        }

        void UpdateMoneyDisplay(int amount)
        {
            if (currentMoneyText != null)
            {
                currentMoneyText.text = $"{moneyPrefix}{amount}";
            }

            if (currentPreviewTheme != null)
            {
                UpdatePreviewButtons(currentPreviewTheme);
            }
        }

        public void ShowMessage(string message)
        {
            if (messagePanel == null || messageText == null)
                return;

            messageText.text = message;
            messagePanel.SetActive(true);

            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            messageCoroutine = StartCoroutine(HideMessageAfterDelay());
        }

        System.Collections.IEnumerator HideMessageAfterDelay()
        {
            yield return new WaitForSeconds(messageDuration);

            if (messagePanel != null)
                messagePanel.SetActive(false);
        }

        public void OpenStore()
        {
            if (storePanel != null)
                storePanel.SetActive(true);

            RefreshStoreUI();
            VerifyAllItemsVisible();
        }

        public void CloseStore()
        {
            if (storePanel != null)
                storePanel.SetActive(false);

            HidePreview();
        }

        [ContextMenu("Force Verify All Items Visible")]
        void ForceVerifyVisibility()
        {
            VerifyAllItemsVisible();

            foreach (var item in spawnedItems)
            {
                if (item != null)
                {
                    item.gameObject.SetActive(true);
                }
            }
        }

        void OnDestroy()
        {
            if (storeManager != null)
            {
                storeManager.OnStoreUpdated -= RefreshStoreUI;
                storeManager.OnThemePurchased -= OnThemePurchased;
                storeManager.OnThemeEquipped -= OnThemeEquipped;
                storeManager.OnPurchaseFailed -= ShowMessage;
            }

            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged -= UpdateMoneyDisplay;
            }
        }
    }
}