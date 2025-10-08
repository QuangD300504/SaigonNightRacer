using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Logic")]
    public float worldSpeed = 6f;
    public int lives = 3;

    [Header("In-Game UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI speedText;
    public Button pauseButton;
    public TextMeshProUGUI highScoreText_InGame;

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

    void Awake()
    {
        Instance = this;
        lives = 3;
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
        if (highScoreText_InGame != null)
        {
            highScoreText_InGame.text = "HIGH SCORE: " + currentHighScore.ToString();
        }
    }

    void Update()
    {
        // Always check input for resume or close settings
        HandleInput();

        // Don't run game logic if game over or paused
        if (isGameOver || isPaused) return;

        // Update UI from other managers
        // Get current score from ScoreManager and display
        int currentScore = ScoreManager.Instance.GetFinalScore();
        if (scoreText) scoreText.text = "SCORE: " + currentScore.ToString();

        // If current score exceeds high score, update high score text
        if (currentScore > currentHighScore)
        {
            if (highScoreText_InGame != null)
            {
                highScoreText_InGame.text = "HIGH SCORE: " + currentScore.ToString();
            }
        }

        // Update speed
        if (speedText) speedText.text = Mathf.RoundToInt(worldSpeed * 10f) + " km/h";
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
            var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
            if (obstacleSpawner != null)
            {
                obstacleSpawner.ToggleInvincibleMode();
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

        lives--;
        Debug.Log($"Player Hit! Health: {lives}/3");
        if (lives <= 0)
        {
            EndGame();
        }
    }

    // ===== GAME OVER =====
    private void EndGame()
    {
        if (isGameOver) return; // Only run once
        isGameOver = true;

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}