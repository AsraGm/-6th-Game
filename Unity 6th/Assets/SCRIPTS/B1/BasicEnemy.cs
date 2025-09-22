using UnityEngine;

// ARCHIVO: BasicEnemy.cs - ACTUALIZADO PARA WAVE SYSTEM B3
// Ejemplo de implementación de enemigo básico con conexión al WaveSystem

namespace ShootingRange
{
    public class BasicEnemy : MonoBehaviour, IShootable, IPoolable
    {
        [Header("Configuración del Enemigo")]
        [Tooltip("Tipo de este enemigo (afecta puntuación)")]
        public EnemyType enemyType = EnemyType.Normal;

        [Header("Configuración de Tema")]
        [Tooltip("ID del tema para efectos visuales (CONEXIÓN CON LISTA C2)")]
        public string themeID = "default";

        [Header("Pool Settings")]
        [Tooltip("Estado interno del pool - no modificar manualmente")]
        public bool IsActiveInPool { get; set; } = false;

        [Header("Efectos Visuales")]
        [Tooltip("Partículas que se reproducen al ser disparado")]
        public ParticleSystem hitParticles;

        [Tooltip("Sonido que se reproduce al ser disparado")]
        public AudioClip hitSound;

        private AudioSource audioSource;

        // NUEVA CONEXIÓN CON WAVE SYSTEM
        private WaveSystem waveSystem;

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

            // CONECTAR CON WAVE SYSTEM
            if (waveSystem == null)
            {
                waveSystem = FindObjectOfType<WaveSystem>();
            }
        }

        // Implementación de IShootable
        public void OnHit(ObjectType objectType, int scoreValue)
        {
            Debug.Log($"{name} ({enemyType}) fue disparado! Puntos: {scoreValue}");

            // Reproducir efectos visuales
            PlayHitEffects();

            // PLACEHOLDER: Efectos específicos según tema (CONEXIÓN CON LISTA C2)
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
            return themeID; // CONEXIÓN CON LISTA C2
        }

        // Implementación de IPoolable (CONEXIÓN CON LISTA B1)
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
            // Restaurar escala normal
            transform.localScale = Vector3.one;

            // Restaurar color si tiene SpriteRenderer
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white; // Solo restaurar a blanco por defecto
            }

            // Verificar collider
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }
        }

        void CleanupEnemyState()
        {
            // Detener partículas si están reproduciéndose
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
            // Reproducir partículas
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
            // PLACEHOLDER: Efectos específicos por tema (CONEXIÓN CON LISTA C2)
            // Aquí se aplicarán diferentes efectos según el tema activo
            // Por ejemplo:
            // - Tema Western: polvo y sonido de campana
            // - Tema Sci-Fi: chispas eléctricas y sonido láser
            // - Tema Medieval: sangre y sonido de metal

            Debug.Log($"Aplicando efectos del tema: {themeID}");
        }

        void HandleDestruction()
        {
            // NUEVA CONEXIÓN CON WAVE SYSTEM B3
            // Notificar al WaveSystem que este enemigo ha sido eliminado
            if (waveSystem != null)
            {
                // Usar invoke para dar tiempo a que se reproduzcan los efectos
                Invoke(nameof(ReturnToWaveSystemPool), 0.5f);
            }
            else
            {
                // Fallback anterior: Si usa pooling básico, retornar al pool después de un pequeño delay
                if (GetComponent<IPoolable>() != null)
                {
                    Invoke(nameof(ReturnToPoolDelayed), 0.5f);
                }
                else
                {
                    // Destruir después de un delay para que se vean los efectos
                    Destroy(gameObject, 0.5f);
                }
            }
        }

        // NUEVO: Método para retornar al pool del WaveSystem
        void ReturnToWaveSystemPool()
        {
            if (waveSystem != null)
            {
                waveSystem.ReturnEnemyToPool(gameObject);
                Debug.Log($"Enemigo {enemyType} retornado al WaveSystem pool");
            }
            else
            {
                // Fallback
                ReturnToPoolDelayed();
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
            themeID = theme;
        }

        // NUEVO: Método para configurar referencia al WaveSystem
        public void SetWaveSystem(WaveSystem system)
        {
            waveSystem = system;
        }

        // NUEVO: Método para auto-destruirse después de cierto tiempo (prevenir enemigos eternos)
        public void StartLifetimeCountdown(float lifetime = 30f)
        {
            Invoke(nameof(ForceReturnToPool), lifetime);
        }

        void ForceReturnToPool()
        {
            Debug.Log($"Enemigo {enemyType} auto-destruido por tiempo de vida excedido");
            ReturnToWaveSystemPool();
        }

        // EVENTOS PARA DETECCIÓN AUTOMÁTICA DE SALIDA DE PANTALLA
        void OnBecameInvisible()
        {
            // Si el enemigo sale completamente de la pantalla, retornarlo al pool
            // Esto ayuda a limpiar enemigos que se perdieron
            if (IsActiveInPool)
            {
                Debug.Log($"Enemigo {enemyType} salió de pantalla - retornando al pool");
                Invoke(nameof(ForceReturnToPool), 1f); // Small delay to avoid false positives
            }
        }

        // MÉTODO DE DEBUG PARA WAVE SYSTEM
        [ContextMenu("Force Return to Wave Pool")]
        public void DebugForceReturnToPool()
        {
            ReturnToWaveSystemPool();
        }

        [ContextMenu("Test Hit Effect")]
        public void DebugTestHit()
        {
            OnHit(ObjectType.Enemy, 10);
        }

        // Información para debugging
        void OnDrawGizmos()
        {
            // Mostrar información del enemigo en Scene View
            if (IsActiveInPool)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.5f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f,
                    $"{enemyType}\nActive: {IsActiveInPool}");
#endif
            }
        }
    }
}