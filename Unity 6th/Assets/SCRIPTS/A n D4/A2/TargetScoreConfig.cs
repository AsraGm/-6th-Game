using UnityEngine;

// ARCHIVO: TargetScoreConfig.cs
// ScriptableObject para configurar puntuaciones

namespace ShootingRange
{
    [CreateAssetMenu(fileName = "TargetScoreConfig", menuName = "Shooting Range/Target Score Config")]
    public class TargetScoreConfig : ScriptableObject
    {
        [Header("Puntuaciones por Tipo de Enemigo")]
        [Tooltip("Puntos que da un enemigo statico al ser disparado")]
        public int staticScore = 5;

        [Tooltip("Puntos que da un enemigo normal al ser disparado")]
        public int normalScore= 10;
        
        [Tooltip("Puntos que da un enemigo rápido al ser disparado")]
        public int zigZagEnemy = 15;
        
        [Tooltip("Puntos que da un enemigo saltador al ser disparado")]
        public int hangedScore = 20;
        
        [Tooltip("Puntos que da un enemigo valioso al ser disparado")]
        public int valuableScore = 50;
        
        [Header("Penalizaciones")]
        [Tooltip("Puntos que se RESTAN al disparar un inocente (número negativo)")]
        public int innocentPenalty = -25;
        
        // Método para obtener puntuación por tipo
        public int GetScoreForEnemyType(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Static:
                    return staticScore;
                case EnemyType.Normal:
                    return normalScore;
                case EnemyType.ZigZag:
                    return zigZagEnemy;
                case EnemyType.Jumper:
                    return hangedScore;
                case EnemyType.Valuable:
                    return valuableScore;
                case EnemyType.Innocent:
                    return innocentPenalty;
                default:
                    return 0;
            }
        }
    }
}