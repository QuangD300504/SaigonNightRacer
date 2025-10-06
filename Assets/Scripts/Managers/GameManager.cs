using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; // Thêm namespace cho TextMeshPro

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Logic")]
    public float worldSpeed = 6f;
    public int lives = 3;

    [Header("In-Game UI References")]
    public TextMeshProUGUI scoreText; // Đổi sang TextMeshProUGUI nếu bạn dùng nó
    public TextMeshProUGUI speedText; // Đổi sang TextMeshProUGUI nếu bạn dùng nó
    public Button pauseButton;
    public TextMeshProUGUI highScoreText_InGame;

    [Header("Menu Panels & Prefabs")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject endGameScreen;
    public TextMeshProUGUI highScoreText_GameOver;
    public GameObject settingsMenuPrefab;

    // Trạng thái game
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
        // Tắt tất cả các panel lúc bắt đầu
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (endGameScreen != null) endGameScreen.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);

        // Đảm bảo game chạy
        Time.timeScale = 1f;
        isGameOver = false;
        isPaused = false;

        // Lấy điểm cao nhất đã lưu và hiển thị
        currentHighScore = HighScoreManager.Instance.GetHighScore("Total");
        if (highScoreText_InGame != null)
        {
            highScoreText_InGame.text = "HIGH SCORE: " + currentHighScore.ToString();
        }
    }

    void Update()
    {
        // Luôn kiểm tra input để có thể resume hoặc đóng settings
        HandleInput();

        // Không chạy logic game nếu đã thua hoặc đang pause
        if (isGameOver || isPaused) return;

        // --- Cập nhật UI từ các Manager khác ---
        // Lấy điểm hiện tại từ ScoreManager và hiển thị
        int currentScore = ScoreManager.Instance.GetFinalScore();
        if (scoreText) scoreText.text = "SCORE: " + currentScore.ToString();

        // Nếu điểm hiện tại vượt qua high score, cập nhật high score text theo điểm hiện tại
        if (currentScore > currentHighScore)
        {
            if (highScoreText_InGame != null)
            {
                highScoreText_InGame.text = "HIGH SCORE: " + currentScore.ToString();
            }
        }

        // Cập nhật tốc độ
        if (speedText) speedText.text = Mathf.RoundToInt(worldSpeed * 10f) + " km/h";
    }

    private void HandleInput()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isGameOver) return; // Không làm gì nếu đã thua

            if (settingsPanel.activeSelf)
            {
                CloseSettingsMenu();
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void PlayerHit()
    {
        if (isGameOver) return; // Không trừ mạng nữa nếu đã thua

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
        if (isGameOver) return; // Chỉ chạy 1 lần
        isGameOver = true;

        Time.timeScale = 0f;

        // 1. Lấy điểm cuối cùng từ ScoreManager
        int finalScore = ScoreManager.Instance.GetFinalScore();

        // 2. Gửi điểm cho HighScoreManager để kiểm tra và lưu
        HighScoreManager.Instance.CheckAndSaveHighScore(finalScore);

        // 3. Lấy high score MỚI NHẤT (sau khi đã check) để hiển thị
        int latestHighScore = HighScoreManager.Instance.GetHighScore("Total");
        if (highScoreText_GameOver != null)
        {
            highScoreText_GameOver.text = "HIGH SCORE: " + latestHighScore.ToString();
        }

        // 4. Kích hoạt (enable) màn hình EndGameScreen
        if (endGameScreen != null)
        {
            endGameScreen.SetActive(true);
        }
    }

    // ===== QUẢN LÝ PAUSE VÀ MENU =====
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

    // ===== CÁC HÀM CHO NÚT BẤM (RETRY, QUIT) =====
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