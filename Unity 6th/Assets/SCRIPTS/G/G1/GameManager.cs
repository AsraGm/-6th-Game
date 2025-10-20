using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager que integra el SaveSystem
/// Maneja la inicialización y flujo del juego
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Data")]
    [SerializeField] private GameData gameData;

    [Header("Current Level Data")]
    private string currentLevelID;
    private int levelMoneyEarned;
    private int levelScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    /// <summary>
    /// Inicializa el juego cargando datos guardados
    /// </summary>
    private void InitializeGame()
    {
        // Cargar datos guardados
        gameData.LoadFromSave();
        
        Debug.Log("[GameManager] Game initialized");
    }

    #region Level Management

    /// <summary>
    /// Inicia un nivel
    /// </summary>
    public void StartLevel(string levelID)
    {
        currentLevelID = levelID;
        levelMoneyEarned = 0;
        levelScore = 0;
        
        Debug.Log($"[GameManager] Starting level: {levelID}");
        // Aquí cargarías la escena del nivel
        // SceneManager.LoadScene("GameplayScene");
    }

    /// <summary>
    /// Añade dinero durante el nivel (sin guardar aún)
    /// </summary>
    public void AddLevelMoney(int amount)
    {
        levelMoneyEarned += amount;
        levelScore += amount; // Score = dinero en este sistema simple
    }

    /// <summary>
    /// Resta dinero durante el nivel (penalización)
    /// </summary>
    public void SubtractLevelMoney(int amount)
    {
        levelMoneyEarned -= amount;
        levelScore = Mathf.Max(0, levelScore - amount);
    }

    /// <summary>
    /// Completa el nivel y guarda los resultados
    /// AUTO-SAVE: Se ejecuta automáticamente
    /// </summary>
    public void CompleteLevel()
    {
        // Auto-save al completar nivel
        SaveSystem.Instance.SaveLevelCompletion(
            currentLevelID, 
            levelMoneyEarned, 
            levelScore
        );

        // Actualizar GameData
        gameData.AddMoney(levelMoneyEarned);

        Debug.Log($"[GameManager] Level {currentLevelID} completed!");
        Debug.Log($"Money earned: {levelMoneyEarned}, Score: {levelScore}");

        // Mostrar pantalla de resultados
        // ShowResultsScreen();
    }

    /// <summary>
    /// Obtiene el mejor puntaje de un nivel
    /// </summary>
    public int GetLevelBestScore(string levelID)
    {
        return SaveSystem.Instance.LoadLevelBestScore(levelID);
    }

    #endregion

    #region Store Management

    /// <summary>
    /// Intenta comprar un tema
    /// AUTO-SAVE: Se ejecuta automáticamente si la compra es exitosa
    /// </summary>
    public bool TryPurchaseTheme(string themeID, int cost)
    {
        // Verificar si ya está comprado
        if (SaveSystem.Instance.IsThemeOwned(themeID))
        {
            Debug.LogWarning($"Theme {themeID} already owned");
            return false;
        }

        // Intentar comprar (auto-save incluido)
        if (SaveSystem.Instance.PurchaseTheme(themeID, cost))
        {
            // Actualizar GameData
            gameData.SpendMoney(cost);
            gameData.EquipTheme(themeID);
            
            Debug.Log($"Theme {themeID} purchased and equipped!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Equipa un tema que ya está comprado
    /// </summary>
    public void EquipTheme(string themeID)
    {
        if (SaveSystem.Instance.IsThemeOwned(themeID))
        {
            gameData.EquipTheme(themeID);
            Debug.Log($"Theme {themeID} equipped");
        }
        else
        {
            Debug.LogWarning($"Cannot equip theme {themeID} - not owned");
        }
    }

    #endregion

    #region Data Management

    /// <summary>
    /// Obtiene el dinero actual del jugador
    /// </summary>
    public int GetCurrentMoney()
    {
        return gameData.CurrentMoney;
    }

    /// <summary>
    /// Obtiene el tema equipado actual
    /// </summary>
    public string GetEquippedTheme()
    {
        return gameData.EquippedThemeID;
    }

    /// <summary>
    /// Resetea todos los datos del juego
    /// Usar con cuidado - solo para testing o botón de reset
    /// </summary>
    public void ResetAllData()
    {
        SaveSystem.Instance.DeleteAllData();
        gameData.ResetData();
        Debug.Log("[GameManager] All data reset");
    }

    #endregion

    #region Debug Methods (opcional)

    [ContextMenu("Debug: Print Save Data")]
    private void DebugPrintData()
    {
        SaveSystem.Instance.DebugPrintSaveData();
    }

    [ContextMenu("Debug: Add 1000 Money")]
    private void DebugAddMoney()
    {
        gameData.AddMoney(1000);
        Debug.Log($"Added 1000 money. Total: {gameData.CurrentMoney}");
    }

    [ContextMenu("Debug: Reset All Data")]
    private void DebugResetData()
    {
        ResetAllData();
    }

    #endregion
}