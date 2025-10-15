using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple heart icon component for lives display
/// </summary>
public class HeartIcon : MonoBehaviour
{
    [Header("Heart Settings")]
    [Tooltip("Full heart sprite")]
    public Sprite fullHeartSprite;
    
    [Tooltip("Empty heart sprite")]
    public Sprite emptyHeartSprite;
    
    [Tooltip("Image component")]
    public Image heartImage;

    void Awake()
    {
        // Get Image component if not assigned
        if (heartImage == null)
        {
            heartImage = GetComponent<Image>();
        }
    }

    /// <summary>
    /// Set heart state (full or empty)
    /// </summary>
    public void SetHeartState(bool isFull)
    {
        if (heartImage == null) return;

        heartImage.sprite = isFull ? fullHeartSprite : emptyHeartSprite;
        heartImage.color = isFull ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
    }
}
