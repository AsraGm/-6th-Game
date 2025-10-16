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

    [Tooltip("Velocidad de la bala VISUAL en unidades por segundo. Más alto = balas más rápidas")]
    public float bulletSpeed = 15f;

    [Tooltip("Tiempo en segundos antes de que la bala visual se auto-destruya")]
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

    [Header("Touch Directo")]
    [Tooltip("Usar touch directo: toca enemigo = hit inmediato + bala visual")]
    public bool useDirectTouch = true;

    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private List<GameObject> activeBullets = new List<GameObject>();

    private bool canShoot = true;
    private float lastShotTime;

    [Header("Control de Disparo")]
    [Tooltip("Tiempo mínimo entre disparos en segundos. 0.1 = 10 disparos por segundo máximo")]
    public float fireRate = 0.2f;

    void Start()
    {
        InitializeBulletPool();

        if (shootingCamera == null)
            shootingCamera = Camera.main;

        if (firePoint == null)
            firePoint = this.transform;
    }

    void Update()
    {
        HandleTouchInput();
        UpdateActiveBullets();
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                ProcessDirectTouch(touch.position);
            }
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            ProcessDirectTouch(Input.mousePosition);
        }
#endif
    }

    void ProcessDirectTouch(Vector2 screenPosition)
    {
        if (!canShoot || Time.time - lastShotTime < fireRate)
            return;

        if (IsInDeadZone(screenPosition))
            return;

        Debug.Log($"Touch directo en: {screenPosition}");

        if (useDirectTouch)
        {
            Ray ray = shootingCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, targetLayer);

            if (hit.collider != null)
            {
                Debug.Log($"Touch directo HIT: {hit.collider.name} en {hit.point}");

                TargetDetectionSystem detector = FindObjectOfType<TargetDetectionSystem>();
                if (detector != null)
                {
                    detector.ProcessBulletHit(hit.collider.gameObject, hit.point);
                }
                else
                {
                    Debug.Log($"Touch directo impactó: {hit.collider.name}");
                }

                CreateVisualBullet(firePoint.position, hit.point);

                if (useHapticFeedback)
                    TriggerHapticFeedback();

                lastShotTime = Time.time;
            }
            else
            {
                Debug.Log("Touch directo: No se detectó objetivo");

                Vector3 worldPos = shootingCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
                worldPos.z = 0;
                CreateVisualBullet(firePoint.position, worldPos);

                if (useHapticFeedback)
                    TriggerHapticFeedback();

                lastShotTime = Time.time;
            }
        }
        else
        {
            ProcessShot(screenPosition);
        }
    }

    void CreateVisualBullet(Vector3 startPos, Vector3 targetPos)
    {
        GameObject bullet = GetPooledBullet();

        if (bullet != null)
        {
            bullet.transform.position = startPos;
            bullet.SetActive(true);

            Vector3 direction = (targetPos - startPos).normalized;

            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            if (bulletBehavior == null)
            {
                bulletBehavior = bullet.AddComponent<BulletBehavior>();
            }

            bulletBehavior.InitializeVisual(this, bulletLifetime, direction, bulletSpeed, targetPos);

            activeBullets.Add(bullet);

            Debug.Log($"Bala visual creada: {startPos} -> {targetPos}, Dir: {direction}");
        }
    }

    void ProcessShot(Vector2 screenPosition)
    {
        Debug.Log("Usando método de disparo anterior");

        Vector3 worldPos = shootingCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        worldPos.z = 0;

        Vector3 direction = (worldPos - firePoint.position).normalized;

        CreateVisualBullet(firePoint.position, worldPos);

        if (useHapticFeedback)
            TriggerHapticFeedback();

        lastShotTime = Time.time;
    }

    void InitializeBulletPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);

            if (bullet.GetComponent<BulletBehavior>() == null)
                bullet.AddComponent<BulletBehavior>();

            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                DestroyImmediate(rb);
            }
        }
    }

    bool IsInDeadZone(Vector2 screenPos)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        float deadZonePixels = deadZone * Screen.height;

        return Vector2.Distance(screenPos, screenCenter) < deadZonePixels;
    }

    GameObject GetPooledBullet()
    {
        if (bulletPool.Count > 0)
        {
            return bulletPool.Dequeue();
        }

        GameObject newBullet = Instantiate(bulletPrefab);
        if (newBullet.GetComponent<BulletBehavior>() == null)
            newBullet.AddComponent<BulletBehavior>();

        Rigidbody2D rb = newBullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            DestroyImmediate(rb);
        }

        return newBullet;
    }

    public void ReturnBulletToPool(GameObject bullet)
    {
        bullet.SetActive(false);
        activeBullets.Remove(bullet);
        bulletPool.Enqueue(bullet);
    }

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
    void TriggerHapticFeedback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}

public class BulletBehavior : MonoBehaviour
{
    private TouchShootingSystem parentSystem;
    private float lifetime;
    private float timeAlive;
    private Vector3 direction;
    private float speed;
    private Vector3 targetPosition;
    private bool isVisualOnly = false;

    public void InitializeVisual(TouchShootingSystem parent, float life, Vector3 dir, float bulletSpeed, Vector3 target)
    {
        parentSystem = parent;
        lifetime = life;
        timeAlive = 0f;
        direction = dir.normalized;
        speed = bulletSpeed;
        targetPosition = target;
        isVisualOnly = true;

        Debug.Log($"Bala visual inicializada hacia: {target}");
    }

    void Update()
    {
        timeAlive += Time.deltaTime;

        if (isVisualOnly)
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            if (Vector3.Distance(transform.position, targetPosition) < 0.5f || timeAlive >= lifetime)
            {
                ReturnToPool();
            }
        }
        else
        {
            if (timeAlive >= lifetime)
            {
                ReturnToPool();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isVisualOnly)
        {
            TargetDetectionSystem detector = FindObjectOfType<TargetDetectionSystem>();
            if (detector != null)
            {
                detector.ProcessBulletHit(other.gameObject, transform.position);
            }

            Debug.Log($"Bala impactó: {other.name}");
        }

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
        ReturnToPool();
    }

    public void Initialize(TouchShootingSystem parent, float life, float speedZ = 0f)
    {
        parentSystem = parent;
        lifetime = life;
        timeAlive = 0f;
        direction = Vector3.forward;
        speed = 10f;
        isVisualOnly = false;
    }
}