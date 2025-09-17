using UnityEngine;
using System.Collections.Generic;

// B1: Sistema base de enemigos con identificación de tipos
public class EnemyPoolManager : MonoBehaviour
{
    public static EnemyPoolManager Instance { get; private set; }
    
    [System.Serializable]
    public class EnemyPool
    {
        public EnemyType enemyType;
        public GameObject enemyPrefab;
        public int poolSize = 10;
        [HideInInspector] public Queue<Enemy> pool = new Queue<Enemy>();
    }
    
    [Header("Enemy Pools Configuration")]
    [SerializeField] private EnemyPool[] enemyPools;
    
    [Header("Pool Parent")]
    [SerializeField] private Transform poolParent;
    
    private Dictionary<EnemyType, EnemyPool> poolDictionary;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePools()
    {
        poolDictionary = new Dictionary<EnemyType, EnemyPool>();
        
        // Crear parent si no existe
        if (poolParent == null)
        {
            GameObject poolParentGO = new GameObject("Enemy Pool Parent");
            poolParent = poolParentGO.transform;
            poolParent.SetParent(this.transform);
        }
        
        // Inicializar cada pool
        foreach (var enemyPool in enemyPools)
        {
            poolDictionary.Add(enemyPool.enemyType, enemyPool);
            
            // Pre-instanciar enemigos del pool
            for (int i = 0; i < enemyPool.poolSize; i++)
            {
                GameObject enemyGO = Instantiate(enemyPool.enemyPrefab, poolParent);
                Enemy enemy = enemyGO.GetComponent<Enemy>();
                
                if (enemy != null)
                {
                    enemy.SetEnemyType(enemyPool.enemyType);
                    enemy.gameObject.SetActive(false);
                    enemyPool.pool.Enqueue(enemy);
                }
            }
        }
    }
    
    public Enemy SpawnEnemy(EnemyType enemyType, Vector3 position, bool asInnocent = false)
    {
        if (!poolDictionary.ContainsKey(enemyType))
        {
            Debug.LogWarning($"No pool found for enemy type: {enemyType}");
            return null;
        }
        
        EnemyPool pool = poolDictionary[enemyType];
        Enemy enemy;
        
        // Obtener enemigo del pool o crear uno nuevo si el pool está vacío
        if (pool.pool.Count > 0)
        {
            enemy = pool.pool.Dequeue();
        }
        else
        {
            // Crear nuevo enemigo si el pool está vacío (fallback)
            GameObject enemyGO = Instantiate(pool.enemyPrefab, poolParent);
            enemy = enemyGO.GetComponent<Enemy>();
            enemy.SetEnemyType(enemyType);
        }
        
        // Configurar enemigo
        enemy.transform.position = position;
        enemy.SetAsInnocent(asInnocent);
        enemy.OnSpawnFromPool();
        
        return enemy;
    }
    
    public void ReturnEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        
        enemy.OnReturnToPool();
        
        // Retornar al pool correspondiente
        if (poolDictionary.ContainsKey(enemy.Type))
        {
            poolDictionary[enemy.Type].pool.Enqueue(enemy);
        }
    }
    
    public int GetActiveEnemies()
    {
        int count = 0;
        foreach (var pool in enemyPools)
        {
            count += (pool.poolSize - pool.pool.Count);
        }
        return count;
    }
    
    public void ReturnAllEnemies()
    {
        // Encontrar todos los enemigos activos y retornarlos
        Enemy[] activeEnemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in activeEnemies)
        {
            if (enemy.gameObject.activeInHierarchy)
            {
                ReturnEnemy(enemy);
            }
        }
    }
}

// Clase Enemy mejorada para el sistema de pool
public class Enemy : MonoBehaviour, IShootable, IPoolable
{
    [Header("Enemy Configuration")]
    [SerializeField] private EnemyType enemyType = EnemyType.Normal;
    [SerializeField] private int moneyValue = 10;
    [SerializeField] private string enemyID = "enemy_01"; // Para identificación única de skins
    
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D enemyCollider;
    
    private bool isInnocent = false;
    private EnemyBehavior enemyBehavior;
    
    // Properties
    public EnemyType Type => enemyType;
    public int MoneyValue => moneyValue;
    public bool IsInnocent => isInnocent;
    public string EnemyID => enemyID;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    
    private void Awake()
    {
        // Obtener referencias
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();
            
        enemyBehavior = GetComponent<EnemyBehavior>();
        
        // Configurar tags optimizados
        UpdateEnemyTag();
    }
    
    public void SetEnemyType(EnemyType newType)
    {
        enemyType = newType;
        UpdateEnemyTag();
    }
    
    public void SetAsInnocent(bool innocent)
    {
        isInnocent = innocent;
        UpdateEnemyTag();
        
        // Aplicar visual de inocente si es necesario
        if (isInnocent && spriteRenderer != null)
        {
            // Cambiar tint para identificar visualmente
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.8f);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }
    
    private void UpdateEnemyTag()
    {
        // Sistema de tags optimizado para identificación rápida
        gameObject.tag = isInnocent ? "Innocent" : "Enemy";
    }
    
    public virtual void OnHit(ObjectType type, int value)
    {
        // Procesar el impacto basado en si es inocente o no
        if (isInnocent)
        {
            OnInnocentHit();
        }
        else
        {
            OnEnemyHit();
        }
        
        // Retornar al pool
        OnReturnToPool();
    }
    
    protected virtual void OnInnocentHit()
    {
        // Procesado por el ScoreManager
        ScoreManager.Instance?.ProcessInnocentHit(moneyValue);
        
        // Feedback visual adicional para inocente
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRed());
        }
    }
    
    protected virtual void OnEnemyHit()
    {
        // Procesado por el ScoreManager
        ScoreManager.Instance?.ProcessEnemyHit(enemyType, moneyValue);
        
        // Feedback visual para enemigo eliminado
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashGreen());
        }
    }
    
    private System.Collections.IEnumerator FlashRed()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    private System.Collections.IEnumerator FlashGreen()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    public virtual void OnSpawnFromPool()
    {
        gameObject.SetActive(true);
        
        // Reiniciar estado del enemigo
        if (enemyCollider != null)
            enemyCollider.enabled = true;
            
        if (enemyBehavior != null)
            enemyBehavior.OnSpawn();
    }
    
    public virtual void OnReturnToPool()
    {
        // Detener comportamientos
        if (enemyBehavior != null)
            enemyBehavior.OnReturn();
        
        gameObject.SetActive(false);
        
        // El EnemyPoolManager se encargará de retornarlo al pool
        if (EnemyPoolManager.Instance != null)
        {
            EnemyPoolManager.Instance.ReturnEnemy(this);
        }
    }
}