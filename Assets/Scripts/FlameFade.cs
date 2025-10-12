using UnityEngine;
using System.Collections;

public class FlameFade : MonoBehaviour
{
    private SpriteRenderer sr;
    private Coroutine fadeRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void FadeOut(float duration)
    {
        // Ensure the GameObject is active before starting coroutine
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(duration));
    }

    private IEnumerator FadeRoutine(float duration)
    {
        float startAlpha = sr.color.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, 0f, t / duration);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, a);
            yield return null;
        }

        gameObject.SetActive(false);
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
    }
}
