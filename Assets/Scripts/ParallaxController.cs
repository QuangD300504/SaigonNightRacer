using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [Tooltip("The main camera the parallax will follow.")]
    public Transform cameraTransform;

    [Tooltip("The speed multiplier for the parallax effect. Closer objects should have a higher value (e.g., 0.8), farther objects a lower value (e.g., 0.1).")]
    public float parallaxEffectMultiplier;

    private float spriteWidth;
    private Vector3 lastCameraPosition;

    void Start()
    {
        // If no camera is assigned, find the main camera
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        lastCameraPosition = cameraTransform.position;

        // Get the sprite renderer from the child object
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("ParallaxController needs a child GameObject with a SpriteRenderer.", this);
            return;
        }

        // Get the width of the sprite in world units
        spriteWidth = spriteRenderer.sprite.bounds.size.x * transform.localScale.x;

        // Create clones for seamless tiling
        CreateClones(spriteRenderer.gameObject);
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Calculate how much the camera has moved since the last frame
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // Move the background layer by a fraction of the camera's movement
        //transform.position += new Vector3(deltaMovement.x * parallaxEffectMultiplier, 0, 0);
        transform.position += new Vector3(deltaMovement.x * parallaxEffectMultiplier, deltaMovement.y * parallaxEffectMultiplier, 0);
        // Update the last camera position for the next frame
        lastCameraPosition = cameraTransform.position;

        // Check if we need to reposition the children to create the infinite effect
        if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= spriteWidth)
        {
            float offsetPositionX = (cameraTransform.position.x - transform.position.x) % spriteWidth;
            transform.position = new Vector3(cameraTransform.position.x - offsetPositionX, transform.position.y, transform.position.z);
        }
    }

    // Creates left and right clones of the original sprite
    private void CreateClones(GameObject originalSprite)
    {
        // Clone for the right side
        GameObject rightClone = Instantiate(originalSprite, transform);
        rightClone.transform.position = new Vector3(originalSprite.transform.position.x + spriteWidth, originalSprite.transform.position.y, originalSprite.transform.position.z);

        // Clone for the left side
        GameObject leftClone = Instantiate(originalSprite, transform);
        leftClone.transform.position = new Vector3(originalSprite.transform.position.x - spriteWidth, originalSprite.transform.position.y, originalSprite.transform.position.z);
    }
}