using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Clips - Addressables Keys")]
    [Tooltip("Ключ Addressable для звука раздачи карт")]
    public string cardDealKey = "";
    
    [Tooltip("Ключ Addressable для звука переворота карты")]
    public string cardFlipKey = "";
    
    [Tooltip("Ключ Addressable для звука свайпа")]
    public string swipeKey = "";
    
    [Tooltip("Ключ Addressable для звука завершения уровня")]
    public string levelCompleteKey = "";
    
    [Tooltip("Ключ Addressable для звука соединения")]
    public string connectionKey = "";
    
    [Tooltip("Ключ Addressable для звука монетки")]
    public string coinKey = "";
    
    [Header("Background Music")]
    [Tooltip("Ключ Addressable для фоновой музыки")]
    public string backgroundMusicKey = "";
    
    [Header("Legacy Audio Clips (для обратной совместимости)")]
    [Tooltip("Старые AudioClip поля - используются только если ключи Addressables пусты")]
    public AudioClip cardDealClip;
    public AudioClip cardFlipClip;
    public AudioClip swipeClip;
    public AudioClip levelCompleteClip;
    public AudioClip connectionClip;
    public AudioClip coinClip;
    public AudioClip backgroundMusicClip;
    
    [Header("Settings")]
    [Tooltip("Громкость звуков (SFX) - будет перезаписана из PlayerPrefs")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.5f;
    
    [Tooltip("Громкость фоновой музыки - будет перезаписана из PlayerPrefs")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    
    [Tooltip("Задержка перед началом загрузки звуков (в секундах) - для lazy loading")]
    [Range(0f, 5f)]
    public float audioLoadDelay = 1f;
    
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    
    private AudioSource sfxAudioSource; // Для звуков
    private AudioSource musicAudioSource; // Для фоновой музыки
    
    // Кэш для загруженных AudioClip из Addressables
    private Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();
    
    // Флаги состояния загрузки
    private bool isAudioLoading = false;
    private bool isAudioLoaded = false;
    
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
        // Запускаем фоновую музыку, если она есть (из legacy или загружаем сразу)
        PlayBackgroundMusic();
        
        // Запускаем lazy loading звуков после задержки
        StartCoroutine(LazyLoadAudioClips());
    }
    
    /// <summary>
    /// Lazy loading звуков из Addressables - загружает после старта игры
    /// </summary>
    private IEnumerator LazyLoadAudioClips()
    {
        // Ждем указанную задержку перед началом загрузки
        yield return new WaitForSeconds(audioLoadDelay);
        
        isAudioLoading = true;
        
#if UNITY_ADDRESSABLES
        // Проверяем доступность Addressables
        if (!IsAddressablesAvailable())
        {
            Debug.LogWarning("AudioManager: Addressables не доступны. Используются legacy AudioClip.");
            isAudioLoading = false;
            isAudioLoaded = true;
            yield break;
        }
        
        // Список всех ключей для загрузки
        List<string> keysToLoad = new List<string>();
        
        if (!string.IsNullOrEmpty(cardDealKey)) keysToLoad.Add(cardDealKey);
        if (!string.IsNullOrEmpty(cardFlipKey)) keysToLoad.Add(cardFlipKey);
        if (!string.IsNullOrEmpty(swipeKey)) keysToLoad.Add(swipeKey);
        if (!string.IsNullOrEmpty(levelCompleteKey)) keysToLoad.Add(levelCompleteKey);
        if (!string.IsNullOrEmpty(connectionKey)) keysToLoad.Add(connectionKey);
        if (!string.IsNullOrEmpty(coinKey)) keysToLoad.Add(coinKey);
        if (!string.IsNullOrEmpty(backgroundMusicKey)) keysToLoad.Add(backgroundMusicKey);
        
        // Загружаем все звуки асинхронно
        foreach (string key in keysToLoad)
        {
            yield return StartCoroutine(LoadAudioClipAsync(key));
        }
        
        Debug.Log($"AudioManager: Загружено {loadedClips.Count} звуков из Addressables.");
        
        // После загрузки запускаем фоновую музыку, если она еще не играет
        // Это важно для WebGL, где загрузка может занять время
        if (!string.IsNullOrEmpty(backgroundMusicKey) && loadedClips.ContainsKey(backgroundMusicKey))
        {
            if (musicAudioSource == null || !musicAudioSource.isPlaying)
            {
                PlayBackgroundMusic();
            }
        }
#else
        Debug.LogWarning("AudioManager: Addressables код не скомпилирован. Используются legacy AudioClip.");
#endif
        
        isAudioLoading = false;
        isAudioLoaded = true;
    }
    
    /// <summary>
    /// Асинхронно загружает AudioClip из Addressables
    /// </summary>
    private IEnumerator LoadAudioClipAsync(string addressableKey)
    {
        if (loadedClips.ContainsKey(addressableKey))
        {
            // Уже загружен
            yield break;
        }
        
#if UNITY_ADDRESSABLES
        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<AudioClip>(addressableKey);
        
        yield return handle;
        
        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            loadedClips[addressableKey] = handle.Result;
            Debug.Log($"AudioManager: Звук '{addressableKey}' успешно загружен из Addressables.");
        }
        else
        {
            Debug.LogError($"AudioManager: Не удалось загрузить звук '{addressableKey}' из Addressables.");
        }
#else
        yield break;
#endif
    }
    
    /// <summary>
    /// Проверяет доступность Addressables во время выполнения
    /// </summary>
    private bool IsAddressablesAvailable()
    {
#if UNITY_ADDRESSABLES
        return true;
#else
        System.Type addressablesType = System.Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
        return addressablesType != null;
#endif
    }
    
    /// <summary>
    /// Получает AudioClip по ключу (из кэша или legacy)
    /// </summary>
    private AudioClip GetAudioClip(string addressableKey, AudioClip legacyClip)
    {
        // Если есть загруженный клип из Addressables, используем его
        if (!string.IsNullOrEmpty(addressableKey) && loadedClips.ContainsKey(addressableKey))
        {
            return loadedClips[addressableKey];
        }
        
        // Если ключ указан, но клип еще не загружен - используем legacy как fallback
        // Это важно для WebGL, где загрузка может занять время
        // Иначе используем legacy клип напрямую
        return legacyClip;
    }
    
    /// <summary>
    /// Загружает настройки громкости из PlayerPrefs
    /// </summary>
    private void LoadSettings()
    {
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.5f);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
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
        
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = musicVolume;
        }
    }
    
    /// <summary>
    /// Воспроизводит фоновую музыку
    /// </summary>
    public void PlayBackgroundMusic()
    {
        AudioClip clipToPlay = GetAudioClip(backgroundMusicKey, backgroundMusicClip);
        
        if (clipToPlay != null && musicAudioSource != null)
        {
            musicAudioSource.clip = clipToPlay;
            musicAudioSource.volume = musicVolume;
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
        if (musicAudioSource != null && !musicAudioSource.isPlaying)
        {
            AudioClip clipToPlay = GetAudioClip(backgroundMusicKey, backgroundMusicClip);
            if (clipToPlay != null)
            {
                musicAudioSource.UnPause();
            }
        }
    }
    
    public void PlayCardDeal()
    {
        PlaySound(GetAudioClip(cardDealKey, cardDealClip));
    }
    
    public void PlayCardFlip()
    {
        PlaySound(GetAudioClip(cardFlipKey, cardFlipClip));
    }
    
    public void PlaySwipe()
    {
        PlaySound(GetAudioClip(swipeKey, swipeClip));
    }
    
    public void PlayLevelComplete()
    {
        PlaySound(GetAudioClip(levelCompleteKey, levelCompleteClip));
    }
    
    public void PlayConnection()
    {
        PlaySound(GetAudioClip(connectionKey, connectionClip));
    }
    
    public void PlayCoin()
    {
        PlaySound(GetAudioClip(coinKey, coinClip));
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxAudioSource != null)
        {
            // Воспроизводим звук - музыка продолжает играть на полной громкости
            sfxAudioSource.PlayOneShot(clip, sfxVolume);
        }
    }
}


