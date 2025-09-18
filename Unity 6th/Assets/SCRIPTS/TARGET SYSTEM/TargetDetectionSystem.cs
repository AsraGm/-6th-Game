using System.Collections.Generic;
using UnityEngine;

// ARCHIVO: TargetDetectionSystem.cs
// Sistema principal de detección de objetivos

namespace ShootingRange
{
    public class TargetDetectionSystem : MonoBehaviour
    {
        [Header("Configuración de Detección")]
        [Tooltip("Usar Trigger Colliders para mejor rendimiento móvil en lugar de solo Raycast")]
        public bool useTriggerOptimization = true;
        
        [Tooltip("Capas de objetivos detectables")]
        public LayerMask targetLayers = -1;
        
        [Header("Configuración de Puntuación")]
        [Tooltip("ARRASTRA AQUÍ tu archivo TargetScoreConfig desde la carpeta del proyecto")]
        public TargetScoreConfig scoreConfig;
        
        [Header("Referencias")]
        [Tooltip("Sistema de puntuación. Si lo dejas vacío se buscará automáticamente")]
        public ScoreSystem scoreSystem;
        
        [Header("Optimización Móvil")]
        [Tooltip("Cache de componentes para evitar GetComponent repetitivo")]
        public bool useComponentCache = true;
        
        // Cache de componentes para optimización móvil
        private Dictionary<GameObject, IShootable> shootableCache = new Dictionary<GameObject, IShootable>();
        private Dictionary<GameObject, EnemyType> enemyTypeCache = new Dictionary<GameObject, EnemyType>();
        
        // Estadísticas de detección
        [Header("Debug Info")]
        public int totalHits = 0;
        public int innocentHits = 0;
        public int enemyHits = 0;
        
        void Start()
        {
            InitializeDetectionSystem();
        }
        
        void InitializeDetectionSystem()
        {
            // Configurar score system si no está asignado
            if (scoreSystem == null)
            {
                scoreSystem = FindObjectOfType<ScoreSystem>();
                if (scoreSystem == null)
                {
                    Debug.LogWarning("ScoreSystem no encontrado. Creando uno básico.");
                    GameObject scoreObj = new GameObject("ScoreSystem");
                    scoreSystem = scoreObj.AddComponent<ScoreSystem>();
                }
            }
            
            // Crear configuración por defecto si no existe
            if (scoreConfig == null)
            {
                Debug.LogWarning("TargetScoreConfig no asignado. Usando valores por defecto.");
                scoreConfig = ScriptableObject.CreateInstance<TargetScoreConfig>();
            }
        }
        
        // MÉTODO PRINCIPAL: Procesar hit de bala
        public void ProcessBulletHit(GameObject hitObject, Vector3 hitPoint)
        {
            if (hitObject == null) return;
            
            // Verificar si el objeto es un objetivo válido
            if (!IsValidTarget(hitObject)) 
            {
                Debug.Log($"Objeto {hitObject.name} no es un objetivo válido");
                return;
            }
            
            // Obtener componente IShootable (con cache)
            IShootable shootable = GetShootableComponent(hitObject);
            if (shootable == null) 
            {
                Debug.LogWarning($"Objeto {hitObject.name} no tiene componente IShootable");
                return;
            }
            
            // Obtener tipo de enemigo
            EnemyType enemyType = GetEnemyType(hitObject, shootable);
            
            // Calcular puntuación
            int scoreValue = scoreConfig.GetScoreForEnemyType(enemyType);
            ObjectType objectType = GetObjectType(enemyType);
            
            // Procesar el hit
            shootable.OnHit(objectType, scoreValue);
            
            // Actualizar estadísticas
            UpdateHitStatistics(objectType);
            
            // Notificar al sistema de puntuación
            if (scoreSystem != null)
            {
                scoreSystem.AddScore(scoreValue, objectType, enemyType);
            }
            
            // PLACEHOLDER: Conexión con sistema de temas (Lista C2)
            ProcessThemeEffects(hitObject, shootable.GetThemeID(), enemyType);
            
            Debug.Log($"Hit procesado: {enemyType} = {scoreValue} puntos");
        }
        
        // Verificar si es un objetivo válido usando tags optimizados
        bool IsValidTarget(GameObject obj)
        {
            // Sistema de tags optimizado para identificación rápida
            return obj.CompareTag("Enemy") || 
                   obj.CompareTag("Innocent") || 
                   obj.CompareTag("Target");
        }
        
        // Obtener componente IShootable con cache
        IShootable GetShootableComponent(GameObject obj)
        {
            if (useComponentCache)
            {
                if (!shootableCache.ContainsKey(obj))
                {
                    IShootable shootable = obj.GetComponent<IShootable>();
                    shootableCache[obj] = shootable;
                    return shootable;
                }
                return shootableCache[obj];
            }
            else
            {
                return obj.GetComponent<IShootable>();
            }
        }
        
        // Obtener tipo de enemigo con cache
        EnemyType GetEnemyType(GameObject obj, IShootable shootable)
        {
            if (useComponentCache)
            {
                if (!enemyTypeCache.ContainsKey(obj))
                {
                    EnemyType type = shootable.GetEnemyType();
                    enemyTypeCache[obj] = type;
                    return type;
                }
                return enemyTypeCache[obj];
            }
            else
            {
                return shootable.GetEnemyType();
            }
        }
        
        // Convertir EnemyType a ObjectType
        ObjectType GetObjectType(EnemyType enemyType)
        {
            return enemyType == EnemyType.Innocent ? ObjectType.Innocent : ObjectType.Enemy;
        }
        
        // Actualizar estadísticas de hits
        void UpdateHitStatistics(ObjectType objectType)
        {
            totalHits++;
            if (objectType == ObjectType.Innocent)
                innocentHits++;
            else
                enemyHits++;
        }
        
        // PLACEHOLDER: Efectos de tema (CONEXIÓN CON LISTA C2)
        void ProcessThemeEffects(GameObject hitObject, string themeID, EnemyType enemyType)
        {
            // TODO: Conectar con Lista C-Instrucción 2 (Theme application)
            // Aquí se aplicarán efectos visuales según el tema activo
            // Por ejemplo: diferentes efectos de muerte, sonidos, partículas
            
            Debug.Log($"Theme effect placeholder: {themeID} - {enemyType}");
        }
        
        // Limpiar cache cuando sea necesario
        public void ClearCache()
        {
            shootableCache.Clear();
            enemyTypeCache.Clear();
        }
        
        // Método para registro de objetos en cache (optimización)
        public void RegisterTarget(GameObject target, IShootable shootable, EnemyType enemyType)
        {
            if (useComponentCache)
            {
                shootableCache[target] = shootable;
                enemyTypeCache[target] = enemyType;
            }
        }
        
        void OnDestroy()
        {
            ClearCache();
        }
    }
}