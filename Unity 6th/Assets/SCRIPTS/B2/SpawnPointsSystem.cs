using UnityEngine;
using System.Collections.Generic;

// ARCHIVO: SpawnPointsSystem.cs
// Sistema de spawn points con rutas predefinidas pero con variación aleatoria (B2)

namespace ShootingRange
{
    [System.Serializable]
    public class SpawnRoute
    {
        [Header("Configuración de Ruta")]
        [Tooltip("Nombre identificativo de la ruta")]
        public string routeName = "Ruta 1";
        
        [Tooltip("Puntos que define esta ruta (mínimo 2)")]
        public List<Transform> routePoints = new List<Transform>();
        
        [Tooltip("Patrón de movimiento para esta ruta")]
        public MovementPattern preferredPattern = MovementPattern.Linear;
        
        [Tooltip("Velocidad base para enemigos en esta ruta")]
        [Range(1f, 10f)]
        public float routeSpeed = 3f;
        
        [Tooltip("Variación aleatoria permitida en esta ruta")]
        [Range(0f, 2f)]
        public float randomVariation = 0.5f;
        
        [Header("Configuración de Spawn")]
        [Tooltip("Tipos de enemigos que pueden usar esta ruta")]
        public List<EnemyType> allowedEnemyTypes = new List<EnemyType>();
        
        [Tooltip("Peso de probabilidad de esta ruta (mayor = más probable)")]
        [Range(1f, 10f)]
        public float routeWeight = 1f;
        
        [Tooltip("Esta ruta está activa")]
        public bool isActive = true;
        
        // Métodos de utilidad
        public bool IsValidRoute()
        {
            return routePoints.Count >= 2 && allowedEnemyTypes.Count > 0;
        }
        
        public Vector3 GetStartPosition()
        {
            return routePoints.Count > 0 ? routePoints[0].position : Vector3.zero;
        }
        
        public Vector3 GetDirection()
        {
            if (routePoints.Count < 2) return Vector3.up;
            
            Vector3 direction = (routePoints[1].position - routePoints[0].position).normalized;
            
            // Agregar variación aleatoria
            if (randomVariation > 0)
            {
                Vector3 randomOffset = Random.insideUnitSphere * randomVariation;
                randomOffset.z = 0; // Mantener en 2D
                direction += randomOffset;
                direction = direction.normalized;
            }
            
            return direction;
        }
    }
    
    public class SpawnPointsSystem : MonoBehaviour
    {
        [Header("Configuración de Rutas")]
        [Tooltip("Todas las rutas disponibles para spawn")]
        public List<SpawnRoute> availableRoutes = new List<SpawnRoute>();
        
        [Header("Configuración de Variación")]
        [Tooltip("Variación aleatoria global en posiciones de spawn")]
        [Range(0f, 3f)]
        public float globalPositionVariation = 1f;
        
        [Tooltip("Variación aleatoria global en direcciones")]
        [Range(0f, 45f)]
        public float globalDirectionVariation = 15f;
        
        [Header("Configuración de Pantalla")]
        [Tooltip("Crear rutas automáticas desde bordes de pantalla")]
        public bool autoCreateScreenEdgeRoutes = true;
        
        [Tooltip("Distancia desde borde de pantalla para spawn automático")]
        [Range(1f, 5f)]
        public float screenEdgeDistance = 2f;
        
        [Header("Debug")]
        [Tooltip("Mostrar rutas en Scene View")]
        public bool showRoutesInScene = true;
        
        [Tooltip("Color para mostrar rutas")]
        public Color routeDebugColor = Color.green;
        
        // Variables privadas
        private Camera gameCamera;
        private List<SpawnRoute> activeRoutes = new List<SpawnRoute>();
        private float totalRouteWeight = 0f;
        
        void Start()
        {
            InitializeSpawnSystem();
        }
        
        void InitializeSpawnSystem()
        {
            gameCamera = Camera.main;
            
            // Crear rutas automáticas de bordes de pantalla si está habilitado
            if (autoCreateScreenEdgeRoutes)
            {
                CreateScreenEdgeRoutes();
            }
            
            // Actualizar lista de rutas activas
            UpdateActiveRoutes();
            
            Debug.Log($"SpawnPointsSystem inicializado con {activeRoutes.Count} rutas activas");
        }
        
        void CreateScreenEdgeRoutes()
        {
            if (gameCamera == null) return;
            
            // Obtener límites de la cámara
            float height = gameCamera.orthographicSize * 2f;
            float width = height * gameCamera.aspect;
            
            Vector3 cameraPos = gameCamera.transform.position;
            
            // Crear rutas desde cada borde hacia el centro
            CreateEdgeRoute("Izquierda a Derecha", 
                           new Vector3(cameraPos.x - width/2 - screenEdgeDistance, cameraPos.y, 0),
                           new Vector3(cameraPos.x + width/2 + screenEdgeDistance, cameraPos.y, 0));
            
            CreateEdgeRoute("Derecha a Izquierda", 
                           new Vector3(cameraPos.x + width/2 + screenEdgeDistance, cameraPos.y, 0),
                           new Vector3(cameraPos.x - width/2 - screenEdgeDistance, cameraPos.y, 0));
            
            CreateEdgeRoute("Abajo a Arriba", 
                           new Vector3(cameraPos.x, cameraPos.y - height/2 - screenEdgeDistance, 0),
                           new Vector3(cameraPos.x, cameraPos.y + height/2 + screenEdgeDistance, 0));
            
            CreateEdgeRoute("Arriba a Abajo", 
                           new Vector3(cameraPos.x, cameraPos.y + height/2 + screenEdgeDistance, 0),
                           new Vector3(cameraPos.x, cameraPos.y - height/2 - screenEdgeDistance, 0));
        }
        
        void CreateEdgeRoute(string routeName, Vector3 startPos, Vector3 endPos)
        {
            // Crear GameObjects para los puntos de ruta
            GameObject startPoint = new GameObject($"{routeName}_Start");
            GameObject endPoint = new GameObject($"{routeName}_End");
            
            startPoint.transform.position = startPos;
            endPoint.transform.position = endPos;
            
            // Hacer que sean hijos de este objeto para organización
            startPoint.transform.SetParent(transform);
            endPoint.transform.SetParent(transform);
            
            // Crear la ruta
            SpawnRoute newRoute = new SpawnRoute();
            newRoute.routeName = routeName;
            newRoute.routePoints.Add(startPoint.transform);
            newRoute.routePoints.Add(endPoint.transform);
            newRoute.preferredPattern = MovementPattern.Linear;
            newRoute.routeSpeed = 3f;
            newRoute.randomVariation = 0.3f;
            newRoute.allowedEnemyTypes.Add(EnemyType.Normal);
            newRoute.allowedEnemyTypes.Add(EnemyType.Fast);
            newRoute.routeWeight = 1f;
            newRoute.isActive = true;
            
            availableRoutes.Add(newRoute);
        }
        
        void UpdateActiveRoutes()
        {
            activeRoutes.Clear();
            totalRouteWeight = 0f;
            
            foreach (SpawnRoute route in availableRoutes)
            {
                if (route.isActive && route.IsValidRoute())
                {
                    activeRoutes.Add(route);
                    totalRouteWeight += route.routeWeight;
                }
            }
            
            Debug.Log($"Rutas activas actualizadas: {activeRoutes.Count} de {availableRoutes.Count}");
        }
        
        // MÉTODOS PRINCIPALES PARA SPAWN
        
        public SpawnData GetRandomSpawnData()
        {
            if (activeRoutes.Count == 0)
            {
                Debug.LogWarning("No hay rutas activas disponibles");
                return null;
            }
            
            SpawnRoute selectedRoute = SelectRandomRoute();
            return CreateSpawnDataFromRoute(selectedRoute);
        }
        
        public SpawnData GetSpawnDataForEnemyType(EnemyType enemyType)
        {
            List<SpawnRoute> validRoutes = new List<SpawnRoute>();
            
            // Filtrar rutas que permiten este tipo de enemigo
            foreach (SpawnRoute route in activeRoutes)
            {
                if (route.allowedEnemyTypes.Contains(enemyType))
                {
                    validRoutes.Add(route);
                }
            }
            
            if (validRoutes.Count == 0)
            {
                Debug.LogWarning($"No hay rutas disponibles para enemigo tipo {enemyType}");
                return GetRandomSpawnData(); // Fallback a ruta aleatoria
            }
            
            SpawnRoute selectedRoute = SelectRandomRouteFromList(validRoutes);
            return CreateSpawnDataFromRoute(selectedRoute);
        }
        
        SpawnRoute SelectRandomRoute()
        {
            if (activeRoutes.Count == 1)
                return activeRoutes[0];
                
            float randomValue = Random.Range(0f, totalRouteWeight);
            float currentWeight = 0f;
            
            foreach (SpawnRoute route in activeRoutes)
            {
                currentWeight += route.routeWeight;
                if (randomValue <= currentWeight)
                {
                    return route;
                }
            }
            
            // Fallback
            return activeRoutes[activeRoutes.Count - 1];
        }
        
        SpawnRoute SelectRandomRouteFromList(List<SpawnRoute> routes)
        {
            float totalWeight = 0f;
            foreach (SpawnRoute route in routes)
            {
                totalWeight += route.routeWeight;
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (SpawnRoute route in routes)
            {
                currentWeight += route.routeWeight;
                if (randomValue <= currentWeight)
                {
                    return route;
                }
            }
            
            return routes[routes.Count - 1];
        }
        
        SpawnData CreateSpawnDataFromRoute(SpawnRoute route)
        {
            SpawnData spawnData = new SpawnData();
            
            // Posición con variación
            spawnData.spawnPosition = route.GetStartPosition();
            if (globalPositionVariation > 0)
            {
                Vector3 posVariation = Random.insideUnitSphere * globalPositionVariation;
                posVariation.z = 0; // Mantener 2D
                spawnData.spawnPosition += posVariation;
            }
            
            // Dirección con variación
            spawnData.initialDirection = route.GetDirection();
            if (globalDirectionVariation > 0)
            {
                float angleVariation = Random.Range(-globalDirectionVariation, globalDirectionVariation);
                spawnData.initialDirection = RotateVector2(spawnData.initialDirection, angleVariation);
            }
            
            // Otros parámetros
            spawnData.movementPattern = route.preferredPattern;
            spawnData.baseSpeed = route.routeSpeed + Random.Range(-route.randomVariation, route.randomVariation);
            spawnData.baseSpeed = Mathf.Max(0.5f, spawnData.baseSpeed); // Mínimo de velocidad
            spawnData.allowedEnemyTypes = new List<EnemyType>(route.allowedEnemyTypes);
            spawnData.routeName = route.routeName;
            
            return spawnData;
        }
        
        Vector2 RotateVector2(Vector2 vector, float angleDegrees)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);
            
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }
        
        // MÉTODOS PÚBLICOS DE CONFIGURACIÓN
        
        public void SetRouteActive(string routeName, bool active)
        {
            foreach (SpawnRoute route in availableRoutes)
            {
                if (route.routeName == routeName)
                {
                    route.isActive = active;
                    UpdateActiveRoutes();
                    break;
                }
            }
        }
        
        public void SetRouteWeight(string routeName, float weight)
        {
            foreach (SpawnRoute route in availableRoutes)
            {
                if (route.routeName == routeName)
                {
                    route.routeWeight = Mathf.Max(0.1f, weight);
                    UpdateActiveRoutes();
                    break;
                }
            }
        }
        
        public List<string> GetActiveRouteNames()
        {
            List<string> names = new List<string>();
            foreach (SpawnRoute route in activeRoutes)
            {
                names.Add(route.routeName);
            }
            return names;
        }
        
        // MÉTODOS DE DEBUG
        [ContextMenu("Update Active Routes")]
        public void DebugUpdateRoutes()
        {
            UpdateActiveRoutes();
        }
        
        [ContextMenu("Create Screen Edge Routes")]
        public void DebugCreateScreenRoutes()
        {
            CreateScreenEdgeRoutes();
        }
        
        [ContextMenu("Test Random Spawn")]
        public void DebugTestSpawn()
        {
            SpawnData testData = GetRandomSpawnData();
            if (testData != null)
            {
                Debug.Log($"Spawn Test - Posición: {testData.spawnPosition}, Dirección: {testData.initialDirection}, Patrón: {testData.movementPattern}");
            }
        }
        
        // Visualización en Scene View
        void OnDrawGizmos()
        {
            if (!showRoutesInScene || availableRoutes == null) return;
            
            Gizmos.color = routeDebugColor;
            
            foreach (SpawnRoute route in availableRoutes)
            {
                if (!route.isActive || route.routePoints.Count < 2) continue;
                
                // Dibujar línea de ruta
                for (int i = 0; i < route.routePoints.Count - 1; i++)
                {
                    if (route.routePoints[i] != null && route.routePoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(route.routePoints[i].position, route.routePoints[i + 1].position);
                    }
                }
                
                // Dibujar puntos de spawn
                foreach (Transform point in route.routePoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.3f);
                    }
                }
            }
        }
        
        void OnDrawGizmosSelected()
        {
            // Mostrar información adicional cuando está seleccionado
            if (availableRoutes == null) return;
            
            Gizmos.color = Color.yellow;
            
            foreach (SpawnRoute route in availableRoutes)
            {
                if (route.isActive && route.routePoints.Count >= 2)
                {
                    // Mostrar dirección de la ruta
                    Vector3 start = route.GetStartPosition();
                    Vector3 direction = route.GetDirection();
                    Gizmos.DrawRay(start, direction * 2f);
                }
            }
        }
    }
    
    // CLASE AUXILIAR: Datos de spawn
    [System.Serializable]
    public class SpawnData
    {
        public Vector3 spawnPosition;
        public Vector2 initialDirection;
        public MovementPattern movementPattern;
        public float baseSpeed;
        public List<EnemyType> allowedEnemyTypes;
        public string routeName;
        
        public SpawnData()
        {
            allowedEnemyTypes = new List<EnemyType>();
        }
    }
}