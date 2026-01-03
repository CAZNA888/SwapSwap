using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip cardDealClip;
    public AudioClip cardFlipClip;
    public AudioClip swipeClip;
    public AudioClip levelCompleteClip;
    public AudioClip connectionClip;
    public AudioClip coinClip; // Звук монетки
    
    [Header("Background Music")]
    [Tooltip("Клип фоновой музыки")]
    public AudioClip backgroundMusicClip;
    
    [Header("Settings")]
    [Tooltip("Громкость звуков (SFX) - будет перезаписана из PlayerPrefs")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    
    [Tooltip("Громкость фоновой музыки - будет перезаписана из PlayerPrefs")]
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    
    [Tooltip("Громкость фоновой музыки во время звуков (0 = полностью приглушена, 1 = полная громкость)")]
    [Range(0f, 1f)]
    public float musicVolumeDuringSFX = 0.3f;
    
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    
    private AudioSource sfxAudioSource; // Для звуков
    private AudioSource musicAudioSource; // Для фоновой музыки
    
    // Счетчик активных звуков для управления громкостью музыки
    private int activeSoundsCount = 0;
    private float originalMusicVolume; // Сохраняем оригинальную громкость музыки
    
    void Awake()
    {
        // Создаем AudioSource для звуков (SFX)
        sfxAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }
        sfxAudioSource.playOnAwake = false;
        
        // Создаем отдельный AudioSource для фоновой музыки
        musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true; // Фоновая музыка зациклена
        
        // Загружаем сохраненные настройки
        LoadSettings();
        
        // Применяем настройки
        ApplySettings();
        
        // Находим SoundSettingsManager и уведомляем его о создании AudioManager
        SoundSettingsManager settingsManager = FindObjectOfType<SoundSettingsManager>();
        if (settingsManager != null)
        {
            settingsManager.SetAudioManager(this);
        }
    }
    
    void Start()
    {
        // Запускаем фоновую музыку, если она есть
        PlayBackgroundMusic();
    }
    
    /// <summary>
    /// Загружает настройки громкости из PlayerPrefs
    /// </summary>
    private void LoadSettings()
    {
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
    }
    
    /// <summary>
    /// Применяет настройки громкости к AudioSource
    /// </summary>
    private void ApplySettings()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = sfxVolume;
        }
        
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = musicVolume;
            originalMusicVolume = musicVolume; // Сохраняем оригинальную громкость
        }
    }
    
    /// <summary>
    /// Устанавливает громкость звуков (SFX)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = sfxVolume;
        }
    }
    
    /// <summary>
    /// Устанавливает громкость фоновой музыки
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        originalMusicVolume = musicVolume; // Обновляем оригинальную громкость
        
        // Применяем громкость только если нет активных звуков
        if (musicAudioSource != null && activeSoundsCount == 0)
        {
            musicAudioSource.volume = musicVolume;
        }
    }
    
    /// <summary>
    /// Воспроизводит фоновую музыку
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (backgroundMusicClip != null && musicAudioSource != null)
        {
            musicAudioSource.clip = backgroundMusicClip;
            musicAudioSource.volume = musicVolume;
            originalMusicVolume = musicVolume; // Сохраняем оригинальную громкость
            musicAudioSource.Play();
        }
    }
    
    /// <summary>
    /// Останавливает фоновую музыку
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }
    
    /// <summary>
    /// Приостанавливает фоновую музыку
    /// </summary>
    public void PauseBackgroundMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
        }
    }
    
    /// <summary>
    /// Возобновляет фоновую музыку
    /// </summary>
    public void ResumeBackgroundMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying && backgroundMusicClip != null)
        {
            musicAudioSource.UnPause();
        }
    }
    
    public void PlayCardDeal()
    {
        PlaySound(cardDealClip);
    }
    
    public void PlayCardFlip()
    {
        PlaySound(cardFlipClip);
    }
    
    public void PlaySwipe()
    {
        PlaySound(swipeClip);
    }
    
    public void PlayLevelComplete()
    {
        PlaySound(levelCompleteClip);
    }
    
    public void PlayConnection()
    {
        PlaySound(connectionClip);
    }
    
    public void PlayCoin()
    {
        PlaySound(coinClip);
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxAudioSource != null)
        {
            // Увеличиваем счетчик активных звуков
            OnSoundStart();
            
            // Воспроизводим звук
            sfxAudioSource.PlayOneShot(clip, sfxVolume);
            
            // Запускаем корутину для отслеживания окончания звука
            StartCoroutine(WaitForSoundEnd(clip.length));
        }
    }
    
    /// <summary>
    /// Обрабатывает начало воспроизведения звука - приглушает музыку
    /// </summary>
    private void OnSoundStart()
    {
        activeSoundsCount++;
        
        // Если это первый звук, приглушаем музыку
        if (activeSoundsCount == 1 && musicAudioSource != null && musicAudioSource.isPlaying)
        {
            // Сохраняем текущую громкость (на случай если она уже была изменена)
            originalMusicVolume = musicAudioSource.volume;
            
            // Приглушаем музыку
            float reducedVolume = originalMusicVolume * musicVolumeDuringSFX;
            musicAudioSource.volume = reducedVolume;
        }
    }
    
    /// <summary>
    /// Обрабатывает окончание воспроизведения звука - восстанавливает громкость музыки
    /// </summary>
    private void OnSoundEnd()
    {
        activeSoundsCount = Mathf.Max(0, activeSoundsCount - 1);
        
        // Если все звуки закончились, восстанавливаем нормальную громкость музыки
        if (activeSoundsCount == 0 && musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.volume = originalMusicVolume;
        }
    }
    
    /// <summary>
    /// Корутина, которая ждет окончания звука
    /// </summary>
    private IEnumerator WaitForSoundEnd(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // Уведомляем об окончании звука
        OnSoundEnd();
    }
}


