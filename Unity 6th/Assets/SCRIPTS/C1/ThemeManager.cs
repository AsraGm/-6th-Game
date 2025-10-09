using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ARCHIVO: ThemeManager.cs
// INSTRUCCIÓN C1 y C2: Sistema de temas visuales y aplicación de skins
// Singleton que maneja el tema actual y lo aplica a enemigos y escenario

namespace ShootingRange
{
    public class ThemeManager : MonoBehaviour
    {
        [Header("Tema Actual")]
        [Tooltip("Tema que está actualmente activo")]
        [SerializeField] private SOGameTheme currentTheme;
        
        [Header("Referencias de Escenario")]
        [Tooltip("ARRASTRA AQUÍ el SpriteRenderer del fondo")]
        public SpriteRenderer backgroundRenderer;
        
        [Tooltip("ARRASTRA AQUÍ el SpriteRenderer de la cortina")]
        public SpriteRenderer curtainRenderer;
        
        [Header("Optimización")]
        [Tooltip("Aplicar tema automáticamente al cambiar")]
        public bool autoApplyOnChange = true;
        
        [Tooltip("Cache de enemigos para actualización en tiempo real")]
        private List<BasicEnemy> cachedEnemies = new List<BasicEnemy>();
        
        // Singleton
        public static ThemeManager Instance { get; private set; }
        
        // Propiedad pública para acceder al tema actual
        public SOGameTheme CurrentTheme => currentTheme;
        
        // Evento cuando cambia el tema
        public event System.Action<SOGameTheme> OnThemeChanged;
        
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
            InitializeThemeManager();
        }
        
        void InitializeThemeManager()
        {
            // Buscar referencias automáticamente si no están asignadas
            FindSceneryReferences();
            
            // Aplicar tema inicial si hay uno asignado
            if (currentTheme != null)
            {
                ApplyCurrentTheme();
            }
            else
            {
                Debug.LogWarning("ThemeManager: No hay tema inicial asignado");
            }
        }
        
        void FindSceneryReferences()
        {
            // Intentar encontrar background y curtain por tag o nombre
            if (backgroundRenderer == null)
            {
                GameObject bgObject = GameObject.Find("Background");
                if (bgObject != null)
                {
                    backgroundRenderer = bgObject.GetComponent<SpriteRenderer>();
                }
            }
            
            if (curtainRenderer == null)
            {
                GameObject curtainObject = GameObject.Find("Curtain");
                if (curtainObject != null)
                {
                    curtainRenderer = curtainObject.GetComponent<SpriteRenderer>();
                }
            }
        }
        
        // ========================================
        // MÉTODO PRINCIPAL: Cambiar tema
        // ========================================
        
        /// <summary>
        /// Cambia el tema actual y lo aplica a la escena
        /// </summary>
        public void SetCurrentTheme(SOGameTheme newTheme)
        {
            if (newTheme == null)
            {
                Debug.LogWarning("ThemeManager: Intentando asignar tema null");
                return;
            }
            
            // Validar que el tema sea válido
            if (!newTheme.IsValidTheme())
            {
                Debug.LogWarning($"ThemeManager: Tema '{newTheme.themeName}' incompleto:\n{newTheme.GetMissingSprites()}");
            }
            
            currentTheme = newTheme;
            
            if (autoApplyOnChange)
            {
                ApplyCurrentTheme();
            }
            
            // Notificar cambio
            OnThemeChanged?.Invoke(currentTheme);
            
            Debug.Log($"ThemeManager: Tema cambiado a '{currentTheme.themeName}'");
        }
        
        /// <summary>
        /// Aplica el tema actual a todos los elementos visuales
        /// </summary>
        public void ApplyCurrentTheme()
        {
            if (currentTheme == null)
            {
                Debug.LogWarning("ThemeManager: No hay tema para aplicar");
                return;
            }
            
            // Aplicar sprites de escenario
            ApplyScenerySprites();
            
            // Aplicar sprites a enemigos existentes
            ApplyThemeToExistingEnemies();
            
            Debug.Log($"ThemeManager: Tema '{currentTheme.themeName}' aplicado");
        }
        
        // ========================================
        // APLICACIÓN DE SPRITES DE ESCENARIO
        // ========================================
        
        void ApplyScenerySprites()
        {
            // Aplicar background
            if (backgroundRenderer != null && currentTheme.backgroundSprite != null)
            {
                backgroundRenderer.sprite = currentTheme.backgroundSprite;
            }
            else if (backgroundRenderer == null)
            {
                Debug.LogWarning("ThemeManager: BackgroundRenderer no asignado");
            }
            
            // Aplicar curtain
            if (curtainRenderer != null && currentTheme.curtainSprite != null)
            {
                curtainRenderer.sprite = currentTheme.curtainSprite;
            }
            else if (curtainRenderer == null)
            {
                Debug.LogWarning("ThemeManager: CurtainRenderer no asignado");
            }
        }
        
        // ========================================
        // APLICACIÓN DE SPRITES A ENEMIGOS
        // ========================================
        
        /// <summary>
        /// Aplica el tema actual a todos los enemigos en la escena
        /// </summary>
        void ApplyThemeToExistingEnemies()
        {
            // Encontrar todos los enemigos activos
            BasicEnemy[] enemies = FindObjectsOfType<BasicEnemy>();
            
            foreach (BasicEnemy enemy in enemies)
            {
                ApplySpriteToEnemy(enemy);
            }
            
            Debug.Log($"ThemeManager: Sprites aplicados a {enemies.Length} enemigos");
        }
        
        /// <summary>
        /// Aplica el sprite correcto a un enemigo según su tipo
        /// MÉTODO PÚBLICO - Llamar cuando se spawnea un enemigo nuevo
        /// </summary>
        public void ApplySpriteToEnemy(BasicEnemy enemy)
        {
            if (enemy == null || currentTheme == null)
                return;
            
            // Obtener el sprite correcto del tema según el tipo de enemigo
            Sprite sprite = currentTheme.GetSpriteForEnemyType(enemy.enemyType);
            
            if (sprite != null)
            {
                // Aplicar sprite al SpriteRenderer del enemigo
                SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                }
                else
                {
                    Debug.LogWarning($"Enemy '{enemy.name}' no tiene SpriteRenderer");
                }
            }
            else
            {
                Debug.LogWarning($"No hay sprite definido para enemigo tipo '{enemy.enemyType}' en tema '{currentTheme.themeName}'");
            }
        }
        
        /// <summary>
        /// Obtiene el sprite para un tipo de enemigo específico
        /// MÉTODO PÚBLICO - Para usar en Enemy Pooling
        /// </summary>
        public Sprite GetSpriteForEnemyType(EnemyType enemyType)
        {
            if (currentTheme == null)
            {
                Debug.LogWarning("ThemeManager: No hay tema actual");
                return null;
            }
            
            return currentTheme.GetSpriteForEnemyType(enemyType);
        }
        
        // ========================================
        // SISTEMA DE CACHE PARA OPTIMIZACIÓN
        // ========================================
        
        /// <summary>
        /// Registra un enemigo en el cache (llamar desde Enemy.OnEnable)
        /// </summary>
        public void RegisterEnemy(BasicEnemy enemy)
        {
            if (enemy != null && !cachedEnemies.Contains(enemy))
            {
                cachedEnemies.Add(enemy);
                ApplySpriteToEnemy(enemy);
            }
        }
        
        /// <summary>
        /// Remueve un enemigo del cache (llamar desde Enemy.OnDisable)
        /// </summary>
        public void UnregisterEnemy(BasicEnemy enemy)
        {
            if (enemy != null && cachedEnemies.Contains(enemy))
            {
                cachedEnemies.Remove(enemy);
            }
        }
        
        /// <summary>
        /// Limpia el cache de enemigos
        /// </summary>
        public void ClearEnemyCache()
        {
            cachedEnemies.Clear();
        }
        
        // ========================================
        // MÉTODOS DE UTILIDAD
        // ========================================
        
        /// <summary>
        /// Verifica si hay un tema activo
        /// </summary>
        public bool HasActiveTheme()
        {
            return currentTheme != null;
        }
        
        /// <summary>
        /// Obtiene el nombre del tema actual
        /// </summary>
        public string GetCurrentThemeName()
        {
            return currentTheme != null ? currentTheme.themeName : "Sin tema";
        }
        
        /// <summary>
        /// Recarga el tema actual (útil después de cambios en el ScriptableObject)
        /// </summary>
        [ContextMenu("Reload Current Theme")]
        public void ReloadCurrentTheme()
        {
            if (currentTheme != null)
            {
                ApplyCurrentTheme();
                Debug.Log($"Tema '{currentTheme.themeName}' recargado");
            }
        }
        
        // ========================================
        // MÉTODOS DE DEBUG
        // ========================================
        
        [ContextMenu("Log Current Theme Info")]
        void LogCurrentThemeInfo()
        {
            if (currentTheme != null)
            {
                Debug.Log("=== CURRENT THEME INFO ===");
                Debug.Log($"Name: {currentTheme.themeName}");
                Debug.Log($"ID: {currentTheme.themeID}");
                Debug.Log($"Valid: {currentTheme.IsValidTheme()}");
                Debug.Log($"Cost: ${currentTheme.themeCost}");
            }
            else
            {
                Debug.Log("No hay tema actual asignado");
            }
        }
        
        [ContextMenu("Apply Theme To All Enemies")]
        void ForceApplyToEnemies()
        {
            ApplyThemeToExistingEnemies();
        }
        
        void OnDestroy()
        {
            ClearEnemyCache();
        }
    }
}