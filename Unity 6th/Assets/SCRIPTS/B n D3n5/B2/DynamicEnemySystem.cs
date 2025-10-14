using UnityEngine;

// ARCHIVO: DynamicEnemySystem.cs
// Integrador que combina BasicEnemy con comportamientos dinámicos (B2)

namespace ShootingRange
{
    [RequireComponent(typeof(BasicEnemy))]
    [RequireComponent(typeof(EnemyMovementPatterns))]
    public class DynamicEnemySystem : MonoBehaviour
    {
        [Header("Referencias de Componentes")]
        [Tooltip("Se configuran automáticamente")]
        private BasicEnemy basicEnemy;
        private EnemyMovementPatterns movementPatterns;
        
        [Header("Configuración Dinámica")]
        [Tooltip("Cambiar comportamiento basado en tiempo de vida")]
        public bool adaptBehaviorOverTime = true;
        
        [Tooltip("Tiempo antes de cambios de comportamiento (segundos)")]
        public float behaviorChangeInterval = 5f;
        
        [Header("Configuración por Tipo de Enemigo")]
        [Tooltip("Configuraciones específicas por tipo")]
        public EnemyTypeConfig[] enemyConfigurations;
        
        // Variables privadas
        private float timeAlive = 0f;
        private float lastBehaviorChange = 0f;
        private EnemyType currentEnemyType;
        private SpawnData spawnData;
        
        [System.Serializable]
        public class EnemyTypeConfig
        {
            public EnemyType enemyType;
            public MovementPattern[] preferredPatterns;
            public float speedMultiplier = 1f;
            public float aggressiveness = 1f;
        }
        
        void Awake()
        {
            GetComponents();
            SetupDefaultConfigurations();
        }
        
        void GetComponents()
        {
            basicEnemy = GetComponent<BasicEnemy>();
            movementPatterns = GetComponent<EnemyMovementPatterns>();
            
            if (basicEnemy == null)
            {
                Debug.LogError($"DynamicEnemySystem requiere BasicEnemy en {gameObject.name}");
            }
            
            if (movementPatterns == null)
            {
                Debug.LogError($"DynamicEnemySystem requiere EnemyMovementPatterns en {gameObject.name}");
            }
        }
        
        void SetupDefaultConfigurations()
        {
            if (enemyConfigurations == null || enemyConfigurations.Length == 0)
            {
                enemyConfigurations = new EnemyTypeConfig[]
                {
                    new EnemyTypeConfig
                    {
                        enemyType = EnemyType.Normal,
                        preferredPatterns = new[] { MovementPattern.Linear, MovementPattern.Zigzag },
                        speedMultiplier = 1f,
                        aggressiveness = 1f
                    },
                    new EnemyTypeConfig
                    {
                        enemyType = EnemyType.ZigZag,
                        preferredPatterns = new[] { MovementPattern.Linear, MovementPattern.Erratic },
                        speedMultiplier = 1.5f,
                        aggressiveness = 1.3f
                    },
                    new EnemyTypeConfig
                    {
                        enemyType = EnemyType.Jumper,
                        preferredPatterns = new[] { MovementPattern.Zigzag, MovementPattern.Erratic },
                        speedMultiplier = 1.2f,
                        aggressiveness = 1.1f
                    },
                    new EnemyTypeConfig
                    {
                        enemyType = EnemyType.Valuable,
                        preferredPatterns = new[] { MovementPattern.Circular, MovementPattern.Erratic },
                        speedMultiplier = 0.8f,
                        aggressiveness = 0.7f
                    },
                    new EnemyTypeConfig
                    {
                        enemyType = EnemyType.Innocent,
                        preferredPatterns = new[] { MovementPattern.Linear },
                        speedMultiplier = 0.9f,
                        aggressiveness = 0.5f
                    }
                };
            }
        }
        
        void Update()
        {
            if (!movementPatterns.enabled) return;
            
            timeAlive += Time.deltaTime;
            
            // Cambiar comportamiento adaptativo
            if (adaptBehaviorOverTime && timeAlive - lastBehaviorChange >= behaviorChangeInterval)
            {
                AdaptBehavior();
                lastBehaviorChange = timeAlive;
            }
        }
        
        // MÉTODO PRINCIPAL: Inicializar enemigo dinámico
        public void InitializeDynamicEnemy(SpawnData data, EnemyType enemyType)
        {
            spawnData = data;
            currentEnemyType = enemyType;
            
            // Configurar BasicEnemy
            if (basicEnemy != null)
            {
                basicEnemy.ConfigureEnemy(enemyType, "default");
            }
            
            // Configurar movimiento inicial
            ConfigureMovementForEnemyType(enemyType);
            
            // Aplicar datos de spawn
            ApplySpawnData(data);
            
            // Reset variables de tiempo
            timeAlive = 0f;
            lastBehaviorChange = 0f;
            
            Debug.Log($"Enemigo dinámico inicializado: {enemyType} con patrón {data.movementPattern}");
        }
        
        void ConfigureMovementForEnemyType(EnemyType enemyType)
        {
            EnemyTypeConfig config = GetConfigForEnemyType(enemyType);
            if (config == null || movementPatterns == null) return;
            
            // Seleccionar patrón preferido aleatoriamente
            MovementPattern selectedPattern = config.preferredPatterns[
                Random.Range(0, config.preferredPatterns.Length)
            ];
            
            // Configurar patrones de movimiento
            movementPatterns.currentPattern = selectedPattern;
            movementPatterns.baseSpeed *= config.speedMultiplier;
            movementPatterns.frequency *= config.aggressiveness;
            movementPatterns.amplitude *= config.aggressiveness;
        }
        
        void ApplySpawnData(SpawnData data)
        {
            if (movementPatterns == null || data == null) return;
            
            // Posición inicial
            transform.position = data.spawnPosition;
            
            // Configurar movimiento
            movementPatterns.initialDirection = data.initialDirection;
            movementPatterns.baseSpeed = data.baseSpeed;
            
            // Usar patrón del spawn data si está especificado
            if (data.movementPattern != movementPatterns.currentPattern)
            {
                movementPatterns.SetPattern(data.movementPattern);
            }
        }
        
        void AdaptBehavior()
        {
            EnemyTypeConfig config = GetConfigForEnemyType(currentEnemyType);
            if (config == null || movementPatterns == null) return;
            
            // Aumentar agresividad con el tiempo
            float timeMultiplier = 1f + (timeAlive * 0.1f); // 10% más agresivo cada 10 segundos
            timeMultiplier = Mathf.Min(timeMultiplier, 2f); // Máximo 2x
            
            // Cambiar patrón ocasionalmente
            if (Random.value < 0.3f) // 30% de probabilidad
            {
                MovementPattern newPattern = config.preferredPatterns[
                    Random.Range(0, config.preferredPatterns.Length)
                ];
                movementPatterns.SetPattern(newPattern);
            }
            
            // Aumentar velocidad gradualmente
            float newSpeed = movementPatterns.baseSpeed * timeMultiplier;
            movementPatterns.SetSpeed(newSpeed);
            
            Debug.Log($"Comportamiento adaptado para {currentEnemyType}: velocidad {newSpeed:F1}, tiempo vivo {timeAlive:F1}s");
        }
        
        EnemyTypeConfig GetConfigForEnemyType(EnemyType enemyType)
        {
            foreach (EnemyTypeConfig config in enemyConfigurations)
            {
                if (config.enemyType == enemyType)
                {
                    return config;
                }
            }
            return null;
        }
        
        // MÉTODOS PÚBLICOS PARA CONTROL EXTERNO
        
        public void PauseMovement()
        {
            if (movementPatterns != null)
            {
                movementPatterns.PauseMovement();
            }
        }
        
        public void ResumeMovement()
        {
            if (movementPatterns != null)
            {
                movementPatterns.ResumeMovement();
            }
        }
        
        public void ForcePatternChange(MovementPattern newPattern)
        {
            if (movementPatterns != null)
            {
                movementPatterns.SetPattern(newPattern);
                lastBehaviorChange = timeAlive; // Reset timer
            }
        }
        
        public void ModifySpeed(float multiplier)
        {
            if (movementPatterns != null)
            {
                float newSpeed = movementPatterns.GetCurrentSpeed() * multiplier;
                movementPatterns.SetSpeed(newSpeed);
            }
        }
        
        // GETTERS PARA INFORMACIÓN
        public EnemyType GetEnemyType() => currentEnemyType;
        public float GetTimeAlive() => timeAlive;
        public MovementPattern GetCurrentPattern() => movementPatterns?.GetCurrentPattern() ?? MovementPattern.Linear;
        public float GetCurrentSpeed() => movementPatterns?.GetCurrentSpeed() ?? 0f;
        public Vector2 GetCurrentVelocity() => movementPatterns?.GetCurrentVelocity() ?? Vector2.zero;
        
        // CONEXIÓN CON POOLING (IPoolable)
        public void OnSpawnFromPool()
        {
            // Activar componentes
            if (movementPatterns != null)
            {
                movementPatterns.enabled = true;
            }
            
            // Reset estado
            timeAlive = 0f;
            lastBehaviorChange = 0f;
        }
        
        public void OnReturnToPool()
        {
            // Pausar componentes
            if (movementPatterns != null)
            {
                movementPatterns.enabled = false;
            }
        }
        
        // MÉTODOS DE DEBUG
        [ContextMenu("Force Pattern Change")]
        public void DebugForcePatternChange()
        {
            AdaptBehavior();
        }
        
        [ContextMenu("Test Linear Pattern")]
        public void DebugTestLinear()
        {
            ForcePatternChange(MovementPattern.Linear);
        }
        
        [ContextMenu("Test Zigzag Pattern")]
        public void DebugTestZigzag()
        {
            ForcePatternChange(MovementPattern.Zigzag);
        }
        
        [ContextMenu("Test Circular Pattern")]
        public void DebugTestCircular()
        {
            ForcePatternChange(MovementPattern.Circular);
        }
        
        [ContextMenu("Test Erratic Pattern")]
        public void DebugTestErratic()
        {
            ForcePatternChange(MovementPattern.Erratic);
        }
        
        [ContextMenu("Increase Speed 50%")]
        public void DebugIncreaseSpeed()
        {
            ModifySpeed(1.5f);
        }
        
        void OnValidate()
        {
            // Asegurar valores válidos
            behaviorChangeInterval = Mathf.Max(1f, behaviorChangeInterval);
            
            if (enemyConfigurations != null)
            {
                foreach (EnemyTypeConfig config in enemyConfigurations)
                {
                    config.speedMultiplier = Mathf.Max(0.1f, config.speedMultiplier);
                    config.aggressiveness = Mathf.Max(0.1f, config.aggressiveness);
                }
            }
        }
    }
}