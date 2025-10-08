using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ARCHIVO: WaveSystem.cs
// Sistema principal de oleadas con DESTRUCCIÓN de enemigos al finalizar

namespace ShootingRange
{
    public class WaveSystem : MonoBehaviour
    {
        [Header("Referencias de Configuración")]
        [Tooltip("ARRASTRA AQUÍ tu LevelConfiguration desde la carpeta del proyecto")]
        public LevelConfiguration levelConfig;

        [Header("Referencias de Sistemas")]
        [Tooltip("Sistema de spawn points (opcional, se busca automáticamente)")]
        public SpawnPointsSystem spawnPointsSystem;

        [Header("Configuración de Limpieza")]
        [Tooltip("Destruir enemigos al finalizar cada oleada")]
        public bool destroyEnemiesOnWaveEnd = true;

        [Tooltip("Delay antes de destruir enemigos (segundos)")]
        [Range(0f, 2f)]
        public float destroyDelay = 0.5f;

        [Header("Estado del Sistema")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private bool isPaused = false;
        [SerializeField] private int currentWaveIndex = -1;
        [SerializeField] private WaveData currentWave = null;
        [SerializeField] private float currentWaveTimeRemaining = 0f;
        [SerializeField] private bool isInWarningPeriod = false;

        [Header("Estadísticas")]
        [SerializeField] private int totalEnemiesSpawned = 0;
        [SerializeField] private List<GameObject> activeEnemies = new List<GameObject>();

        // Eventos para integración con TimerIntegration
        public event System.Action<WaveData> OnWaveStarted;
        public event System.Action<WaveData> OnWaveCompleted;
        public event System.Action OnAllWavesCompleted;
        public event System.Action<int> OnEnemySpawned;
        public event System.Action OnGameStarted;
        public event System.Action OnFinalWave;

        // Variables privadas
        private Coroutine waveCoroutine;
        private List<GameObject> spawningQueue = new List<GameObject>();
        private Dictionary<EnemyType, GameObject> enemyPrefabCache = new Dictionary<EnemyType, GameObject>();

        // Propiedades públicas
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;
        public int CurrentWaveIndex => currentWaveIndex;
        public WaveData CurrentWave => currentWave;
        public int TotalWaves => levelConfig?.GetTotalWaveCount() ?? 0;
        public float CurrentWaveTimeRemaining => currentWaveTimeRemaining;
        public int GetTotalEnemiesSpawned() => totalEnemiesSpawned;
        public int GetActiveEnemyCount() => activeEnemies.Count;

        [System.Serializable]
        public struct EnemySpawnPointMapping
        {
            public EnemyType enemyType;
            public Transform[] spawnPoints;
        }

        [Header("Spawn Points Configuration")]
        [Tooltip("Puntos de spawn por tipo de enemigo - arrastra GameObjects de la escena")]
        public EnemySpawnPointMapping[] enemySpawnPoints;

        void Start()
        {
            InitializeWaveSystem();
        }

        void InitializeWaveSystem()
        {
            if (spawnPointsSystem == null)
                spawnPointsSystem = FindObjectOfType<SpawnPointsSystem>();

            CacheEnemyPrefabs();

            Debug.Log("WaveSystem inicializado");
        }

        void CacheEnemyPrefabs()
        {
            enemyPrefabCache.Clear();

            if (levelConfig?.enemyPrefabs != null)
            {
                foreach (var mapping in levelConfig.enemyPrefabs)
                {
                    if (mapping.prefab != null)
                    {
                        enemyPrefabCache[mapping.enemyType] = mapping.prefab;
                    }
                }
                Debug.Log($"Cacheados {enemyPrefabCache.Count} prefabs de enemigos");
            }
        }

        public void StartWaveSystem()
        {
            if (levelConfig == null)
            {
                Debug.LogError("No LevelConfiguration asignada al WaveSystem");
                return;
            }

            if (!levelConfig.IsValidConfiguration())
            {
                Debug.LogError($"LevelConfiguration '{levelConfig.levelName}' tiene configuración inválida");
                return;
            }

            if (isRunning)
            {
                Debug.LogWarning("WaveSystem ya está ejecutándose");
                return;
            }

            isRunning = true;
            isPaused = false;
            currentWaveIndex = -1;
            totalEnemiesSpawned = 0;
            activeEnemies.Clear();

            OnGameStarted?.Invoke();

            StartCoroutine(StartWaveSequence());

            Debug.Log($"WaveSystem iniciado: {levelConfig.levelName} con {TotalWaves} oleadas");
        }

        IEnumerator StartWaveSequence()
        {
            for (int i = 0; i < TotalWaves; i++)
            {
                currentWaveIndex = i;
                currentWave = levelConfig.GetWave(i);

                if (currentWave == null)
                {
                    Debug.LogError($"Wave {i} es null");
                    continue;
                }

                if (i == TotalWaves - 1)
                {
                    OnFinalWave?.Invoke();
                }

                yield return StartCoroutine(ExecuteWave(currentWave));

                // DESTRUIR ENEMIGOS AL FINALIZAR OLEADA
                if (destroyEnemiesOnWaveEnd)
                {
                    yield return StartCoroutine(CleanupWaveEnemies());
                }

                if (i < TotalWaves - 1)
                {
                    yield return StartCoroutine(WaveTransition());
                }

                if (!isRunning)
                    yield break;
            }

            CompleteAllWaves();
        }

        IEnumerator ExecuteWave(WaveData wave)
        {
            OnWaveStarted?.Invoke(wave);

            currentWaveTimeRemaining = wave.waveDuration;
            isInWarningPeriod = false;

            Debug.Log($"Ejecutando oleada: {wave.waveName} - Duración: {wave.waveDuration}s");

            if (wave.initialDelay > 0)
            {
                yield return new WaitForSeconds(wave.initialDelay);
            }

            StartCoroutine(SpawnEnemiesForWave(wave));

            float elapsedTime = 0f;

            while (elapsedTime < wave.waveDuration && isRunning)
            {
                if (!isPaused)
                {
                    elapsedTime += Time.deltaTime;
                    currentWaveTimeRemaining = wave.waveDuration - elapsedTime;

                    if (!isInWarningPeriod && currentWaveTimeRemaining <= wave.warningTime)
                    {
                        isInWarningPeriod = true;
                        StartWarningPeriod();
                    }
                }
                yield return null;
            }

            CompleteWave(wave);
        }

        // NUEVO: Limpieza de enemigos al finalizar oleada
        IEnumerator CleanupWaveEnemies()
        {
            if (activeEnemies.Count == 0)
            {
                yield break;
            }

            Debug.Log($"Limpiando {activeEnemies.Count} enemigos de la oleada...");

            // Delay opcional antes de destruir
            if (destroyDelay > 0)
            {
                yield return new WaitForSeconds(destroyDelay);
            }

            // Destruir todos los enemigos activos
            int destroyedCount = 0;
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    Destroy(activeEnemies[i]);
                    destroyedCount++;
                }
            }

            // Limpiar lista
            activeEnemies.Clear();

            Debug.Log($"✓ {destroyedCount} enemigos destruidos");
        }

        IEnumerator SpawnEnemiesForWave(WaveData wave)
        {
            List<EnemySpawnInfo> spawnList = new List<EnemySpawnInfo>();

            foreach (var spawnInfo in wave.enemiesToSpawn)
            {
                for (int i = 0; i < spawnInfo.quantity; i++)
                {
                    spawnList.Add(spawnInfo);
                }
            }

            if (wave.spawnInRandomOrder)
            {
                ShuffleList(spawnList);
            }

            foreach (var spawnInfo in spawnList)
            {
                if (!isRunning || isPaused)
                    yield return new WaitUntil(() => !isPaused || !isRunning);

                if (!isRunning) yield break;

                SpawnEnemyDirect(spawnInfo);

                yield return new WaitForSeconds(spawnInfo.spawnDelay);
            }
        }

        void SpawnEnemyDirect(EnemySpawnInfo spawnInfo)
        {
            if (!enemyPrefabCache.TryGetValue(spawnInfo.enemyType, out GameObject prefab))
            {
                Debug.LogError($"No se encontró prefab para enemigo tipo: {spawnInfo.enemyType}");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition(spawnInfo.enemyType);

            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

            enemy.transform.rotation = Quaternion.Euler(90, 0, 0);

            ConfigureSpawnedEnemy(enemy, spawnInfo);

            activeEnemies.Add(enemy);
            totalEnemiesSpawned++;

            StartCoroutine(DelayedRotation(enemy, spawnInfo.rotationDelay));

            OnEnemySpawned?.Invoke(totalEnemiesSpawned);

            Debug.Log($"Enemigo spawneado: {spawnInfo.enemyType} en {spawnPosition} (Total activos: {activeEnemies.Count})");
        }

        IEnumerator DelayedRotation(GameObject enemy, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (enemy != null)
            {
                yield return StartCoroutine(SmoothRotateToZero(enemy));
            }
        }

        IEnumerator SmoothRotateToZero(GameObject enemy)
        {
            if (enemy == null) yield break;

            Quaternion startRotation = enemy.transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, 0, 0);

            float duration = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < duration && enemy != null)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;

                enemy.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
                yield return null;
            }

            if (enemy != null)
            {
                enemy.transform.rotation = targetRotation;
            }
        }

        void ConfigureSpawnedEnemy(GameObject enemy, EnemySpawnInfo spawnInfo)
        {
            BasicEnemy basicEnemy = enemy.GetComponent<BasicEnemy>();
            if (basicEnemy != null)
            {
                basicEnemy.ConfigureEnemy(spawnInfo.enemyType, "default");
            }

            EnemyMovementPatterns movement = enemy.GetComponent<EnemyMovementPatterns>();
            if (movement != null)
            {
                movement.SetPattern(MovementPattern.Linear);
                movement.changePatternsInFlight = false;
            }
        }

        Vector3 GetSpawnPosition(EnemyType enemyType)
        {
            if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
            {
                foreach (var mapping in enemySpawnPoints)
                {
                    if (mapping.enemyType == enemyType &&
                        mapping.spawnPoints != null &&
                        mapping.spawnPoints.Length > 0)
                    {
                        int randomIndex = Random.Range(0, mapping.spawnPoints.Length);
                        return mapping.spawnPoints[randomIndex].position;
                    }
                }
            }

            if (spawnPointsSystem != null)
            {
                SpawnData spawnData = spawnPointsSystem.GetRandomSpawnData();
                if (spawnData != null)
                {
                    return spawnData.spawnPosition;
                }
            }

            return GetRandomScreenEdgePosition();
        }

        Vector3 GetRandomScreenEdgePosition()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;

            float height = cam.orthographicSize;
            float width = height * cam.aspect;
            Vector3 camPos = cam.transform.position;

            int edge = Random.Range(0, 4);
            Vector3 position = camPos;

            switch (edge)
            {
                case 0:
                    position.x = camPos.x - width - 1f;
                    position.y = Random.Range(camPos.y - height, camPos.y + height);
                    break;
                case 1:
                    position.x = camPos.x + width + 1f;
                    position.y = Random.Range(camPos.y - height, camPos.y + height);
                    break;
                case 2:
                    position.x = Random.Range(camPos.x - width, camPos.x + width);
                    position.y = camPos.y - height - 1f;
                    break;
                case 3:
                    position.x = Random.Range(camPos.x - width, camPos.x + width);
                    position.y = camPos.y + height + 1f;
                    break;
            }

            return position;
        }

        void StartWarningPeriod()
        {
            Debug.Log($"Warning period iniciado - {currentWaveTimeRemaining:F1}s restantes");
            StartCoroutine(RotateAllEnemiesToWarning());
        }

        IEnumerator RotateAllEnemiesToWarning()
        {
            List<GameObject> enemiesToRotate = new List<GameObject>(activeEnemies);

            foreach (GameObject enemy in enemiesToRotate)
            {
                if (enemy != null)
                {
                    StartCoroutine(SmoothRotateToNinety(enemy));
                }
            }

            yield return null;
        }

        IEnumerator SmoothRotateToNinety(GameObject enemy)
        {
            if (enemy == null) yield break;

            BasicEnemy basicEnemy = enemy.GetComponent<BasicEnemy>();

            // Si es Jumper, usar su método específico
            if (basicEnemy != null && basicEnemy.GetEnemyType() == EnemyType.Jumper)
            {
                EnemyMovementPatterns movement = enemy.GetComponent<EnemyMovementPatterns>();
                if (movement != null)
                {
                    movement.RotateToWarning();
                }
                yield break;
            }

            // Para los demás enemigos, rotación normal en X
            Quaternion startRotation = enemy.transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(90, 0, 0);

            float duration = 0.1f;
            float elapsedTime = 0f;

            while (elapsedTime < duration && enemy != null)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;

                enemy.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
                yield return null;
            }

            if (enemy != null)
            {
                enemy.transform.rotation = targetRotation;
            }
        }

        void CompleteWave(WaveData wave)
        {
            Debug.Log($"Oleada completada: {wave.waveName}");
            OnWaveCompleted?.Invoke(wave);
        }

        IEnumerator WaveTransition()
        {
            Debug.Log($"Transición entre oleadas - Delay: {levelConfig.waveTransitionDelay}s");

            if (levelConfig.showWaveTransitionMessages)
            {
                Debug.Log($"Siguiente oleada en {levelConfig.waveTransitionDelay} segundos...");
            }

            yield return new WaitForSeconds(levelConfig.waveTransitionDelay);
        }

        void CompleteAllWaves()
        {
            Debug.Log("¡Todas las oleadas completadas!");
            isRunning = false;
            OnAllWavesCompleted?.Invoke();
        }

        public void PauseWaveSystem()
        {
            if (isRunning)
            {
                isPaused = true;
                Debug.Log("WaveSystem pausado");
            }
        }

        public void ResumeWaveSystem()
        {
            if (isRunning && isPaused)
            {
                isPaused = false;
                Debug.Log("WaveSystem resumido");
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

            StopAllCoroutines();

            Debug.Log("WaveSystem detenido");
        }

        public void ResetWaveSystem()
        {
            StopWaveSystem();
            CleanupActiveEnemies();

            currentWaveIndex = -1;
            currentWave = null;
            currentWaveTimeRemaining = 0f;
            isInWarningPeriod = false;
            totalEnemiesSpawned = 0;

            Debug.Log("WaveSystem reseteado");
        }

        void CleanupActiveEnemies()
        {
            foreach (GameObject enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            activeEnemies.Clear();
        }

        public bool CanSpawnMore()
        {
            return isRunning && !isPaused && currentWave != null;
        }

        public void ForceSpawnEnemy(EnemyType enemyType)
        {
            if (!CanSpawnMore()) return;

            EnemySpawnInfo forceSpawn = new EnemySpawnInfo
            {
                enemyType = enemyType,
                quantity = 1,
                spawnDelay = 0.1f,
                rotationDelay = 1f
            };

            SpawnEnemyDirect(forceSpawn);
        }

        public void SkipCurrentWave()
        {
            if (currentWave != null && isRunning)
            {
                currentWaveTimeRemaining = 0f;
                Debug.Log($"Oleada saltada: {currentWave.waveName}");
            }
        }

        public void RemoveEnemyFromActiveList(GameObject enemy)
        {
            activeEnemies.Remove(enemy);
        }

        void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        void Update()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                }
            }
        }

        public void DebugLogWaveInfo()
        {
            Debug.Log($"=== WAVE SYSTEM INFO ===");
            Debug.Log($"Running: {isRunning} | Paused: {isPaused}");
            Debug.Log($"Current Wave: {currentWaveIndex + 1}/{TotalWaves}");
            Debug.Log($"Wave Name: {(currentWave != null ? currentWave.waveName : "None")}");
            Debug.Log($"Time Remaining: {currentWaveTimeRemaining:F1}s");
            Debug.Log($"Warning Period: {isInWarningPeriod}");
            Debug.Log($"Total Spawned: {totalEnemiesSpawned}");
            Debug.Log($"Active Enemies: {activeEnemies.Count}");
        }

        [ContextMenu("Start Wave System")]
        public void DebugStartWaveSystem()
        {
            StartWaveSystem();
        }

        [ContextMenu("Stop Wave System")]
        public void DebugStopWaveSystem()
        {
            StopWaveSystem();
        }

        [ContextMenu("Skip Current Wave")]
        public void DebugSkipWave()
        {
            SkipCurrentWave();
        }

        [ContextMenu("Force Spawn Normal Enemy")]
        public void DebugForceSpawnNormal()
        {
            ForceSpawnEnemy(EnemyType.Normal);
        }

        [ContextMenu("Log Wave Info")]
        public void DebugLogInfo()
        {
            DebugLogWaveInfo();
        }

        void OnDestroy()
        {
            StopWaveSystem();
            CleanupActiveEnemies();
        }
    }
}