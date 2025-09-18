using UnityEngine;
using DG.Tweening;

// ARCHIVO: MoneySystem.cs
// Sistema principal de economía y dinero en tiempo real

namespace ShootingRange
{
    public class MoneySystem : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("ARRASTRA AQUÍ tu MoneyConfig desde la carpeta del proyecto")]
        public MoneyConfig moneyConfig;
        
        [Header("Referencias UI")]
        [Tooltip("ARRASTRA AQUÍ tu MoneyDisplay para conectar con la UI")]
        public MoneyDisplay moneyDisplay;
        
        [Header("Estado Actual")]
        [Tooltip("Dinero actual del jugador (se actualiza automáticamente)")]
        [SerializeField] private int currentMoney = 0;
        
        [Tooltip("Dinero total ganado en esta sesión de juego")]
        [SerializeField] private int sessionEarnings = 0;
        
        [Header("Estadísticas")]
        [Tooltip("Total de dinero ganado históricamente")]
        public int totalEarningsAllTime = 0;
        
        [Tooltip("Dinero gastado en la tienda")]
        public int totalSpent = 0;
        
        // Eventos para notificar cambios
        public event System.Action<int> OnMoneyChanged;
        public event System.Action<int, bool> OnMoneyEarned; // amount, isPositive
        public event System.Action<int> OnMoneySpent;
        
        // Propiedades públicas
        public int CurrentMoney => currentMoney;
        public int SessionEarnings => sessionEarnings;
        
        void Start()
        {
            InitializeMoneySystem();
        }
        
        void InitializeMoneySystem()
        {
            // Cargar dinero guardado
            LoadMoney();
            
            // Configurar dinero inicial si es la primera vez
            if (currentMoney <= 0 && moneyConfig != null)
            {
                currentMoney = moneyConfig.startingMoney;
                SaveMoney();
            }
            
            // Buscar UI si no está asignada
            if (moneyDisplay == null)
            {
                moneyDisplay = FindObjectOfType<MoneyDisplay>();
            }
            
            // Configurar UI inicial
            if (moneyDisplay != null)
            {
                moneyDisplay.SetMoney(currentMoney, false); // Sin animación inicial
            }
            
            // Notificar estado inicial
            OnMoneyChanged?.Invoke(currentMoney);
            
            Debug.Log($"MoneySystem inicializado. Dinero actual: {currentMoney}");
        }
        
        // MÉTODO PRINCIPAL: Agregar dinero por enemigo eliminado
        public void AddMoneyForEnemy(EnemyType enemyType)
        {
            if (moneyConfig == null)
            {
                Debug.LogWarning("MoneyConfig no asignado en MoneySystem");
                return;
            }
            
            int moneyAmount = moneyConfig.GetMoneyForEnemyType(enemyType);
            AddMoney(moneyAmount, enemyType != EnemyType.Innocent);
            
            Debug.Log($"Dinero por {enemyType}: {moneyAmount} (Total: {currentMoney})");
        }
        
        // Agregar dinero con animación
        public void AddMoney(int amount, bool isPositive = true)
        {
            int previousMoney = currentMoney;
            currentMoney += amount;
            
            // No permitir dinero negativo
            currentMoney = Mathf.Max(0, currentMoney);
            
            // Actualizar estadísticas
            if (isPositive && amount > 0)
            {
                sessionEarnings += amount;
                totalEarningsAllTime += amount;
            }
            
            // Guardar automáticamente
            SaveMoney();
            
            // Notificar cambios con animación
            if (moneyDisplay != null)
            {
                moneyDisplay.SetMoney(currentMoney, true, amount, isPositive);
            }
            
            // Disparar eventos
            OnMoneyChanged?.Invoke(currentMoney);
            OnMoneyEarned?.Invoke(amount, isPositive);
            
            Debug.Log($"Dinero {(isPositive ? "ganado" : "perdido")}: {amount}. Total: {currentMoney}");
        }
        
        // Gastar dinero (para la tienda)
        public bool SpendMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("Cantidad a gastar debe ser positiva");
                return false;
            }
            
            if (currentMoney < amount)
            {
                Debug.LogWarning($"Dinero insuficiente. Actual: {currentMoney}, Necesario: {amount}");
                return false;
            }
            
            currentMoney -= amount;
            totalSpent += amount;
            
            // Guardar automáticamente
            SaveMoney();
            
            // Actualizar UI
            if (moneyDisplay != null)
            {
                moneyDisplay.SetMoney(currentMoney, true, -amount, false);
            }
            
            // Disparar eventos
            OnMoneyChanged?.Invoke(currentMoney);
            OnMoneySpent?.Invoke(amount);
            
            Debug.Log($"Dinero gastado: {amount}. Restante: {currentMoney}");
            return true;
        }
        
        // Verificar si se puede comprar algo
        public bool CanAfford(int cost)
        {
            return currentMoney >= cost;
        }
        
        // Resetear dinero de sesión (para nuevos niveles)
        public void ResetSessionEarnings()
        {
            sessionEarnings = 0;
            Debug.Log("Ganancias de sesión reseteadas");
        }
        
        // CONEXIÓN CON SISTEMA DE GUARDADO (Lista G)
        public void SaveMoney()
        {
            PlayerPrefs.SetInt("CurrentMoney", currentMoney);
            PlayerPrefs.SetInt("TotalEarnings", totalEarningsAllTime);
            PlayerPrefs.SetInt("TotalSpent", totalSpent);
            PlayerPrefs.Save();
        }
        
        public void LoadMoney()
        {
            currentMoney = PlayerPrefs.GetInt("CurrentMoney", 0);
            totalEarningsAllTime = PlayerPrefs.GetInt("TotalEarnings", 0);
            totalSpent = PlayerPrefs.GetInt("TotalSpent", 0);
        }
        
        // Métodos para debugging y testing
        [ContextMenu("Add 100 Money")]
        public void AddTestMoney()
        {
            AddMoney(100, true);
        }
        
        [ContextMenu("Spend 50 Money")]
        public void SpendTestMoney()
        {
            SpendMoney(50);
        }
        
        [ContextMenu("Reset All Money")]
        public void ResetMoney()
        {
            currentMoney = moneyConfig != null ? moneyConfig.startingMoney : 100;
            sessionEarnings = 0;
            totalEarningsAllTime = 0;
            totalSpent = 0;
            
            SaveMoney();
            
            if (moneyDisplay != null)
            {
                moneyDisplay.SetMoney(currentMoney, false);
            }
            
            OnMoneyChanged?.Invoke(currentMoney);
            Debug.Log("Dinero reseteado");
        }
        
        // Getters para otros sistemas
        public int GetCurrentMoney() => currentMoney;
        public int GetSessionEarnings() => sessionEarnings;
        public int GetTotalEarnings() => totalEarningsAllTime;
        public float GetMoneyMultiplier() => moneyConfig != null ? moneyConfig.moneyMultiplier : 1.0f;
    }
}