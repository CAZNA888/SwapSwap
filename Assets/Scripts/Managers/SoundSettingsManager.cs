using UnityEngine;
using UnityEngine.UI;
using PlayerPrefs = RedefineYG.PlayerPrefs;

/// <summary>
/// Менеджер настроек звука - управляет слайдерами громкости.
/// Работает с AudioManager (SFX) и MusicManager (фоновая музыка).
/// </summary>
public class SoundSettingsManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Слайдер для громкости звуков (SFX)")]
    public Slider sfxVolumeSlider;
    
    [Tooltip("Слайдер для громкости фоновой музыки")]
    public Slider musicVolumeSlider;
    
    [Header("Settings")]
    [Tooltip("Значение громкости звуков по умолчанию")]
    [Range(0f, 1f)]
    public float defaultSFXVolume = 0.5f;
    
    [Tooltip("Значение громкости музыки по умолчанию")]
    [Range(0f, 1f)]
    public float defaultMusicVolume = 0.5f;
    
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    
    private AudioManager audioManager;
    private MusicManager musicManager;
    
    void Start()
    {
        // Находим AudioManager
        audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogWarning("SoundSettingsManager: AudioManager not found!");
        }
        
        // Получаем MusicManager (синглтон)
        musicManager = MusicManager.Instance;
        if (musicManager == null)
        {
            Debug.LogWarning("SoundSettingsManager: MusicManager not found!");
        }
        
        // Загружаем сохраненные значения
        LoadSettings();
        
        // Настраиваем слайдеры
        SetupSliders();
    }
    
    /// <summary>
    /// Настраивает слайдеры и подписывается на их изменения
    /// </summary>
    private void SetupSliders()
    {
        // Настраиваем слайдер SFX
        if (sfxVolumeSlider != null)
        {
            float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);
            sfxVolumeSlider.value = savedSFXVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        else
        {
            Debug.LogWarning("SoundSettingsManager: SFX Volume Slider is not assigned!");
        }
        
        // Настраиваем слайдер музыки
        if (musicVolumeSlider != null)
        {
            float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
            musicVolumeSlider.value = savedMusicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        else
        {
            Debug.LogWarning("SoundSettingsManager: Music Volume Slider is not assigned!");
        }
    }
    
    /// <summary>
    /// Вызывается при изменении слайдера SFX
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        // Сохраняем значение
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        PlayerPrefs.Save();
        
        // Применяем к AudioManager
        if (audioManager != null)
        {
            audioManager.SetSFXVolume(value);
        }
        
        Debug.Log($"SoundSettingsManager: SFX Volume changed to {value}");
    }
    
    /// <summary>
    /// Вызывается при изменении слайдера музыки
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        // Сохраняем значение
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
        PlayerPrefs.Save();
        
        // Применяем к MusicManager
        if (musicManager != null)
        {
            musicManager.SetMusicVolume(value);
        }
        
        Debug.Log($"SoundSettingsManager: Music Volume changed to {value}");
    }
    
    /// <summary>
    /// Загружает сохраненные настройки и применяет их к менеджерам
    /// </summary>
    private void LoadSettings()
    {
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
        
        // Применяем к AudioManager, если он уже существует
        if (audioManager != null)
        {
            audioManager.SetSFXVolume(sfxVolume);
        }
        
        // Применяем к MusicManager, если он уже существует
        if (musicManager != null)
        {
            musicManager.SetMusicVolume(musicVolume);
        }
    }
    
    /// <summary>
    /// Устанавливает ссылку на AudioManager (вызывается из AudioManager при его создании)
    /// </summary>
    public void SetAudioManager(AudioManager manager)
    {
        audioManager = manager;
        // Применяем текущие настройки к новому AudioManager
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);
        if (audioManager != null)
        {
            audioManager.SetSFXVolume(sfxVolume);
        }
    }
    
    /// <summary>
    /// Устанавливает ссылку на MusicManager (вызывается при необходимости)
    /// </summary>
    public void SetMusicManager(MusicManager manager)
    {
        musicManager = manager;
        // Применяем текущие настройки к MusicManager
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
        if (musicManager != null)
        {
            musicManager.SetMusicVolume(musicVolume);
        }
    }
}
