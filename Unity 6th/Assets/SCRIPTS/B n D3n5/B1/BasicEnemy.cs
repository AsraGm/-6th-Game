using UnityEngine;

namespace ShootingRange
{
    public class BasicEnemy : MonoBehaviour, IShootable, IPoolable
    {
        [Header("Configuración del Enemigo")]
        [Tooltip("Tipo de este enemigo (afecta puntuación)")]
        public EnemyType enemyType = EnemyType.Normal;

        [Header("Configuración de Tema")]
        [Tooltip("ID del tema para efectos visuales")]
        [HideInInspector] public string themeID = "default";

        [Header("Pool Settings")]
        [Tooltip("Estado interno del pool - no modificar manualmente")]
        public bool IsActiveInPool { get; set; } = false;

        [Header("💥 Efectos al Morir")]
        [Tooltip("Prefab de partículas que se INSTANCIA al morir")]
        public GameObject hitParticlesPrefab;

        [Tooltip("Sonido que se reproduce al ser disparado")]
        public AudioClip hitSound;

        [Tooltip("Volumen del sonido (0-1)")]
        [Range(0f, 1f)]
        public float soundVolume = 1f;

        [Header("⏱️ Timing")]
        [Tooltip("Tiempo antes de retornar al pool después de morir")]
        public float returnToPoolDelay = 0.3f;

        [Tooltip("Tiempo que durarán las partículas antes de destruirse")]
        public float particleLifetime = 2f;

        // Referencias internas
        private SpriteRenderer spriteRenderer;

        // GUARDAR configuración original del prefab
        private Vector3 originalScale;
        private EnemyType originalEnemyType;

        // Control de estado
        private bool isDying = false;

        void Awake()
        {
            // GUARDAR configuración original
            originalScale = transform.localScale;
            originalEnemyType = enemyType;

            // Obtener SpriteRenderer
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Asegurar que tenga un collider configurado como trigger
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public void OnHit(ObjectType objectType, int scoreValue)
        {
            // Evitar múltiples hits mientras está muriendo
            if (isDying) return;

            if (StatsTracker.Instance != null && enemyType != EnemyType.Innocent)
            {
                StatsTracker.Instance.AddEnemyKilled();
            }

            Debug.Log($"💥 {name} ({enemyType}) fue disparado! Puntos: {scoreValue}");

            // 🎯 MARCAR COMO MURIENDO
            isDying = true;

            // 🔫 Desactivar collider para evitar más hits
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }

            // 🎭 Pausar movimiento
            EnemyMovementPatterns movement = GetComponent<EnemyMovementPatterns>();
            if (movement != null)
            {
                movement.PauseMovement();
            }

            // 💥💥💥 INSTANCIAR PARTÍCULAS EN EL MUNDO (INDEPENDIENTES)
            SpawnHitParticles();

            // 🔊 Reproducir sonido
            PlayHitSound();

            // 🎨 Ocultar el sprite INMEDIATAMENTE
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            // 🎨 Efectos específicos según tema
            PlayThemeSpecificEffects();

            // ⏱️ Retornar al pool rápidamente
            HandleDestruction();
        }

        void SpawnHitParticles()
        {
            if (hitParticlesPrefab == null)
            {
                Debug.LogWarning($"⚠️ No hay prefab de partículas asignado en {name}");
                return;
            }

            // 🌟 INSTANCIAR partículas en la posición del enemigo
            GameObject particlesObj = Instantiate(hitParticlesPrefab, transform.position, Quaternion.identity);

            Debug.Log($"✅ Partículas instanciadas en {transform.position}");

            // Buscar el ParticleSystem en el objeto instanciado
            ParticleSystem ps = particlesObj.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = particlesObj.GetComponentInChildren<ParticleSystem>();
            }

            if (ps != null)
            {
                // Reproducir las partículas
                ps.Play();
                Debug.Log($"🎆 ParticleSystem reproduciendo");
            }
            else
            {
                Debug.LogWarning($"⚠️ El prefab {hitParticlesPrefab.name} no tiene ParticleSystem");
            }

            // 🗑️ Destruir el objeto de partículas después de X segundos
            Destroy(particlesObj, particleLifetime);
        }

        void PlayHitSound()
        {
            if (hitSound == null)
            {
                Debug.LogWarning($"⚠️ No hay AudioClip asignado en {name}");
                return;
            }

            // 🔊 Reproducir sonido en la posición del enemigo (3D espacial)
            AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);

            Debug.Log($"🔊 Audio reproducido en {transform.position}");
        }

        public EnemyType GetEnemyType()
        {
            return enemyType;
        }

        public string GetThemeID()
        {
            return themeID;
        }

        // Implementación de IPoolable
        public void OnSpawnFromPool()
        {
            IsActiveInPool = true;
            gameObject.SetActive(true);

            // Resetear estado del enemigo
            ResetEnemyState();
        }

        public void OnReturnToPool()
        {
            IsActiveInPool = false;
            gameObject.SetActive(false);
        }

        void ResetEnemyState()
        {
            // 🔄 Resetear estado de muerte
            isDying = false;

            // RESPETAR escala original del prefab
            transform.localScale = originalScale;

            // RESPETAR tipo original del prefab
            enemyType = originalEnemyType;

            // 👁️ Reactivar sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = Color.white;
            }

            // ✅ Reactivar collider
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = true;
            }

            // 🎬 Reactivar movimiento
            EnemyMovementPatterns movement = GetComponent<EnemyMovementPatterns>();
            if (movement != null)
            {
                movement.ResumeMovement();
            }
        }

        void OnEnable()
        {
            // Registrar enemigo con ThemeManager para aplicar skin
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.RegisterEnemy(this);
            }
        }

        void OnDisable()
        {
            // Desregistrar del ThemeManager
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.UnregisterEnemy(this);
            }
        }

        void PlayThemeSpecificEffects()
        {
            // PLACEHOLDER: Efectos específicos por tema
            Debug.Log($"Aplicando efectos del tema: {themeID}");
        }

        void HandleDestruction()
        {
            // Si usa pooling, retornar al pool rápidamente
            if (GetComponent<IPoolable>() != null)
            {
                Invoke(nameof(ReturnToPoolDelayed), returnToPoolDelay);
            }
            else
            {
                // Destruir después del delay
                Destroy(gameObject, returnToPoolDelay);
            }
        }

        void ReturnToPoolDelayed()
        {
            OnReturnToPool();
        }

        // Método para configurar el enemigo desde el sistema de spawn
        public void ConfigureEnemy(EnemyType type, string theme)
        {
            enemyType = type;
            originalEnemyType = type; // Actualizar también el original
            themeID = theme;
        }

        // 🛠️ MÉTODO DE DEBUG para probar efectos
        [ContextMenu("🧪 Test Hit Effects")]
        void TestHitEffects()
        {
            Debug.Log("=== TESTING HIT EFFECTS ===");
            SpawnHitParticles();
            PlayHitSound();
        }

        // 📊 Información de debug en Inspector
        void OnValidate()
        {
            // Validar configuración
            if (hitParticlesPrefab != null)
            {
                ParticleSystem ps = hitParticlesPrefab.GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    ps = hitParticlesPrefab.GetComponentInChildren<ParticleSystem>();
                }

                if (ps == null)
                {
                    Debug.LogWarning($"⚠️ El prefab {hitParticlesPrefab.name} no tiene ParticleSystem");
                }
            }

            returnToPoolDelay = Mathf.Max(0.1f, returnToPoolDelay);
            particleLifetime = Mathf.Max(0.5f, particleLifetime);
            soundVolume = Mathf.Clamp01(soundVolume);
        }
    }
}