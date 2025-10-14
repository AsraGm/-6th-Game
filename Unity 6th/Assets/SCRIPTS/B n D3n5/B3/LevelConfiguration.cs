using UnityEngine;
using System.Collections.Generic;

// ARCHIVO: LevelConfiguration.cs
// ScriptableObject para configurar secuencia completa de oleadas de un nivel (B3)

namespace ShootingRange
{
    [CreateAssetMenu(fileName = "LevelConfiguration", menuName = "Shooting Range/Level Configuration")]
    public class LevelConfiguration : ScriptableObject
    {
        [Header("Información del Nivel")]
        [Tooltip("Nombre del nivel")]
        public string levelName = "Level 1";
        
        [Tooltip("Número del nivel (para ordenamiento)")]
        public int levelNumber = 1;
        
        [Header("Configuración de Oleadas")]
        [Tooltip("Secuencia de oleadas para este nivel")]
        public WaveData[] waves;
        
        [Header("Configuración de Transiciones")]
        [Tooltip("Delay entre oleadas (segundos)")]
        public float waveTransitionDelay = 1f;
        
        [Tooltip("Mostrar mensaje entre oleadas")]
        public bool showWaveTransitionMessages = true;
        
        [Header("Configuración de Spawning")]
        [Tooltip("Prefabs de enemigos por tipo")]
        public EnemyPrefabMapping[] enemyPrefabs;
        
        //[Tooltip("Puntos de spawn a usar (si está vacío, usa sistema automático)")]
        //public GameObject[] customSpawnPoints;
        
        [Header("Debug Information")]
        [SerializeField] private int totalWaves = 0;
        [SerializeField] private int totalEnemies = 0;
        [SerializeField] private float estimatedDuration = 0f;
        [SerializeField] private string[] enemyTypesUsed;
        
        [System.Serializable]
        public struct EnemyPrefabMapping
        {
            public EnemyType enemyType;
            public GameObject prefab;
        }
        
        // Métodos de utilidad
        public int GetTotalWaveCount()
        {
            return waves != null ? waves.Length : 0;
        }
        
        public int GetTotalEnemyCount()
        {
            int total = 0;
            if (waves != null)
            {
                foreach (var wave in waves)
                {
                    if (wave != null)
                    {
                        total += wave.GetTotalEnemyCount();
                    }
                }
            }
            return total;
        }
        
        public float GetEstimatedTotalDuration()
        {
            float total = 0f;
            if (waves != null)
            {
                foreach (var wave in waves)
                {
                    if (wave != null)
                    {
                        total += wave.waveDuration + waveTransitionDelay;
                    }
                }
            }
            return total;
        }
        
        public List<EnemyType> GetAllEnemyTypes()
        {
            List<EnemyType> allTypes = new List<EnemyType>();
            if (waves != null)
            {
                foreach (var wave in waves)
                {
                    if (wave != null)
                    {
                        var waveTypes = wave.GetUniqueEnemyTypes();
                        foreach (var type in waveTypes)
                        {
                            if (!allTypes.Contains(type))
                            {
                                allTypes.Add(type);
                            }
                        }
                    }
                }
            }
            return allTypes;
        }
        
        public bool IsValidConfiguration()
        {
            if (waves == null || waves.Length == 0)
                return false;
                
            foreach (var wave in waves)
            {
                if (wave == null || !wave.IsValidWave())
                    return false;
            }
            
            return true;
        }
        
        public GameObject GetPrefabForEnemyType(EnemyType enemyType)
        {
            if (enemyPrefabs != null)
            {
                foreach (var mapping in enemyPrefabs)
                {
                    if (mapping.enemyType == enemyType)
                    {
                        return mapping.prefab;
                    }
                }
            }
            
            Debug.LogWarning($"No prefab found for enemy type: {enemyType} in level {levelName}");
            return null;
        }
        
        public WaveData GetWave(int waveIndex)
        {
            if (waves != null && waveIndex >= 0 && waveIndex < waves.Length)
            {
                return waves[waveIndex];
            }
            return null;
        }
        
        //public bool HasCustomSpawnPoints()
        //{
        //    return customSpawnPoints != null && customSpawnPoints.Length > 0;
        //}
        
        //public Vector3 GetRandomCustomSpawnPoint()
        //{
        //    if (HasCustomSpawnPoints())
        //    {
        //        int randomIndex = Random.Range(0, customSpawnPoints.Length);
        //        return customSpawnPoints[randomIndex].transform.position;
        //    }
        //    return Vector3.zero;
        //}
        
        // Validación automática en el editor
        void OnValidate()
        {
            // Actualizar información de debug
            totalWaves = GetTotalWaveCount();
            totalEnemies = GetTotalEnemyCount();
            estimatedDuration = GetEstimatedTotalDuration();
            
            var enemyTypes = GetAllEnemyTypes();
            enemyTypesUsed = new string[enemyTypes.Count];
            for (int i = 0; i < enemyTypes.Count; i++)
            {
                enemyTypesUsed[i] = enemyTypes[i].ToString();
            }
            
            // Validar que todos los tipos de enemigos tengan prefabs
            if (enemyPrefabs != null && waves != null)
            {
                var usedTypes = GetAllEnemyTypes();
                foreach (var type in usedTypes)
                {
                    bool foundPrefab = false;
                    foreach (var mapping in enemyPrefabs)
                    {
                        if (mapping.enemyType == type && mapping.prefab != null)
                        {
                            foundPrefab = true;
                            break;
                        }
                    }
                    
                    if (!foundPrefab)
                    {
                        Debug.LogWarning($"Level '{levelName}': No prefab assigned for enemy type {type}");
                    }
                }
            }
            
            // Validar oleadas
            if (waves != null)
            {
                for (int i = 0; i < waves.Length; i++)
                {
                    if (waves[i] == null)
                    {
                        Debug.LogWarning($"Level '{levelName}': Wave {i} is null");
                    }
                    else if (!waves[i].IsValidWave())
                    {
                        Debug.LogWarning($"Level '{levelName}': Wave {i} ('{waves[i].waveName}') has invalid configuration");
                    }
                }
            }
            
            // Validar números de nivel duplicados
            levelNumber = Mathf.Max(1, levelNumber);
            
            // Validar delays
            waveTransitionDelay = Mathf.Max(0.1f, waveTransitionDelay);
        }
        
        // Métodos de debug para el editor
        [ContextMenu("Log Level Summary")]
        public void LogLevelSummary()
        {
            Debug.Log($"=== LEVEL SUMMARY: {levelName} ===");
            Debug.Log($"Total Waves: {totalWaves}");
            Debug.Log($"Total Enemies: {totalEnemies}");
            Debug.Log($"Estimated Duration: {estimatedDuration:F1} seconds");
            Debug.Log($"Enemy Types: {string.Join(", ", enemyTypesUsed)}");
            
            if (waves != null)
            {
                for (int i = 0; i < waves.Length; i++)
                {
                    if (waves[i] != null)
                    {
                        Debug.Log($"  Wave {i + 1}: {waves[i].waveName} - {waves[i].GetTotalEnemyCount()} enemies - {waves[i].waveDuration}s");
                    }
                }
            }
        }
        
        [ContextMenu("Validate Configuration")]
        public void ValidateConfiguration()
        {
            bool isValid = IsValidConfiguration();
            Debug.Log($"Level '{levelName}' configuration is {(isValid ? "VALID" : "INVALID")}");
            
            if (!isValid)
            {
                Debug.LogError("Please check the warnings above and fix the configuration");
            }
        }
    }
}