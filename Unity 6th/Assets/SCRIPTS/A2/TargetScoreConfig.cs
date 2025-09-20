using UnityEngine;

// ARCHIVO: TargetScoreConfig.cs
// ScriptableObject para configurar puntuaciones

namespace ShootingRange
{
    [CreateAssetMenu(fileName = "TargetScoreConfig", menuName = "Shooting Range/Target Score Config")]
    public class TargetScoreConfig : ScriptableObject
    {
        [Header("Puntuaciones por Tipo de Enemigo")]
        [Tooltip("Puntos que da un enemigo normal al ser disparado")]
        public int normalEnemyScore = 10;
        
        [Tooltip("Puntos que da un enemigo rápido al ser disparado")]
        public int fastEnemyScore = 15;
        
        [Tooltip("Puntos que da un enemigo saltador al ser disparado")]
        public int jumperEnemyScore = 20;
        
        [Tooltip("Puntos que da un enemigo valioso al ser disparado")]
        public int valuableEnemyScore = 50;
        
        [Header("Penalizaciones")]
        [Tooltip("Puntos que se RESTAN al disparar un inocente (número negativo)")]
        public int innocentPenalty = -25;
        
        // Método para obtener puntuación por tipo
        public int GetScoreForEnemyType(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Normal:
                    return normalEnemyScore;
                case EnemyType.Fast:
                    return fastEnemyScore;
                case EnemyType.Jumper:
                    return jumperEnemyScore;
                case EnemyType.Valuable:
                    return valuableEnemyScore;
                case EnemyType.Innocent:
                    return innocentPenalty;
                default:
                    return 0;
            }
        }
    }
}