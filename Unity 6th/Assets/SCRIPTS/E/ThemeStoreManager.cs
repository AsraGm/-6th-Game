using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ARCHIVO: ThemeStoreManager.cs
// INSTRUCCIÓN E1: Store Manager para temas únicamente
// Maneja compra, desbloqueo y equipamiento de temas

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

        // Eventos para la UI
        public event System.Action OnStoreUpdated;
        public event System.Action<SOGameTheme> OnThemePurchased;
        public event System.Action<SOGameTheme> OnThemeEquipped;
        public event System.Action<string> OnPurchaseFailed; // Mensaje de error

        // Singleton pattern para fácil acceso
        public static ThemeStoreManager Instance { get; private set; }

        // Datos persistentes
        private HashSet<string> unlockedThemeIDs = new HashSet<string>();
        private string equippedThemeID = "";

        void Awake()
        {
            // Singleton setup
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
            // Buscar sistemas si no están asignados
            FindSystems();

            // Cargar datos guardados PRIMERO
            LoadStoreData();

            // Desbloquear tema por defecto si es la primera vez
            if (unlockedThemeIDs.Count == 0 && defaultTheme != null)
            {
                Debug.Log("Primera vez jugando, desbloqueando tema por defecto");
                UnlockTheme(defaultTheme.themeID, false); // false = no guardar aún
                EquipTheme(defaultTheme.themeID, false);
                SaveStoreData(); // Guardar después de configuración inicial
            }
            else
            {
                Debug.Log($"Datos cargados. Temas desbloqueados: {unlockedThemeIDs.Count}");
            }

            // Aplicar tema equipado
            ApplyEquippedTheme();

            Debug.Log($"ThemeStoreManager inicializado. Temas desbloqueados: {unlockedThemeIDs.Count}");
        }

        void FindSystems()
        {
            if (moneySystem == null)
            {
                moneySystem = FindObjectOfType<MoneySystem>();
                if (moneySystem == null)
                {
                    Debug.LogError("MoneySystem no encontrado. Asegúrate de tenerlo en la escena.");
                }
            }

            if (themeManager == null)
            {
                themeManager = FindObjectOfType<ThemeManager>();
                if (themeManager == null)
                {
                    Debug.LogError("ThemeManager no encontrado. Asegúrate de tenerlo en la escena.");
                }
            }
        }

        // ========================================
        // MÉTODOS PRINCIPALES DE COMPRA
        // ========================================

        /// <summary>
        /// Intenta comprar un tema con el dinero actual
        /// </summary>
        public bool TryPurchaseTheme(string themeID)
        {
            // Validar que el tema existe
            SOGameTheme theme = GetThemeByID(themeID);
            if (theme == null)
            {
                OnPurchaseFailed?.Invoke($"Tema '{themeID}' no encontrado");
                Debug.LogError($"Tema con ID '{themeID}' no existe en la lista de temas disponibles");
                return false;
            }

            // Verificar si ya está desbloqueado
            if (IsThemeUnlocked(themeID))
            {
                OnPurchaseFailed?.Invoke("Este tema ya está desbloqueado");
                Debug.LogWarning($"Tema '{theme.themeName}' ya está desbloqueado");
                return false;
            }

            // Verificar si tiene suficiente dinero
            if (moneySystem == null || !moneySystem.CanAfford(theme.themeCost))
            {
                OnPurchaseFailed?.Invoke($"Dinero insuficiente. Necesitas ${theme.themeCost}");
                Debug.LogWarning($"No hay suficiente dinero para comprar '{theme.themeName}'. Costo: {theme.themeCost}");
                return false;
            }

            // Procesar compra
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

        /// <summary>
        /// Desbloquea un tema (por compra o gratis)
        /// </summary>
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

        /// <summary>
        /// Equipa un tema (debe estar desbloqueado)
        /// </summary>
        public bool EquipTheme(string themeID, bool save = true)
        {
            // Verificar que esté desbloqueado
            if (!IsThemeUnlocked(themeID))
            {
                Debug.LogWarning($"No se puede equipar tema '{themeID}' porque no está desbloqueado");
                return false;
            }

            // Verificar que el tema existe
            SOGameTheme theme = GetThemeByID(themeID);
            if (theme == null)
            {
                Debug.LogError($"Tema '{themeID}' no encontrado");
                return false;
            }

            // Equipar tema
            equippedThemeID = themeID;

            // Aplicar tema visual
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

        // ========================================
        // MÉTODOS DE CONSULTA
        // ========================================

        /// <summary>
        /// Verifica si un tema está desbloqueado
        /// </summary>
        public bool IsThemeUnlocked(string themeID)
        {
            return unlockedThemeIDs.Contains(themeID);
        }

        /// <summary>
        /// Verifica si un tema está equipado actualmente
        /// </summary>
        public bool IsThemeEquipped(string themeID)
        {
            return equippedThemeID == themeID;
        }

        /// <summary>
        /// Obtiene el tema equipado actual
        /// </summary>
        public SOGameTheme GetEquippedTheme()
        {
            return GetThemeByID(equippedThemeID);
        }

        /// <summary>
        /// Obtiene un tema por su ID
        /// </summary>
        public SOGameTheme GetThemeByID(string themeID)
        {
            return availableThemes.FirstOrDefault(t => t.themeID == themeID);
        }

        /// <summary>
        /// Obtiene todos los temas desbloqueados
        /// </summary>
        public List<SOGameTheme> GetUnlockedThemes()
        {
            return availableThemes.Where(t => IsThemeUnlocked(t.themeID)).ToList();
        }

        /// <summary>
        /// Obtiene todos los temas bloqueados
        /// </summary>
        public List<SOGameTheme> GetLockedThemes()
        {
            return availableThemes.Where(t => !IsThemeUnlocked(t.themeID)).ToList();
        }

        /// <summary>
        /// Obtiene el estado de un tema (Equipado/Desbloqueado/Bloqueado)
        /// </summary>
        public ThemeState GetThemeState(string themeID)
        {
            if (IsThemeEquipped(themeID))
                return ThemeState.Equipped;
            else if (IsThemeUnlocked(themeID))
                return ThemeState.Unlocked;
            else
                return ThemeState.Locked;
        }

        // ========================================
        // SISTEMA DE GUARDADO (PlayerPrefs - Lista G)
        // ========================================

        void SaveStoreData()
        {
            // Asegurar que unlockedThemeIDs tenga contenido
            if (unlockedThemeIDs == null || unlockedThemeIDs.Count == 0)
            {
                Debug.LogWarning("No hay temas desbloqueados para guardar");
                return;
            }

            // Guardar temas desbloqueados como string separado por comas
            string unlockedThemesString = string.Join(",", unlockedThemeIDs);
            PlayerPrefs.SetString("UnlockedThemes", unlockedThemesString);

            // Guardar tema equipado
            PlayerPrefs.SetString("EquippedTheme", equippedThemeID);

            PlayerPrefs.Save();
            Debug.Log($"✓ Datos guardados. Temas desbloqueados: {unlockedThemeIDs.Count} ({unlockedThemesString})");
        }

        void LoadStoreData()
        {
            // Cargar temas desbloqueados
            string unlockedThemesString = PlayerPrefs.GetString("UnlockedThemes", "");

            if (!string.IsNullOrEmpty(unlockedThemesString))
            {
                // Dividir por comas
                string[] themeIDs = unlockedThemesString.Split(',');

                // Limpiar espacios y vacíos
                unlockedThemeIDs = new HashSet<string>();
                foreach (string id in themeIDs)
                {
                    string cleanID = id.Trim();
                    if (!string.IsNullOrEmpty(cleanID))
                    {
                        unlockedThemeIDs.Add(cleanID);
                    }
                }

                Debug.Log($"✓ Temas cargados: {unlockedThemeIDs.Count} ({unlockedThemesString})");
            }
            else
            {
                unlockedThemeIDs = new HashSet<string>();
                Debug.Log("No hay temas guardados, iniciando lista vacía");
            }

            // Cargar tema equipado
            equippedThemeID = PlayerPrefs.GetString("EquippedTheme", "");
            Debug.Log($"Tema equipado cargado: '{equippedThemeID}'");
        }

        void ApplyEquippedTheme()
        {
            if (!string.IsNullOrEmpty(equippedThemeID))
            {
                SOGameTheme theme = GetThemeByID(equippedThemeID);
                if (theme != null && themeManager != null)
                {
                    themeManager.SetCurrentTheme(theme);
                    Debug.Log($"Tema '{theme.themeName}' aplicado al iniciar");
                }
            }
        }

        // ========================================
        // MÉTODOS DE DEBUG Y TESTING
        // ========================================

        [ContextMenu("Unlock All Themes")]
        public void UnlockAllThemes()
        {
            foreach (var theme in availableThemes)
            {
                UnlockTheme(theme.themeID, false);
            }
            SaveStoreData();
            OnStoreUpdated?.Invoke();
            Debug.Log("Todos los temas desbloqueados");
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
            Debug.Log("Temas reseteados al estado inicial");
        }

        [ContextMenu("Log Store Status")]
        public void LogStoreStatus()
        {
            Debug.Log("=== THEME STORE STATUS ===");
            Debug.Log($"Total de temas: {availableThemes.Count}");
            Debug.Log($"Temas desbloqueados: {unlockedThemeIDs.Count}");
            Debug.Log($"Tema equipado: {equippedThemeID}");
            Debug.Log($"Dinero actual: ${moneySystem?.CurrentMoney ?? 0}");

            Debug.Log("\n--- LISTA DE TEMAS DESBLOQUEADOS ---");
            foreach (string id in unlockedThemeIDs)
            {
                Debug.Log($"  ✓ {id}");
            }

            Debug.Log("\n--- ESTADO DE TODOS LOS TEMAS ---");
            foreach (var theme in availableThemes)
            {
                string status = GetThemeState(theme.themeID).ToString();
                Debug.Log($"  - {theme.themeName} ({theme.themeID}): {status} - ${theme.themeCost}");
            }

            // Verificar PlayerPrefs
            string savedThemes = PlayerPrefs.GetString("UnlockedThemes", "VACÍO");
            string savedEquipped = PlayerPrefs.GetString("EquippedTheme", "VACÍO");
            Debug.Log($"\n--- PLAYERPREFS ---");
            Debug.Log($"UnlockedThemes: {savedThemes}");
            Debug.Log($"EquippedTheme: {savedEquipped}");
        }
    }

    // Enum para estados de tema
    public enum ThemeState
    {
        Locked,    // No comprado, debe comprarse
        Unlocked,  // Comprado pero no equipado
        Equipped   // Comprado y actualmente en uso
    }
}