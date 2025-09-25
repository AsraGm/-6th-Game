using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ARCHIVO: WaveSystem.cs
// Sistema de oleadas fijas pre-diseñadas (INSTRUCCIÓN B3)

namespace ShootingRange
{
    public class WaveSystem : MonoBehaviour
    {
        [Header("Referencias de Prefabs")]
        [Tooltip("Array de prefabs de enemigos - ORDEN DEBE COINCIDIR CON ENUM EnemyType")]
        public GameObject[] enemyPrefabs = new GameObject[5]; // Normal, Fast, Jumper, Valuable, Innocent

        [Header("Configuración de Waves")]
        [Tooltip("Lista de todas las waves del nivel actual")]
        public List<WaveData> levelWaves = new List<WaveData>();

        [Header("Referencias de Sistemas")]
        [Tooltip("Se conecta automáticamente si está vacío")]
        public SpawnPointsSystem spawnSystem;

        [Tooltip("Se conecta automáticamente si está vacío")]
        public LevelManager levelManager;

        [Header("Configuración de Pool")]
        [Tooltip("Tamaño del pool por tipo de enemigo")]
        [Range(5, 50)]
        public int poolSizePerType = 20;

        [Header("Configuración del Efecto Cartón")]
        [Tooltip("Duración de la animación de aparición (90° → 0°)")]
        [Range(0.1f, 2f)]
        public float cardboardEffectDuration = 0.5f;

        [Header("Debug")]
        [Tooltip("Mostrar información detallada en consola")]
        public bool enableDetailedLogs = true;

        // Estado del sistema
        private bool isRunning = false;
        private bool isPaused = false;
        private int currentWaveIndex = 0;
        private float currentWaveTime = 0f;
        private WaveData activeWave;

        // Pool de enemigos
        private Dictionary<EnemyType, Queue<GameObject>> enemyPools = new Dictionary<EnemyType, Queue<GameObject>>();
        private List<GameObject> activeEnemies = new List<GameObject>();

        // Control de spawning
        private Coroutine waveCoroutine;

        // Eventos para integración con otros sistemas
        public event System.Action<WaveData> OnWaveStarted;
        public event System.Action<WaveData> OnWaveCompleted;
        public event System.Action OnAllWavesCompleted;
        public event System.Action<int> OnEnemySpawned;
        public event System.Action OnGameStarted; // "INICIO DE JUEGO"
        public event System.Action OnFinalWave;   // "CERCA DE TERMINAR"

        // Propiedades públicas
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;
        public int CurrentWaveIndex => currentWaveIndex;
        public int TotalWaves => levelWaves.Count;
        public WaveData CurrentWave => activeWave;
        public int GetActiveEnemyCount() => activeEnemies.Count;
        public int GetTotalEnemiesSpawned() => totalEnemiesSpawned;

        // Estadísticas
        private int totalEnemiesSpawned = 0;

        void Start()
        {
            InitializeWaveSystem();
        }

        void InitializeWaveSystem()
        {
            // Buscar sistemas automáticamente
            FindRequiredSystems();

            // Configurar waves desde LevelManager si existe
            LoadWavesFromLevelManager();

            // Inicializar pools
            InitializeEnemyPools();

            // Conectar eventos del LevelManager
            ConnectToLevelManager();

            if (enableDetailedLogs)
            {
                Debug.Log($"WaveSystem inicializado - {TotalWaves} waves configuradas");
            }
        }

        void FindRequiredSystems()
        {
            if (spawnSystem == null)
                spawnSystem = FindObjectOfType<SpawnPointsSystem>();

            if (levelManager == null)
                levelManager = FindObjectOfType<LevelManager>();
        }

        void LoadWavesFromLevelManager()
        {
            // INTEGRACIÓN CON TU SOLevelData EXISTENTE
            if (levelManager != null && levelManager.CurrentLevel != null)
            {
                SOLevelData levelData = levelManager.CurrentLevel;

                // Si no hay waves configuradas manualmente, crear una wave automática
                if (levelWaves.Count == 0)
                {
                    CreateDefaultWaveFromLevelData(levelData);
                }
            }
        }

        void CreateDefaultWaveFromLevelData(SOLevelData levelData)
        {
            // Crear WaveData automática basada en tu SOLevelData existente
            WaveData autoWave = ScriptableObject.CreateInstance<WaveData>();
            autoWave.waveName = $"Auto Wave - {levelData.levelName}";
            autoWave.waveDuration = levelData.levelDuration;
            autoWave.isFinalWave = true;

            // Crear configuraciones para cada tipo permitido
            foreach (EnemyType enemyType in levelData.allowedSpawnTypes)
            {
                WaveSpawnConfig config = new WaveSpawnConfig();
                config.enemyType = enemyType;
                config.totalCount = GetDefaultCountForEnemyType(enemyType);
                config.spawnInterval = 1f / levelData.baseSpawnRate; // Convertir rate a interval
                config.startDelay = 0f;

                autoWave.spawnConfigs.Add(config);
            }

            levelWaves.Add(autoWave);

            if (enableDetailedLogs)
            {
                Debug.Log($"Wave automática creada desde {levelData.levelName}");
            }
        }

        int GetDefaultCountForEnemyType(EnemyType enemyType)
        {
            // Cantidad por defecto según tipo de enemigo
            switch (enemyType)
            {
                case EnemyType.Normal: return 10;
                case EnemyType.Fast: return 8;
                case EnemyType.Jumper: return 6;
                case EnemyType.Valuable: return 3;
                case EnemyType.Innocent: return 5;
                default: return 5;
            }
        }

        void InitializeEnemyPools()
        {
            // Inicializar pools para cada tipo de enemigo
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    EnemyType enemyType = (EnemyType)i;
                    enemyPools[enemyType] = new Queue<GameObject>();

                    // Pre-instanciar enemigos en el pool
                    for (int j = 0; j < poolSizePerType; j++)
                    {
                        GameObject enemy = Instantiate(enemyPrefabs[i]);
                        enemy.SetActive(false);
                        enemy.transform.SetParent(transform);
                        enemyPools[enemyType].Enqueue(enemy);
                    }

                    if (enableDetailedLogs)
                    {
                        Debug.Log($"Pool creado para {enemyType}: {poolSizePerType} instancias");
                    }
                }
            }
        }

        void ConnectToLevelManager()
        {
            if (levelManager != null)
            {
                LevelManager.OnLevelLoaded += HandleLevelLoaded;
            }
        }

        void HandleLevelLoaded(SOLevelData levelData)
        {
            // Recargar waves cuando cambie el nivel
            levelWaves.Clear();
            LoadWavesFromLevelManager();
            ResetWaveSystem();
        }

        // MÉTODOS PRINCIPALES DE CONTROL

        public void StartWaveSystem()
        {
            if (isRunning || levelWaves.Count == 0)
            {
                if (enableDetailedLogs && levelWaves.Count == 0)
                    Debug.LogWarning("No hay waves configuradas para iniciar");
                return;
            }

            isRunning = true;
            isPaused = false;
            currentWaveIndex = 0;
            totalEnemiesSpawned = 0;

            // Disparar evento de inicio de juego
            OnGameStarted?.Invoke();

            StartWave(0);

            if (enableDetailedLogs)
            {
                Debug.Log("WaveSystem iniciado");
            }
        }

        public void PauseWaveSystem()
        {
            if (isRunning && !isPaused)
            {
                isPaused = true;

                if (waveCoroutine != null)
                {
                    StopCoroutine(waveCoroutine);
                    waveCoroutine = null;
                }

                // Pausar movimiento de enemigos activos
                foreach (GameObject enemy in activeEnemies)
                {
                    if (enemy != null)
                    {
                        DynamicEnemySystem dynamicSystem = enemy.GetComponent<DynamicEnemySystem>();
                        if (dynamicSystem != null)
                        {
                            dynamicSystem.PauseMovement();
                        }
                    }
                }

                if (enableDetailedLogs)
                {
                    Debug.Log("WaveSystem pausado");
                }
            }
        }

        public void ResumeWaveSystem()
        {
            if (isRunning && isPaused)
            {
                isPaused = false;

                // Resumir corrutina de wave actual
                if (activeWave != null)
                {
                    waveCoroutine = StartCoroutine(WaveCoroutine(activeWave));
                }

                // Resumir movimiento de enemigos activos
                foreach (GameObject enemy in activeEnemies)
                {
                    if (enemy != null)
                    {
                        DynamicEnemySystem dynamicSystem = enemy.GetComponent<DynamicEnemySystem>();
                        if (dynamicSystem != null)
                        {
                            dynamicSystem.ResumeMovement();
                        }
                    }
                }

                if (enableDetailedLogs)
                {
                    Debug.Log("WaveSystem resumido");
                }
            }
        }

        public void StopWaveSystem()
        {
            isRunning = false;
            isPaused = false;

            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
                waveCoroutine = null;
            }

            // Devolver todos los enemigos activos al pool
            ReturnAllEnemiesToPool();

            if (enableDetailedLogs)
            {
                Debug.Log("WaveSystem detenido");
            }
        }

        public void ResetWaveSystem()
        {
            StopWaveSystem();

            currentWaveIndex = 0;
            currentWaveTime = 0f;
            activeWave = null;
            totalEnemiesSpawned = 0;

            // Resetear configuración de todas las waves
            foreach (WaveData wave in levelWaves)
            {
                wave.ResetWave();
            }

            if (enableDetailedLogs)
            {
                Debug.Log("WaveSystem reseteado");
            }
        }

        void StartWave(int waveIndex)
        {
            if (waveIndex >= levelWaves.Count)
            {
                CompleteAllWaves();
                return;
            }

            currentWaveIndex = waveIndex;
            activeWave = levelWaves[waveIndex];
            currentWaveTime = 0f;

            // Resetear configuración de la wave
            activeWave.ResetWave();

            // Disparar evento de wave final si corresponde
            if (activeWave.isFinalWave)
            {
                OnFinalWave?.Invoke();
            }

            // Iniciar corrutina de la wave
            waveCoroutine = StartCoroutine(WaveCoroutine(activeWave));

            // Notificar inicio de wave
            OnWaveStarted?.Invoke(activeWave);

            if (enableDetailedLogs)
            {
                Debug.Log($"Wave iniciada: {activeWave.waveName} ({currentWaveIndex + 1}/{TotalWaves})");
            }
        }

        IEnumerator WaveCoroutine(WaveData wave)
        {
            float waveStartTime = Time.time;

            while (currentWaveTime < wave.waveDuration && isRunning)
            {
                // No procesar si está pausado
                if (isPaused)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                currentWaveTime = Time.time - waveStartTime;

                // Procesar spawning de cada configuración
                foreach (WaveSpawnConfig config in wave.spawnConfigs)
                {
                    if (config.ShouldSpawn(currentWaveTime))
                    {
                        SpawnEnemy(config.enemyType);
                        config.spawnedCount++;
                        config.lastSpawnTime = currentWaveTime;

                        totalEnemiesSpawned++;
                        OnEnemySpawned?.Invoke(totalEnemiesSpawned);
                    }
                }

                // Verificar si la wave está completa
                if (wave.IsWaveComplete())
                {
                    CompleteCurrentWave();
                    yield break;
                }

                yield return new WaitForSeconds(0.1f); // Verificar cada 0.1 segundos
            }

            // Wave completada por tiempo
            CompleteCurrentWave();
        }

        void SpawnEnemy(EnemyType enemyType)
        {
            GameObject enemy = GetEnemyFromPool(enemyType);
            if (enemy == null)
            {
                Debug.LogWarning($"No se pudo obtener enemigo del pool: {enemyType}");
                return;
            }

            // Obtener datos de spawn
            SpawnData spawnData = GetSpawnDataForEnemy(enemyType);
            if (spawnData == null)
            {
                Debug.LogWarning("No se pudieron obtener datos de spawn");
                ReturnEnemyToPool(enemy, enemyType);
                return;
            }

            // Configurar enemigo
            ConfigureSpawnedEnemy(enemy, spawnData, enemyType);

            // EFECTO CARTÓN: Iniciar rotado 90° en Z
            StartCardboardEffect(enemy);

            // Agregar a lista de activos
            activeEnemies.Add(enemy);

            if (enableDetailedLogs)
            {
                Debug.Log($"Enemigo spawneado: {enemyType} en {spawnData.spawnPosition}");
            }
        }

        GameObject GetEnemyFromPool(EnemyType enemyType)
        {
            if (!enemyPools.ContainsKey(enemyType) || enemyPools[enemyType].Count == 0)
            {
                // Crear nuevo enemigo si el pool está vacío
                int prefabIndex = (int)enemyType;
                if (prefabIndex < enemyPrefabs.Length && enemyPrefabs[prefabIndex] != null)
                {
                    GameObject newEnemy = Instantiate(enemyPrefabs[prefabIndex]);
                    newEnemy.transform.SetParent(transform);
                    return newEnemy;
                }
                return null;
            }

            GameObject enemy = enemyPools[enemyType].Dequeue();
            enemy.SetActive(true);

            // Activar componente IPoolable si existe
            IPoolable poolable = enemy.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawnFromPool();
            }

            return enemy;
        }

        SpawnData GetSpawnDataForEnemy(EnemyType enemyType)
        {
            if (spawnSystem != null)
            {
                return spawnSystem.GetSpawnDataForEnemyType(enemyType);
            }

            // Fallback: crear spawn data básico
            SpawnData fallbackData = new SpawnData();
            fallbackData.spawnPosition = GetRandomEdgePosition();
            fallbackData.initialDirection = Vector2.up;
            fallbackData.movementPattern = MovementPattern.Linear;
            fallbackData.baseSpeed = 3f;
            fallbackData.allowedEnemyTypes = new List<EnemyType> { enemyType };
            fallbackData.routeName = "Fallback Route";

            return fallbackData;
        }

        Vector3 GetRandomEdgePosition()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;

            float height = cam.orthographicSize;
            float width = height * cam.aspect;
            Vector3 camPos = cam.transform.position;

            // Posición aleatoria en uno de los 4 bordes
            int edge = Random.Range(0, 4);
            switch (edge)
            {
                case 0: // Izquierda
                    return new Vector3(camPos.x - width - 1f, Random.Range(camPos.y - height, camPos.y + height), 0);
                case 1: // Derecha
                    return new Vector3(camPos.x + width + 1f, Random.Range(camPos.y - height, camPos.y + height), 0);
                case 2: // Abajo
                    return new Vector3(Random.Range(camPos.x - width, camPos.x + width), camPos.y - height - 1f, 0);
                case 3: // Arriba
                    return new Vector3(Random.Range(camPos.x - width, camPos.x + width), camPos.y + height + 1f, 0);
                default:
                    return camPos;
            }
        }

        void ConfigureSpawnedEnemy(GameObject enemy, SpawnData spawnData, EnemyType enemyType)
        {
            // Posicionar enemigo
            enemy.transform.position = spawnData.spawnPosition;

            // Configurar DynamicEnemySystem si existe
            DynamicEnemySystem dynamicSystem = enemy.GetComponent<DynamicEnemySystem>();
            if (dynamicSystem != null)
            {
                dynamicSystem.InitializeDynamicEnemy(spawnData, enemyType);
            }
            else
            {
                // Configurar BasicEnemy directamente
                BasicEnemy basicEnemy = enemy.GetComponent<BasicEnemy>();
                if (basicEnemy != null)
                {
                    basicEnemy.ConfigureEnemy(enemyType, "default");
                }

                // Configurar movimiento si existe
                EnemyMovementPatterns movement = enemy.GetComponent<EnemyMovementPatterns>();
                if (movement != null)
                {
                    movement.initialDirection = spawnData.initialDirection;
                    movement.baseSpeed = spawnData.baseSpeed;
                    movement.currentPattern = spawnData.movementPattern;
                }
            }
        }

        void StartCardboardEffect(GameObject enemy)
        {
            // EFECTO CARTÓN: Iniciar en 90° y animar a 0°
            enemy.transform.rotation = Quaternion.Euler(0, 0, 90f);
            StartCoroutine(CardboardAppearanceCoroutine(enemy));
        }

        IEnumerator CardboardAppearanceCoroutine(GameObject enemy)
        {
            if (enemy == null) yield break;

            float elapsed = 0f;
            Quaternion startRotation = Quaternion.Euler(0, 0, 90f);
            Quaternion endRotation = Quaternion.Euler(0, 0, 0f);

            while (elapsed < cardboardEffectDuration)
            {
                if (enemy == null) yield break;

                elapsed += Time.deltaTime;
                float progress = elapsed / cardboardEffectDuration;

                enemy.transform.rotation = Quaternion.Lerp(startRotation, endRotation, progress);

                yield return null;
            }

            if (enemy != null)
            {
                enemy.transform.rotation = endRotation;
            }
        }

        void CompleteCurrentWave()
        {
            if (activeWave == null) return;

            OnWaveCompleted?.Invoke(activeWave);

            if (enableDetailedLogs)
            {
                Debug.Log($"Wave completada: {activeWave.waveName}");
            }

            // MECHANIC ESPECIAL: Si hay enemigos activos cuando llega la nueva wave, rotarlos a 90°
            if (currentWaveIndex + 1 < TotalWaves && activeEnemies.Count > 0)
            {
                RotateActiveEnemiesTo90();
            }

            // Iniciar siguiente wave
            StartWave(currentWaveIndex + 1);
        }

        void RotateActiveEnemiesTo90()
        {
            foreach (GameObject enemy in activeEnemies)
            {
                if (enemy != null && enemy.activeInHierarchy)
                {
                    StartCoroutine(RotateEnemyTo90Coroutine(enemy));
                }
            }

            if (enableDetailedLogs)
            {
                Debug.Log($"Rotando {activeEnemies.Count} enemigos activos a 90° para nueva wave");
            }
        }

        IEnumerator RotateEnemyTo90Coroutine(GameObject enemy)
        {
            if (enemy == null) yield break;

            float elapsed = 0f;
            Quaternion startRotation = enemy.transform.rotation;
            Quaternion endRotation = Quaternion.Euler(0, 0, 90f);

            while (elapsed < 0.3f) // Más rápido que la aparición
            {
                if (enemy == null) yield break;

                elapsed += Time.deltaTime;
                float progress = elapsed / 0.3f;

                enemy.transform.rotation = Quaternion.Lerp(startRotation, endRotation, progress);

                yield return null;
            }

            if (enemy != null)
            {
                enemy.transform.rotation = endRotation;

                // Pausar movimiento del enemigo
                EnemyMovementPatterns movement = enemy.GetComponent<EnemyMovementPatterns>();
                if (movement != null)
                {
                    movement.PauseMovement();
                }
            }
        }

        void CompleteAllWaves()
        {
            isRunning = false;
            OnAllWavesCompleted?.Invoke();

            if (enableDetailedLogs)
            {
                Debug.Log($"Todas las waves completadas. Total enemigos spawneados: {totalEnemiesSpawned}");
            }
        }

        public void ReturnEnemyToPool(GameObject enemy, EnemyType enemyType)
        {
            if (enemy == null) return;

            // Remover de activos
            activeEnemies.Remove(enemy);

            // Pausar componentes
            IPoolable poolable = enemy.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }
            else
            {
                enemy.SetActive(false);
            }

            // Resetear rotación
            enemy.transform.rotation = Quaternion.identity;

            // Devolver al pool
            if (enemyPools.ContainsKey(enemyType))
            {
                enemyPools[enemyType].Enqueue(enemy);
            }
        }

        void ReturnAllEnemiesToPool()
        {
            List<GameObject> enemiesToReturn = new List<GameObject>(activeEnemies);

            foreach (GameObject enemy in enemiesToReturn)
            {
                if (enemy != null)
                {
                    BasicEnemy basicEnemy = enemy.GetComponent<BasicEnemy>();
                    EnemyType enemyType = basicEnemy != null ? basicEnemy.GetEnemyType() : EnemyType.Normal;
                    ReturnEnemyToPool(enemy, enemyType);
                }
            }

            activeEnemies.Clear();
        }

        // MÉTODOS PÚBLICOS PARA OTROS SISTEMAS

        public bool CanSpawnMore()
        {
            if (activeWave == null) return false;

            foreach (WaveSpawnConfig config in activeWave.spawnConfigs)
            {
                if (!config.IsComplete) return true;
            }
            return false;
        }

        public void SkipCurrentWave()
        {
            if (activeWave != null)
            {
                CompleteCurrentWave();
            }
        }

        public void ForceSpawnEnemy(EnemyType enemyType)
        {
            if (isRunning && !isPaused)
            {
                SpawnEnemy(enemyType);
            }
        }

        // MÉTODOS DE DEBUG
        [ContextMenu("Start Wave System")]
        public void DebugStartWaves()
        {
            StartWaveSystem();
        }

        [ContextMenu("Skip Current Wave")]
        public void DebugSkipWave()
        {
            SkipCurrentWave();
        }

        [ContextMenu("Spawn Normal Enemy")]
        public void DebugSpawnNormal()
        {
            ForceSpawnEnemy(EnemyType.Normal);
        }

        [ContextMenu("Reset Wave System")]
        public void DebugResetSystem()
        {
            ResetWaveSystem();
        }

        [ContextMenu("Log Wave Info")]
        public void DebugLogWaveInfo()
        {
            if (activeWave != null)
            {
                Debug.Log($"Wave activa: {activeWave.waveName} | Tiempo: {currentWaveTime:F1}s / {activeWave.waveDuration}s");
                Debug.Log($"Enemigos activos: {activeEnemies.Count} | Total spawneados: {totalEnemiesSpawned}");
            }
        }

        void OnDestroy()
        {
            // Limpiar eventos
            if (levelManager != null)
            {
                LevelManager.OnLevelLoaded -= HandleLevelLoaded;
            }

            // Detener corrutinas
            StopAllCoroutines();
        }
    }
}