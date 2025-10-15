using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Logic")]
    public int lives = 3;

    [Header("Events")]
    [Tooltip("Fired when score changes (currentScore, highScore)")]
    public UnityEvent<int, int> OnScoreChanged;
    
    [Tooltip("Fired when lives change")]
    public UnityEvent<int> OnLivesChanged;
    
    [Tooltip("Fired when speed changes")]
    public UnityEvent<float> OnSpeedChanged;

    [Header("HUD Controller")]
    [Tooltip("HUD Controller for managing all UI elements")]
    public HUDController hudController;

    [Header("UI References")]
    [Tooltip("Pause button for game control")]
    public Button pauseButton;

    [Header("Menu Panels & Prefabs")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject endGameScreen;
    public TextMeshProUGUI highScoreText_GameOver;
    public GameObject settingsMenuPrefab;

    // Game state
    private bool isPaused = false;
    private bool isGameOver = false;
    private GameObject currentSettingsInstance;
    private int currentHighScore;
    
    // Score event tracking
    private int lastScore = -1;
    private int lastHighScore = -1;
    private float lastSpeed = -1f;
    
    // Score update timer
    private float scoreUpdateTimer = 0f;
    private float scoreUpdateInterval = 0.1f; // Update every 0.1 seconds

    void Awake()
    {
        Instance = this;
        // Don't hardcode lives - use Inspector value
        
        // Initialize events if not set in Inspector
        if (OnScoreChanged == null) OnScoreChanged = new UnityEvent<int, int>();
        if (OnLivesChanged == null) OnLivesChanged = new UnityEvent<int>();
        if (OnSpeedChanged == null) OnSpeedChanged = new UnityEvent<float>();
    }

    void Start()
    {
        // Disable all panels at start
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (endGameScreen != null) endGameScreen.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);

        // Ensure game runs
        Time.timeScale = 1f;
        isGameOver = false;
        isPaused = false;

        // Get and display high score
        currentHighScore = HighScoreManager.Instance.GetHighScore("Total");
    }

    void Update()
    {
        // Always check input for resume or close settings
        HandleInput();

        // Don't run game logic if game over or paused
        if (isGameOver || isPaused) return;

        // Fire score events periodically (since ScoreManager updates continuously)
        scoreUpdateTimer += Time.deltaTime;
        if (scoreUpdateTimer >= scoreUpdateInterval)
        {
            UpdateScoreEvents();
            scoreUpdateTimer = 0f;
        }
        UpdateSpeedEvents();
    }

    /// <summary>
    /// Update score events when score changes
    /// </summary>
    private void UpdateScoreEvents()
    {
        if (ScoreManager.Instance != null)
        {
            int currentScore = ScoreManager.Instance.GetFinalScore();
            int currentHighScore = HighScoreManager.Instance.GetHighScore("Total");
            
            // Fire event only when score increases by 5+ points or high score changes
            if ((currentScore - lastScore) >= 5 || currentHighScore != lastHighScore)
            {
                OnScoreChanged.Invoke(currentScore, currentHighScore);
                lastScore = currentScore;
                lastHighScore = currentHighScore;
            }
        }
    }

    /// <summary>
    /// Update speed events when speed changes
    /// </summary>
    private void UpdateSpeedEvents()
    {
        // Get player speed from Rigidbody2D velocity (more reliable than BikeController calculation)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float currentSpeed = Mathf.Abs(rb.linearVelocity.x) * 3.6f; // Convert to km/h
                
                // Fire event only when speed changes significantly
                if (Mathf.Abs(currentSpeed - lastSpeed) >= 0.1f)
                {
                    OnSpeedChanged.Invoke(currentSpeed);
                    lastSpeed = currentSpeed;
                }
            }
        }
    }

    private void HandleInput()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isGameOver) return; // Do nothing if game over

            if (settingsPanel.activeSelf)
            {
                CloseSettingsMenu();
            }
            else
            {
                TogglePause();
            }
        }
        
        // Invincible mode toggle (I key)
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            Debug.Log("I key pressed - attempting to toggle invincible mode");
            var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
            if (obstacleSpawner != null)
            {
                obstacleSpawner.ToggleInvincibleMode();
                Debug.Log($"Invincible mode toggled. Current state: {obstacleSpawner.IsInvincibleModeEnabled()}");
            }
            else
            {
                Debug.LogWarning("ObstacleSpawnerNew not found!");
            }
        }
    }

    public void PlayerHit()
    {
        if (isGameOver) return; // Don't reduce lives if already game over

        // Check for invincible mode cheat
        var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
        if (obstacleSpawner != null && obstacleSpawner.IsInvincibleModeEnabled())
        {
            Debug.Log("=== INVINCIBLE MODE: Player hit ignored ===");
            return; // Skip damage if invincible mode is enabled
        }

        // Check for shield protection
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var bikeController = player.GetComponent<BikeController>();
            if (bikeController != null)
            {
                // Check for shield protection
                if (bikeController.IsShielded())
                {
                    Debug.Log("=== SHIELD PROTECTION: Player hit blocked ===");
                    return; // Skip damage if shield is active
                }
                
                // Check for Mario-style invincibility
                if (bikeController.IsInvincible())
                {
                    Debug.Log("=== INVINCIBILITY FRAMES: Player hit ignored ===");
                    return; // Skip damage if invincible
                }
                
                // Player takes damage - activate Mario-style invincibility
                lives--;
                Debug.Log($"Player Hit! Health: {lives} HP - Invincibility activated!");
                
                // Play damage sound and reset audio
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.OnPlayerDeath();
                }
                
                // Activate invincibility frames
                bikeController.ActivateInvincibility();
                
                // Fire lives changed event
                OnLivesChanged.Invoke(lives);
                
                if (lives <= 0)
                {
                    EndGame();
                }
            }
        }
    }

    /// <summary>
    /// Cheat method to directly reduce health, bypassing all protection
    /// </summary>
    public void CheatReduceHealth()
    {
        if (isGameOver) return; // Don't reduce lives if already game over
        
        // Directly reduce health without any protection checks
        lives--;
        Debug.Log($"CHEAT: Player health reduced! Health: {lives} HP");
        
        // Play damage sound and reset audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnPlayerDeath();
        }
        
        // Fire lives changed event
        OnLivesChanged.Invoke(lives);
        
        if (lives <= 0)
        {
            EndGame();
        }
    }

    /// <summary>
    /// Debug method to set player health (for testing)
    /// </summary>
    public void SetPlayerHealth(int newHealth)
    {
        if (isGameOver) return; // Don't change health if game is over
        
        int previousLives = lives;
        lives = Mathf.Clamp(newHealth, 0, int.MaxValue); // No upper cap - unlimited HP
        
        Debug.Log($"Debug: Player health changed from {previousLives} to {lives}");
        
        // Fire lives changed event
        OnLivesChanged.Invoke(lives);
        
        // Check for game over
        if (lives <= 0)
        {
            EndGame();
        }
    }

    /// <summary>
    /// Restore player health (for health collectibles)
    /// </summary>
    public void RestoreHealth(int amount)
    {
        if (isGameOver) return; // Don't restore health if game is over
        
        int previousLives = lives;
        lives = lives + amount; // No cap - unlimited HP
        int actualRestore = lives - previousLives;
        
        if (actualRestore > 0)
        {
            Debug.Log($"Health restored! +{actualRestore} HP. Current: {lives} HP");
            
            // Fire lives changed event
            OnLivesChanged.Invoke(lives);
        }
        else
        {
            Debug.Log("Health already at maximum!");
        }
    }

    // ===== GAME OVER =====
    private void EndGame()
    {
        if (isGameOver) return; // Only run once
        isGameOver = true;

        // Play game over sound and reset all audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnGameOver();
        }

        Time.timeScale = 0f;

        // 1. Get final score from ScoreManager
        int finalScore = ScoreManager.Instance.GetFinalScore();

        // 2. Send score to HighScoreManager to check and save
        HighScoreManager.Instance.CheckAndSaveHighScore(finalScore);

        // 3. Get latest high score (after check) to display
        int latestHighScore = HighScoreManager.Instance.GetHighScore("Total");
        if (highScoreText_GameOver != null)
        {
            highScoreText_GameOver.text = "HIGH SCORE: " + latestHighScore.ToString();
        }

        // 4. Enable EndGameScreen
        if (endGameScreen != null)
        {
            endGameScreen.SetActive(true);
        }
    }

    // ===== PAUSE AND MENU MANAGEMENT =====
    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        if (currentSettingsInstance != null)
        {
            Destroy(currentSettingsInstance);
            currentSettingsInstance = null;
        }
    }

    public void OpenSettingsMenu()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);

        if (currentSettingsInstance == null && settingsMenuPrefab != null)
        {
            currentSettingsInstance = Instantiate(settingsMenuPrefab, settingsPanel.transform);
            Button closeButton = currentSettingsInstance.GetComponentInChildren<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseSettingsMenu);
            }
        }
    }

    public void CloseSettingsMenu()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
        if (currentSettingsInstance != null)
        {
            Destroy(currentSettingsInstance);
            currentSettingsInstance = null;
        }
    }

    // ===== BUTTON FUNCTIONS (RETRY, QUIT) =====
    public void RestartGame()
    {
        Time.timeScale = 1f;
        
        // Reset audio before scene transition
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSceneTransition();
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        
        // Reset audio before scene transition
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSceneTransition();
        }
        
        SceneManager.LoadScene("MainMenu");
    }
}