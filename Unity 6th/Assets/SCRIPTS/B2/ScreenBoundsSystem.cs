using UnityEngine;

// ARCHIVO: ScreenBoundsSystem.cs
// Sistema de barreras invisibles para mantener enemigos en pantalla

namespace ShootingRange
{
    public class ScreenBoundsSystem : MonoBehaviour
    {
        [Header("Configuración de Barreras")]
        [Tooltip("Grosor de las barreras invisibles")]
        [Range(0.1f, 2f)]
        public float barrierThickness = 0.5f;
        
        [Tooltip("Distancia extra fuera de la pantalla donde colocar barreras")]
        [Range(0.1f, 3f)]
        public float barrierDistance = 1f;
        
        [Header("Referencias")]
        [Tooltip("Cámara para calcular límites. Si está vacío usa Camera.main")]
        public Camera targetCamera;
        
        [Header("Configuración Visual")]
        [Tooltip("Mostrar barreras en Scene View")]
        public bool showBarriersInScene = true;
        
        [Tooltip("Color de las barreras en Scene View")]
        public Color barrierColor = Color.red;
        
        // Referencias a los colliders creados
        private BoxCollider2D leftBarrier;
        private BoxCollider2D rightBarrier;
        private BoxCollider2D topBarrier;
        private BoxCollider2D bottomBarrier;
        
        // Información de límites
        private float screenWidth;
        private float screenHeight;
        private Vector3 cameraPosition;
        
        void Start()
        {
            SetupCamera();
            CreateInvisibleBarriers();
        }
        
        void SetupCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
            
            if (targetCamera == null)
            {
                Debug.LogError("ScreenBoundsSystem: No se encontró cámara. Asigna una en el inspector.");
                return;
            }
            
            UpdateCameraBounds();
        }
        
        void UpdateCameraBounds()
        {
            if (targetCamera == null) return;
            
            screenHeight = targetCamera.orthographicSize * 2f;
            screenWidth = screenHeight * targetCamera.aspect;
            cameraPosition = targetCamera.transform.position;
        }
        
        void CreateInvisibleBarriers()
        {
            if (targetCamera == null) return;
            
            UpdateCameraBounds();
            
            // Crear barreras si no existen
            if (leftBarrier == null) CreateBarrier("LeftBarrier", out leftBarrier);
            if (rightBarrier == null) CreateBarrier("RightBarrier", out rightBarrier);
            if (topBarrier == null) CreateBarrier("TopBarrier", out topBarrier);
            if (bottomBarrier == null) CreateBarrier("BottomBarrier", out bottomBarrier);
            
            // Configurar posiciones y tamaños
            SetupLeftBarrier();
            SetupRightBarrier();
            SetupTopBarrier();
            SetupBottomBarrier();
            
            Debug.Log("Barreras invisibles creadas y configuradas");
        }
        
        void CreateBarrier(string name, out BoxCollider2D barrier)
        {
            GameObject barrierObj = new GameObject(name);
            barrierObj.transform.SetParent(transform);
            barrierObj.layer = gameObject.layer;
            
            barrier = barrierObj.AddComponent<BoxCollider2D>();
            barrier.isTrigger = true;
            
            // Agregar script de rebote
            ScreenBoundsBarrier bounceScript = barrierObj.AddComponent<ScreenBoundsBarrier>();
            bounceScript.SetBoundsSystem(this);
        }
        
        void SetupLeftBarrier()
        {
            if (leftBarrier == null) return;
            
            float posX = cameraPosition.x - screenWidth/2 - barrierDistance - barrierThickness/2;
            float posY = cameraPosition.y;
            
            leftBarrier.transform.position = new Vector3(posX, posY, 0);
            leftBarrier.size = new Vector2(barrierThickness, screenHeight + barrierDistance * 2);
        }
        
        void SetupRightBarrier()
        {
            if (rightBarrier == null) return;
            
            float posX = cameraPosition.x + screenWidth/2 + barrierDistance + barrierThickness/2;
            float posY = cameraPosition.y;
            
            rightBarrier.transform.position = new Vector3(posX, posY, 0);
            rightBarrier.size = new Vector2(barrierThickness, screenHeight + barrierDistance * 2);
        }
        
        void SetupTopBarrier()
        {
            if (topBarrier == null) return;
            
            float posX = cameraPosition.x;
            float posY = cameraPosition.y + screenHeight/2 + barrierDistance + barrierThickness/2;
            
            topBarrier.transform.position = new Vector3(posX, posY, 0);
            topBarrier.size = new Vector2(screenWidth + barrierDistance * 2, barrierThickness);
        }
        
        void SetupBottomBarrier()
        {
            if (bottomBarrier == null) return;
            
            float posX = cameraPosition.x;
            float posY = cameraPosition.y - screenHeight/2 - barrierDistance - barrierThickness/2;
            
            bottomBarrier.transform.position = new Vector3(posX, posY, 0);
            bottomBarrier.size = new Vector2(screenWidth + barrierDistance * 2, barrierThickness);
        }
        
        // Método para rebotar enemigo (llamado por ScreenBoundsBarrier)
        public void BounceEnemy(GameObject enemy, Vector3 barrierPosition, Vector3 barrierNormal)
        {
            // Obtener componente de movimiento
            EnemyMovementPatterns movement = enemy.GetComponent<EnemyMovementPatterns>();
            if (movement != null)
            {
                // Calcular nueva dirección rebotada
                Vector2 currentDirection = movement.GetCurrentDirection();
                Vector2 newDirection = Vector2.Reflect(currentDirection, barrierNormal);
                
                // Aplicar nueva dirección
                movement.SetDirection(newDirection);
                
                // Mover enemigo ligeramente hacia adentro para evitar múltiples rebotes
                Vector3 pushDirection = barrierNormal * 0.2f;
                enemy.transform.position += pushDirection;
                
                Debug.Log($"Enemigo {enemy.name} rebotó en barrera. Nueva dirección: {newDirection}");
            }
            else
            {
                // Fallback: mover hacia el centro de la pantalla
                Vector3 directionToCenter = (cameraPosition - enemy.transform.position).normalized;
                enemy.transform.position += directionToCenter * 0.5f;
            }
        }
        
        // Métodos públicos para reconfigurar barreras
        public void ReconfigureBarriers()
        {
            UpdateCameraBounds();
            
            SetupLeftBarrier();
            SetupRightBarrier();
            SetupTopBarrier();
            SetupBottomBarrier();
            
            Debug.Log("Barreras reconfiguradas");
        }
        
        public void SetBarrierDistance(float newDistance)
        {
            barrierDistance = Mathf.Max(0.1f, newDistance);
            ReconfigureBarriers();
        }
        
        public void SetBarrierThickness(float newThickness)
        {
            barrierThickness = Mathf.Max(0.1f, newThickness);
            ReconfigureBarriers();
        }
        
        // Método para testing
        [ContextMenu("Recreate Barriers")]
        public void DebugRecreateBarriers()
        {
            // Destruir barreras existentes
            if (leftBarrier != null) DestroyImmediate(leftBarrier.gameObject);
            if (rightBarrier != null) DestroyImmediate(rightBarrier.gameObject);
            if (topBarrier != null) DestroyImmediate(topBarrier.gameObject);
            if (bottomBarrier != null) DestroyImmediate(bottomBarrier.gameObject);
            
            // Crear nuevas
            CreateInvisibleBarriers();
        }
        
        // Visualización en Scene View
        void OnDrawGizmos()
        {
            if (!showBarriersInScene || targetCamera == null) return;
            
            UpdateCameraBounds();
            
            Gizmos.color = barrierColor;
            
            // Dibujar límites de pantalla
            Vector3[] screenCorners = {
                new Vector3(cameraPosition.x - screenWidth/2, cameraPosition.y - screenHeight/2, 0),
                new Vector3(cameraPosition.x + screenWidth/2, cameraPosition.y - screenHeight/2, 0),
                new Vector3(cameraPosition.x + screenWidth/2, cameraPosition.y + screenHeight/2, 0),
                new Vector3(cameraPosition.x - screenWidth/2, cameraPosition.y + screenHeight/2, 0)
            };
            
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(screenCorners[i], screenCorners[(i + 1) % 4]);
            }
            
            // Dibujar barreras
            Gizmos.color = new Color(barrierColor.r, barrierColor.g, barrierColor.b, 0.3f);
            
            // Barrera izquierda
            Vector3 leftPos = new Vector3(cameraPosition.x - screenWidth/2 - barrierDistance - barrierThickness/2, cameraPosition.y, 0);
            Vector3 leftSize = new Vector3(barrierThickness, screenHeight + barrierDistance * 2, 1);
            Gizmos.DrawCube(leftPos, leftSize);
            
            // Barrera derecha
            Vector3 rightPos = new Vector3(cameraPosition.x + screenWidth/2 + barrierDistance + barrierThickness/2, cameraPosition.y, 0);
            Vector3 rightSize = new Vector3(barrierThickness, screenHeight + barrierDistance * 2, 1);
            Gizmos.DrawCube(rightPos, rightSize);
            
            // Barrera superior
            Vector3 topPos = new Vector3(cameraPosition.x, cameraPosition.y + screenHeight/2 + barrierDistance + barrierThickness/2, 0);
            Vector3 topSize = new Vector3(screenWidth + barrierDistance * 2, barrierThickness, 1);
            Gizmos.DrawCube(topPos, topSize);
            
            // Barrera inferior
            Vector3 bottomPos = new Vector3(cameraPosition.x, cameraPosition.y - screenHeight/2 - barrierDistance - barrierThickness/2, 0);
            Vector3 bottomSize = new Vector3(screenWidth + barrierDistance * 2, barrierThickness, 1);
            Gizmos.DrawCube(bottomPos, bottomSize);
        }
    }
    
    // SCRIPT AUXILIAR: Componente que va en cada barrera
    public class ScreenBoundsBarrier : MonoBehaviour
    {
        private ScreenBoundsSystem boundsSystem;
        
        public void SetBoundsSystem(ScreenBoundsSystem system)
        {
            boundsSystem = system;
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            // Solo rebotar enemigos
            if (other.CompareTag("Enemy") || other.GetComponent<EnemyMovementPatterns>() != null)
            {
                // Calcular normal de la barrera (dirección hacia adentro)
                Vector3 barrierToCenter = Camera.main.transform.position - transform.position;
                Vector3 normal = barrierToCenter.normalized;
                
                // Llamar al sistema para rebotar
                if (boundsSystem != null)
                {
                    boundsSystem.BounceEnemy(other.gameObject, transform.position, normal);
                }
                
                Debug.Log($"Barrera {gameObject.name} detectó enemigo {other.name}");
            }
        }
        
        void OnTriggerStay2D(Collider2D other)
        {
            // Por si el enemigo se queda "pegado" en la barrera
            if (other.CompareTag("Enemy") || other.GetComponent<EnemyMovementPatterns>() != null)
            {
                Vector3 pushDirection = (Camera.main.transform.position - transform.position).normalized;
                other.transform.position += pushDirection * Time.deltaTime * 2f;
            }
        }
    }
}