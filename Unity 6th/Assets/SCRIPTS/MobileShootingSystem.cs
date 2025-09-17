using UnityEngine;
using System.Collections;

public class MobileShootingSystem : MonoBehaviour
{
    [Header("Touch Settings")]
    [SerializeField] private float deadzoneRadius = 0.5f; // Radio del deadzone para evitar disparos accidentales
    [SerializeField] private LayerMask shootableLayerMask = -1; // Capas que se pueden disparar
    
    [Header("Feedback")]
    [SerializeField] private bool enableHapticFeedback = true;
    
    [Header("Projectile Pool")]
    [SerializeField] private GameObject projectileVisualPrefab;
    [SerializeField] private int poolSize = 20;
    
    private Camera playerCamera;
    private ProjectilePool projectilePool;
    private Vector2 lastTouchPosition;
    private bool canShoot = true;
    
    private void Start()
    {
        // Obtener referencia de la cámara
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
        
        // Inicializar pool de proyectiles
        projectilePool = new ProjectilePool(projectileVisualPrefab, poolSize, transform);
    }
    
    private void Update()
    {
        HandleTouchInput();
    }
    
    private void HandleTouchInput()
    {
        // Para dispositivos móviles - Touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                ProcessShoot(touch.position);
            }
        }
        
        // Para testing en editor - Mouse input
        #if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            ProcessShoot(Input.mousePosition);
        }
        #endif
    }
    
    private void ProcessShoot(Vector2 screenPosition)
    {
        if (!canShoot) return;
        
        // Verificar deadzone - evitar disparos accidentales
        if (IsInDeadzone(screenPosition)) return;
        
        // Crear raycast desde la cámara hacia la posición del toque
        Ray shootRay = playerCamera.ScreenPointToRay(screenPosition);
        
        // Realizar raycast optimizado con layer mask
        RaycastHit2D hit = Physics2D.GetRayIntersection(shootRay, Mathf.Infinity, shootableLayerMask);
        
        // Crear efecto visual del proyectil
        CreateProjectileVisual(shootRay.origin, hit.point != Vector2.zero ? hit.point : shootRay.GetPoint(10f));
        
        // Si impactó algo, procesarlo
        if (hit.collider != null)
        {
            ProcessHit(hit);
        }
        
        // Feedback háptico
        if (enableHapticFeedback)
        {
            Handheld.Vibrate();
        }
        
        lastTouchPosition = screenPosition;
    }
    
    private bool IsInDeadzone(Vector2 screenPosition)
    {
        // Convertir deadzone a píxeles basado en la resolución de pantalla
        float deadzonePixels = deadzoneRadius * Screen.dpi / 2.54f; // Convertir cm a píxeles
        
        // Verificar si está muy cerca del último toque (para evitar múltiples disparos)
        if (Vector2.Distance(screenPosition, lastTouchPosition) < deadzonePixels)
        {
            return true;
        }
        
        return false;
    }
    
    private void ProcessHit(RaycastHit2D hit)
    {
        // Buscar componente IShootable en el objeto impactado
        IShootable shootable = hit.collider.GetComponent<IShootable>();
        if (shootable != null)
        {
            // Determinar tipo de objeto basado en tag o componente
            ObjectType objectType = DetermineObjectType(hit.collider.gameObject);
            int value = DetermineValue(hit.collider.gameObject);
            
            shootable.OnHit(objectType, value);
        }
    }
    
    private ObjectType DetermineObjectType(GameObject target)
    {
        // Usar sistema de tags optimizado para identificación rápida
        if (target.CompareTag("Enemy"))
            return ObjectType.Enemy;
        else if (target.CompareTag("Innocent"))
            return ObjectType.Innocent;
        
        // Fallback usando componente
        Enemy enemyComponent = target.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            return enemyComponent.IsInnocent ? ObjectType.Innocent : ObjectType.Enemy;
        }
        
        return ObjectType.Enemy; // Default
    }
    
    private int DetermineValue(GameObject target)
    {
        Enemy enemyComponent = target.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            return enemyComponent.MoneyValue;
        }
        
        return 10; // Valor default
    }
    
    private void CreateProjectileVisual(Vector3 startPos, Vector3 endPos)
    {
        GameObject projectile = projectilePool.GetProjectile();
        if (projectile != null)
        {
            ProjectileVisual visual = projectile.GetComponent<ProjectileVisual>();
            visual.FireProjectile(startPos, endPos);
        }
    }
    
    public void SetCanShoot(bool canShoot)
    {
        this.canShoot = canShoot;
    }
}