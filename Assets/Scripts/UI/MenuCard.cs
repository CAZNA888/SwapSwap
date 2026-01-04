using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MenuCard : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Image компонент для отображения карточки")]
    public Image cardImage;
    
    [Header("Settings")]
    [Tooltip("Спрайт обратной стороны карточки (закрытая)")]
    public Sprite cardBackSprite;
    
    [Tooltip("Длительность анимации переворота")]
    public float flipDuration = 0.15f;
    
    private Sprite cardFrontSprite;
    private bool isUnlocked = false;
    private bool isAnimating = false;
    
    void Awake()
    {
        // Автоматически находим Image компонент, если не установлен
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
            if (cardImage == null)
            {
                cardImage = gameObject.AddComponent<Image>();
            }
        }
    }
    
    /// <summary>
    /// Устанавливает спрайт лицевой стороны карточки
    /// </summary>
    public void SetCardSprite(Sprite sprite)
    {
        cardFrontSprite = sprite;
        UpdateVisualState();
    }
    
    /// <summary>
    /// Устанавливает состояние карточки (закрыта/открыта)
    /// </summary>
    public void SetUnlocked(bool unlocked)
    {
        if (isUnlocked == unlocked) return;
        
        isUnlocked = unlocked;
        
        // Если карточка уже разблокирована, просто обновляем визуальное состояние
        if (isUnlocked && !isAnimating)
        {
            UpdateVisualState();
        }
    }
    
    /// <summary>
    /// Анимация переворота карточки (горизонтальный scale)
    /// </summary>
    public void FlipCard(System.Action onComplete = null)
    {
        if (isAnimating)
        {
            Debug.LogWarning("MenuCard: Flip animation already in progress!");
            onComplete?.Invoke();
            return;
        }
        
        if (isUnlocked)
        {
            Debug.LogWarning("MenuCard: Card is already unlocked!");
            onComplete?.Invoke();
            return;
        }
        
        isAnimating = true;
        RectTransform rectTransform = cardImage.rectTransform;
        Vector3 originalScale = rectTransform.localScale;
        
        // Уменьшаем по X до 0
        rectTransform.DOScaleX(0, flipDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                // Меняем состояние и спрайт
                isUnlocked = true;
                UpdateVisualState();
                
                // Увеличиваем обратно
                rectTransform.DOScaleX(originalScale.x, flipDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        isAnimating = false;
                        onComplete?.Invoke();
                    });
            });
    }
    
    /// <summary>
    /// Обновляет визуальное состояние карточки
    /// </summary>
    public void UpdateVisualState()
    {
        if (cardImage == null) return;
        
        if (isUnlocked && cardFrontSprite != null)
        {
            // Показываем лицевую сторону
            cardImage.sprite = cardFrontSprite;
        }
        else if (cardBackSprite != null)
        {
            // Показываем обратную сторону
            cardImage.sprite = cardBackSprite;
        }
        else
        {
            // Если нет спрайтов, делаем карточку прозрачной
            cardImage.sprite = null;
            cardImage.color = new Color(1, 1, 1, 0);
        }
    }
    
    /// <summary>
    /// Возвращает состояние карточки
    /// </summary>
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
    
    /// <summary>
    /// Проверяет, идет ли анимация
    /// </summary>
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    void OnDestroy()
    {
        // Останавливаем все анимации DOTween при уничтожении
        if (cardImage != null && cardImage.rectTransform != null)
        {
            cardImage.rectTransform.DOKill();
        }
    }
}




