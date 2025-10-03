using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Logic")]
    public GameObject spawnObjects;
    public GameObject[] spawnPoints;
    public float timer;
    public float timeBetweenSpawns;
    public float worldSpeed = 6f;
    public int score = 0;
    public int lives = 3;

    [Header("In-Game UI References")]
    public Text scoreText;
    public Text speedText;
    public Button pauseButton;

    // THAY ĐỔI: Đơn giản hóa các biến panel để khớp với Hierarchy
    [Header("Menu Panels & Prefabs")]
    [Tooltip("Panel chứa các nút Resume, Settings, Quit")]
    public GameObject pausePanel;
    [Tooltip("Panel sẽ chứa prefab Settings được tạo ra")]
    public GameObject settingsPanel;
    [Tooltip("Prefab của menu Settings")]
    public GameObject settingsMenuPrefab;

    private bool isPaused = false;
    private GameObject currentSettingsInstance; // Biến riêng cho instance của settings

    void Awake()
    {
        Instance = this;
        lives = 3;
    }

    void Start()
    {
        // Vô hiệu hóa các panel menu lúc bắt đầu game
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Gán sự kiện cho nút Pause
        if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);

        Time.timeScale = 1f;
    }

    void Update()
    {
        // THAY ĐỔI: Chuyển khối code kiểm tra input lên đầu
        // để nó luôn được chạy, kể cả khi game đang pause.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Nếu đang ở trong settings, nút Escape sẽ đóng settings trước
            if (settingsPanel.activeSelf)
            {
                CloseSettingsMenu();
            }
            else
            {
                TogglePause();
            }
        }

        // Dòng này bây giờ chỉ chặn logic game, không chặn input nữa
        if (isPaused) return;

        // --- Logic spawn và tính điểm (giữ nguyên) ---
        timer += Time.deltaTime;
        if (timer >= timeBetweenSpawns)
        {
            if (spawnObjects != null && spawnPoints != null && spawnPoints.Length > 0)
            {
                int randNum = Random.Range(0, spawnPoints.Length);
                Instantiate(spawnObjects, spawnPoints[randNum].transform.position, Quaternion.identity);
                timer = 0;
            }
        }
        score += Mathf.FloorToInt(Time.deltaTime * 1f);
        if (scoreText) scoreText.text = score.ToString();
        if (speedText) speedText.text = Mathf.RoundToInt(worldSpeed * 10f) + " km/h";
    }

    public void PlayerHit()
    {
        lives--;
        Debug.Log($"Player Hit! Health: {lives}/3");
        if (lives <= 0)
        {
            // Tạm thời vô hiệu hóa logic game over
            Debug.Log("Player Died! Game Over logic is currently disabled.");
        }
    }

    // ===== QUẢN LÝ PAUSE VÀ MENU =====

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
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

        // Tắt hết các panel và hủy instance settings
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
        pausePanel.SetActive(false); // Ẩn menu pause
        settingsPanel.SetActive(true); // Hiện panel settings

        // Tạo prefab settings nếu chưa có
        if (currentSettingsInstance == null && settingsMenuPrefab != null)
        {
            currentSettingsInstance = Instantiate(settingsMenuPrefab, settingsPanel.transform);

            // Tìm nút Close/Back trong prefab và gán sự kiện
            Button closeButton = currentSettingsInstance.GetComponentInChildren<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseSettingsMenu);
            }
        }
    }

    public void CloseSettingsMenu()
    {
        settingsPanel.SetActive(false); // Ẩn panel settings
        pausePanel.SetActive(true);     // Hiện lại menu pause

        // Hủy prefab settings
        if (currentSettingsInstance != null)
        {
            Destroy(currentSettingsInstance);
            currentSettingsInstance = null;
        }
    }

    // ===== CÁC HÀM CHO NÚT BẤM =====
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}