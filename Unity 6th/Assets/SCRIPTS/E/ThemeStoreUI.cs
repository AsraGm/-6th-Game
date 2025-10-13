using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// ARCHIVO: ThemeStoreUI.cs - VERSIÓN CORREGIDA
// Ahora mantiene todos los items visibles siempre

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

        // Variables privadas
        private List<ThemeStoreItem> spawnedItems = new List<ThemeStoreItem>();
        private SOGameTheme currentPreviewTheme;
        private Coroutine messageCoroutine;

        void Start()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // Buscar sistemas si no están asignados
            FindSystems();

            // Ocultar paneles inicialmente
            if (messagePanel != null)
                messagePanel.SetActive(false);

            if (previewPanel != null)
                previewPanel.SetActive(false);

            // Configurar listeners de botones
            SetupButtons();

            // Suscribirse a eventos del store manager
            if (storeManager != null)
            {
                storeManager.OnStoreUpdated += RefreshStoreUI;
                storeManager.OnThemePurchased += OnThemePurchased;
                storeManager.OnThemeEquipped += OnThemeEquipped;
                storeManager.OnPurchaseFailed += ShowMessage;
            }

            // Suscribirse a cambios de dinero
            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged += UpdateMoneyDisplay;
                UpdateMoneyDisplay(moneySystem.CurrentMoney);
            }

            // Popular la tienda
            PopulateStore();

            Debug.Log("ThemeStoreUI inicializada");
        }

        void FindSystems()
        {
            if (storeManager == null)
            {
                storeManager = ThemeStoreManager.Instance;
                if (storeManager == null)
                {
                    Debug.LogError("ThemeStoreManager no encontrado");
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

        // ========================================
        // POBLACIÓN DE LA TIENDA
        // ========================================

        /// <summary>
        /// Crea todos los items de temas en la UI
        /// </summary>
        void PopulateStore()
        {
            if (storeManager == null || itemsContainer == null || themeItemPrefab == null)
            {
                Debug.LogError("Faltan referencias para popular la tienda");
                return;
            }

            // Limpiar items existentes
            ClearStoreItems();

            // Crear item para cada tema
            foreach (var theme in storeManager.availableThemes)
            {
                CreateThemeItem(theme);
            }

            Debug.Log($"Tienda poblada con {spawnedItems.Count} items");

            // VERIFICACIÓN: Asegurar que todos están activos
            VerifyAllItemsVisible();
        }

        /// <summary>
        /// Crea un item individual de tema
        /// </summary>
        void CreateThemeItem(SOGameTheme theme)
        {
            GameObject itemObj = Instantiate(themeItemPrefab, itemsContainer);

            // FORZAR que el objeto esté activo
            itemObj.SetActive(true);

            ThemeStoreItem item = itemObj.GetComponent<ThemeStoreItem>();

            if (item != null)
            {
                item.Initialize(theme, this);
                spawnedItems.Add(item);
                Debug.Log($"[ThemeStoreUI] Item creado para '{theme.themeName}' - Estado inicial verificado");
            }
            else
            {
                Debug.LogError("ThemeItemPrefab no tiene componente ThemeStoreItem");
                Destroy(itemObj);
            }
        }

        /// <summary>
        /// Limpia todos los items de la tienda
        /// </summary>
        void ClearStoreItems()
        {
            foreach (var item in spawnedItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            spawnedItems.Clear();

            Debug.Log("[ThemeStoreUI] Items limpiados");
        }

        /// <summary>
        /// Refresca todos los items (cuando hay cambios)
        /// </summary>
        void RefreshStoreUI()
        {
            Debug.Log($"[ThemeStoreUI] Refrescando UI - Total items: {spawnedItems.Count}");

            int visibleCount = 0;
            foreach (var item in spawnedItems)
            {
                if (item != null)
                {
                    // FORZAR que el item esté activo antes de actualizar
                    item.gameObject.SetActive(true);
                    item.UpdateState();

                    if (item.gameObject.activeInHierarchy)
                        visibleCount++;
                }
            }

            Debug.Log($"[ThemeStoreUI] Items visibles después del refresh: {visibleCount}/{spawnedItems.Count}");

            // Refrescar preview si hay uno activo
            if (currentPreviewTheme != null)
            {
                ShowPreview(currentPreviewTheme);
            }
        }

        /// <summary>
        /// Verifica que todos los items estén visibles
        /// </summary>
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
                Debug.LogWarning($"[ThemeStoreUI] ⚠️ Hay {spawnedItems.Count - activeCount} items INVISIBLES!");
            }
        }

        // ========================================
        // SISTEMA DE PREVIEW
        // ========================================

        /// <summary>
        /// Muestra el preview de un tema
        /// </summary>
        public void ShowPreview(SOGameTheme theme)
        {
            if (theme == null || previewPanel == null)
                return;

            currentPreviewTheme = theme;
            previewPanel.SetActive(true);

            // Actualizar imágenes
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

            // Actualizar textos
            if (previewThemeNameText != null)
                previewThemeNameText.text = theme.themeName;

            if (previewDescriptionText != null)
                previewDescriptionText.text = theme.themeDescription;

            if (previewPriceText != null)
                previewPriceText.text = $"{moneyPrefix}{theme.themeCost}";

            // Actualizar botones según estado del tema
            UpdatePreviewButtons(theme);
        }

        /// <summary>
        /// Actualiza los botones del preview según el estado del tema
        /// </summary>
        void UpdatePreviewButtons(SOGameTheme theme)
        {
            if (storeManager == null) return;

            ThemeState state = storeManager.GetThemeState(theme.themeID);

            switch (state)
            {
                case ThemeState.Locked:
                    // Mostrar botón de compra
                    if (buyButton != null)
                    {
                        buyButton.gameObject.SetActive(true);
                        bool canAfford = moneySystem != null && moneySystem.CanAfford(theme.themeCost);
                        buyButton.interactable = canAfford;

                        if (buyButtonText != null)
                            buyButtonText.text = canAfford ? "COMPRAR" : "PLAY MORE!!";
                    }

                    if (equipButton != null)
                        equipButton.gameObject.SetActive(false);
                    break;

                case ThemeState.Unlocked:
                    // Mostrar botón de equipar
                    if (buyButton != null)
                        buyButton.gameObject.SetActive(false);

                    if (equipButton != null)
                    {
                        equipButton.gameObject.SetActive(true);
                        equipButton.interactable = true;
                    }
                    break;

                case ThemeState.Equipped:
                    // Tema equipado, mostrar estado
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

        /// <summary>
        /// Oculta el preview
        /// </summary>
        public void HidePreview()
        {
            if (previewPanel != null)
                previewPanel.SetActive(false);

            currentPreviewTheme = null;
        }

        // ========================================
        // EVENTOS DE BOTONES
        // ========================================

        void OnBuyButtonClicked()
        {
            if (currentPreviewTheme == null || storeManager == null)
                return;

            if (storeManager.TryPurchaseTheme(currentPreviewTheme.themeID))
            {
                ShowMessage($"¡{currentPreviewTheme.themeName} comprado!");

                // CRÍTICO: Esperar un frame antes de refrescar
                StartCoroutine(RefreshAfterPurchase());
            }
        }

        System.Collections.IEnumerator RefreshAfterPurchase()
        {
            yield return null; // Esperar un frame

            // Refrescar el preview
            if (currentPreviewTheme != null)
            {
                ShowPreview(currentPreviewTheme);
            }

            // Verificar visibilidad
            VerifyAllItemsVisible();

            Debug.Log("[ThemeStoreUI] UI refrescada después de compra");
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

        // ========================================
        // CALLBACKS DE EVENTOS
        // ========================================

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

            // Actualizar preview si está abierto
            if (currentPreviewTheme != null)
            {
                UpdatePreviewButtons(currentPreviewTheme);
            }
        }

        // ========================================
        // SISTEMA DE MENSAJES
        // ========================================

        /// <summary>
        /// Muestra un mensaje temporal
        /// </summary>
        public void ShowMessage(string message)
        {
            if (messagePanel == null || messageText == null)
                return;

            messageText.text = message;
            messagePanel.SetActive(true);

            // Cancelar mensaje anterior si existe
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

        // ========================================
        // MÉTODOS PÚBLICOS PARA UI
        // ========================================

        /// <summary>
        /// Abre la tienda
        /// </summary>
        public void OpenStore()
        {
            if (storePanel != null)
                storePanel.SetActive(true);

            RefreshStoreUI();
            VerifyAllItemsVisible();
        }

        /// <summary>
        /// Cierra la tienda
        /// </summary>
        public void CloseStore()
        {
            if (storePanel != null)
                storePanel.SetActive(false);

            HidePreview();
        }

        // ========================================
        // DEBUG - CONTEXT MENU
        // ========================================

        [ContextMenu("Force Verify All Items Visible")]
        void ForceVerifyVisibility()
        {
            VerifyAllItemsVisible();

            // Reactivar todos manualmente
            foreach (var item in spawnedItems)
            {
                if (item != null)
                {
                    item.gameObject.SetActive(true);
                    Debug.Log($"[ThemeStoreUI] Forzando visibilidad de: {item.name}");
                }
            }
        }

        void OnDestroy()
        {
            // Desuscribirse de eventos
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