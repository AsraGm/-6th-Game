using UnityEngine;

/// <summary>
/// ScriptableObject que mantiene los datos del juego en runtime
/// Se sincroniza con SaveSystem
/// </summary>
[CreateAssetMenu(fileName = "GameData", menuName = "ShootingRange/Game Data")]
public class GameData : ScriptableObject
{
    [Header("Player Money")]
    [SerializeField] private int currentMoney;
    
    [Header("Current Theme")]
    [SerializeField] private string equippedThemeID = "Default";

    // Propiedades públicas con eventos
    public int CurrentMoney 
    { 
        get => currentMoney;
        set
        {
            currentMoney = value;
            OnMoneyChanged?.Invoke(currentMoney);
        }
    }

    public string EquippedThemeID
    {
        get => equippedThemeID;
        set
        {
            equippedThemeID = value;
            OnThemeChanged?.Invoke(equippedThemeID);
        }
    }

    // Eventos para notificar cambios
    public System.Action<int> OnMoneyChanged;
    public System.Action<string> OnThemeChanged;

    /// <summary>
    /// Carga los datos desde SaveSystem
    /// Llamar al inicio del juego
    /// </summary>
    public void LoadFromSave()
    {
        currentMoney = SaveSystem.Instance.LoadTotalMoney();
        equippedThemeID = SaveSystem.Instance.LoadEquippedTheme();
        
        Debug.Log($"[GameData] Loaded - Money: {currentMoney}, Theme: {equippedThemeID}");
    }

    /// <summary>
    /// Guarda los datos actuales
    /// </summary>
    public void SaveData()
    {
        SaveSystem.Instance.SaveTotalMoney(currentMoney);
        SaveSystem.Instance.SaveEquippedTheme(equippedThemeID);
    }

    /// <summary>
    /// Añade dinero y guarda automáticamente
    /// </summary>
    public void AddMoney(int amount)
    {
        CurrentMoney += amount;
        SaveSystem.Instance.SaveTotalMoney(currentMoney);
    }

    /// <summary>
    /// Gasta dinero si hay suficiente
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            CurrentMoney -= amount;
            SaveSystem.Instance.SaveTotalMoney(currentMoney);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Cambia el tema equipado y guarda
    /// </summary>
    public void EquipTheme(string themeID)
    {
        EquippedThemeID = themeID;
        SaveSystem.Instance.SaveEquippedTheme(themeID);
    }

    /// <summary>
    /// Resetea los datos a valores por defecto
    /// </summary>
    public void ResetData()
    {
        currentMoney = 0;
        equippedThemeID = "Default";
        SaveData();
    }
}