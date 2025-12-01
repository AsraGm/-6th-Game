using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Clips de Audio")]
    public AudioClip musicaMenu;
    public AudioClip musicaJuego;

    [Header("Configuración")]
    [Range(0f, 1f)]
    public float volumen = 0.7f;

    [Header("Detección Automática de Escenas")]
    [Tooltip("Nombres de escenas de menú (sin extensión)")]
    public string[] escerasMenu = { "MenuPrincipal", "MenuOpciones", "MenuNiveles", "Creditos" };

    [Tooltip("Nombres de escenas de juego/niveles (sin extensión)")]
    public string[] escenasJuego = { "Nivel1", "Nivel2", "Nivel3", "Gameplay" };

    private AudioSource audioSource;
    private string tipoEscenaActual = "";

    void Awake()
    {
        // Implementar Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Configurar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.volume = volumen;
        audioSource.playOnAwake = false;

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Iniciar con música de menú
        ReproducirMusicaMenu();
    }

    void OnDestroy()
    {
        // Desuscribirse del evento
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DetectarYCambiarMusica(scene.name);
    }

    private void DetectarYCambiarMusica(string nombreEscena)
    {
        // Verificar si es escena de menú
        foreach (string escenaMenu in escerasMenu)
        {
            if (nombreEscena == escenaMenu)
            {
                ReproducirMusicaMenu();
                return;
            }
        }

        // Verificar si es escena de juego
        foreach (string escenaJuego in escenasJuego)
        {
            if (nombreEscena == escenaJuego)
            {
                ReproducirMusicaJuego();
                return;
            }
        }
    }

    public void ReproducirMusicaMenu()
    {
        CambiarMusica(musicaMenu, "menu");
    }

    public void ReproducirMusicaJuego()
    {
        CambiarMusica(musicaJuego, "juego");
    }

    private void CambiarMusica(AudioClip nuevoClip, string tipoEscena)
    {
        // Si ya está sonando esta música, no hacer nada
        if (audioSource.clip == nuevoClip && audioSource.isPlaying)
        {
            return;
        }

        // Cambiar a la nueva música
        audioSource.clip = nuevoClip;
        audioSource.Play();
        tipoEscenaActual = tipoEscena;
    }

    public void DetenerMusica()
    {
        audioSource.Stop();
    }

    public void PausarMusica()
    {
        audioSource.Pause();
    }

    public void ReanudarMusica()
    {
        audioSource.UnPause();
    }

    public void CambiarVolumen(float nuevoVolumen)
    {
        volumen = Mathf.Clamp01(nuevoVolumen);
        audioSource.volume = volumen;
    }
}