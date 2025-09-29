using UnityEngine;
using System.Collections.Generic;

// ARCHIVO: WaveData.cs
// ScriptableObject para configurar oleadas individuales (B3)

namespace ShootingRange
{
    [System.Serializable]
    public struct EnemySpawnInfo
    {
        [Header("Configuración de Spawn")]
        [Tooltip("Tipo de enemigo a spawnear")]
        public EnemyType enemyType;
        
        [Tooltip("Cantidad de enemigos de este tipo")]
        [Range(1, 20)]
        public int quantity;
        
        [Tooltip("Delay entre spawns individuales (segundos)")]
        [Range(0.1f, 5f)]
        public float spawnDelay;
        
        [Header("Configuración Visual")]
        [Tooltip("Tiempo antes de rotar a 0 grados (efecto cartón feria)")]
        [Range(0.5f, 3f)]
        public float rotationDelay;
    }

    [CreateAssetMenu(fileName = "WaveData", menuName = "Shooting Range/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [Header("Información de Oleada")]
        [Tooltip("Nombre identificativo de la oleada")]
        public string waveName = "Wave 1";
        
        [Tooltip("Duración total de esta oleada (segundos)")]
        [Range(5f, 60f)]
        public float waveDuration = 15f;
        
        [Tooltip("Tiempo de warning antes del final (para rotar enemigos)")]
        [Range(1f, 5f)]
        public float warningTime = 3f;
        
        [Header("Configuración de Enemigos")]
        [Tooltip("Enemigos que aparecerán en esta oleada")]
        public EnemySpawnInfo[] enemiesToSpawn;
        
        [Header("Configuración de Spawn")]
        [Tooltip("Delay inicial antes de comenzar a spawnear")]
        [Range(0f, 3f)]
        public float initialDelay = 0.5f;
        
        [Tooltip("Spawnear enemigos en orden secuencial o aleatorio")]
        public bool spawnInRandomOrder = false;
        
        [Header("Debug Info")]
        [Tooltip("Información calculada automáticamente")]
        [SerializeField] private int totalEnemies = 0;
        [SerializeField] private float estimatedSpawnTime = 0f;
        
        // Métodos de utilidad
        public int GetTotalEnemyCount()
        {
            int total = 0;
            if (enemiesToSpawn != null)
            {
                foreach (var spawnInfo in enemiesToSpawn)
                {
                    total += spawnInfo.quantity;
                }
            }
            return total;
        }
        
        public float GetEstimatedSpawnTime()
        {
            float totalTime = initialDelay;
            if (enemiesToSpawn != null)
            {
                foreach (var spawnInfo in enemiesToSpawn)
                {
                    totalTime += spawnInfo.quantity * spawnInfo.spawnDelay;
                }
            }
            return totalTime;
        }
        
        public bool IsValidWave()
        {
            return enemiesToSpawn != null && 
                   enemiesToSpawn.Length > 0 && 
                   GetTotalEnemyCount() > 0 &&
                   waveDuration > 0f;
        }
        
        public List<EnemyType> GetUniqueEnemyTypes()
        {
            List<EnemyType> uniqueTypes = new List<EnemyType>();
            if (enemiesToSpawn != null)
            {
                foreach (var spawnInfo in enemiesToSpawn)
                {
                    if (!uniqueTypes.Contains(spawnInfo.enemyType))
                    {
                        uniqueTypes.Add(spawnInfo.enemyType);
                    }
                }
            }
            return uniqueTypes;
        }
        
        // Validación automática en el editor
        void OnValidate()
        {
            // Actualizar información de debug
            totalEnemies = GetTotalEnemyCount();
            estimatedSpawnTime = GetEstimatedSpawnTime();
            
            // Validar que warning time no sea mayor que wave duration
            if (warningTime >= waveDuration)
            {
                warningTime = waveDuration - 1f;
            }
            
            // Validar delays
            if (enemiesToSpawn != null)
            {
                for (int i = 0; i < enemiesToSpawn.Length; i++)
                {
                    if (enemiesToSpawn[i].spawnDelay <= 0)
                    {
                        enemiesToSpawn[i].spawnDelay = 0.1f;
                    }
                    
                    if (enemiesToSpawn[i].quantity <= 0)
                    {
                        enemiesToSpawn[i].quantity = 1;
                    }
                    
                    if (enemiesToSpawn[i].rotationDelay <= 0)
                    {
                        enemiesToSpawn[i].rotationDelay = 1f;
                    }
                }
            }
            
            // Warning si el tiempo estimado es mayor que la duración
            if (estimatedSpawnTime > waveDuration)
            {
                Debug.LogWarning($"Wave '{waveName}': Tiempo estimado de spawn ({estimatedSpawnTime:F1}s) es mayor que la duración de oleada ({waveDuration}s)");
            }
        }
    }
}