using UnityEngine;

// ARCHIVO: EnemyMovementPatterns.cs
// Patrones de movimiento variables para enemigos estilo RE4 Remake (B2)

namespace ShootingRange
{
    // ENUM para tipos de movimiento
    public enum MovementPattern
    {
        Linear,     // Movimiento recto
        Zigzag,     // Movimiento en zigzag
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

        // Cache de componentes para optimización
        private Transform cachedTransform;

        void Start()
        {
            InitializeMovement();
        }

        void InitializeMovement()
        {
            // Cache de componentes
            cachedTransform = transform;
            gameCamera = Camera.main;

            // Configuración inicial
            startPosition = cachedTransform.position;
            currentDirection = initialDirection.normalized;

            // Velocidad inicial con variación
            currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
            currentSpeed = Mathf.Max(0.5f, currentSpeed); // Mínimo de velocidad

            // Configurar patrón inicial
            SetupPattern(currentPattern);

            Debug.Log($"Enemigo inicializado - Patrón: {currentPattern}, Velocidad: {currentSpeed:F1}");
        }

        void Update()
        {
            timeAlive += Time.deltaTime;

            // Cambiar patrón en vuelo si está habilitado
            if (changePatternsInFlight && timeAlive - lastPatternChange >= patternChangeInterval)
            {
                ChangePatternRandomly();
                lastPatternChange = timeAlive;
            }

            // Calcular movimiento según patrón actual
            CalculateMovement();

            // Aplicar movimiento
            ApplyMovement();

            // Mantener en límites de pantalla
            if (keepInBounds)
            {
                KeepInScreenBounds();
            }
        }

        void SetupPattern(MovementPattern pattern)
        {
            currentPattern = pattern;

            switch (pattern)
            {
                case MovementPattern.Linear:
                    // Sin configuración adicional necesaria
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
            // Avanzar la fase del zigzag
            zigzagPhase += Time.deltaTime * frequency;

            // Dirección base (hacia adelante)
            Vector2 forward = currentDirection;

            // Componente lateral (zigzag)
            Vector2 perpendicular = new Vector2(-forward.y, forward.x); // Perpendicular a forward
            float sideMovement = Mathf.Sin(zigzagPhase) * amplitude;

            return (forward + perpendicular * sideMovement) * currentSpeed;
        }

        Vector2 CalculateCircularMovement()
        {
            // Incrementar ángulo
            circularAngle += Time.deltaTime * frequency;

            // Calcular posición en círculo
            float x = Mathf.Cos(circularAngle) * amplitude;
            float y = Mathf.Sin(circularAngle) * amplitude;

            Vector3 targetPosition = circularCenter + new Vector2(x, y);
            Vector2 direction = (targetPosition - cachedTransform.position).normalized;

            return direction * currentSpeed;
        }

        Vector2 CalculateErraticMovement()
        {
            erraticChangeTime += Time.deltaTime;

            // Cambiar dirección aleatoriamente cada cierto tiempo
            if (erraticChangeTime >= 1f / frequency)
            {
                erraticDirection = Random.insideUnitCircle.normalized;
                erraticChangeTime = 0f;

                // Ocasionalmente cambiar velocidad también
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
            // Mover usando cached transform para mejor rendimiento
            Vector3 newPosition = cachedTransform.position + (Vector3)currentVelocity * Time.deltaTime;
            cachedTransform.position = newPosition;
        }

        void KeepInScreenBounds()
        {
            if (gameCamera == null) return;

            Vector3 viewportPosition = gameCamera.WorldToViewportPoint(cachedTransform.position);
            bool bounced = false;

            // Verificar límites y rebotar si es necesario
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

            // Si rebotó, normalizar dirección
            if (bounced)
            {
                currentDirection = currentDirection.normalized;

                // Para circular, actualizar centro
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
            } while (newPattern == currentPattern); // Evitar el mismo patrón

            SetupPattern(newPattern);
            Debug.Log($"Patrón cambiado a: {newPattern}");
        }

        // MÉTODOS PÚBLICOS PARA CONTROL EXTERNO

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

        // GETTERS PARA INFORMACIÓN
        public MovementPattern GetCurrentPattern() => currentPattern;
        public float GetCurrentSpeed() => currentSpeed;
        public Vector2 GetCurrentDirection() => currentDirection;
        public Vector2 GetCurrentVelocity() => currentVelocity;

        // MÉTODOS DE DEBUG
        [ContextMenu("Change Pattern Randomly")]
        public void DebugChangePattern()
        {
            ChangePatternRandomly();
        }

        [ContextMenu("Set Linear Pattern")]
        public void DebugSetLinear()
        {
            SetupPattern(MovementPattern.Linear);
        }

        [ContextMenu("Set Zigzag Pattern")]
        public void DebugSetZigzag()
        {
            SetupPattern(MovementPattern.Zigzag);
        }

        [ContextMenu("Set Circular Pattern")]
        public void DebugSetCircular()
        {
            SetupPattern(MovementPattern.Circular);
        }

        [ContextMenu("Set Erratic Pattern")]
        public void DebugSetErratic()
        {
            SetupPattern(MovementPattern.Erratic);
        }

        // Visualización de debug en Scene View
        void OnDrawGizmos()
        {
            // Mostrar límites de pantalla siempre (incluso cuando no está en Play)
            DrawScreenBounds();

            if (!Application.isPlaying) return;

            // Mostrar dirección actual
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)currentDirection * 2f);

            // Mostrar velocidad actual
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)currentVelocity);

            // Para patrón circular, mostrar centro
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

            // Calcular límites de pantalla
            float height = cam.orthographicSize;
            float width = height * cam.aspect;
            Vector3 camPos = cam.transform.position;

            // Límites con margen
            float leftBound = camPos.x - width + screenMargin;
            float rightBound = camPos.x + width - screenMargin;
            float bottomBound = camPos.y - height + screenMargin;
            float topBound = camPos.y + height - screenMargin;

            // Dibujar límites de pantalla (externos)
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

            // Dibujar límites con margen (donde rebotan los enemigos)
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

                // Mostrar zona de margen
                Gizmos.color = new Color(1f, 0f, 0f, 0.1f);

                // Líneas de margen
                Gizmos.DrawLine(screenCorners[0], marginBounds[0]); // Bottom-left
                Gizmos.DrawLine(screenCorners[1], marginBounds[1]); // Bottom-right
                Gizmos.DrawLine(screenCorners[2], marginBounds[2]); // Top-right
                Gizmos.DrawLine(screenCorners[3], marginBounds[3]); // Top-left
            }
        }
    }
}