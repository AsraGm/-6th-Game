using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShootingRange;

public class TouchShootingSystem : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    [Tooltip("Zona muerta en el centro de la pantalla donde NO se puede disparar (0-1). 0 = sin zona muerta, 0.2 = 20% del centro")]
    public float deadZone = 0.5f;

    [Tooltip("Capas que detecta el raycast. Pon aquí las capas de tus objetivos/enemigos. 'Everything' detecta todo")]
    public LayerMask targetLayer = -1;

    [Tooltip("Velocidad de la bala en unidades por segundo. Más alto = balas más rápidas")]
    public float bulletSpeed = 15f;

    [Tooltip("Tiempo en segundos antes de que la bala se auto-destruya")]
    public float bulletLifetime = 3f;

    [Header("Referencias IMPORTANTES")]
    [Tooltip("ARRASTRA AQUÍ tu cámara principal (normalmente 'Main Camera'). Si lo dejas vacío usará Camera.main")]
    public Camera shootingCamera;

    [Tooltip("ARRASTRA AQUÍ un Transform vacío donde quieres que aparezcan las balas (posición del arma/personaje)")]
    public Transform firePoint;

    [Header("Configuración de Bala Sprite")]
    [Tooltip("ARRASTRA AQUÍ tu prefab de bala. Debe tener SpriteRenderer y Collider2D como Trigger")]
    public GameObject bulletPrefab;

    [Tooltip("Cuántas balas mantener en memoria para reutilizar. Más = mejor rendimiento pero más RAM")]
    public int poolSize = 50;

    [Header("Efectos")]
    [Tooltip("¿Vibrar el dispositivo al disparar? Solo funciona en móvil")]
    public bool useHapticFeedback = true;

    [Tooltip("Duración de la vibración en segundos (actualmente no se usa)")]
    public float vibrationDuration = 0.1f;

    // Sistema de Pool para balas
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private List<GameObject> activeBullets = new List<GameObject>();

    // Control de input
    private bool canShoot = true;
    private float lastShotTime;

    [Header("Control de Disparo")]
    [Tooltip("Tiempo mínimo entre disparos en segundos. 0.1 = 10 disparos por segundo máximo")]
    public float fireRate = 0.2f;

    void Start()
    {
        InitializeBulletPool();

        // Si no se asigna cámara, usar la principal
        if (shootingCamera == null)
            shootingCamera = Camera.main;

        // Si no se asigna firePoint, usar la posición de este objeto
        if (firePoint == null)
            firePoint = this.transform;
    }

    void Update()
    {
        HandleTouchInput();
        UpdateActiveBullets();
    }

    // Inicializar pool de balas
    void InitializeBulletPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);

            // Asegurar que tenga los componentes necesarios
            if (bullet.GetComponent<Rigidbody2D>() == null)
                bullet.AddComponent<Rigidbody2D>();

            if (bullet.GetComponent<BulletBehavior>() == null)
                bullet.AddComponent<BulletBehavior>();
        }
    }

    // Manejo de input táctil
    void HandleTouchInput()
    {
        // Para móvil - Touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                ProcessShot(touch.position);
            }
        }

        // Para testing en editor - Mouse input
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            ProcessShot(Input.mousePosition);
        }
#endif
    }

    void ProcessShot(Vector2 screenPosition)
    {
        Debug.Log($"SCREEN TOUCH: {screenPosition}");
        Debug.Log($"SCREEN CENTER: {new Vector2(Screen.width / 2, Screen.height / 2)}");
        // Raycast directo desde cámara
        Ray ray = shootingCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, targetLayer);

        if (hit.collider != null)
        {
            // Disparar directamente hacia el punto exacto donde hiciste hit
            Vector3 targetPoint = hit.point;
            Vector3 direction = (targetPoint - firePoint.position).normalized;
            ShootBullet(direction, targetPoint);
        }
        else
        {
            // Fallback: disparar hacia adelante
            ShootBullet(Vector3.forward, firePoint.position + Vector3.forward * 10);
        }
    }

    bool IsInDeadZone(Vector2 screenPos)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        float deadZonePixels = deadZone * Screen.height; // Basado en altura de pantalla

        return Vector2.Distance(screenPos, screenCenter) < deadZonePixels;
    }

    void ShootBullet(Vector2 direction, Vector3 targetPosition)
    {
        GameObject bullet = GetPooledBullet();

        if (bullet != null)
        {
            // Posición inicial
            bullet.transform.position = firePoint.position;
            bullet.SetActive(true);

            // Configurar Rigidbody2D
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = bullet.AddComponent<Rigidbody2D>();
                rb.freezeRotation = true;
            }

            // MOVIMIENTO RECTO EN 2D - Sin componente Z
            rb.linearVelocity = direction * bulletSpeed;

            // Configurar comportamiento SIN movimiento en Z
            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            if (bulletBehavior == null)
            {
                bulletBehavior = bullet.AddComponent<BulletBehavior>();
            }
            // ELIMINAR el parámetro Z speed
            bulletBehavior.Initialize(this, bulletLifetime, 0f); // 0f = sin movimiento Z

            activeBullets.Add(bullet);
        }
    }

    // Obtener bala del pool
    GameObject GetPooledBullet()
    {
        if (bulletPool.Count > 0)
        {
            return bulletPool.Dequeue();
        }

        // Si no hay balas en pool, crear una nueva
        GameObject newBullet = Instantiate(bulletPrefab);
        if (newBullet.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = newBullet.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
        }
        if (newBullet.GetComponent<BulletBehavior>() == null)
            newBullet.AddComponent<BulletBehavior>();

        return newBullet;
    }

    // Devolver bala al pool
    public void ReturnBulletToPool(GameObject bullet)
    {
        bullet.SetActive(false);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        activeBullets.Remove(bullet);
        bulletPool.Enqueue(bullet);
    }

    // Actualizar balas activas
    void UpdateActiveBullets()
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            if (activeBullets[i] == null || !activeBullets[i].activeInHierarchy)
            {
                activeBullets.RemoveAt(i);
            }
        }
    }

    // Feedback táctil
    void TriggerHapticFeedback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
        // Para iOS necesitarías un plugin específico
        Handheld.Vibrate();
#endif
    }

    // Debug visual
    void OnDrawGizmos()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
        }

        // Mostrar deadzone
        if (shootingCamera != null)
        {
            Vector3 screenCenter = shootingCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, shootingCamera.nearClipPlane));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(screenCenter, deadZone);
        }
    }
}

// COMPONENTE AUXILIAR: Comportamiento de bala
public class BulletBehavior : MonoBehaviour
{
    private TouchShootingSystem parentSystem;
    private float lifetime;
    private float timeAlive;
    private float zSpeed; 

    public void Initialize(TouchShootingSystem parent, float life, float speedZ = 0f)
    {
        parentSystem = parent;
        lifetime = life;
        timeAlive = 0f;
        zSpeed = speedZ;
    }

    void Update()
    {
        timeAlive += Time.deltaTime;

        // Movimiento en Z para parallax
        if (zSpeed != 0f)
        {
            transform.Translate(0, 0, zSpeed * Time.deltaTime, Space.World);
        }

        // Destruir por tiempo
        if (timeAlive >= lifetime)
        {
            ReturnToPool();
        }
    }

        void OnTriggerEnter2D(Collider2D other)
        {
        // NUEVA FUNCIONALIDAD: Conectar con sistema de detección
        TargetDetectionSystem detector = FindObjectOfType<TargetDetectionSystem>();
        if (detector != null)
        {
            detector.ProcessBulletHit(other.gameObject, transform.position);
        }
        else
        {
            Debug.Log($"Bala impactó: {other.name} (TargetDetectionSystem no encontrado)");
        }
        //Aquí puedes agregar lógica de colisión con objetivos
        Debug.Log($"Bala impactó: {other.name}");

            // Opcional: Agregar efectos de impacto aquí

            ReturnToPool();
        }

        void ReturnToPool()
        {
            if (parentSystem != null)
            {
                parentSystem.ReturnBulletToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnBecameInvisible()
        {
            // Devolver al pool si sale de pantalla
            ReturnToPool();
        }
}