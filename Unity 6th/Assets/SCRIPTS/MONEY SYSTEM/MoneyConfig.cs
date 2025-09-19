using UnityEngine;

// ARCHIVO: MoneyConfig.cs
// ScriptableObject para configurar valores de dinero por tipo de enemigo

namespace ShootingRange
{
    [CreateAssetMenu(fileName = "MoneyConfig", menuName = "Shooting Range/Money Config")]
    public class MoneyConfig : ScriptableObject
    {
        [Header("Dinero por Tipo de Enemigo")]
        [Tooltip("Dinero que da un enemigo normal al ser eliminado")]
        public int normalEnemyMoney = 5;
        
        [Tooltip("Dinero que da un enemigo rápido al ser eliminado")]
        public int fastEnemyMoney = 8;
        
        [Tooltip("Dinero que da un enemigo saltador al ser eliminado")]
        public int jumperEnemyMoney = 10;
        
        [Tooltip("Dinero que da un enemigo valioso al ser eliminado")]
        public int valuableEnemyMoney = 25;
        
        [Header("Penalizaciones")]
        [Tooltip("Dinero que se RESTA al disparar un inocente (número positivo, se resta automáticamente)")]
        public int innocentPenalty = 15;
        
        [Header("Configuración de Economía")]
        [Tooltip("Multiplicador de dinero para balancear la economía (1.0 = normal, 1.5 = 50% más dinero)")]
        [Range(0.1f, 3.0f)]
        public float moneyMultiplier = 1.0f;
        
        [Tooltip("Dinero inicial que tiene el jugador al comenzar")]
        public int startingMoney = 100;
        
        // Método para obtener dinero por tipo de enemigo
        public int GetMoneyForEnemyType(EnemyType enemyType)
        {
            int baseMoney = 0;
            
            switch (enemyType)
            {
                case EnemyType.Normal:
                    baseMoney = normalEnemyMoney;
                    break;
                case EnemyType.Fast:
                    baseMoney = fastEnemyMoney;
                    break;
                case EnemyType.Jumper:
                    baseMoney = jumperEnemyMoney;
                    break;
                case EnemyType.Valuable:
                    baseMoney = valuableEnemyMoney;
                    break;
                case EnemyType.Innocent:
                    baseMoney = -innocentPenalty; // Negativo para restar
                    break;
                default:
                    baseMoney = 0;
                    break;
            }
            
            // Aplicar multiplicador
            return Mathf.RoundToInt(baseMoney * moneyMultiplier);
        }
        
        // Método para validar que los valores sean positivos
        void OnValidate()
        {
            normalEnemyMoney = Mathf.Max(0, normalEnemyMoney);
            fastEnemyMoney = Mathf.Max(0, fastEnemyMoney);
            jumperEnemyMoney = Mathf.Max(0, jumperEnemyMoney);
            valuableEnemyMoney = Mathf.Max(0, valuableEnemyMoney);
            innocentPenalty = Mathf.Max(0, innocentPenalty);
            startingMoney = Mathf.Max(0, startingMoney);
        }
    }
}