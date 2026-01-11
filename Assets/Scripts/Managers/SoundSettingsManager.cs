using UnityEngine;
using UnityEngine.UI;

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
    public float defaultSFXVolume = 1f;
    
    [Tooltip("Значение громкости музыки по умолчанию")]
    [Range(0f, 1f)]
    public float defaultMusicVolume = 1f;
    
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    
    private AudioManager audioManager;
    
    void Start()
    {
        // Находим AudioManager
        audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogWarning("SoundSettingsManager: AudioManager not found!");
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
        
        // Применяем к AudioManager
        if (audioManager != null)
        {
            audioManager.SetMusicVolume(value);
        }
        
        Debug.Log($"SoundSettingsManager: Music Volume changed to {value}");
    }
    
    /// <summary>
    /// Загружает сохраненные настройки и применяет их к AudioManager
    /// </summary>
    private void LoadSettings()
    {
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
        
        // Применяем к AudioManager, если он уже существует
        if (audioManager != null)
        {
            audioManager.SetSFXVolume(sfxVolume);
            audioManager.SetMusicVolume(musicVolume);
        }
    }
    
    /// <summary>
    /// Устанавливает ссылку на AudioManager (вызывается из AudioManager при его создании)
    /// </summary>
    public void SetAudioManager(AudioManager manager)
    {
        audioManager = manager;
        // Применяем текущие настройки к новому AudioManager
        LoadSettings();
    }
}




