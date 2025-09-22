using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ARCHIVO: WaveSystem.cs
// Sistema de oleadas fijas pre-diseñadas (B3)
// CONEXIÓN CON: Lista A4 (Level timing), Lista F1 (Level data)

namespace ShootingRange
{
    [System.Serializable]
    public class EnemyPrefabConfig
    {
        [Header("Configuración de Prefab")]
        [Tooltip("Tipo de enemigo")]
        public EnemyType enemyType = EnemyType.Normal;

        [Tooltip("Prefab para este tipo de enemigo")]
        public GameObject prefab;

        [Header("Configuración Visual")]
        [Tooltip("Nombre descriptivo (opcional)")]
        public string displayName = "";

        // Validación
        public bool IsValid()
        {
            return prefab != null && prefab.GetComponent<BasicEnemy>() != null;
        }

        // Información para debug
        public override string ToString()
        {
            return $"{enemyType}: {(prefab ? prefab.name : "NULL")}";
        }
    }

    [System.Serializable]
    public class WaveSpawnTask
    {
        public EnemyType enemyType;
        public int remainingCount;
        public float nextSpawnTime;
        public float spawnInterval;
        public MovementPattern preferredPattern;
        public float speedMultiplier;
        public bool isCompleted = false;
    }

    public class WaveSystem : MonoBehaviour
    {
        [Header("Configuración de Oleadas")]
        [Tooltip("Oleadas pre-configuradas para el nivel actual")]
        public List<SOWaveData> levelWaves = new List<SOWaveData>();

        [Tooltip("Cargar oleadas desde LevelManager automáticamente")]
        public bool loadWavesFromLevelManager = true;

        [Header("Referencias de Sistema")]
        [Tooltip("ARRASTRA AQUÍ tu SpawnPointsSystem")]
        public SpawnPointsSystem spawnPointsSystem;

        [Tooltip("Lista de prefabs por tipo de enemigo")]
        public List<EnemyPrefabConfig> enemyPrefabs = new List<EnemyPrefabConfig>();

        [Tooltip("Transform padre para organizar enemigos spawneados")]
        public Transform enemyParent;

        [Header("Configuración de Pool")]
        [Tooltip("Tamaño del pool por tipo de enemigo")]
        [Range(10, 100)]
        public int poolSizePerType = 20;

        [Tooltip("Pool total máximo de enemigos")]
        [Range(50, 500)]
        public int maxTotalPoolSize = 200;

        [Header("Estado del Sistema")]
        [Tooltip("Índice de la oleada actual")]
        [SerializeField] private int currentWaveIndex = 0;

        [Tooltip("¿El sistema está activo?")]
        [SerializeField] private bool isSystemActive = false;

        [Tooltip("¿La oleada actual está corriendo?")]
        [SerializeField] private bool isWaveRunning = false;

        [Tooltip("Enemigos actualmente en pantalla")]
        [SerializeField] private int activeEnemyCount = 0;

        // Eventos para comunicación con otros sistemas
        public event System.Action<SOWaveData> OnWaveStarted;
        public event System.Action<SOWaveData> OnWaveCompleted;
        public event System.Action OnAllWavesCompleted;
        public event System.Action<EnemyType, Vector3> OnEnemySpawned;

        // Pool de enemigos por tipo
        private Dictionary<EnemyType, Queue<GameObject>> enemyPools = new Dictionary<EnemyType, Queue<GameObject>>();
        private List<GameObject> activeEnemies = new List<GameObject>();

        // Control de oleadas
        private SOWaveData currentWave;
        private List<WaveSpawnTask> currentSpawnTasks = new List<WaveSpawnTask>();
        private float waveStartTime;
        private bool wavesPreloaded = false;

        // Referencias a sistemas conectados
        private LevelManager levelManager;
        private LevelTimer levelTimer;

        void Start()
        {
            InitializeWaveSystem();
        }

        void InitializeWaveSystem()
        {
            // Buscar sistemas conectados
            FindConnectedSystems();

            // Validar configuración de prefabs
            ValidateEnemyPrefabs();

            // Conectar con eventos de nivel
            ConnectToLevelSystem();

            // Precargar oleadas
            if (loadWavesFromLevelManager)
            {
                LoadWavesFromLevel();
            }

            PreloadWaves();

            Debug.Log($"WaveSystem inicializado con {levelWaves.Count} oleadas y {enemyPrefabs.Count} tipos de prefabs");
        }

        // NUEVO: Validar configuración de prefabs
        void ValidateEnemyPrefabs()
        {
            if (enemyPrefabs == null || enemyPrefabs.Count == 0)
            {
                Debug.LogError("WaveSystem: No hay prefabs de enemigos configurados! Agrega prefabs en el inspector.");
                return;
            }

            // Verificar que cada prefab sea válido
            for (int i = enemyPrefabs.Count - 1; i >= 0; i--)
            {
                EnemyPrefabConfig config = enemyPrefabs[i];
                if (!config.IsValid())
                {
                    Debug.LogError($"WaveSystem: Configuración inválida para {config.enemyType} - Prefab: {(config.prefab ? config.prefab.name : "NULL")}");
                    enemyPrefabs.RemoveAt(i);
                }
            }

            // Verificar que hay al menos un prefab válido
            if (enemyPrefabs.Count == 0)
            {
                Debug.LogError("WaveSystem: No hay prefabs válidos configurados! El sistema no funcionará.");
                return;
            }

            // Log de configuración válida
            Debug.Log($"WaveSystem: Prefabs validados:");
            foreach (EnemyPrefabConfig config in enemyPrefabs)
            {
                Debug.Log($"  - {config}");
            }
        }

        void FindConnectedSystems()
        {
            if (spawnPointsSystem == null)
                spawnPointsSystem = FindObjectOfType<SpawnPointsSystem>();

            levelManager = FindObjectOfType<LevelManager>();
            levelTimer = FindObjectOfType<LevelTimer>();

            // Configurar parent por defecto
            if (enemyParent == null)
            {
                GameObject parentObj = new GameObject("Active Enemies");
                parentObj.transform.SetParent(transform);
                enemyParent = parentObj.transform;
            }
        }

        void ConnectToLevelSystem()
        {
            // CONEXIÓN CON F1 - LevelManager
            if (levelManager != null)
            {
                LevelManager.OnLevelLoaded += HandleLevelLoaded;
                LevelManager.OnSpawnConfigChanged += HandleSpawnConfigChanged;
            }

            // CONEXIÓN CON A4 - LevelTimer (las líneas comentadas que mencionaste)
            if (levelTimer != null)
            {
                levelTimer.OnTimerStarted += HandleTimerStarted;
                levelTimer.OnTimeUp += HandleTimeUp;
                levelTimer.OnTimerPaused += HandleTimerPaused;
                levelTimer.OnTimerResumed += HandleTimerResumed;
            }
        }

        void LoadWavesFromLevel()
        {
            if (levelManager?.CurrentLevel != null)
            {
                // AQUÍ SE CONECTARÍA CON SOLevelData si tuviera oleadas configuradas
                // Por ahora, creamos oleadas automáticamente basadas en los datos del nivel
                CreateDefaultWavesFromLevelData(levelManager.CurrentLevel);
            }
        }

        void CreateDefaultWavesFromLevelData(SOLevelData levelData)
        {
            levelWaves.Clear();

            // Crear oleadas automáticas basadas en la duración y tipos del nivel
            float totalDuration = levelData.levelDuration;
            int waveCount = Mathf.Max(1, Mathf.FloorToInt(totalDuration / 30f)); // Una oleada cada 30 segundos

            for (int i = 0; i < waveCount; i++)
            {
                SOWaveData wave = CreateWaveFromLevelData(levelData, i, totalDuration / waveCount);
                if (wave != null)
                {
                    levelWaves.Add(wave);
                }
            }

            Debug.Log($"Creadas {levelWaves.Count} oleadas automáticas para nivel {levelData.levelNumber}");
        }

        SOWaveData CreateWaveFromLevelData(SOLevelData levelData, int waveIndex, float waveDuration)
        {
            // Crear ScriptableObject en runtime (no se guardará)
            SOWaveData wave = ScriptableObject.CreateInstance<SOWaveData>();
            wave.waveName = $"Auto Wave {waveIndex + 1}";
            wave.waveDuration = waveDuration;
            wave.difficultyMultiplier = levelData.difficultyMultiplier + (waveIndex * 0.1f); // Aumentar dificultad
            wave.maxSimultaneousEnemies = levelData.maxEnemiesAtOnce;
            wave.moneyMultiplier = levelData.moneyMultiplier;
            wave.scoreMultiplier = levelData.scoreMultiplier;

            // Crear spawn entries basados en los tipos permitidos del nivel
            wave.spawnEntries = new List<WaveSpawnEntry>();
            foreach (EnemyType enemyType in levelData.allowedSpawnTypes)
            {
                WaveSpawnEntry entry = new WaveSpawnEntry();
                entry.enemyType = enemyType;
                entry.count = Random.Range(3, 8); // 3-7 enemigos por tipo
                entry.spawnInterval = 1f / levelData.baseSpawnRate; // Basado en spawn rate del nivel
                entry.initialDelay = Random.Range(0f, 5f);
                entry.preferredPattern = GetRandomPatternForEnemyType(enemyType);
                entry.speedMultiplier = 1f + (waveIndex * 0.1f); // Aumentar velocidad por oleada

                wave.spawnEntries.Add(entry);
            }

            return wave;
        }

        MovementPattern GetRandomPatternForEnemyType(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Fast:
                    return Random.value < 0.5f ? MovementPattern.Linear : MovementPattern.Erratic;
                case EnemyType.Jumper:
                    return Random.value < 0.5f ? MovementPattern.Zigzag : MovementPattern.Erratic;
                case EnemyType.Valuable:
                    return MovementPattern.Circular;
                case EnemyType.Innocent:
                    return MovementPattern.Linear;
                default:
                    return MovementPattern.Linear;
            }
        }

        // OPTIMIZACIÓN MÓVIL: Pre-carga de waves completas
        void PreloadWaves()
        {
            if (wavesPreloaded) return;

            // Crear pools para todos los tipos de enemigos que aparecen en las oleadas
            HashSet<EnemyType> allEnemyTypes = new HashSet<EnemyType>();
            foreach (SOWaveData wave in levelWaves)
            {
                foreach (EnemyType type in wave.GetAllEnemyTypes())
                {
                    allEnemyTypes.Add(type);
                }
            }

            // Crear pools
            foreach (EnemyType enemyType in allEnemyTypes)
            {
                CreateEnemyPool(enemyType, poolSizePerType);
            }

            wavesPreloaded = true;
            Debug.Log($"Pools pre-cargados para {allEnemyTypes.Count} tipos de enemigos");
        }

        void CreateEnemyPool(EnemyType enemyType, int poolSize)
        {
            if (enemyPools.ContainsKey(enemyType)) return;

            // Buscar el prefab correcto para este tipo de enemigo
            GameObject prefabToUse = GetPrefabForEnemyType(enemyType);
            if (prefabToUse == null)
            {
                Debug.LogError($"No se encontró prefab para el tipo de enemigo: {enemyType}");
                return;
            }

            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < poolSize; i++)
            {
                // INSTANCIACIÓN LIMPIA SIN MODIFICACIONES
                GameObject enemy = Instantiate(prefabToUse, enemyParent);
                enemy.name = $"{prefabToUse.name}_Pool_{i}"; // Solo cambiar nombre para organización

                // SOLO desactivar, sin tocar nada más
                enemy.SetActive(false);
                pool.Enqueue(enemy);
            }

            enemyPools[enemyType] = pool;
            Debug.Log($"Pool limpio creado para {enemyType} usando prefab {prefabToUse.name}: {poolSize} enemigos");
        }

        // NUEVO: Validar componentes del prefab original
        void ValidatePrefabComponents(GameObject prefab, EnemyType enemyType)
        {
            SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogError($"PREFAB PROBLEMA: {prefab.name} para {enemyType} no tiene SpriteRenderer!");
            }
            else if (sr.sprite == null)
            {
                Debug.LogError($"PREFAB PROBLEMA: {prefab.name} tiene SpriteRenderer pero sin sprite asignado!");
            }
            else
            {
                Debug.Log($"Prefab OK: {prefab.name} tiene sprite: {sr.sprite.name}");
            }

            BasicEnemy basicEnemy = prefab.GetComponent<BasicEnemy>();
            if (basicEnemy == null)
            {
                Debug.LogError($"PREFAB PROBLEMA: {prefab.name} no tiene componente BasicEnemy!");
            }
        }

        // NUEVO: Validar componentes después de instanciar
        void ValidateInstancedComponents(GameObject instance, GameObject originalPrefab, EnemyType enemyType)
        {
            SpriteRenderer instanceSR = instance.GetComponent<SpriteRenderer>();
            SpriteRenderer prefabSR = originalPrefab.GetComponent<SpriteRenderer>();

            if (instanceSR == null)
            {
                Debug.LogError($"INSTANCIA PROBLEMA: {instance.name} perdió SpriteRenderer al instanciar!");
            }
            else if (instanceSR.sprite == null)
            {
                Debug.LogError($"INSTANCIA PROBLEMA: {instance.name} perdió sprite al instanciar!");

                // INTENTAR REPARAR: Copiar sprite del prefab original
                if (prefabSR != null && prefabSR.sprite != null)
                {
                    instanceSR.sprite = prefabSR.sprite;
                    instanceSR.color = prefabSR.color;
                    Debug.Log($"REPARADO: Sprite restaurado para {instance.name}");
                }
            }
            else
            {
                Debug.Log($"Instancia OK: {instance.name} tiene sprite: {instanceSR.sprite.name}");
            }
        }

        // NUEVO: Método para obtener el prefab correcto por tipo
        GameObject GetPrefabForEnemyType(EnemyType enemyType)
        {
            // Buscar configuración específica para este tipo
            foreach (EnemyPrefabConfig config in enemyPrefabs)
            {
                if (config.enemyType == enemyType && config.IsValid())
                {
                    return config.prefab;
                }
            }

            // Fallback: usar el primer prefab válido disponible
            foreach (EnemyPrefabConfig config in enemyPrefabs)
            {
                if (config.IsValid())
                {
                    Debug.LogWarning($"No se encontró prefab específico para {enemyType}, usando {config.prefab.name} como fallback");
                    return config.prefab;
                }
            }

            return null;
        }

        // MÉTODOS PRINCIPALES DE CONTROL (CONEXIÓN CON A4)

        public void StartWaves()
        {
            if (levelWaves.Count == 0)
            {
                Debug.LogWarning("No hay oleadas configuradas para iniciar");
                return;
            }

            isSystemActive = true;
            currentWaveIndex = 0;

            StartNextWave();
            Debug.Log("Sistema de oleadas iniciado");
        }

        public void PauseWaves()
        {
            isSystemActive = false;
            // Las corrutinas de spawn se pausarán automáticamente al verificar isSystemActive
            Debug.Log("Sistema de oleadas pausado");
        }

        public void ResumeWaves()
        {
            isSystemActive = true;
            Debug.Log("Sistema de oleadas resumido");
        }

        public void StopWaveSystem()
        {
            isSystemActive = false;
            isWaveRunning = false;

            // Detener todas las corrutinas
            StopAllCoroutines();

            // Opcional: Desactivar enemigos activos
            foreach (GameObject enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    ReturnEnemyToPool(enemy);
                }
            }
            activeEnemies.Clear();
            activeEnemyCount = 0;

            Debug.Log("Sistema de oleadas detenido completamente");
        }

        public void ResetWaves()
        {
            StopWaveSystem();
            currentWaveIndex = 0;
            currentSpawnTasks.Clear();
            Debug.Log("Sistema de oleadas reseteado");
        }

        void StartNextWave()
        {
            if (currentWaveIndex >= levelWaves.Count)
            {
                // Todas las oleadas completadas
                Debug.Log("¡Todas las oleadas completadas!");
                OnAllWavesCompleted?.Invoke();
                return;
            }

            currentWave = levelWaves[currentWaveIndex];
            if (currentWave == null || !currentWave.IsValidWave())
            {
                Debug.LogError($"Oleada {currentWaveIndex} no es válida");
                currentWaveIndex++;
                StartNextWave();
                return;
            }

            Debug.Log($"Iniciando oleada: {currentWave.waveName}");

            // Configurar spawn tasks para esta oleada
            SetupWaveSpawnTasks();

            // Iniciar oleada
            isWaveRunning = true;
            waveStartTime = Time.time;

            // Iniciar corrutina de spawn
            StartCoroutine(WaveSpawnCoroutine());

            // Iniciar monitoreo de oleada
            StartCoroutine(WaveMonitorCoroutine());

            // Notificar inicio de oleada
            OnWaveStarted?.Invoke(currentWave);
        }

        void SetupWaveSpawnTasks()
        {
            currentSpawnTasks.Clear();

            foreach (WaveSpawnEntry entry in currentWave.spawnEntries)
            {
                WaveSpawnTask task = new WaveSpawnTask();
                task.enemyType = entry.enemyType;
                task.remainingCount = entry.count;
                task.spawnInterval = entry.spawnInterval;
                task.nextSpawnTime = Time.time + entry.initialDelay;
                task.preferredPattern = entry.preferredPattern;
                task.speedMultiplier = entry.speedMultiplier * currentWave.difficultyMultiplier;
                task.isCompleted = false;

                currentSpawnTasks.Add(task);
            }
        }

        // CORRUTINA PRINCIPAL DE SPAWN
        IEnumerator WaveSpawnCoroutine()
        {
            while (isWaveRunning && isSystemActive)
            {
                // Verificar límite de enemigos simultáneos
                if (activeEnemyCount >= currentWave.maxSimultaneousEnemies)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                // Procesar cada spawn task
                bool hasActiveTasks = false;
                foreach (WaveSpawnTask task in currentSpawnTasks)
                {
                    if (!task.isCompleted && Time.time >= task.nextSpawnTime)
                    {
                        SpawnEnemyFromTask(task);

                        task.remainingCount--;
                        if (task.remainingCount <= 0)
                        {
                            task.isCompleted = true;
                        }
                        else
                        {
                            task.nextSpawnTime = Time.time + task.spawnInterval;
                            hasActiveTasks = true;
                        }
                    }
                    else if (!task.isCompleted)
                    {
                        hasActiveTasks = true;
                    }
                }

                // Si no hay más tasks activas, la oleada está completa en términos de spawn
                if (!hasActiveTasks)
                {
                    Debug.Log($"Spawn completado para oleada: {currentWave.waveName}");
                    break;
                }

                yield return new WaitForSeconds(0.1f); // Verificar cada 100ms
            }
        }

        void SpawnEnemyFromTask(WaveSpawnTask task)
        {
            // Obtener enemigo del pool
            GameObject enemy = GetEnemyFromPool(task.enemyType);
            if (enemy == null)
            {
                Debug.LogWarning($"No se pudo obtener enemigo del tipo {task.enemyType} del pool");
                return;
            }

            // Obtener datos de spawn
            SpawnData spawnData = spawnPointsSystem?.GetSpawnDataForEnemyType(task.enemyType);
            if (spawnData == null)
            {
                spawnData = spawnPointsSystem?.GetRandomSpawnData();
            }

            if (spawnData == null)
            {
                Debug.LogWarning("No se pudieron obtener datos de spawn");
                ReturnEnemyToPool(enemy);
                return;
            }

            // Aplicar multiplicador de velocidad de la task
            spawnData.baseSpeed *= task.speedMultiplier;
            spawnData.movementPattern = task.preferredPattern;

            // Configurar y activar enemigo
            ConfigureAndActivateEnemy(enemy, spawnData, task.enemyType);

            // Añadir a lista de activos
            activeEnemies.Add(enemy);
            activeEnemyCount++;

            // Notificar spawn
            OnEnemySpawned?.Invoke(task.enemyType, enemy.transform.position);

            Debug.Log($"Spawneado {task.enemyType} en {spawnData.spawnPosition} (Activos: {activeEnemyCount})");
        }

        void ConfigureAndActivateEnemy(GameObject enemy, SpawnData spawnData, EnemyType enemyType)
        {
            // Posicionar y activar PRIMERO
            enemy.transform.position = spawnData.spawnPosition;
            enemy.SetActive(true);

            // CONFIGURACIÓN MÍNIMA - Solo sistemas dinámicos
            DynamicEnemySystem dynamicSystem = enemy.GetComponent<DynamicEnemySystem>();
            if (dynamicSystem != null)
            {
                dynamicSystem.InitializeDynamicEnemy(spawnData, enemyType);
            }
            else
            {
                // Fallback: configurar movimiento básico solo si no tiene DynamicEnemySystem
                EnemyMovementPatterns movement = enemy.GetComponent<EnemyMovementPatterns>();
                if (movement != null)
                {
                    movement.initialDirection = spawnData.initialDirection;
                    movement.baseSpeed = spawnData.baseSpeed;
                    movement.currentPattern = spawnData.movementPattern;
                }
            }

            // Si implementa IPoolable, notificar spawn
            IPoolable poolable = enemy.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawnFromPool();
            }
        }

        // MONITOREO DE OLEADA
        IEnumerator WaveMonitorCoroutine()
        {
            while (isWaveRunning && isSystemActive)
            {
                // Verificar si la oleada ha terminado por tiempo
                if (Time.time - waveStartTime >= currentWave.waveDuration)
                {
                    CompleteCurrentWave();
                    yield break;
                }

                // Verificar si todos los spawn tasks están completos Y no hay enemigos activos
                bool allTasksCompleted = true;
                foreach (WaveSpawnTask task in currentSpawnTasks)
                {
                    if (!task.isCompleted)
                    {
                        allTasksCompleted = true;
                        break;
                    }
                }

                if (allTasksCompleted && activeEnemyCount <= 0)
                {
                    CompleteCurrentWave();
                    yield break;
                }

                yield return new WaitForSeconds(1f); // Verificar cada segundo
            }
        }

        void CompleteCurrentWave()
        {
            isWaveRunning = false;

            Debug.Log($"Oleada completada: {currentWave.waveName}");
            OnWaveCompleted?.Invoke(currentWave);

            currentWaveIndex++;

            // Iniciar siguiente oleada después de un pequeño delay
            if (currentWaveIndex < levelWaves.Count)
            {
                Invoke(nameof(StartNextWave), 2f);
            }
            else
            {
                OnAllWavesCompleted?.Invoke();
            }
        }

        // GESTIÓN DE POOLS
        GameObject GetEnemyFromPool(EnemyType enemyType)
        {
            if (!enemyPools.ContainsKey(enemyType) || enemyPools[enemyType].Count == 0)
            {
                // Crear más enemigos si el pool está vacío
                CreateEnemyPool(enemyType, 5);
            }

            if (enemyPools[enemyType].Count > 0)
            {
                return enemyPools[enemyType].Dequeue();
            }

            return null;
        }

        public void ReturnEnemyToPool(GameObject enemy)
        {
            if (enemy == null) return;

            // Obtener tipo del enemigo
            BasicEnemy basicEnemy = enemy.GetComponent<BasicEnemy>();
            if (basicEnemy == null) return;

            EnemyType enemyType = basicEnemy.GetEnemyType();

            // Desactivar enemigo
            enemy.SetActive(false);

            // Retornar al pool
            if (enemyPools.ContainsKey(enemyType))
            {
                enemyPools[enemyType].Enqueue(enemy);
            }

            // Remover de lista de activos
            activeEnemies.Remove(enemy);
            activeEnemyCount = activeEnemies.Count;

            // Si implementa IPoolable
            IPoolable poolable = enemy.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }
        }

        // MANEJADORES DE EVENTOS (CONEXIONES A4 y F1)

        void HandleTimerStarted()
        {
            // CONEXIÓN CON A4 - Iniciar oleadas cuando empiece el timer
            if (isSystemActive)
            {
                StartWaves();
            }
        }

        void HandleTimeUp()
        {
            // CONEXIÓN CON A4 - Detener sistema cuando se acabe el tiempo
            StopWaveSystem();
        }

        void HandleTimerPaused()
        {
            // CONEXIÓN CON A4 - Pausar oleadas cuando se pause el timer
            PauseWaves();
        }

        void HandleTimerResumed()
        {
            // CONEXIÓN CON A4 - Resumir oleadas cuando se reanude el timer
            ResumeWaves();
        }

        void HandleLevelLoaded(SOLevelData levelData)
        {
            // CONEXIÓN CON F1 - Reconfigurar oleadas cuando cambie el nivel
            LoadWavesFromLevel();
            PreloadWaves();
            ResetWaves();
        }

        void HandleSpawnConfigChanged(List<EnemyType> allowedTypes, float spawnRate)
        {
            // CONEXIÓN CON F1 - Actualizar configuración de spawn
            Debug.Log($"Configuración de spawn actualizada: {allowedTypes.Count} tipos, rate: {spawnRate}");
        }

        // MÉTODOS PÚBLICOS PARA INFORMACIÓN

        public bool IsSystemActive() => isSystemActive;
        public bool IsWaveRunning() => isWaveRunning;
        public int GetCurrentWaveIndex() => currentWaveIndex;
        public int GetTotalWaveCount() => levelWaves.Count;
        public int GetActiveEnemyCount() => activeEnemyCount;
        public SOWaveData GetCurrentWave() => currentWave;
        public float GetWaveProgress() => isWaveRunning ? (Time.time - waveStartTime) / currentWave.waveDuration : 0f;

        // MÉTODOS DE DEBUG
        [ContextMenu("Start Waves")]
        public void DebugStartWaves()
        {
            StartWaves();
        }

        [ContextMenu("Stop Waves")]
        public void DebugStopWaves()
        {
            StopWaveSystem();
        }

        [ContextMenu("Next Wave")]
        public void DebugNextWave()
        {
            if (isWaveRunning)
            {
                CompleteCurrentWave();
            }
        }

        [ContextMenu("Reset Waves")]
        public void DebugResetWaves()
        {
            ResetWaves();
        }

        [ContextMenu("Log Wave Info")]
        public void DebugLogWaveInfo()
        {
            if (currentWave != null)
            {
                Debug.Log($"Oleada actual: {currentWave.waveName} | Progreso: {GetWaveProgress():F2} | Enemigos activos: {activeEnemyCount}");
            }
            else
            {
                Debug.Log("No hay oleada activa");
            }
        }

        [ContextMenu("Validate Prefab Configuration")]
        public void DebugValidatePrefabs()
        {
            ValidateEnemyPrefabs();
        }

        [ContextMenu("Log Available Prefabs")]
        public void DebugLogPrefabs()
        {
            Debug.Log($"Prefabs configurados ({enemyPrefabs.Count}):");
            foreach (EnemyPrefabConfig config in enemyPrefabs)
            {
                Debug.Log($"  - {config} | Válido: {config.IsValid()}");
            }
        }

        [ContextMenu("Setup Default Prefabs")]
        public void DebugSetupDefaultPrefabs()
        {
            // Helper para configuración rápida en el editor
            if (enemyPrefabs == null)
            {
                enemyPrefabs = new List<EnemyPrefabConfig>();
            }

            // Crear configuraciones por defecto si están vacías
            if (enemyPrefabs.Count == 0)
            {
                EnemyType[] allTypes = { EnemyType.Normal, EnemyType.Fast, EnemyType.Jumper, EnemyType.Valuable, EnemyType.Innocent };
                foreach (EnemyType type in allTypes)
                {
                    EnemyPrefabConfig config = new EnemyPrefabConfig();
                    config.enemyType = type;
                    config.displayName = type.ToString() + " Enemy";
                    config.prefab = null; // Necesita ser asignado manualmente
                    enemyPrefabs.Add(config);
                }

                Debug.Log("Configuraciones por defecto creadas. Asigna los prefabs en el inspector.");
            }
        }

        public List<EnemyType> GetConfiguredEnemyTypes()
        {
            List<EnemyType> types = new List<EnemyType>();
            foreach (EnemyPrefabConfig config in enemyPrefabs)
            {
                if (config.IsValid())
                {
                    types.Add(config.enemyType);
                }
            }
            return types;
        }

        void OnDestroy()
        {
            // Desconectar eventos para evitar errores
            if (levelManager != null)
            {
                LevelManager.OnLevelLoaded -= HandleLevelLoaded;
                LevelManager.OnSpawnConfigChanged -= HandleSpawnConfigChanged;
            }

            if (levelTimer != null)
            {
                levelTimer.OnTimerStarted -= HandleTimerStarted;
                levelTimer.OnTimeUp -= HandleTimeUp;
                levelTimer.OnTimerPaused -= HandleTimerPaused;
                levelTimer.OnTimerResumed -= HandleTimerResumed;
            }
        }
    }
}