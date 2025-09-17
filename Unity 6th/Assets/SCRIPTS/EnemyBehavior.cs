using UnityEngine;
using System.Collections;

// B2: Comportamientos dinámicos de enemigos estilo RE4 Remake
public class EnemyBehavior : MonoBehaviour
{
    [System.Serializable]
    public enum MovementPattern
    {
        Linear,
        Zigzag,
        Circular,
        Erratic,
        Jumper
    }
    
    [Header("Movement Settings")]
    [SerializeField] private MovementPattern movementPattern = MovementPattern.Linear;
    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private float speedVariation = 0.5f;
    
    [Header("Pattern Specific Settings")]
    [SerializeField] private float zigzagAmplitude = 1f;
    [SerializeField] private float zigzagFrequency = 2f;
    [SerializeField] private float circularRadius = 1f;
    [SerializeField] private float erraticChangeInterval = 0.5f;
    
    [Header("Optimization")]
    [SerializeField] private bool useTransformCaching = true;
    
    // Cached transform para optimización
    private Transform cachedTransform;
    
    // Movement variables
    private Vector3 targetPosition;
    private Vector3 currentDirection;
    private float currentSpeed;
    private float movementTimer;
    private Vector3 startPosition;
    private bool isActive = false;
    
    // Pattern specific variables
    private float zigzagTimer;
    private float circularAngle;
    private Vector3 circularCenter;
    private Vector3 erraticDirection;
    private float erraticTimer;
    
    private void Awake()
    {
        // Cache transform para optimización
        if (useTransformCaching)
            cachedTransform = transform;
    }
    
    public void OnSpawn()
    {
        isActive = true;
        InitializeMovement();
        StartCoroutine(MovementCoroutine());
    }
    
    public void OnReturn()
    {
        isActive = false;
        StopAllCoroutines();
    }
    
    private void InitializeMovement()
    {
        startPosition = GetTransform().position;
        
        // Velocidad variable
        currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        
        // Inicializar según el patrón
        switch (movementPattern)
        {
            case MovementPattern.Linear:
                InitializeLinear();
                break;
            case MovementPattern.Zigzag:
                InitializeZigzag();
                break;
            case MovementPattern.Circular:
                InitializeCircular();
                break;
            case MovementPattern.Erratic:
                InitializeErratic();
                break;
            case MovementPattern.Jumper:
                InitializeJumper();
                break;
        }
    }
    
    private void InitializeLinear()
    {
        // Dirección aleatoria con variación
        float angle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
        currentDirection = new Vector3(Mathf.Sin(angle), -Mathf.Abs(Mathf.Cos(angle)), 0).normalized;
    }
    
    private void InitializeZigzag()
    {
        currentDirection = Vector3.down;
        zigzagTimer = 0f;
    }
    
    private void InitializeCircular()
    {
        circularCenter = startPosition + Vector3.down * 2f;
        circularAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
    }
    
    private void InitializeErratic()
    {
        erraticDirection = Random.insideUnitCircle.normalized;
        erraticTimer = 0f;
    }
    
    private void InitializeJumper()
    {
        // Jumper tendrá movimiento especial
        currentDirection = Vector3.down;
    }
    
    private IEnumerator MovementCoroutine()
    {
        while (isActive && GetTransform().position.y > -10f) // Límite de pantalla
        {
            UpdateMovement();
            yield return new WaitForFixedUpdate(); // Usar FixedUpdate para movimiento suave
        }
        
        // Si sale de pantalla, retornar al pool
        if (isActive)
        {
            GetComponent<Enemy>()?.OnReturnToPool();
        }
    }
    
    private void UpdateMovement()
    {
        movementTimer += Time.fixedDeltaTime;
        Vector3 movement = Vector3.zero;
        
        switch (movementPattern)
        {
            case MovementPattern.Linear:
                movement = UpdateLinearMovement();
                break;
            case MovementPattern.Zigzag:
                movement = UpdateZigzagMovement();
                break;
            case MovementPattern.Circular:
                movement = UpdateCircularMovement();
                break;
            case MovementPattern.Erratic:
                movement = UpdateErraticMovement();
                break;
            case MovementPattern.Jumper:
                movement = UpdateJumperMovement();
                break;
        }
        
        // Aplicar movimiento
        GetTransform().position += movement * Time.fixedDeltaTime;
        
        // Cambiar velocidad ocasionalmente para variación
        if (Random.Range(0f, 1f) < 0.01f) // 1% de chance por frame
        {
            currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        }
    }
    
    private Vector3 UpdateLinearMovement()
    {
        // Pequeñas variaciones en la dirección
        if (Random.Range(0f, 1f) < 0.005f) // 0.5% de chance
        {
            float angle = Random.Range(-15f, 15f) * Mathf.Deg2Rad;
            Vector3 variation = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0) * 0.1f;
            currentDirection = (currentDirection + variation).normalized;
        }
        
        return currentDirection * currentSpeed;
    }
    
    private Vector3 UpdateZigzagMovement()
    {
        zigzagTimer += Time.fixedDeltaTime;
        
        float horizontalOffset = Mathf.Sin(zigzagTimer * zigzagFrequency) * zigzagAmplitude;
        Vector3 zigzagMovement = Vector3.down + Vector3.right * horizontalOffset;
        
        return zigzagMovement.normalized * currentSpeed;
    }
    
    private Vector3 UpdateCircularMovement()
    {
        circularAngle += currentSpeed * Time.fixedDeltaTime;
        
        Vector3 circularPosition = circularCenter + new Vector3(
            Mathf.Cos(circularAngle) * circularRadius,
            Mathf.Sin(circularAngle) * circularRadius,
            0
        );
        
        // Mover el centro hacia abajo gradualmente
        circularCenter += Vector3.down * currentSpeed * 0.3f * Time.fixedDeltaTime;
        
        return (circularPosition - GetTransform().position).normalized * currentSpeed;
    }
    
    private Vector3 UpdateErraticMovement()
    {
        erraticTimer += Time.fixedDeltaTime;
        
        // Cambiar dirección erraticamente
        if (erraticTimer >= erraticChangeInterval)
        {
            erraticDirection = Random.insideUnitCircle.normalized;
            erraticTimer = 0f;
        }
        
        // Mezclar movimiento errático con tendencia hacia abajo
        Vector3 combinedDirection = (erraticDirection + Vector3.down * 2f).normalized;
        return combinedDirection * currentSpeed;
    }
    
    private Vector3 UpdateJumperMovement()
    {
        // Movimiento tipo "salto" - pausas y ráfagas de movimiento
        float jumpCycle = Mathf.Sin(movementTimer * 3f);
        
        if (jumpCycle > 0.5f)
        {
            // Fase de salto rápido
            return currentDirection * currentSpeed * 2f;
        }
        else if (jumpCycle > -0.5f)
        {
            // Fase de movimiento normal
            return currentDirection * currentSpeed;
        }
        else
        {
            // Fase de pausa
            return Vector3.zero;
        }
    }
    
    // Método optimizado para obtener transform
    private Transform GetTransform()
    {
        return useTransformCaching ? cachedTransform : transform;
    }
    
    // Métodos públicos para configuración dinámica
    public void SetMovementPattern(MovementPattern pattern)
    {
        movementPattern = pattern;
        if (isActive)
        {
            InitializeMovement();
        }
    }
    
    public void SetSpeed(float newSpeed)
    {
        baseSpeed = newSpeed;
        currentSpeed = newSpeed;
    }
    
    public void AddSpeedVariation(float variation)
    {
        currentSpeed += variation;
        if (currentSpeed < 0.1f) currentSpeed = 0.1f;
    }
}