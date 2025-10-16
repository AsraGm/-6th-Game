using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ShootingRange
{
    public class ThemeStoreManager : MonoBehaviour
    {
        [Header("Sistema de Temas")]
        [Tooltip("ARRASTRA AQUÍ todos los temas disponibles del juego")]
        public List<SOGameTheme> availableThemes = new List<SOGameTheme>();

        [Header("Referencias de Sistemas")]
        [Tooltip("ARRASTRA AQUÍ tu MoneySystem")]
        public MoneySystem moneySystem;

        [Tooltip("ARRASTRA AQUÍ tu ThemeManager")]
        public ThemeManager themeManager;

        [Header("Configuración de Economía")]
        [Tooltip("Tema por defecto que está desbloqueado desde el inicio")]
        public SOGameTheme defaultTheme;

        public event System.Action OnStoreUpdated;
        public event System.Action<SOGameTheme> OnThemePurchased;
        public event System.Action<SOGameTheme> OnThemeEquipped;
        public event System.Action<string> OnPurchaseFailed;

        public static ThemeStoreManager Instance { get; private set; }

        private HashSet<string> unlockedThemeIDs = new HashSet<string>();
        private string equippedThemeID = "";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            InitializeStore();
        }

        void InitializeStore()
        {
            FindSystems();

            LoadStoreData();

            if (unlockedThemeIDs.Count == 0 && defaultTheme != null)
            {
                UnlockTheme(defaultTheme.themeID, false);
                EquipTheme(defaultTheme.themeID, false);
                SaveStoreData();
            }

            ApplyEquippedTheme();
        }

        void FindSystems()
        {
            if (moneySystem == null)
            {
                moneySystem = FindObjectOfType<MoneySystem>();
                if (moneySystem == null)
                {
                }
            }

            if (themeManager == null)
            {
                themeManager = FindObjectOfType<ThemeManager>();
                if (themeManager == null)
                {
                }
            }
        }

        public bool TryPurchaseTheme(string themeID)
        {
            SOGameTheme theme = GetThemeByID(themeID);
            if (theme == null)
            {
                OnPurchaseFailed?.Invoke($"Tema '{themeID}' no encontrado");
                return false;
            }

            if (IsThemeUnlocked(themeID))
            {
                OnPurchaseFailed?.Invoke("Este tema ya está desbloqueado");
                return false;
            }

            if (moneySystem == null || !moneySystem.CanAfford(theme.themeCost))
            {
                OnPurchaseFailed?.Invoke($"Dinero insuficiente. Necesitas ${theme.themeCost}");
                Debug.LogWarning($"No hay suficiente dinero para comprar '{theme.themeName}'. Costo: {theme.themeCost}");
                return false;
            }

            if (moneySystem.SpendMoney(theme.themeCost))
            {
                UnlockTheme(themeID);
                OnThemePurchased?.Invoke(theme);

                Debug.Log($"✓ Tema '{theme.themeName}' comprado exitosamente por ${theme.themeCost}");
                return true;
            }

            OnPurchaseFailed?.Invoke("Error al procesar la compra");
            return false;
        }

        public void UnlockTheme(string themeID, bool save = true)
        {
            if (string.IsNullOrEmpty(themeID))
            {
                Debug.LogWarning("ThemeID vacío, no se puede desbloquear");
                return;
            }

            if (!unlockedThemeIDs.Contains(themeID))
            {
                unlockedThemeIDs.Add(themeID);

                if (save)
                {
                    SaveStoreData();
                }

                OnStoreUpdated?.Invoke();
                Debug.Log($"Tema '{themeID}' desbloqueado");
            }
        }

        public bool EquipTheme(string themeID, bool save = true)
        {
            if (!IsThemeUnlocked(themeID))
            {
                Debug.LogWarning($"No se puede equipar tema '{themeID}' porque no está desbloqueado");
                return false;
            }

            SOGameTheme theme = GetThemeByID(themeID);
            if (theme == null)
            {
                Debug.LogError($"Tema '{themeID}' no encontrado");
                return false;
            }

            equippedThemeID = themeID;

            if (themeManager != null)
            {
                themeManager.SetCurrentTheme(theme);
            }

            if (save)
            {
                SaveStoreData();
            }

            OnThemeEquipped?.Invoke(theme);
            OnStoreUpdated?.Invoke();

            Debug.Log($"Tema '{theme.themeName}' equipado");
            return true;
        }

        public bool IsThemeUnlocked(string themeID)
        {
            return unlockedThemeIDs.Contains(themeID);
        }

        public bool IsThemeEquipped(string themeID)
        {
            return equippedThemeID == themeID;
        }

        public SOGameTheme GetEquippedTheme()
        {
            return GetThemeByID(equippedThemeID);
        }

        public SOGameTheme GetThemeByID(string themeID)
        {
            return availableThemes.FirstOrDefault(t => t.themeID == themeID);
        }

        public List<SOGameTheme> GetUnlockedThemes()
        {
            return availableThemes.Where(t => IsThemeUnlocked(t.themeID)).ToList();
        }

        public List<SOGameTheme> GetLockedThemes()
        {
            return availableThemes.Where(t => !IsThemeUnlocked(t.themeID)).ToList();
        }

        public ThemeState GetThemeState(string themeID)
        {
            if (IsThemeEquipped(themeID))
                return ThemeState.Equipped;
            else if (IsThemeUnlocked(themeID))
                return ThemeState.Unlocked;
            else
                return ThemeState.Locked;
        }

        void SaveStoreData()
        {
            if (unlockedThemeIDs == null || unlockedThemeIDs.Count == 0)
            {
                return;
            }

            string unlockedThemesString = string.Join(",", unlockedThemeIDs);
            PlayerPrefs.SetString("UnlockedThemes", unlockedThemesString);

            PlayerPrefs.SetString("EquippedTheme", equippedThemeID);

            PlayerPrefs.Save();
        }

        void LoadStoreData()
        {
            string unlockedThemesString = PlayerPrefs.GetString("UnlockedThemes", "");

            if (!string.IsNullOrEmpty(unlockedThemesString))
            {
                string[] themeIDs = unlockedThemesString.Split(',');

                unlockedThemeIDs = new HashSet<string>();
                foreach (string id in themeIDs)
                {
                    string cleanID = id.Trim();
                    if (!string.IsNullOrEmpty(cleanID))
                    {
                        unlockedThemeIDs.Add(cleanID);
                    }
                }
            }
            else
            {
                unlockedThemeIDs = new HashSet<string>();
            }
            equippedThemeID = PlayerPrefs.GetString("EquippedTheme", "");
        }

        void ApplyEquippedTheme()
        {
            if (!string.IsNullOrEmpty(equippedThemeID))
            {
                SOGameTheme theme = GetThemeByID(equippedThemeID);
                if (theme != null && themeManager != null)
                {
                    themeManager.SetCurrentTheme(theme);
                }
            }
        }

        [ContextMenu("Unlock All Themes")]
        public void UnlockAllThemes()
        {
            foreach (var theme in availableThemes)
            {
                UnlockTheme(theme.themeID, false);
            }
            SaveStoreData();
            OnStoreUpdated?.Invoke();
        }

        [ContextMenu("Reset All Themes")]
        public void ResetAllThemes()
        {
            unlockedThemeIDs.Clear();
            equippedThemeID = "";

            // Desbloquear solo el tema por defecto
            if (defaultTheme != null)
            {
                UnlockTheme(defaultTheme.themeID, false);
                EquipTheme(defaultTheme.themeID, false);
            }

            SaveStoreData();
            OnStoreUpdated?.Invoke();
        }

        [ContextMenu("Log Store Status")]
        public void LogStoreStatus()
        {
            Debug.Log("\n--- LISTA DE TEMAS DESBLOQUEADOS ---");
            foreach (string id in unlockedThemeIDs)
            {
                Debug.Log($"  ✓ {id}");
            }

            foreach (var theme in availableThemes)
            {
                string status = GetThemeState(theme.themeID).ToString();
            }
            string savedThemes = PlayerPrefs.GetString("UnlockedThemes", "VACÍO");
            string savedEquipped = PlayerPrefs.GetString("EquippedTheme", "VACÍO");
        }
    }

    public enum ThemeState
    {
        Locked,   
        Unlocked,  
        Equipped   
    }
}