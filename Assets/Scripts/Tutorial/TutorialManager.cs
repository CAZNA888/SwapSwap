using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Менеджер обучения - управляет показом tutorial эффектов при первом запуске
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private const string FIRST_LAUNCH_KEY = "Tutorial_FirstLaunchCompleted";
    
    [Header("Play Button")]
    [Tooltip("Кнопка Play для анимации")]
    public Button playButton;
    
    [Tooltip("Компонент анимации пульсации (добавится автоматически если не указан)")]
    public ButtonPulseAnimation pulseAnimation;
    
    [Tooltip("Компонент свечения (добавится автоматически если не указан)")]
    public GlowEffect glowEffect;
    
    [Header("Tutorial Settings")]
    [Tooltip("Включить обучение только при первом запуске")]
    public bool onlyFirstLaunch = true;
    
    [Tooltip("Автоматически завершить обучение при клике на кнопку")]
    public bool completeOnClick = true;
    
    [Header("Debug")]
    [Tooltip("Принудительно показать tutorial (игнорирует сохраненное состояние)")]
    public bool forceShowTutorial = false;
    
    private bool tutorialActive = false;
    
    void Start()
    {
        if (ShouldShowTutorial())
        {
            StartTutorial();
        }
    }
    
    /// <summary>
    /// Проверяет, нужно ли показывать обучение
    /// </summary>
    private bool ShouldShowTutorial()
    {
        if (forceShowTutorial) return true;
        if (!onlyFirstLaunch) return true;
        
        // Проверяем, был ли первый запуск
        return PlayerPrefs.GetInt(FIRST_LAUNCH_KEY, 0) == 0;
    }
    
    /// <summary>
    /// Запускает обучение с эффектами для кнопки Play
    /// </summary>
    public void StartTutorial()
    {
        if (playButton == null)
        {
            Debug.LogError("TutorialManager: Play button is not assigned!");
            return;
        }
        
        // Получаем или добавляем компонент анимации пульсации
        if (pulseAnimation == null)
        {
            pulseAnimation = playButton.GetComponent<ButtonPulseAnimation>();
            if (pulseAnimation == null)
            {
                pulseAnimation = playButton.gameObject.AddComponent<ButtonPulseAnimation>();
            }
        }
        
        // Получаем или добавляем компонент свечения
        if (glowEffect == null)
        {
            glowEffect = playButton.GetComponent<GlowEffect>();
            if (glowEffect == null)
            {
                glowEffect = playButton.gameObject.AddComponent<GlowEffect>();
            }
        }
        
        // Запускаем эффекты
        pulseAnimation.StartPulseAnimation();
        glowEffect.StartGlow();
        
        tutorialActive = true;
        
        // Добавляем слушатель для завершения обучения при клике
        if (completeOnClick)
        {
            playButton.onClick.AddListener(CompleteTutorial);
        }
        
        Debug.Log("TutorialManager: Tutorial started");
    }
    
    /// <summary>
    /// Завершает обучение и сохраняет состояние
    /// </summary>
    public void CompleteTutorial()
    {
        if (!tutorialActive) return;
        
        // Останавливаем эффекты
        if (pulseAnimation != null)
        {
            pulseAnimation.StopPulseAnimation();
        }
        
        if (glowEffect != null)
        {
            glowEffect.StopGlow();
        }
        
        // Отмечаем, что обучение пройдено
        PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
        PlayerPrefs.Save();
        
        tutorialActive = false;
        
        // Удаляем слушатель
        if (playButton != null && completeOnClick)
        {
            playButton.onClick.RemoveListener(CompleteTutorial);
        }
        
        Debug.Log("TutorialManager: Tutorial completed");
    }
    
    /// <summary>
    /// Проверяет, активно ли обучение
    /// </summary>
    public bool IsTutorialActive()
    {
        return tutorialActive;
    }
    
    /// <summary>
    /// Проверяет, было ли обучение пройдено ранее
    /// </summary>
    public static bool IsTutorialCompleted()
    {
        return PlayerPrefs.GetInt(FIRST_LAUNCH_KEY, 0) == 1;
    }
    
    /// <summary>
    /// Сбросить состояние обучения (для тестирования)
    /// </summary>
    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(FIRST_LAUNCH_KEY);
        PlayerPrefs.Save();
        Debug.Log("TutorialManager: Tutorial reset - will show on next launch");
    }
    
    /// <summary>
    /// Пометить обучение как завершенное без показа
    /// </summary>
    [ContextMenu("Mark Tutorial Completed")]
    public void MarkTutorialCompleted()
    {
        PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("TutorialManager: Tutorial marked as completed");
    }
    
    void OnDestroy()
    {
        // Убираем слушатель при уничтожении
        if (playButton != null && completeOnClick)
        {
            playButton.onClick.RemoveListener(CompleteTutorial);
        }
    }
}
