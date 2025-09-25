using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject[] obstaclePrefabs;
    public float spawnInterval = 1.5f;
    float timer;

    void Update() {
        timer -= Time.deltaTime;
        if(timer <= 0f){
            Spawn();
            timer = spawnInterval;
            // optionally shorten interval by speed:
            spawnInterval = Mathf.Max(0.5f, spawnInterval - 0.001f);
        }
    }

    void Spawn(){
        int idx = Random.Range(0, obstaclePrefabs.Length);
        GameObject go = Instantiate(obstaclePrefabs[idx], transform.position, Quaternion.identity);
        // Add script to move left by GameManager.worldSpeed
        var mover = go.GetComponent<MoveLeft>();
        if(mover == null) mover = go.AddComponent<MoveLeft>();
    }
}
