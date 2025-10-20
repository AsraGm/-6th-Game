using UnityEngine;
using System.Collections.Generic;

namespace ShootingRange
{
    // ScriptableObject para datos de nivel - INTEGRADO CON TU ARQUITECTURA
    [CreateAssetMenu(fileName = "LevelData", menuName = "Shooting Range/Level Data")]
    public class SOLevelData : ScriptableObject
    {
        [Header("Level Identification")]
        [Tooltip("ID único para tracking de estadísticas (ej: Level_01, Level_02)")]
        public string levelID = "Level_01"; // NUEVO - Para StatsTracker

        [Header("Level Basic Info")]
        public int levelNumber;
        public string levelName;

        [Header("Timing Configuration")]
        public float levelDuration = 60f; // duración en segundos

        [Header("Spawn Configuration - USANDO TUS ENUMS")]
        public List<EnemyType> allowedSpawnTypes = new List<EnemyType>
        {
            EnemyType.Normal,
            EnemyType.ZigZag,
        };

        public float baseSpawnRate = 2f; // enemigos por segundo
        public int maxEnemiesAtOnce = 10;

        [Header("Level Difficulty")]
        [Range(0.5f, 3f)]
        public float difficultyMultiplier = 1f;

        [Header("Reward Configuration")]
        [Tooltip("Multiplicador de dinero para este nivel")]
        [Range(0.5f, 3f)]
        public float moneyMultiplier = 1f;

        [Tooltip("Multiplicador de puntuación para este nivel")]
        [Range(0.5f, 3f)]
        public float scoreMultiplier = 1f;

        // Método helper para generar levelID automáticamente si está vacío
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(levelID))
            {
                levelID = $"Level_{levelNumber:D2}";
            }
        }
    }
}