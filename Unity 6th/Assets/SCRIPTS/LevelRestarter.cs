using UnityEngine;
using UnityEngine.SceneManagement;
using ShootingRange;

/// <summary>
/// Sistema simple y dedicado para hacer restart del nivel
/// Pon este script en un GameObject vacío en tu escena de gameplay
/// </summary>
public class LevelRestarter : MonoBehaviour
{
    public static LevelRestarter Instance { get; private set; }
    
    [Header("Referencias")]
    [Tooltip("Arrastra aquí tu WaveSystem")]
    public WaveSystem waveSystem;
    
    void Awake()
    {
        Instance = this;
        
        // Buscar WaveSystem automáticamente si no está asignado
        if (waveSystem == null)
        {
            waveSystem = FindObjectOfType<WaveSystem>();
        }
    }
    
    /// <summary>
    /// Método principal de restart - Llama a este desde tu botón de retry
    /// </summary>
    public void RestartLevel()
    {
        Debug.Log("🔄 === INICIANDO RESTART DE NIVEL ===");
        
        // 1. DETENER WAVESYSTEM
        if (waveSystem != null)
        {
            Debug.Log("⏹️ Deteniendo WaveSystem...");
            waveSystem.StopWaveSystem();
            waveSystem.ResetWaveSystem();
        }
        else
        {
            Debug.LogWarning("⚠️ WaveSystem no encontrado");
        }
        
        // 2. RESETEAR STASTRACKER
        if (StatsTracker.Instance != null)
        {
            Debug.Log("📊 Reseteando StatsTracker...");
            // Usar el método ResetLevel que agregamos antes
            StatsTracker.Instance.EndLevelSession();
        }
        
        // 3. DESTRUIR TODOS LOS ENEMIGOS ACTIVOS
        Debug.Log("💀 Destruyendo enemigos activos...");
        DestroyAllEnemies();
        
        // 4. RESETEAR TIME SCALE (por si estaba pausado)
        Time.timeScale = 1f;
        
        // 5. PEQUEÑO DELAY Y RECARGAR
        Debug.Log("🔃 Recargando escena...");
        Invoke(nameof(ReloadScene), 0.1f);
    }
    
    void DestroyAllEnemies()
    {
        // Buscar TODOS los enemigos en la escena
        BasicEnemy[] enemies = FindObjectsOfType<BasicEnemy>();
        
        Debug.Log($"🎯 Encontrados {enemies.Length} enemigos para destruir");
        
        foreach (BasicEnemy enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        
        // También buscar por tag si tienes
        GameObject[] taggedEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in taggedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
    }
    
    void ReloadScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"✅ Recargando escena: {currentScene}");
        SceneManager.LoadScene(currentScene);
    }
    
    /// <summary>
    /// Método alternativo sin delay (por si el delay causa problemas)
    /// </summary>
    public void RestartLevelImmediate()
    {
        Debug.Log("🔄 === RESTART INMEDIATO ===");
        
        // Detener todo
        if (waveSystem != null)
        {
            waveSystem.StopWaveSystem();
            waveSystem.ResetWaveSystem();
        }
        
        if (StatsTracker.Instance != null)
        {
            StatsTracker.Instance.EndLevelSession();
        }
        
        // Destruir enemigos
        DestroyAllEnemies();
        
        // Resetear time scale
        Time.timeScale = 1f;
        
        // Recargar inmediatamente
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // Método de utilidad para limpiar todo sin recargar (útil para testing)
    [ContextMenu("🧹 Limpiar Todo (Sin Reload)")]
    public void CleanupWithoutReload()
    {
        if (waveSystem != null)
        {
            waveSystem.StopWaveSystem();
            waveSystem.ResetWaveSystem();
        }
        
        DestroyAllEnemies();
        
        Debug.Log("✅ Limpieza completada");
    }
}
