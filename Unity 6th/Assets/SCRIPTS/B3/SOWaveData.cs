using UnityEngine;
using System.Collections.Generic;

// ARCHIVO: SOWaveData.cs  
// ScriptableObject para configurar oleadas fijas pre-diseñadas (B3)

namespace ShootingRange
{
    [System.Serializable]
    public class WaveSpawnEntry
    {
        [Header("Configuración de Spawn")]
        [Tooltip("Tipo de enemigo a generar")]
        public EnemyType enemyType = EnemyType.Normal;
        
        [Tooltip("Cantidad de enemigos de este tipo")]
        [Range(1, 20)]
        public int count = 1;
        
        [Tooltip("Tiempo entre spawns de este tipo (segundos)")]
        [Range(0.1f, 5f)]
        public float spawnInterval = 1f;
        
        [Tooltip("Delay antes de empezar a generar este tipo")]
        [Range(0f, 10f)]
        public float initialDelay = 0f;
        
        [Header("Configuración de Movimiento")]
        [Tooltip("Patrón de movimiento preferido para este spawn")]
        public MovementPattern preferredPattern = MovementPattern.Linear;
        
        [Tooltip("Multiplicador de velocidad para este spawn")]
        [Range(0.5f, 2f)]
        public float speedMultiplier = 1f;
    }
    
    [CreateAssetMenu(fileName = "WaveData", menuName = "Shooting Range/Wave Data")]
    public class SOWaveData : ScriptableObject
    {
        [Header("Información de la Oleada")]
        [Tooltip("Nombre identificativo de la oleada")]
        public string waveName = "Wave 1";
        
        [Tooltip("Duración total de la oleada en segundos")]
        [Range(5f, 120f)]
        public float waveDuration = 30f;
        
        [Header("Configuración de Spawns")]
        [Tooltip("Enemigos que aparecerán en esta oleada")]
        public List<WaveSpawnEntry> spawnEntries = new List<WaveSpawnEntry>();
        
        [Header("Configuración Global")]
        [Tooltip("Multiplicador de dificultad para toda la oleada")]
        [Range(0.5f, 3f)]
        public float difficultyMultiplier = 1f;
        
        [Tooltip("Máximo de enemigos simultáneos en pantalla")]
        [Range(5, 30)]
        public int maxSimultaneousEnemies = 10;
        
        [Header("Configuración de Rutas")]
        [Tooltip("Usar solo rutas específicas (dejar vacío para usar todas)")]
        public List<string> allowedRouteNames = new List<string>();
        
        [Header("Recompensas")]
        [Tooltip("Multiplicador de dinero para esta oleada")]
        [Range(0.5f, 2f)]
        public float moneyMultiplier = 1f;
        
        [Tooltip("Multiplicador de puntuación para esta oleada")]
        [Range(0.5f, 2f)]
        public float scoreMultiplier = 1f;
        
        // Métodos de utilidad
        public int GetTotalEnemyCount()
        {
            int total = 0;
            foreach (WaveSpawnEntry entry in spawnEntries)
            {
                total += entry.count;
            }
            return total;
        }
        
        public float GetEstimatedDuration()
        {
            float maxDuration = 0f;
            foreach (WaveSpawnEntry entry in spawnEntries)
            {
                float entryDuration = entry.initialDelay + (entry.count * entry.spawnInterval);
                maxDuration = Mathf.Max(maxDuration, entryDuration);
            }
            return Mathf.Max(maxDuration, waveDuration);
        }
        
        public List<EnemyType> GetAllEnemyTypes()
        {
            List<EnemyType> types = new List<EnemyType>();
            foreach (WaveSpawnEntry entry in spawnEntries)
            {
                if (!types.Contains(entry.enemyType))
                {
                    types.Add(entry.enemyType);
                }
            }
            return types;
        }
        
        public bool IsValidWave()
        {
            return spawnEntries.Count > 0 && waveDuration > 0 && !string.IsNullOrEmpty(waveName);
        }
        
        // Método para validar configuración
        void OnValidate()
        {
            // Asegurar valores mínimos
            waveDuration = Mathf.Max(5f, waveDuration);
            maxSimultaneousEnemies = Mathf.Max(1, maxSimultaneousEnemies);
            
            // Validar spawn entries
            foreach (WaveSpawnEntry entry in spawnEntries)
            {
                entry.count = Mathf.Max(1, entry.count);
                entry.spawnInterval = Mathf.Max(0.1f, entry.spawnInterval);
                entry.speedMultiplier = Mathf.Max(0.1f, entry.speedMultiplier);
            }
        }
    }
}