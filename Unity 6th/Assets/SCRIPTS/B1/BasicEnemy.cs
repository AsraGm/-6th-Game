using UnityEngine;

namespace ShootingRange
{
    public class BasicEnemy : MonoBehaviour, IShootable, IPoolable
    {
        [Header("ConfiguraciÃ³n del Enemigo")]
        [Tooltip("Tipo de este enemigo (afecta puntuaciÃ³n)")]
        public EnemyType enemyType = EnemyType.Normal;

        [Header("ConfiguraciÃ³n de Tema")]
        [Tooltip("ID del tema para efectos visuales (CONEXIÃ“N CON LISTA C2)")]
        public string themeID = "default";

        [Header("Pool Settings")]
        [Tooltip("Estado interno del pool - no modificar manualmente")]
        public bool IsActiveInPool { get; set; } = false;

        [Header("Efectos Visuales")]
        [Tooltip("PartÃ­culas que se reproducen al ser disparado")]
        public ParticleSystem hitParticles;

        [Tooltip("Sonido que se reproduce al ser disparado")]
        public AudioClip hitSound;

        private AudioSource audioSource;

        void Start()
        {
            // Configurar componentes
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && hitSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Asegurar que tenga un collider configurado como trigger
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        // ImplementaciÃ³n de IShootable
        public void OnHit(ObjectType objectType, int scoreValue)
        {
            Debug.Log($"{name} ({enemyType}) fue disparado! Puntos: {scoreValue}");

            // Reproducir efectos visuales
            PlayHitEffects();

            // PLACEHOLDER: Efectos especÃ­ficos segÃºn tema (CONEXIÃ“N CON LISTA C2)
            PlayThemeSpecificEffects();

            // Retornar al pool o destruir
            HandleDestruction();
        }

        public EnemyType GetEnemyType()
        {
            return enemyType;
        }

        public string GetThemeID()
        {
            return themeID; // CONEXIÃ“N CON LISTA C2
        }

        // ImplementaciÃ³n de IPoolable (CONEXIÃ“N CON LISTA B1)
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

            // Limpiar estado del enemigo
            CleanupEnemyState();
        }

        void ResetEnemyState()
        {
            // Resetear cualquier animaciÃ³n, posiciÃ³n, etc.
            transform.localScale = Vector3.one;

            // Resetear componentes visuales
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white;
            }
        }

        void CleanupEnemyState()
        {
            // Detener partÃ­culas si estÃ¡n reproduciÃ©ndose
            if (hitParticles != null && hitParticles.isPlaying)
            {
                hitParticles.Stop();
            }

            // Detener sonidos
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        void PlayHitEffects()
        {
            // Reproducir partÃ­culas
            if (hitParticles != null)
            {
                hitParticles.Play();
            }

            // Reproducir sonido
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }

            // Efecto visual simple: parpadeo
            StartCoroutine(FlashEffect());
        }

        System.Collections.IEnumerator FlashEffect()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color originalColor = sr.color;
                sr.color = Color.red;
                yield return new UnityEngine.WaitForSeconds(0.1f);
                sr.color = originalColor;
            }
        }

        void PlayThemeSpecificEffects()
        {
            // PLACEHOLDER: Efectos especÃ­ficos por tema (CONEXIÃ“N CON LISTA C2)
            // AquÃ­ se aplicarÃ¡n diferentes efectos segÃºn el tema activo
            // Por ejemplo:
            // - Tema Western: polvo y sonido de campana
            // - Tema Sci-Fi: chispas elÃ©ctricas y sonido lÃ¡ser
            // - Tema Medieval: sangre y sonido de metal

            Debug.Log($"Aplicando efectos del tema: {themeID}");
        }

        void HandleDestruction()
        {
            // Si usa pooling, retornar al pool despuÃ©s de un pequeÃ±o delay
            if (GetComponent<IPoolable>() != null)
            {
                Invoke(nameof(ReturnToPoolDelayed), 0.5f);
            }
            else
            {
                // Destruir despuÃ©s de un delay para que se vean los efectos
                Destroy(gameObject, 0.5f);
            }
        }

        void ReturnToPoolDelayed()
        {
            OnReturnToPool();
        }

        // MÃ©todo para configurar el enemigo desde el sistema de spawn
        public void ConfigureEnemy(EnemyType type, string theme)
        {
            enemyType = type;
            themeID = theme;
        }
    }
}