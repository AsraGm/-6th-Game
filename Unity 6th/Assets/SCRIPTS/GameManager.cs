using UnityEngine;
using UnityEngine.SceneManagement;

// GameManager que conecta todos los sistemas de la Lista A
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] private float levelDuration = 60f;
    [SerializeField] private bool autoStartLevel = true;
    
    [Header("System References")]
    [SerializeField] private MobileShootingSystem shootingSystem;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private LevelTimer levelTimer;
    
    [Header("UI References")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private TMPro.TextMeshProUGUI finalScoreText;
    
    // Game State
    public enum GameState
    {
        Starting,
        Playing,
        Paused,
        Finished
    }
    
    public GameState CurrentState { get; private set; }
    
    // Events
    public System.Action OnGameStarted;
    public System.Action OnGameFinished;
    public System.Action<GameState> OnGameStateChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        InitializeGame();
        
        if (autoStartLevel)
        {
            StartLevel();
        }
    }
    
    private void InitializeGame()
    {
        // Encontrar referencias si no están asignadas
        if (shootingSystem == null)
            shootingSystem = FindObjectOfType<MobileShootingSystem>();
            
        if (scoreManager == null)
            scoreManager = FindObjectOfType<ScoreManager>();
            
        if (levelTimer == null)
            levelTimer = FindObjectOfType<LevelTimer>();
        
        // Suscribirse a eventos del timer
        if (levelTimer != null)
        {
            levelTimer.OnTimerFinished += OnLevelTimeFinished;
        }
        
        // Inicializar UI
        SetupUI();
        
        CurrentState = GameState.Starting;
    }
    
    private void SetupUI()
    {
        if (gameUI != null) gameUI.SetActive(true);
        if (levelCompleteUI != null) levelCompleteUI.SetActive(false);
    }
    
    public void StartLevel()
    {
        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
        
        // Activar sistemas
        if (shootingSystem != null)
            shootingSystem.SetCanShoot(true);
            
        if (levelTimer != null)
            levelTimer.StartTimer(levelDuration);
            
        if (scoreManager != null)
            scoreManager.ResetMoney();
        
        OnGameStarted?.Invoke();
        
        Debug.Log("Level Started!");
    }
    
    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        
        CurrentState = GameState.Paused;
        OnGameStateChanged?.Invoke(CurrentState);
        
        // Pausar sistemas
        if (shootingSystem != null)
            shootingSystem.SetCanShoot(false);
            
        if (levelTimer != null)
            levelTimer.PauseTimer();
            
        Time.timeScale = 0f;
    }
    
    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        
        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
        
        // Reanudar sistemas
        if (shootingSystem != null)
            shootingSystem.SetCanShoot(true);
            
        if (levelTimer != null)
            levelTimer.ResumeTimer();
            
        Time.timeScale = 1f;
    }
    
    private void OnLevelTimeFinished()
    {
        FinishLevel();
    }
    
    public void FinishLevel()
    {
        if (CurrentState == GameState.Finished) return;
        
        CurrentState = GameState.Finished;
        OnGameStateChanged?.Invoke(CurrentState);
        
        // Desactivar sistemas
        if (shootingSystem != null)
            shootingSystem.SetCanShoot(false);
            
        if (levelTimer != null)
            levelTimer.StopTimer();
        
        ShowLevelResults();
        OnGameFinished?.Invoke();
        
        Debug.Log("Level Finished!");
    }
    
    private void ShowLevelResults()
    {
        if (gameUI != null) gameUI.SetActive(false);
        if (levelCompleteUI != null) levelCompleteUI.SetActive(true);
        
        // Mostrar puntaje final
        if (finalScoreText != null && scoreManager != null)
        {
            finalScoreText.text = $"Final Score: ${scoreManager.CurrentMoney}";
        }
    }
    
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // Asegúrate de tener esta escena
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
    
    // Métodos para procesar hits (conecta con sistemas de puntuación)
    public void ProcessEnemyHit(EnemyType enemyType, int value)
    {
        if (scoreManager != null)
        {
            scoreManager.ProcessEnemyHit(enemyType, value);
        }
    }
    
    public void ProcessInnocentHit(int penalty)
    {
        if (scoreManager != null)
        {
            scoreManager.ProcessInnocentHit(penalty);
        }
    }
    
    private void OnDestroy()
    {
        // Desuscribirse de eventos
        if (levelTimer != null)
        {
            levelTimer.OnTimerFinished -= OnLevelTimeFinished;
        }
    }
    
    // Métodos públicos para UI buttons
    [System.Serializable]
    public class GameEvents
    {
        public UnityEngine.Events.UnityEvent OnLevelStart;
        public UnityEngine.Events.UnityEvent OnLevelComplete;
        public UnityEngine.Events.UnityEvent OnGamePause;
        public UnityEngine.Events.UnityEvent OnGameResume;
    }
    
    [SerializeField] private GameEvents gameEvents;
    
    public void InvokeGameStartEvent() => gameEvents?.OnLevelStart?.Invoke();
    public void InvokeGameCompleteEvent() => gameEvents?.OnLevelComplete?.Invoke();
}