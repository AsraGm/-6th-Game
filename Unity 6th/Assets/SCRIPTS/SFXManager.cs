using UnityEngine;

namespace ShootingRange
{
    /// <summary>
    /// Sistema centralizado para reproducir efectos de sonido
    /// Soluciona problemas de volumen bajo con AudioSource.PlayClipAtPoint
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        private static SFXManager _instance;
        public static SFXManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SFXManager");
                    _instance = go.AddComponent<SFXManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Configuración de Audio")]
        [Tooltip("Volumen maestro para todos los SFX")]
        [Range(0f, 2f)]
        public float masterSFXVolume = 1f;

        [Header("Pool de AudioSources")]
        [Tooltip("Número de AudioSources disponibles simultáneamente")]
        public int poolSize = 10;

        private AudioSource[] audioSourcePool;
        private int currentIndex = 0;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioPool();
        }

        void InitializeAudioPool()
        {
            audioSourcePool = new AudioSource[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                GameObject child = new GameObject($"AudioSource_{i}");
                child.transform.SetParent(transform);
                
                AudioSource audioSource = child.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D por defecto
                
                audioSourcePool[i] = audioSource;
            }

            Debug.Log($"✅ SFXManager inicializado con {poolSize} AudioSources");
        }

        /// <summary>
        /// Reproduce un sonido en 2D (sin posición espacial)
        /// </summary>
        public void PlaySound2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("⚠️ Clip de audio es null");
                return;
            }

            AudioSource source = GetAvailableAudioSource();
            source.spatialBlend = 0f; // 2D
            source.volume = volume * masterSFXVolume;
            source.clip = clip;
            source.Play();

            Debug.Log($"🔊 Reproduciendo '{clip.name}' en 2D - Volumen: {source.volume}");
        }

        /// <summary>
        /// Reproduce un sonido en una posición 3D específica
        /// </summary>
        public void PlaySound3D(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("⚠️ Clip de audio es null");
                return;
            }

            AudioSource source = GetAvailableAudioSource();
            source.transform.position = position;
            source.spatialBlend = spatialBlend; // 0 = 2D, 1 = 3D completo
            source.volume = volume * masterSFXVolume;
            source.clip = clip;
            source.Play();

            Debug.Log($"🔊 Reproduciendo '{clip.name}' en 3D - Pos: {position} - Volumen: {source.volume}");
        }

        /// <summary>
        /// Reproduce un sonido ONE-SHOT (no interrumpe otros sonidos)
        /// MEJOR PARA MÚLTIPLES SONIDOS SIMULTÁNEOS
        /// </summary>
        public void PlaySoundOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("⚠️ Clip de audio es null");
                return;
            }

            AudioSource source = GetAvailableAudioSource();
            source.spatialBlend = 0f;
            float finalVolume = volume * masterSFXVolume;
            source.PlayOneShot(clip, finalVolume);

            Debug.Log($"🔊 PlayOneShot '{clip.name}' - Volumen: {finalVolume}");
        }

        AudioSource GetAvailableAudioSource()
        {
            // Buscar un AudioSource que no esté reproduciendo
            for (int i = 0; i < poolSize; i++)
            {
                currentIndex = (currentIndex + 1) % poolSize;
                if (!audioSourcePool[currentIndex].isPlaying)
                {
                    return audioSourcePool[currentIndex];
                }
            }

            // Si todos están ocupados, usar el siguiente en el ciclo
            currentIndex = (currentIndex + 1) % poolSize;
            return audioSourcePool[currentIndex];
        }

        /// <summary>
        /// Detener todos los sonidos
        /// </summary>
        public void StopAllSounds()
        {
            foreach (AudioSource source in audioSourcePool)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        /// <summary>
        /// Cambiar volumen maestro en runtime
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterSFXVolume = Mathf.Clamp(volume, 0f, 2f);
            Debug.Log($"🔊 Volumen maestro SFX cambiado a: {masterSFXVolume}");
        }
    }
}
