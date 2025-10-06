using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Manages high scores using PlayerPrefs
/// Supports multiple score categories and leaderboard display
/// </summary>
public class HighScoreManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component to display high score")]
    public TextMeshProUGUI highScoreText;
    
    [Tooltip("Text component to display 'New Record!' message")]
    public TextMeshProUGUI newRecordText;
    
    [Tooltip("Panel to show detailed score breakdown")]
    public GameObject scoreBreakdownPanel;
    
    [Tooltip("Text components for score breakdown (optional)")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI survivalText;
    public TextMeshProUGUI airtimeText;
    public TextMeshProUGUI flipsText;
    public TextMeshProUGUI totalText;

    [Header("Settings")]
    [Tooltip("Key prefix for PlayerPrefs storage")]
    public string scoreKeyPrefix = "HighScore_";
    
    [Tooltip("Maximum number of high scores to store")]
    public int maxScores = 10;
    
    [Tooltip("Duration to show 'New Record!' message")]
    public float newRecordDisplayTime = 3f;

    [Header("Categories")]
    [Tooltip("Score categories to track separately")]
    public ScoreCategory[] scoreCategories = new ScoreCategory[]
    {
        new ScoreCategory { name = "Total", key = "Total" },
        new ScoreCategory { name = "Distance", key = "Distance" },
        new ScoreCategory { name = "Survival", key = "Survival" }
    };

    public static HighScoreManager Instance { get; private set; }

    [System.Serializable]
    public struct ScoreCategory
    {
        public string name;
        public string key;
    }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        UpdateHighScoreDisplay();
        
        // Hide new record text initially
        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(false);
        }
        
        // Hide score breakdown panel initially
        if (scoreBreakdownPanel != null)
        {
            scoreBreakdownPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Check if the Caps Lock key was pressed this frame
        if (Keyboard.current != null && Keyboard.current.capsLockKey.wasPressedThisFrame)
        {
            // Call the reset score function
            ResetAllHighScores();
        }
    }

    /// <summary>
    /// Check if current score is a new high score and save it
    /// </summary>
    public bool CheckAndSaveHighScore(int score, ScoreBreakdown breakdown = default)
    {
        bool isNewRecord = false;
        
        // Check each category
        foreach (var category in scoreCategories)
        {
            string key = scoreKeyPrefix + category.key;
            int currentHigh = PlayerPrefs.GetInt(key, 0);
            
            if (score > currentHigh)
            {
                PlayerPrefs.SetInt(key, score);
                PlayerPrefs.Save();
                isNewRecord = true;
                
                Debug.Log($"New {category.name} record: {score}!");
            }
        }
        
        // Save detailed breakdown if provided
        if (breakdown.total > 0)
        {
            SaveScoreBreakdown(breakdown);
        }
        
        if (isNewRecord)
        {
            ShowNewRecordMessage();
            UpdateHighScoreDisplay();
        }
        
        return isNewRecord;
    }

    /// <summary>
    /// Save detailed score breakdown
    /// </summary>
    private void SaveScoreBreakdown(ScoreBreakdown breakdown)
    {
        PlayerPrefs.SetInt(scoreKeyPrefix + "Last_Distance", breakdown.distance);
        PlayerPrefs.SetInt(scoreKeyPrefix + "Last_Survival", breakdown.survival);
        PlayerPrefs.SetInt(scoreKeyPrefix + "Last_Airtime", breakdown.airtime);
        PlayerPrefs.SetInt(scoreKeyPrefix + "Last_Flips", breakdown.flips);
        PlayerPrefs.SetInt(scoreKeyPrefix + "Last_Total", breakdown.total);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get high score for a specific category
    /// </summary>
    public int GetHighScore(string categoryKey)
    {
        string key = scoreKeyPrefix + categoryKey;
        return PlayerPrefs.GetInt(key, 0);
    }

    /// <summary>
    /// Get all high scores
    /// </summary>
    public Dictionary<string, int> GetAllHighScores()
    {
        var scores = new Dictionary<string, int>();
        
        foreach (var category in scoreCategories)
        {
            scores[category.name] = GetHighScore(category.key);
        }
        
        return scores;
    }

    /// <summary>
    /// Update UI display with current high score
    /// </summary>
    public void UpdateHighScoreDisplay()
    {
        int highScore = GetHighScore("Total");
        
        if (highScoreText != null)
        {
            highScoreText.text = "HIGH SCORE: " + highScore.ToString();
        }
    }

    /// <summary>
    /// Show 'New Record!' message
    /// </summary>
    private void ShowNewRecordMessage()
    {
        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(true);
            
            // Hide after specified time
            Invoke(nameof(HideNewRecordMessage), newRecordDisplayTime);
        }
    }

    /// <summary>
    /// Hide 'New Record!' message
    /// </summary>
    private void HideNewRecordMessage()
    {
        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Show detailed score breakdown
    /// </summary>
    public void ShowScoreBreakdown(ScoreBreakdown breakdown)
    {
        if (scoreBreakdownPanel == null) return;
        
        scoreBreakdownPanel.SetActive(true);
        
        // Update individual score texts if available
        if (distanceText != null) distanceText.text = breakdown.distance.ToString();
        if (survivalText != null) survivalText.text = breakdown.survival.ToString();
        if (airtimeText != null) airtimeText.text = breakdown.airtime.ToString();
        if (flipsText != null) flipsText.text = breakdown.flips.ToString();
        if (totalText != null) totalText.text = breakdown.total.ToString();
    }

    /// <summary>
    /// Hide score breakdown panel
    /// </summary>
    public void HideScoreBreakdown()
    {
        if (scoreBreakdownPanel != null)
        {
            scoreBreakdownPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Reset all high scores (for testing or reset functionality)
    /// </summary>
    public void ResetAllHighScores()
    {
        foreach (var category in scoreCategories)
        {
            string key = scoreKeyPrefix + category.key;
            PlayerPrefs.DeleteKey(key);
        }
        
        // Clear detailed breakdown
        PlayerPrefs.DeleteKey(scoreKeyPrefix + "Last_Distance");
        PlayerPrefs.DeleteKey(scoreKeyPrefix + "Last_Survival");
        PlayerPrefs.DeleteKey(scoreKeyPrefix + "Last_Airtime");
        PlayerPrefs.DeleteKey(scoreKeyPrefix + "Last_Flips");
        PlayerPrefs.DeleteKey(scoreKeyPrefix + "Last_Total");
        
        PlayerPrefs.Save();
        UpdateHighScoreDisplay();
        
        Debug.Log("All high scores have been reset!");
    }

    /// <summary>
    /// Get last played score breakdown
    /// </summary>
    public ScoreBreakdown GetLastScoreBreakdown()
    {
        return new ScoreBreakdown
        {
            distance = PlayerPrefs.GetInt(scoreKeyPrefix + "Last_Distance", 0),
            survival = PlayerPrefs.GetInt(scoreKeyPrefix + "Last_Survival", 0),
            airtime = PlayerPrefs.GetInt(scoreKeyPrefix + "Last_Airtime", 0),
            flips = PlayerPrefs.GetInt(scoreKeyPrefix + "Last_Flips", 0),
            total = PlayerPrefs.GetInt(scoreKeyPrefix + "Last_Total", 0)
        };
    }
}
