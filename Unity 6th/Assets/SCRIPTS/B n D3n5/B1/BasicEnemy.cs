using UnityEngine;
using UnityEngine.Audio; // NUEVO

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

        [Tooltip("Volumen del sonido - PUEDES PONER MÁS DE 1 para que suene más fuerte")]
        [Range(0f, 10f)] // 🔥 RANGO AMPLIADO hasta 10
        public float soundVolume = 3f; // 🔥 VALOR POR DEFECTO EN 3

        [Tooltip("(Opcional) Audio Mixer Group para efectos de sonido")]
        public AudioMixerGroup sfxMixerGroup; // 🔥 NUEVO

        [Header("⏱️ Timing")]
        [Tooltip("Tiempo antes de retornar al pool después de morir")]
        public float returnToPoolDelay = 0.3f;

        [Tooltip("Tiempo que durarán las partículas antes de destruirse")]
        public float particleLifetime = 2f;

        // Referencias internas
        private SpriteRenderer spriteRenderer;
        private Vector3 originalScale;
        private EnemyType originalEnemyType;
        private bool isDying = false;

        void Awake()
        {
            originalScale = transform.localScale;
            originalEnemyType = enemyType;
            spriteRenderer = GetComponent<SpriteRenderer>();

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public void OnHit(ObjectType objectType, int scoreValue)
        {
            if (isDying) return;

            if (StatsTracker.Instance != null && enemyType != EnemyType.Innocent)
            {
                StatsTracker.Instance.AddEnemyKilled();
            }

            Debug.Log($"💥 {name} ({enemyType}) fue disparado! Puntos: {scoreValue}");

            isDying = true;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }

            EnemyMovementPatterns movement = GetComponent<EnemyMovementPatterns>();
            if (movement != null)
            {
                movement.PauseMovement();
            }

            SpawnHitParticles();
            PlayHitSound();

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            PlayThemeSpecificEffects();
            HandleDestruction();
        }

        void SpawnHitParticles()
        {
            if (hitParticlesPrefab == null)
            {
                Debug.LogWarning($"⚠️ No hay prefab de partículas asignado en {name}");
                return;
            }

            GameObject particlesObj = Instantiate(hitParticlesPrefab, transform.position, Quaternion.identity);
            Debug.Log($"✅ Partículas instanciadas en {transform.position}");

            ParticleSystem ps = particlesObj.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = particlesObj.GetComponentInChildren<ParticleSystem>();
            }

            if (ps != null)
            {
                ps.Play();
                Debug.Log($"🎆 ParticleSystem reproduciendo");
            }
            else
            {
                Debug.LogWarning($"⚠️ El prefab {hitParticlesPrefab.name} no tiene ParticleSystem");
            }

            Destroy(particlesObj, particleLifetime);
        }

        void PlayHitSound()
        {
            if (hitSound == null)
            {
                Debug.LogWarning($"⚠️ No hay AudioClip asignado en {name}");
                return;
            }

            // 🔥 NUEVO: Usar SFXManager para mejor control de volumen
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlaySoundOneShot(hitSound, soundVolume);
                Debug.Log($"🔊 Audio reproducido con SFXManager - Volumen: {soundVolume}");
            }
            else
            {
                // Fallback al método original
                AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
                Debug.Log($"🔊 Audio reproducido en {transform.position}");
            }
        }

        public EnemyType GetEnemyType()
        {
            return enemyType;
        }

        public string GetThemeID()
        {
            return themeID;
        }

        public void OnSpawnFromPool()
        {
            IsActiveInPool = true;
            gameObject.SetActive(true);
            ResetEnemyState();
        }

        public void OnReturnToPool()
        {
            IsActiveInPool = false;
            gameObject.SetActive(false);
        }

        void ResetEnemyState()
        {
            isDying = false;
            transform.localScale = originalScale;
            enemyType = originalEnemyType;

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = Color.white;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = true;
            }

            EnemyMovementPatterns movement = GetComponent<EnemyMovementPatterns>();
            if (movement != null)
            {
                movement.ResumeMovement();
            }
        }

        void OnEnable()
        {
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.RegisterEnemy(this);
            }
        }

        void OnDisable()
        {
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.UnregisterEnemy(this);
            }
        }

        void PlayThemeSpecificEffects()
        {
            Debug.Log($"Aplicando efectos del tema: {themeID}");
        }

        void HandleDestruction()
        {
            if (GetComponent<IPoolable>() != null)
            {
                Invoke(nameof(ReturnToPoolDelayed), returnToPoolDelay);
            }
            else
            {
                Destroy(gameObject, returnToPoolDelay);
            }
        }

        void ReturnToPoolDelayed()
        {
            OnReturnToPool();
        }

        public void ConfigureEnemy(EnemyType type, string theme)
        {
            enemyType = type;
            originalEnemyType = type;
            themeID = theme;
        }

        [ContextMenu("🧪 Test Hit Effects")]
        void TestHitEffects()
        {
            Debug.Log("=== TESTING HIT EFFECTS ===");
            SpawnHitParticles();
            PlayHitSound();
        }

        void OnValidate()
        {
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
            // 🔥 ELIMINADO: soundVolume = Mathf.Clamp01(soundVolume);
            // Ahora puede ser mayor a 1
        }
    }
}