using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// NUEVO SCRIPT: CanvasThemeManager.cs
// Sistema híbrido: Canvas UI para fondo/telón + SpriteRenderer para enemigos
// El telón siempre estará adelante de los enemigos

namespace ShootingRange
{
    public class CanvasThemeManager : MonoBehaviour
    {
        [Header("Tema Actual")]
        [Tooltip("Tema que está actualmente activo")]
        [SerializeField] private SOGameTheme currentTheme;
        
        [Header("Referencias Canvas UI - Escenario")]
        [Tooltip("ARRASTRA AQUÍ el Canvas Background")]
        public Canvas backgroundCanvas;
        
        [Tooltip("ARRASTRA AQUÍ la Image del fondo dentro del Canvas")]
        public Image backgroundImage;
        
        [Tooltip("ARRASTRA AQUÍ el Canvas Curtain (telón)")]
        public Canvas curtainCanvas;
        
        [Tooltip("ARRASTRA AQUÍ la Image del telón dentro del Canvas")]
        public Image curtainImage;
        
        [Header("Configuración de Sorting")]
        [Tooltip("Sorting Order del Canvas de fondo (debe ser menor que enemigos)")]
        public int backgroundSortingOrder = 0;
        
        [Tooltip("Sorting Order del Canvas del telón (debe ser mayor que enemigos)")]
        public int curtainSortingOrder = 10;
        
        [Tooltip("Sorting Order de los enemigos (entre fondo y telón)")]
        public int enemySortingOrder = 5;
        
        [Header("Optimización")]
        [Tooltip("Aplicar tema automáticamente al cambiar")]
        public bool autoApplyOnChange = true;
        
        [Tooltip("Cache de enemigos para actualización en tiempo real")]
        private List<BasicEnemy> cachedEnemies = new List<BasicEnemy>();
        
        // Singleton
        public static CanvasThemeManager Instance { get; private set; }
        
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
            // Configurar Canvas Sorting Orders
            SetupCanvasSorting();
            
            // Aplicar tema inicial si hay uno asignado
            if (currentTheme != null)
            {
                ApplyCurrentTheme();
            }
            else
            {
                Debug.LogWarning("CanvasThemeManager: No hay tema inicial asignado");
            }
        }
        
        void SetupCanvasSorting()
        {
            // Configurar Background Canvas
            if (backgroundCanvas != null)
            {
                backgroundCanvas.overrideSorting = true;
                backgroundCanvas.sortingLayerName = "Default";
                backgroundCanvas.sortingOrder = backgroundSortingOrder;
                Debug.Log($"✅ Background Canvas - Sorting Order: {backgroundSortingOrder}");
            }
            else
            {
                Debug.LogWarning("⚠️ Background Canvas no asignado");
            }
            
            // Configurar Curtain Canvas
            if (curtainCanvas != null)
            {
                curtainCanvas.overrideSorting = true;
                curtainCanvas.sortingLayerName = "Default";
                curtainCanvas.sortingOrder = curtainSortingOrder;
                Debug.Log($"✅ Curtain Canvas - Sorting Order: {curtainSortingOrder}");
            }
            else
            {
                Debug.LogWarning("⚠️ Curtain Canvas no asignado");
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
                Debug.LogWarning("CanvasThemeManager: Intentando asignar tema null");
                return;
            }
            
            // Validar que el tema sea válido
            if (!newTheme.IsValidTheme())
            {
                Debug.LogWarning($"CanvasThemeManager: Tema '{newTheme.themeName}' incompleto:\n{newTheme.GetMissingSprites()}");
            }
            
            currentTheme = newTheme;
            
            if (autoApplyOnChange)
            {
                ApplyCurrentTheme();
            }
            
            // Notificar cambio
            OnThemeChanged?.Invoke(currentTheme);
            
            Debug.Log($"CanvasThemeManager: Tema cambiado a '{currentTheme.themeName}'");
        }
        
        /// <summary>
        /// Aplica el tema actual a todos los elementos visuales
        /// </summary>
        public void ApplyCurrentTheme()
        {
            if (currentTheme == null)
            {
                Debug.LogWarning("CanvasThemeManager: No hay tema para aplicar");
                return;
            }
            
            // Aplicar sprites de escenario (Canvas UI)
            ApplyCanvasSprites();
            
            // Aplicar sprites a enemigos existentes (SpriteRenderer)
            ApplyThemeToExistingEnemies();
            
            Debug.Log($"CanvasThemeManager: Tema '{currentTheme.themeName}' aplicado");
        }
        
        // ========================================
        // APLICACIÓN DE SPRITES DE CANVAS UI
        // ========================================
        
        void ApplyCanvasSprites()
        {
            // Aplicar background al Canvas Image
            if (backgroundImage != null && currentTheme.backgroundSprite != null)
            {
                backgroundImage.sprite = currentTheme.backgroundSprite;
                backgroundImage.preserveAspect = false; // Llenar toda la pantalla
                Debug.Log($"✅ Background aplicado: {currentTheme.backgroundSprite.name}");
            }
            else if (backgroundImage == null)
            {
                Debug.LogWarning("⚠️ Background Image no asignado");
            }
            
            // Aplicar curtain al Canvas Image
            if (curtainImage != null && currentTheme.curtainSprite != null)
            {
                curtainImage.sprite = currentTheme.curtainSprite;
                curtainImage.preserveAspect = true; // Mantener aspecto del telón
                Debug.Log($"✅ Curtain aplicado: {currentTheme.curtainSprite.name}");
            }
            else if (curtainImage == null)
            {
                Debug.LogWarning("⚠️ Curtain Image no asignado");
            }
        }
        
        // ========================================
        // APLICACIÓN DE SPRITES A ENEMIGOS (SpriteRenderer)
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
            
            Debug.Log($"CanvasThemeManager: Sprites aplicados a {enemies.Length} enemigos");
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
                    
                    // IMPORTANTE: Configurar el sorting order del enemigo
                    spriteRenderer.sortingOrder = enemySortingOrder;
                    
                    Debug.Log($"✅ Sprite aplicado a {enemy.name}: {sprite.name} (Order: {enemySortingOrder})");
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
                Debug.LogWarning("CanvasThemeManager: No hay tema actual");
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
        
        /// <summary>
        /// Reconfigura los Sorting Orders manualmente
        /// </summary>
        [ContextMenu("Reconfigure Sorting Orders")]
        public void ReconfigureSortingOrders()
        {
            SetupCanvasSorting();
            ApplyThemeToExistingEnemies();
            Debug.Log("✅ Sorting Orders reconfigurados");
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
        
        [ContextMenu("Log Sorting Setup")]
        void LogSortingSetup()
        {
            Debug.Log("=== SORTING ORDER SETUP ===");
            Debug.Log($"Background Canvas: {backgroundSortingOrder}");
            Debug.Log($"Enemies (SpriteRenderer): {enemySortingOrder}");
            Debug.Log($"Curtain Canvas: {curtainSortingOrder}");
            Debug.Log("\n📊 Orden visual (de atrás hacia adelante):");
            Debug.Log($"1. Fondo ({backgroundSortingOrder})");
            Debug.Log($"2. Enemigos ({enemySortingOrder})");
            Debug.Log($"3. Telón ({curtainSortingOrder})");
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