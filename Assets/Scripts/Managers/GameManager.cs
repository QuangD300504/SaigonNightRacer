using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject spawnObjects;
    public GameObject[] spawnPoints;
    public float timer;
    public float timeBetweenSpawns;

    public float worldSpeed = 6f;
    public int score = 0;
    public int lives = 3;

    public Text scoreText;
    public Text speedText;
    public GameObject endScreen;

    void Awake(){ 
        Instance = this; 
        // Initialize health
        lives = 3;
        Debug.Log($"Game Started! Player Health: {lives}/3");
    }

    void Update(){
        timer += Time.deltaTime;
        if (timer >= timeBetweenSpawns){
            if (spawnObjects != null && spawnPoints != null && spawnPoints.Length > 0){
                int randNum = Random.Range(0, spawnPoints.Length);
                Instantiate(spawnObjects, spawnPoints[randNum].transform.position, Quaternion.identity);
                timer = 0;
            }
        }
        score += Mathf.FloorToInt(Time.deltaTime * 1f); // +1 per second approx
        if (scoreText) scoreText.text = score.ToString();
        if (speedText) speedText.text = Mathf.RoundToInt(worldSpeed * 10f) + " km/h";
        
    }

    public void SetWorldSpeed(float s){
        worldSpeed = s;
    }

    public void AddScore(int v){
        score += v;
    }

    public void PlayerHit(){
        lives--;
        
        // Log health to console
        Debug.Log($"Player Hit! Health: {lives}/3");
        
        // Apply knockback to player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var bikeController = player.GetComponent<BikeController>();
            if (bikeController != null)
            {
                bikeController.ApplyKnockback();
            }
        }
        
        if (lives <= 0) 
        {
            PlayerDied();
        }
        // Note: No respawn needed since player is static in scene
    }
    
    /// <summary>
    /// Debug method to reduce player health (Press H key)
    /// </summary>
    public void ReducePlayerHealth()
    {
        if (lives > 0)
        {
            lives--;
            Debug.Log($"Debug: Health reduced! Health: {lives}/3");
            
            if (lives <= 0)
            {
                PlayerDied();
            }
        }
        else
        {
            Debug.Log("Player is already dead!");
        }
    }
    
    /// <summary>
    /// Called when player dies (health reaches 0)
    /// </summary>
    public void PlayerDied()
    {
        Debug.Log("Player Died! Game Over!");
        // Show end screen, save highscore
        int best = PlayerPrefs.GetInt("HighScore",0);
        if (score > best) PlayerPrefs.SetInt("HighScore", score);
        if (endScreen != null) endScreen.SetActive(true);
        Time.timeScale = 0f;
    }

}
