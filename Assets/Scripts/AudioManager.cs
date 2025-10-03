using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip background;
    public AudioClip death;
    public AudioClip jump;
    public AudioClip run;
    public AudioClip coin;
    public AudioClip motor;
    public AudioClip finish;

    [Header("Mixer (optional)")]
    public AudioMixer audioMixer; // route your sources to Mixer Groups in Inspector
    public string musicVolumeParameter = "MusicVolume";
    public string sfxVolumeParameter = "SFXVolume";

    void Awake()
    {
        // Singleton persistent across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (musicSource != null && background != null)
        {
            if (musicSource.clip != background)
            {
                musicSource.clip = background;
            }
            if (!musicSource.isPlaying) musicSource.Play();
        }

        // Load saved volumes
        float music = PlayerPrefs.GetFloat("MusicVolume01", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume01", 1f);
        SetMusicVolume01(music);
        SetSFXVolume01(sfx);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PauseAudio()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Pause();
    }

    public void ResumeAudio()
    {
        if (musicSource != null) musicSource.UnPause();
        if (sfxSource != null) sfxSource.UnPause();
    }

    // volume in 0..1
    public void SetMusicVolume01(float value)
    {
        value = Mathf.Clamp01(value);
        if (audioMixer != null && !string.IsNullOrEmpty(musicVolumeParameter))
        {
            audioMixer.SetFloat(musicVolumeParameter, ToDecibels(value));
        }
        if (musicSource != null) musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume01", value);
    }

    public void SetSFXVolume01(float value)
    {
        value = Mathf.Clamp01(value);
        if (audioMixer != null && !string.IsNullOrEmpty(sfxVolumeParameter))
        {
            audioMixer.SetFloat(sfxVolumeParameter, ToDecibels(value));
        }
        if (sfxSource != null) sfxSource.volume = value;
        PlayerPrefs.SetFloat("SFXVolume01", value);
    }

    static float ToDecibels(float value01)
    {
        // Map 0..1 to -80dB..0dB (avoid -Infinity at 0)
        return value01 > 0.0001f ? Mathf.Log10(value01) * 20f : -80f;
    }
}
