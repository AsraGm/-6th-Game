using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// B3: Sistema de oleadas fijas pre-diseñadas
[CreateAssetMenu(fileName = "WaveConfiguration", menuName = "ShootingRange/Wave Configuration")]
public class WaveConfiguration : ScriptableObject
{
    [System.Serializable]
    public class SpawnEvent
    {
        [Header("Timing")]
        public float spawnTime; // Tiempo en segundos desde el inicio de la oleada
        
        [Header("Enemy Configuration")]
        public EnemyType enemyType;
        public Vector3 spawnPosition;
        public bool spawnAsInnocent = false;
        
        [Header("Behavior Override")]
        public bool overrideBehavior = false;
        public EnemyBehavior.MovementPattern movementPattern;
        public float customSpeed = 0f;
    }
    
    [Header("Wave Settings")]
    public string waveName = "Wave 1";
    public float waveDuration = 30f;
    
    [Header("Spawn Events")]
    public SpawnEvent[] spawnEvents;
    
    [Header("Wave Statistics")]
    [SerializeField] private int totalEnemies;
    [SerializeField] private int totalInnocents;
    [SerializeField] private float spawnRate;
    
    private void OnValidate()
    {
        // Auto-calcular estadísticas
        totalEnemies = 0;
        totalInnocents = 0;
        
        foreach (var spawnEvent in spawnEvents)
        {
            if (spawnEvent.spawnAsInnocent)
                totalInnocents++;
            else
                totalEnemies++;
        }
        
        spawnRate = spawnEvents.Length > 0 ? spawnEvents.Length / waveDuration : 0f;
    }
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    
    [Header("Wave Configuration")]
    [SerializeField] private WaveConfiguration[] waveConfigurations;
    [SerializeField] private bool preloadWaves = true;
    
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnAreaWidth = 8f;
    [SerializeField] private float spawnHeight = 6f;
    
    [Header("Current Wave Info")]
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private bool isWaveActive = false;
    
    // Events
    public System.Action<int> OnWaveStarted;
    public System.Action<int> OnWaveCompleted;
    public System.Action OnAllWavesCompleted;
    
    private WaveConfiguration currentWave;
    private Coroutine currentWaveCoroutine;
    private List<WaveConfiguration.SpawnEvent> currentWaveEvents;
    private int spawnedEnemies = 0;
    
    public WaveConfiguration CurrentWave => currentWave;
    public int CurrentWaveIndex => currentWaveIndex;
    public bool IsWaveActive => isWaveActive;
    public int SpawnedEnemies => spawnedEnemies;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            if (preloadWaves)
                PreloadWaveConfigurations();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void PreloadWaveConfigurations()
    {
        // Pre-cargar configuraciones para transiciones suaves
        foreach (var wave in waveConfigurations)
        {
            if (wave != null)
            {
                // Validar configuración de oleada
                ValidateWaveConfiguration(wave);
            }
        }
    }
    
    private void ValidateWaveConfiguration(WaveConfiguration wave)
    {
        if (wave.spawnEvents == null || wave.spawnEvents.Length == 0)
        {
            Debug.LogWarning($"Wave {wave.waveName} has no spawn events!");
            return;
        }
        
        // Ordenar eventos por tiempo de spawn
        System.Array.Sort(wave.spawnEvents, (a, b) => a.spawnTime.CompareTo(b.spawnTime));
    }
    
    public void StartWave(int waveIndex = -1)
    {
        if (isWaveActive)
        {
            Debug.LogWarning("Wave is already active!");
            return;
        }
        
        // Usar índice específico o continuar con el actual
        if (waveIndex >= 0 && waveIndex < waveConfigurations.Length)
        {
            currentWaveIndex = waveIndex;
        }
        
        if (currentWaveIndex >= waveConfigurations.Length)
        {
            Debug.Log("All waves completed!");
            OnAllWavesCompleted?.Invoke();
            return;
        }
        
        currentWave = waveConfigurations[currentWaveIndex];
        if (currentWave == null)
        {
            Debug.LogError($"Wave configuration at index {currentWaveIndex} is null!");
            return;
        }
        
        // Inicializar oleada
        isWaveActive = true;
        spawnedEnemies = 0;
        currentWaveEvents = new List<WaveConfiguration.SpawnEvent>(currentWave.spawnEvents);
        
        // Iniciar corrutina de spawn
        currentWaveCoroutine = StartCoroutine(ExecuteWaveCoroutine());
        
        OnWaveStarted?.Invoke(currentWaveIndex);
        Debug.Log($"Started {currentWave.waveName}");
    }
    
    private IEnumerator ExecuteWaveCoroutine()
    {
        float waveTimer = 0f;
        int eventIndex = 0;
        
        while (waveTimer < currentWave.waveDuration && eventIndex < currentWaveEvents.Count)
        {
            // Verificar si es tiempo de spawn del siguiente evento
            if (eventIndex < currentWaveEvents.Count && 
                waveTimer >= currentWaveEvents[eventIndex].spawnTime)
            {
                SpawnEventEnemy(currentWaveEvents[eventIndex]);
                eventIndex++;
            }
            
            waveTimer += Time.deltaTime;
            yield return null;
        }
        
        // Esperar a que termine la duración completa de la oleada
        while (waveTimer < currentWave.waveDuration)
        {
            waveTimer += Time.deltaTime;
            yield return null;
        }
        
        CompleteWave();
    }
    
    private void SpawnEventEnemy(WaveConfiguration.SpawnEvent spawnEvent)
    {
        Vector3 spawnPosition = GetSpawnPosition(spawnEvent.spawnPosition);
        
        // Spawn enemy usando el pool
        Enemy spawnedEnemy = EnemyPoolManager.Instance?.SpawnEnemy(
            spawnEvent.enemyType, 
            spawnPosition, 
            spawnEvent.spawnAsInnocent
        );
        
        if (spawnedEnemy != null)
        {
            spawnedEnemies++;
            
            // Aplicar comportamiento personalizado si está especificado
            if (spawnEvent.overrideBehavior)
            {
                EnemyBehavior behavior = spawnedEnemy.GetComponent<EnemyBehavior>();
                if (behavior != null)
                {
                    behavior.SetMovementPattern(spawnEvent.movementPattern);
                    
                    if (spawnEvent.customSpeed > 0)
                    {
                        behavior.SetSpeed(spawnEvent.customSpeed);
                    }
                }
            }
            
            // B4: Aplicar configuración de inocente
            if (spawnEvent.spawnAsInnocent)
            {
                ApplyInnocentVisualFeedback(spawnedEnemy);
            }
        }
    }
    
    private Vector3 GetSpawnPosition(Vector3 configuredPosition)
    {
        // Si la posición está configurada como (0,0,0), usar spawn points automáticos
        if (configuredPosition == Vector3.zero)
        {
            return GetRandomSpawnPoint();
        }
        
        return configuredPosition;
    }
    
    private Vector3 GetRandomSpawnPoint()
    {
        // Si hay spawn points definidos, usar uno aleatorio
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return randomSpawnPoint.position;
        }
        
        // Generar posición aleatoria en el área de spawn
        float randomX = Random.Range(-spawnAreaWidth / 2f, spawnAreaWidth / 2f);
        return new Vector3(randomX, spawnHeight, 0f);
    }
    
    // B4: Sistema simplificado de inocentes
    private void ApplyInnocentVisualFeedback(Enemy enemy)
    {
        // Aplicar feedback visual inmediato para inocentes
        if (enemy.SpriteRenderer != null)
        {
            // Cambiar tint para identificar visualmente como inocente
            enemy.SpriteRenderer.color = new Color(0.8f, 0.8f, 1f, 1f); // Tint azulado
        }
        
        // Opcional: añadir un pequeño icono o efecto visual
        StartCoroutine(InnocentVisualEffect(enemy));
    }
    
    private IEnumerator InnocentVisualEffect(Enemy enemy)
    {
        if (enemy.SpriteRenderer == null) yield break;
        
        // Efecto de parpadeo sutil para identificar inocente
        Color originalColor = enemy.SpriteRenderer.color;
        
        for (int i = 0; i < 3; i++)
        {
            enemy.SpriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            enemy.SpriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void CompleteWave()
    {
        isWaveActive = false;
        
        OnWaveCompleted?.Invoke(currentWaveIndex);
        Debug.Log($"Completed {currentWave.waveName}");
        
        // Avanzar al siguiente wave
        currentWaveIndex++;
        
        // Verificar si hay más oleadas
        if (currentWaveIndex >= waveConfigurations.Length)
        {
            OnAllWavesCompleted?.Invoke();
        }
    }
    
    public void StopCurrentWave()
    {
        if (currentWaveCoroutine != null)
        {
            StopCoroutine(currentWaveCoroutine);
            currentWaveCoroutine = null;
        }
        
        isWaveActive = false;
        
        // Retornar todos los enemigos al pool
        EnemyPoolManager.Instance?.ReturnAllEnemies();
    }
    
    public void StartNextWave()
    {
        if (!isWaveActive && currentWaveIndex < waveConfigurations.Length)
        {
            StartWave();
        }
    }
    
    public void RestartCurrentWave()
    {
        StopCurrentWave();
        StartWave(currentWaveIndex);
    }
    
    // Métodos de información
    public float GetWaveProgress()
    {
        if (!isWaveActive || currentWave == null) return 0f;
        
        // Calcular progreso basado en tiempo transcurrido
        float elapsedTime = Time.time - (Time.time - currentWave.waveDuration);
        return Mathf.Clamp01(elapsedTime / currentWave.waveDuration);
    }
    
    public int GetTotalWaves()
    {
        return waveConfigurations.Length;
    }
    
    public string GetCurrentWaveName()
    {
        return currentWave != null ? currentWave.waveName : "No Wave";
    }
    
    public int GetRemainingEnemiesInWave()
    {
        if (!isWaveActive || currentWave == null) return 0;
        return currentWave.spawnEvents.Length - spawnedEnemies;
    }
}