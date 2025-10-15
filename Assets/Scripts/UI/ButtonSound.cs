using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple component to add sound effects to UI buttons
/// Attach this to any Button GameObject to automatically play sounds on click/hover
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    [Header("Button Sounds")]
    [Tooltip("Sound to play when button is clicked")]
    public AudioClip clickSound;
    
    [Tooltip("Sound to play when button is hovered (optional)")]
    public AudioClip hoverSound;
    
    [Tooltip("Volume for click sound")]
    [Range(0f, 1f)]
    public float clickVolume = 0.8f;
    
    [Tooltip("Volume for hover sound")]
    [Range(0f, 1f)]
    public float hoverVolume = 0.6f;
    
    private Button button;
    
    void Start()
    {
        button = GetComponent<Button>();
        
        // Add click sound listener
        if (button != null)
        {
            button.onClick.AddListener(PlayClickSound);
        }
        
        // Add hover sound listener (if using EventTrigger)
        var eventTrigger = GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null && hoverSound != null)
        {
            // Add EventTrigger component for hover detection
            eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            // Add pointer enter event
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { PlayHoverSound(); });
            eventTrigger.triggers.Add(pointerEnter);
        }
    }
    
    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }
    
    /// <summary>
    /// Play click sound using AudioManager
    /// </summary>
    public void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
        // Fallback to individual sound if AudioManager doesn't have the sound
        else if (clickSound != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position, clickVolume);
        }
    }
    
    /// <summary>
    /// Play hover sound using AudioManager
    /// </summary>
    public void PlayHoverSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonHoverSound();
        }
        // Fallback to individual sound if AudioManager doesn't have the sound
        else if (hoverSound != null)
        {
            AudioSource.PlayClipAtPoint(hoverSound, Camera.main.transform.position, hoverVolume);
        }
    }
}
