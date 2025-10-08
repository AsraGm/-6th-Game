using UnityEngine;

// ARCHIVO: EnemyMovementPatterns.cs
// VERSIÓN ACTUALIZADA con comportamientos específicos por tipo de enemigo

namespace ShootingRange
{
    public enum MovementPattern
    {
        Linear,     // Movimiento recto - NORMAL e INNOCENT
        Zigzag,     // Movimiento en zigzag - ZIGZAG (antes Fast) con retorno prematuro
        Circular,   // Movimiento circular
        Erratic     // Movimiento errático/aleatorio
    }

    public class EnemyMovementPatterns : MonoBehaviour
    {
        [Header("Configuración de Movimiento")]
        [Tooltip("Patrón de movimiento que usará este enemigo")]
        public MovementPattern currentPattern = MovementPattern.Linear;

        [Tooltip("Velocidad base del enemigo")]
        [Range(1f, 10f)]
        public float baseSpeed = 3f;

        [Tooltip("Variación aleatoria de velocidad (±)")]
        [Range(0f, 2f)]
        public float speedVariation = 0.5f;

        [Header("Configuración por Patrón")]
        [Tooltip("Amplitud para movimientos zigzag y circular")]
        [Range(0.5f, 5f)]
        public float amplitude = 2f;

        [Tooltip("Frecuencia de cambios de dirección")]
        [Range(0.5f, 5f)]
        public float frequency = 1f;

        [Tooltip("Dirección inicial de movimiento")]
        public Vector2 initialDirection = Vector2.up;

        [Header("Configuración Dinámica")]
        [Tooltip("Cambiar patrón durante el vuelo")]
        public bool changePatternsInFlight = false;

        [Tooltip("Intervalo para cambio de patrón (segundos)")]
        [Range(1f, 10f)]
        public float patternChangeInterval = 3f;

        [Header("Limites de Pantalla")]
        [Tooltip("Mantener enemigo dentro de límites")]
        public bool keepInBounds = true;

        [Tooltip("Margen desde bordes de pantalla")]
        public float screenMargin = 1f;

        [Header("NUEVOS: Comportamientos Especiales")]
        [Tooltip("Tipo de enemigo (para comportamientos especiales)")]
        public EnemyType enemyType = EnemyType.Normal;

        [Header("ZIGZAG: Retorno Prematuro")]
        [Tooltip("Probabilidad de NO completar ruta (0-1)")]
        [Range(0f, 1f)]
        public float zigzagReturnChance = 0.4f;

        [Tooltip("Progreso mínimo antes de poder regresar")]
        [Range(0.1f, 0.9f)]
        public float zigzagMinProgress = 0.3f;

        [Header("JUMPER: Rotación Continua")]
        [Tooltip("Velocidad de rotación en Z (grados/segundo) - efecto de balanceo")]
        public float jumperRotationSpeed = 180f;
        [Tooltip("Ángulo máximo de balanceo (límites -X a +X grados)")]
        public float jumperMaxAngle = 45f;
        private float jumperCurrentAngle = 0f;
        private float jumperDirection = 1f;

        [Header("VALUABLE: Volteo al Final")]
        [Tooltip("Se voltea 180° al llegar al destino")]
        public bool valuableFlipsAtEnd = true;
        [Tooltip("Duración de la rotación")]
        [Range(0.1f, 1f)]
        public float valuableFlipDuration = 0.3f;
        [Tooltip("Tiempo antes de voltear (segundos)")]
        [Range(1f, 10f)]
        public float valuableFlipTime = 3f;

        // Variables privadas para cálculos
        private Vector2 currentDirection;
        private Vector2 currentVelocity;
        private float currentSpeed;
        private float timeAlive = 0f;
        private float lastPatternChange = 0f;
        private Vector3 startPosition;
        private Camera gameCamera;

        // Variables para patrones específicos
        private float zigzagPhase = 0f;
        private float circularAngle = 0f;
        private Vector2 circularCenter;
        private float erraticChangeTime = 0f;
        private Vector2 erraticDirection;

        // NUEVAS VARIABLES para comportamientos especiales
        private float routeProgress = 0f;
        private bool hasDecidedToReturn = false;
        private bool hasReachedDestination = false;
        private bool hasFlipped = false;
        private Vector3 destinationPoint;

        private Transform cachedTransform;

        void Start()
        {
            InitializeMovement();
        }

        void InitializeMovement()
        {
            cachedTransform = transform;
            gameCamera = Camera.main;

            startPosition = cachedTransform.position;
            currentDirection = initialDirection.normalized;

            currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
            currentSpeed = Mathf.Max(0.5f, currentSpeed);

            SetupPattern(currentPattern);

            BasicEnemy basicEnemy = GetComponent<BasicEnemy>();

            // POR esto:
            if (enemyType == EnemyType.Jumper)
            {
                jumperCurrentAngle = -jumperMaxAngle; // Empieza desde un extremo
                jumperDirection = 1f; // Va hacia el otro extremo
                cachedTransform.rotation = Quaternion.Euler(0, 0, jumperCurrentAngle);
            }

            Debug.Log($"Enemigo inicializado - Tipo: {enemyType}, Patrón: {currentPattern}, Velocidad: {currentSpeed:F1}");
            
        }

        void Update()
        {
            timeAlive += Time.deltaTime;

            if (changePatternsInFlight && timeAlive - lastPatternChange >= patternChangeInterval)
            {
                ChangePatternRandomly();
                lastPatternChange = timeAlive;
            }

            // APLICAR COMPORTAMIENTOS ESPECIALES POR TIPO
            ApplySpecialBehaviors();

            CalculateMovement();
            ApplyMovement();

            if (keepInBounds)
            {
                KeepInScreenBounds();
            }
        }

        void ApplySpecialBehaviors()
        {
            switch (enemyType)
            {
                case EnemyType.ZigZag: // ZIGZAG con retorno prematuro
                    ApplyZigZagBehavior();
                    break;

                case EnemyType.Jumper: // Rotación continua en X
                    ApplyJumperBehavior();
                    break;

                case EnemyType.Valuable: // Volteo al llegar al destino
                    CheckValuableDestination();
                    break;
            }
        }

        // ZIGZAG: Puede NO completar la ruta y regresar antes
        void ApplyZigZagBehavior()
        {
            if (hasDecidedToReturn) return;

            UpdateRouteProgress();

            // Solo decide regresar si ha avanzado suficiente
            if (routeProgress >= zigzagMinProgress)
            {
                // Probabilidad de regresar cada frame
                if (Random.value < zigzagReturnChance * Time.deltaTime * 2f)
                {
                    hasDecidedToReturn = true;
                    Debug.Log($"ZIGZAG decidió regresar en {routeProgress:P0} de la ruta");

                    // Invertir dirección
                    currentDirection = -currentDirection;
                    initialDirection = currentDirection;
                }
            }
        }
        // Método público para rotar rápidamente el Jumper al finalizar oleada
        public void RotateToWarning()
        {
            if (enemyType == EnemyType.Jumper)
            {
                // Para Jumper: rotar rápidamente a 90° en Z (mantiene el eje de balanceo)
                StartCoroutine(QuickRotateJumper());
            }
        }

        System.Collections.IEnumerator QuickRotateJumper()
        {
            float duration = 0.1f; // Rotación rápida
            float elapsed = 0f;
            Quaternion startRotation = cachedTransform.rotation;
            Quaternion targetRotation = Quaternion.Euler(90, 0, 0); // 90° en Z

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                cachedTransform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
                yield return null;
            }

            cachedTransform.rotation = targetRotation;

            // Detener el balanceo
            enabled = false;
        }

        void UpdateRouteProgress()
        {
            // Calcular progreso basado en distancia desde inicio
            float distanceFromStart = Vector3.Distance(startPosition, cachedTransform.position);

            // Estimar distancia total (puedes ajustar este valor según tus rutas)
            float estimatedTotalDistance = 10f;

            routeProgress = Mathf.Clamp01(distanceFromStart / estimatedTotalDistance);
        }
        void ApplyJumperBehavior()
        {
            // Calcular nuevo ángulo
            jumperCurrentAngle += jumperRotationSpeed * jumperDirection * Time.deltaTime;

            // Si llega al límite, invertir dirección
            if (jumperCurrentAngle >= jumperMaxAngle)
            {
                jumperCurrentAngle = jumperMaxAngle;
                jumperDirection = -1f;
            }
            else if (jumperCurrentAngle <= -jumperMaxAngle)
            {
                jumperCurrentAngle = -jumperMaxAngle;
                jumperDirection = 1f;
            }

            // Aplicar rotación
            cachedTransform.rotation = Quaternion.Euler(0, 0, jumperCurrentAngle);
        }

        // VALUABLE: Detecta cuando llega al destino y se voltea
        void CheckValuableDestination()
        {
            if (hasFlipped || !valuableFlipsAtEnd) return;

            if (timeAlive >= valuableFlipTime)
            {
                hasReachedDestination = true;
                StartCoroutine(FlipValuableEnemy());
            }
        }
        System.Collections.IEnumerator FlipValuableEnemy()
        {
            hasFlipped = true;
            Debug.Log($"VALUABLE volteándose 180° - ¡Ya no le pueden disparar!");

            Quaternion startRotation = cachedTransform.rotation;
            Quaternion targetRotation = startRotation * Quaternion.Euler(90, 0, 0);

            float elapsed = 0f;

            while (elapsed < valuableFlipDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / valuableFlipDuration;

                cachedTransform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);

                yield return null;
            }

            cachedTransform.rotation = targetRotation;
        }

        void SetupPattern(MovementPattern pattern)
        {
            currentPattern = pattern;

            switch (pattern)
            {
                case MovementPattern.Linear:
                    break;

                case MovementPattern.Zigzag:
                    zigzagPhase = 0f;
                    break;

                case MovementPattern.Circular:
                    circularCenter = cachedTransform.position;
                    circularAngle = 0f;
                    break;

                case MovementPattern.Erratic:
                    erraticDirection = Random.insideUnitCircle.normalized;
                    erraticChangeTime = 0f;
                    break;
            }
        }

        void CalculateMovement()
        {
            Vector2 movement = Vector2.zero;

            switch (currentPattern)
            {
                case MovementPattern.Linear:
                    movement = CalculateLinearMovement();
                    break;

                case MovementPattern.Zigzag:
                    movement = CalculateZigzagMovement();
                    break;

                case MovementPattern.Circular:
                    movement = CalculateCircularMovement();
                    break;

                case MovementPattern.Erratic:
                    movement = CalculateErraticMovement();
                    break;
            }

            currentVelocity = movement;
        }

        Vector2 CalculateLinearMovement()
        {
            return currentDirection * currentSpeed;
        }

        Vector2 CalculateZigzagMovement()
        {
            zigzagPhase += Time.deltaTime * frequency;

            Vector2 forward = currentDirection;
            Vector2 perpendicular = new Vector2(-forward.y, forward.x);
            float sideMovement = Mathf.Sin(zigzagPhase) * amplitude;

            return (forward + perpendicular * sideMovement) * currentSpeed;
        }

        Vector2 CalculateCircularMovement()
        {
            circularAngle += Time.deltaTime * frequency;

            float x = Mathf.Cos(circularAngle) * amplitude;
            float y = Mathf.Sin(circularAngle) * amplitude;

            Vector3 targetPosition = circularCenter + new Vector2(x, y);
            Vector2 direction = (targetPosition - cachedTransform.position).normalized;

            return direction * currentSpeed;
        }

        Vector2 CalculateErraticMovement()
        {
            erraticChangeTime += Time.deltaTime;

            if (erraticChangeTime >= 1f / frequency)
            {
                erraticDirection = Random.insideUnitCircle.normalized;
                erraticChangeTime = 0f;

                if (Random.value < 0.3f)
                {
                    currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
                    currentSpeed = Mathf.Max(0.5f, currentSpeed);
                }
            }

            return erraticDirection * currentSpeed;
        }

        void ApplyMovement()
        {
            Vector3 newPosition = cachedTransform.position + (Vector3)currentVelocity * Time.deltaTime;
            cachedTransform.position = newPosition;
        }

        void KeepInScreenBounds()
        {
            if (gameCamera == null) return;

            Vector3 viewportPosition = gameCamera.WorldToViewportPoint(cachedTransform.position);
            bool bounced = false;

            if (viewportPosition.x < screenMargin / 10f)
            {
                currentDirection.x = Mathf.Abs(currentDirection.x);
                bounced = true;
            }
            else if (viewportPosition.x > 1f - screenMargin / 10f)
            {
                currentDirection.x = -Mathf.Abs(currentDirection.x);
                bounced = true;
            }

            if (viewportPosition.y < screenMargin / 10f)
            {
                currentDirection.y = Mathf.Abs(currentDirection.y);
                bounced = true;
            }
            else if (viewportPosition.y > 1f - screenMargin / 10f)
            {
                currentDirection.y = -Mathf.Abs(currentDirection.y);
                bounced = true;
            }

            if (bounced)
            {
                currentDirection = currentDirection.normalized;

                if (currentPattern == MovementPattern.Circular)
                {
                    circularCenter = cachedTransform.position;
                }
            }
        }

        void ChangePatternRandomly()
        {
            MovementPattern[] patterns = { MovementPattern.Linear, MovementPattern.Zigzag,
                                         MovementPattern.Circular, MovementPattern.Erratic };

            MovementPattern newPattern;
            do
            {
                newPattern = patterns[Random.Range(0, patterns.Length)];
            } while (newPattern == currentPattern);

            SetupPattern(newPattern);
            Debug.Log($"Patrón cambiado a: {newPattern}");
        }

        public void SetPattern(MovementPattern newPattern)
        {
            SetupPattern(newPattern);
        }

        public void SetSpeed(float newSpeed)
        {
            currentSpeed = Mathf.Max(0.5f, newSpeed);
        }

        public void SetDirection(Vector2 newDirection)
        {
            currentDirection = newDirection.normalized;
        }

        public void PauseMovement()
        {
            enabled = false;
        }

        public void ResumeMovement()
        {
            enabled = true;
        }

        public MovementPattern GetCurrentPattern() => currentPattern;
        public float GetCurrentSpeed() => currentSpeed;
        public Vector2 GetCurrentDirection() => currentDirection;
        public Vector2 GetCurrentVelocity() => currentVelocity;

        [ContextMenu("Change Pattern Randomly")]
        public void DebugChangePattern()
        {
            ChangePatternRandomly();
        }

        void OnDrawGizmos()
        {
            DrawScreenBounds();

            if (!Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)currentDirection * 2f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)currentVelocity);

            if (currentPattern == MovementPattern.Circular)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(circularCenter, amplitude);
            }
        }

        void DrawScreenBounds()
        {
            Camera cam = gameCamera;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            float height = cam.orthographicSize;
            float width = height * cam.aspect;
            Vector3 camPos = cam.transform.position;

            float leftBound = camPos.x - width + screenMargin;
            float rightBound = camPos.x + width - screenMargin;
            float bottomBound = camPos.y - height + screenMargin;
            float topBound = camPos.y + height - screenMargin;

            Gizmos.color = Color.cyan;
            Vector3[] screenCorners = {
                new Vector3(camPos.x - width, camPos.y - height, 0),
                new Vector3(camPos.x + width, camPos.y - height, 0),
                new Vector3(camPos.x + width, camPos.y + height, 0),
                new Vector3(camPos.x - width, camPos.y + height, 0)
            };

            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(screenCorners[i], screenCorners[(i + 1) % 4]);
            }

            if (keepInBounds)
            {
                Gizmos.color = Color.red;
                Vector3[] marginBounds = {
                    new Vector3(leftBound, bottomBound, 0),
                    new Vector3(rightBound, bottomBound, 0),
                    new Vector3(rightBound, topBound, 0),
                    new Vector3(leftBound, topBound, 0)
                };

                for (int i = 0; i < 4; i++)
                {
                    Gizmos.DrawLine(marginBounds[i], marginBounds[(i + 1) % 4]);
                }
            }
        }
    }
}