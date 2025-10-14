using UnityEngine;

// ARCHIVO: SOGameTheme.cs
// INSTRUCCIÓN C1: ScriptableObject para temas visuales
// Solo contiene sprites para enemigos, fondo y cortina (SIN audio, colores, tamaños)

namespace ShootingRange
{
    [CreateAssetMenu(fileName = "NewGameTheme", menuName = "Shooting Range/Game Theme")]
    public class SOGameTheme : ScriptableObject
    {
        [Header("Información del Tema")]
        [Tooltip("Nombre del tema (ej: Western, Cyberpunk, Medieval)")]
        public string themeName = "Default Theme";
        
        [Tooltip("ID único del tema para referencia")]
        public string themeID = "default";
        
        [Tooltip("Descripción del tema (para la tienda)")]
        [TextArea(2, 4)]
        public string themeDescription = "Tema por defecto del juego";
        
        [Tooltip("Icono del tema para mostrar en tienda")]
        public Sprite themeIcon;
        
        [Header("Precio (para Store System - Lista E)")]
        [Tooltip("Costo del tema en monedas del juego")]
        public int themeCost = 0; // 0 = gratis/desbloqueado
        
        [Header("SPRITES DE ENEMIGOS")]
        [Tooltip("Sprite para enemigo NORMAL")]
        public Sprite normalEnemySprite;
        
        [Tooltip("Sprite para enemigo STATIC")]
        public Sprite staticEnemySprite;
        
        [Tooltip("Sprite para enemigo ZIGZAG (rápido)")]
        public Sprite zigzagEnemySprite;
        
        [Tooltip("Sprite para enemigo JUMPER (saltador)")]
        public Sprite jumperEnemySprite;
        
        [Tooltip("Sprite para enemigo VALUABLE (valioso)")]
        public Sprite valuableEnemySprite;
        
        [Tooltip("Sprite para INNOCENT (inocente)")]
        public Sprite innocentEnemySprite;
        
        [Header("SPRITES DE ESCENARIO")]
        [Tooltip("Sprite del fondo de la escena")]
        public Sprite backgroundSprite;
        
        [Tooltip("Sprite de la cortina (elemento decorativo frontal)")]
        public Sprite curtainSprite;
        
        // ========================================
        // MÉTODOS PÚBLICOS
        // ========================================
        
        /// <summary>
        /// Obtiene el sprite correspondiente a un tipo de enemigo
        /// </summary>
        public Sprite GetSpriteForEnemyType(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Normal:
                    return normalEnemySprite;
                    
                case EnemyType.Static:
                    return staticEnemySprite;
                    
                case EnemyType.ZigZag:
                    return zigzagEnemySprite;
                    
                case EnemyType.Jumper:
                    return jumperEnemySprite;
                    
                case EnemyType.Valuable:
                    return valuableEnemySprite;
                    
                case EnemyType.Innocent:
                    return innocentEnemySprite;
                    
                default:
                    Debug.LogWarning($"No sprite definido para enemigo tipo: {enemyType}");
                    return normalEnemySprite; // Fallback
            }
        }
        
        /// <summary>
        /// Verifica si el tema tiene todos los sprites necesarios
        /// </summary>
        public bool IsValidTheme()
        {
            bool hasEnemySprites = normalEnemySprite != null &&
                                   staticEnemySprite != null &&
                                   zigzagEnemySprite != null &&
                                   jumperEnemySprite != null &&
                                   valuableEnemySprite != null &&
                                   innocentEnemySprite != null;
            
            bool hasScenerySprites = backgroundSprite != null &&
                                     curtainSprite != null;
            
            return hasEnemySprites && hasScenerySprites;
        }
        
        /// <summary>
        /// Obtiene lista de sprites faltantes (para debug)
        /// </summary>
        public string GetMissingSprites()
        {
            System.Text.StringBuilder missing = new System.Text.StringBuilder();
            
            if (normalEnemySprite == null) missing.AppendLine("- Normal Enemy");
            if (staticEnemySprite == null) missing.AppendLine("- Static Enemy");
            if (zigzagEnemySprite == null) missing.AppendLine("- ZigZag Enemy");
            if (jumperEnemySprite == null) missing.AppendLine("- Jumper Enemy");
            if (valuableEnemySprite == null) missing.AppendLine("- Valuable Enemy");
            if (innocentEnemySprite == null) missing.AppendLine("- Innocent Enemy");
            if (backgroundSprite == null) missing.AppendLine("- Background");
            if (curtainSprite == null) missing.AppendLine("- Curtain");
            
            return missing.Length > 0 ? missing.ToString() : "Ninguno";
        }
        
        // ========================================
        // VALIDACIÓN EN EDITOR
        // ========================================
        
        void OnValidate()
        {
            // Validar que themeID no tenga espacios
            if (!string.IsNullOrEmpty(themeID))
            {
                themeID = themeID.ToLower().Replace(" ", "_");
            }
            
            // Validar costo
            themeCost = Mathf.Max(0, themeCost);
            
            // Log de advertencias
            if (!IsValidTheme())
            {
                Debug.LogWarning($"Tema '{themeName}' incompleto. Sprites faltantes:\n{GetMissingSprites()}");
            }
        }
        
        // ========================================
        // MÉTODOS DE DEBUG
        // ========================================
        
        [ContextMenu("Validate Theme")]
        public void ValidateTheme()
        {
            if (IsValidTheme())
            {
                Debug.Log($"✓ Tema '{themeName}' está completo y listo para usar");
            }
            else
            {
                Debug.LogError($"✗ Tema '{themeName}' está incompleto:\n{GetMissingSprites()}");
            }
        }
        
        [ContextMenu("Log Theme Info")]
        public void LogThemeInfo()
        {
            Debug.Log($"=== THEME INFO: {themeName} ===");
            Debug.Log($"ID: {themeID}");
            Debug.Log($"Cost: ${themeCost}");
            Debug.Log($"Valid: {IsValidTheme()}");
            Debug.Log($"Description: {themeDescription}");
        }
    }
}