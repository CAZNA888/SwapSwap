using UnityEngine;
using System.Collections;
using PlayerPrefs = RedefineYG.PlayerPrefs;

/// <summary>
/// Менеджер фоновой музыки - синглтон, который не уничтожается между сценами.
/// Музыка продолжает играть при переходе между сценами.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Background Music - Addressables Keys")]
    [Tooltip("Ключ Addressable для фоновой музыки")]
    public string backgroundMusicKey = "";

    [Header("Legacy Audio Clips (для обратной совместимости)")]
    [Tooltip("Старый AudioClip - используется только если ключ Addressables пуст")]
    public AudioClip backgroundMusicClip;

    [Header("Settings")]
    [Tooltip("Громкость фоновой музыки - будет перезаписана из PlayerPrefs")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Tooltip("Задержка перед началом загрузки музыки (в секундах)")]
    [Range(0f, 5f)]
    public float musicLoadDelay = 0.5f;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";

    private AudioSource musicAudioSource;
    private AudioClip loadedMusicClip;
    private bool isMusicLoading = false;
    private bool isMusicLoaded = false;

    void Awake()
    {
        // Singleton pattern с DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            // Уже существует экземпляр - уничтожаем дубликат
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Создаем AudioSource для музыки
        musicAudioSource = GetComponent<AudioSource>();
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
        }
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true;

        // Загружаем сохраненные настройки громкости
        LoadSettings();
        ApplySettings();
    }

    void Start()
    {
        // Запускаем фоновую музыку, если она еще не играет
        if (!musicAudioSource.isPlaying)
        {
            PlayBackgroundMusic();
        }

        // Запускаем lazy loading музыки из Addressables
        StartCoroutine(LazyLoadMusicClip());
    }

    /// <summary>
    /// Lazy loading музыки из Addressables
    /// </summary>
    private IEnumerator LazyLoadMusicClip()
    {
        // Ждем указанную задержку перед началом загрузки
        yield return new WaitForSeconds(musicLoadDelay);

        if (string.IsNullOrEmpty(backgroundMusicKey))
        {
            isMusicLoaded = true;
            yield break;
        }

        isMusicLoading = true;

#if UNITY_ADDRESSABLES
        // Проверяем доступность Addressables
        if (!IsAddressablesAvailable())
        {
            Debug.LogWarning("MusicManager: Addressables не доступны. Используется legacy AudioClip.");
            isMusicLoading = false;
            isMusicLoaded = true;
            yield break;
        }

        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<AudioClip>(backgroundMusicKey);

        yield return handle;

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            loadedMusicClip = handle.Result;
            Debug.Log($"MusicManager: Музыка '{backgroundMusicKey}' успешно загружена из Addressables.");

            // Если музыка еще не играет или играет legacy клип - переключаемся на загруженный
            if (!musicAudioSource.isPlaying || musicAudioSource.clip == backgroundMusicClip)
            {
                float currentTime = musicAudioSource.time;
                musicAudioSource.clip = loadedMusicClip;
                musicAudioSource.time = currentTime; // Сохраняем позицию воспроизведения
                if (!musicAudioSource.isPlaying)
                {
                    musicAudioSource.Play();
                }
            }
        }
        else
        {
            Debug.LogError($"MusicManager: Не удалось загрузить музыку '{backgroundMusicKey}' из Addressables.");
        }
#else
        Debug.LogWarning("MusicManager: Addressables код не скомпилирован. Используется legacy AudioClip.");
#endif

        isMusicLoading = false;
        isMusicLoaded = true;
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
    /// Получает AudioClip для воспроизведения (из кэша или legacy)
    /// </summary>
    private AudioClip GetMusicClip()
    {
        // Если есть загруженный клип из Addressables, используем его
        if (loadedMusicClip != null)
        {
            return loadedMusicClip;
        }

        // Иначе используем legacy клип
        return backgroundMusicClip;
    }

    /// <summary>
    /// Загружает настройки громкости из PlayerPrefs
    /// </summary>
    private void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
    }

    /// <summary>
    /// Применяет настройки громкости к AudioSource
    /// </summary>
    private void ApplySettings()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = musicVolume;
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
    /// Получает текущую громкость музыки
    /// </summary>
    public float GetMusicVolume()
    {
        return musicVolume;
    }

    /// <summary>
    /// Воспроизводит фоновую музыку
    /// </summary>
    public void PlayBackgroundMusic()
    {
        AudioClip clipToPlay = GetMusicClip();

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
            AudioClip clipToPlay = GetMusicClip();
            if (clipToPlay != null)
            {
                musicAudioSource.UnPause();
            }
        }
    }

    /// <summary>
    /// Проверяет, играет ли музыка
    /// </summary>
    public bool IsPlaying()
    {
        return musicAudioSource != null && musicAudioSource.isPlaying;
    }

    /// <summary>
    /// Проверяет, загружена ли музыка из Addressables
    /// </summary>
    public bool IsMusicLoaded()
    {
        return isMusicLoaded;
    }
}
