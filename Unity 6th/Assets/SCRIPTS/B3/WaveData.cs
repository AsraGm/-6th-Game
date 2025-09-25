using UnityEngine;
using System.Collections.Generic;

// ARCHIVO: WaveData.cs
// ScriptableObject para configurar oleadas fijas pre-diseñadas (B3)

namespace ShootingRange
{
    [System.Serializable]
    public class WaveSpawnConfig
    {
        [Header("Configuración de Spawn")]
        [Tooltip("Tipo de enemigo a spawnear")]
        public EnemyType enemyType = EnemyType.Normal;
        
        [Tooltip("Cantidad total de este tipo de enemigo en esta wave")]
        [Range(1, 50)]
        public int totalCount = 5;
        
        [Tooltip("Cada cuántos segundos aparece uno de este tipo")]
        [Range(0.5f, 10f)]
        public float spawnInterval = 2f;
        
        [Tooltip("Retraso antes de empezar a spawnear este tipo (desde inicio de wave)")]
        [Range(0f, 30f)]
        public float startDelay = 0f;
        
        // Variables de control interno
        [System.NonSerialized]
        public int spawnedCount = 0;
        
        [System.NonSerialized]
        public float lastSpawnTime = 0f;
        
        [System.NonSerialized]
        public bool hasStarted = false;
        
        public bool IsComplete => spawnedCount >= totalCount;
        public bool ShouldSpawn(float waveTime) 
        {
            if (!hasStarted && waveTime >= startDelay)
            {
                hasStarted = true;
                lastSpawnTime = waveTime;
                return true;
            }
            
            return hasStarted && !IsComplete && (waveTime - lastSpawnTime) >= spawnInterval;
        }
        
        public void ResetConfig()
        {
            spawnedCount = 0;
            lastSpawnTime = 0f;
            hasStarted = false;
        }
    }
    
    [CreateAssetMenu(fileName = "WaveData", menuName = "Shooting Range/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [Header("Información de la Wave")]
        [Tooltip("Nombre identificativo de esta wave")]
        public string waveName = "Wave 1";
        
        [Tooltip("Duración máxima de esta wave en segundos")]
        [Range(10f, 120f)]
        public float waveDuration = 30f;
        
        [Header("Configuración de Spawns")]
        [Tooltip("Lista de tipos de enemigos y sus configuraciones de spawn")]
        public List<WaveSpawnConfig> spawnConfigs = new List<WaveSpawnConfig>();
        
        [Header("Configuración Especial")]
        [Tooltip("Esta es la wave final del nivel")]
        public bool isFinalWave = false;
        
        [Tooltip("Multiplicador de dificultad para esta wave")]
        [Range(0.5f, 3f)]
        public float difficultyMultiplier = 1f;
        
        [Tooltip("Mensaje que aparece cuando inicia esta wave")]
        public string startMessage = "";
        
        // Métodos de utilidad
        public int GetTotalEnemies()
        {
            int total = 0;
            foreach (var config in spawnConfigs)
            {
                total += config.totalCount;
            }
            return total;
        }
        
        public bool IsWaveComplete()
        {
            foreach (var config in spawnConfigs)
            {
                if (!config.IsComplete)
                    return false;
            }
            return true;
        }
        
        public void ResetWave()
        {
            foreach (var config in spawnConfigs)
            {
                config.ResetConfig();
            }
        }
        
        public List<EnemyType> GetAllEnemyTypes()
        {
            List<EnemyType> types = new List<EnemyType>();
            foreach (var config in spawnConfigs)
            {
                if (!types.Contains(config.enemyType))
                {
                    types.Add(config.enemyType);
                }
            }
            return types;
        }
        
        // Validación en editor
        void OnValidate()
        {
            if (spawnConfigs == null)
                spawnConfigs = new List<WaveSpawnConfig>();
                
            foreach (var config in spawnConfigs)
            {
                if (config.totalCount <= 0)
                    config.totalCount = 1;
                if (config.spawnInterval <= 0)
                    config.spawnInterval = 1f;
            }
        }
    }
}