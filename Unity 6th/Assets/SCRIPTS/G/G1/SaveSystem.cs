using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de persistencia básico usando PlayerPrefs
/// Auto-save al completar nivel y comprar tema
/// Datos: dinero total, tema equipado, mejores puntajes
/// </summary>
public class SaveSystem : MonoBehaviour
{
    private static SaveSystem _instance;
    public static SaveSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveSystem");
                _instance = go.AddComponent<SaveSystem>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Keys para PlayerPrefs
    private const string KEY_TOTAL_MONEY = "TotalMoney";
    private const string KEY_EQUIPPED_THEME = "EquippedTheme";
    private const string KEY_LEVEL_BEST_SCORE = "LevelBestScore_"; // + levelID
    private const string KEY_OWNED_THEMES = "OwnedThemes"; // Separados por coma

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Money Management
    
    /// <summary>
    /// Guarda el dinero total del jugador
    /// </summary>
    public void SaveTotalMoney(int money)
    {
        PlayerPrefs.SetInt(KEY_TOTAL_MONEY, money);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Carga el dinero total del jugador
    /// </summary>
    public int LoadTotalMoney()
    {
        return PlayerPrefs.GetInt(KEY_TOTAL_MONEY, 0);
    }

    /// <summary>
    /// Añade dinero al total guardado
    /// </summary>
    public void AddMoney(int amount)
    {
        int current = LoadTotalMoney();
        SaveTotalMoney(current + amount);
    }

    /// <summary>
    /// Resta dinero del total guardado
    /// </summary>
    public bool SpendMoney(int amount)
    {
        int current = LoadTotalMoney();
        if (current >= amount)
        {
            SaveTotalMoney(current - amount);
            return true;
        }
        return false;
    }

    #endregion

    #region Theme Management

    /// <summary>
    /// Guarda el tema equipado actualmente
    /// </summary>
    public void SaveEquippedTheme(string themeID)
    {
        PlayerPrefs.SetString(KEY_EQUIPPED_THEME, themeID);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Carga el ID del tema equipado
    /// </summary>
    public string LoadEquippedTheme()
    {
        return PlayerPrefs.GetString(KEY_EQUIPPED_THEME, "Default");
    }

    /// <summary>
    /// Guarda un tema como comprado
    /// </summary>
    public void SaveThemePurchased(string themeID)
    {
        List<string> ownedThemes = LoadOwnedThemes();
        if (!ownedThemes.Contains(themeID))
        {
            ownedThemes.Add(themeID);
            string themesString = string.Join(",", ownedThemes);
            PlayerPrefs.SetString(KEY_OWNED_THEMES, themesString);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Verifica si un tema está comprado
    /// </summary>
    public bool IsThemeOwned(string themeID)
    {
        List<string> ownedThemes = LoadOwnedThemes();
        return ownedThemes.Contains(themeID);
    }

    /// <summary>
    /// Carga la lista de temas comprados
    /// </summary>
    public List<string> LoadOwnedThemes()
    {
        string themesString = PlayerPrefs.GetString(KEY_OWNED_THEMES, "Default");
        List<string> themes = new List<string>();
        
        if (!string.IsNullOrEmpty(themesString))
        {
            string[] themeArray = themesString.Split(',');
            themes.AddRange(themeArray);
        }
        
        return themes;
    }

    #endregion

    #region Level Score Management

    /// <summary>
    /// Guarda el mejor puntaje de un nivel si es superior al actual
    /// </summary>
    public void SaveLevelBestScore(string levelID, int score)
    {
        string key = KEY_LEVEL_BEST_SCORE + levelID;
        int currentBest = PlayerPrefs.GetInt(key, 0);
        
        if (score > currentBest)
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Carga el mejor puntaje de un nivel
    /// </summary>
    public int LoadLevelBestScore(string levelID)
    {
        string key = KEY_LEVEL_BEST_SCORE + levelID;
        return PlayerPrefs.GetInt(key, 0);
    }

    /// <summary>
    /// Verifica si el score es un nuevo récord
    /// </summary>
    public bool IsNewBestScore(string levelID, int score)
    {
        int currentBest = LoadLevelBestScore(levelID);
        return score > currentBest;
    }

    #endregion

    #region Complete Level Save (Auto-save)

    /// <summary>
    /// Auto-save completo al terminar un nivel
    /// Guarda: dinero ganado + mejor puntaje
    /// </summary>
    public void SaveLevelCompletion(string levelID, int moneyEarned, int finalScore)
    {
        // Añadir dinero ganado
        AddMoney(moneyEarned);
        
        // Guardar mejor puntaje si aplica
        SaveLevelBestScore(levelID, finalScore);
        
        Debug.Log($"[SaveSystem] Level {levelID} completed - Money: +{moneyEarned}, Score: {finalScore}");
    }

    #endregion

    #region Theme Purchase (Auto-save)

    /// <summary>
    /// Auto-save al comprar un tema
    /// Gasta dinero y desbloquea el tema
    /// </summary>
    public bool PurchaseTheme(string themeID, int cost)
    {
        if (SpendMoney(cost))
        {
            SaveThemePurchased(themeID);
            SaveEquippedTheme(themeID); // Auto-equipar al comprar
            Debug.Log($"[SaveSystem] Theme {themeID} purchased for {cost}");
            return true;
        }
        
        Debug.LogWarning($"[SaveSystem] Not enough money to purchase theme {themeID}");
        return false;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Borra todos los datos guardados (para testing/reset)
    /// </summary>
    public void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] All save data deleted");
    }

    /// <summary>
    /// Verifica si existen datos guardados
    /// </summary>
    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(KEY_TOTAL_MONEY);
    }

    /// <summary>
    /// Imprime todos los datos guardados (debug)
    /// </summary>
    public void DebugPrintSaveData()
    {
        Debug.Log("=== SAVE DATA ===");
        Debug.Log($"Total Money: {LoadTotalMoney()}");
        Debug.Log($"Equipped Theme: {LoadEquippedTheme()}");
        Debug.Log($"Owned Themes: {string.Join(", ", LoadOwnedThemes())}");
        Debug.Log("================");
    }

    #endregion
}