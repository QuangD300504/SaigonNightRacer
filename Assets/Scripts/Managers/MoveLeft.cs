using UnityEngine;
public class MoveLeft : MonoBehaviour {
    void Update(){
        float s = GameManager.Instance != null ? GameManager.Instance.worldSpeed : 6f;
        transform.Translate(Vector3.left * s * Time.deltaTime);
        if (transform.position.x < -20f) Destroy(gameObject);
    }
}
