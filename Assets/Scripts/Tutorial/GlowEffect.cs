using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Эффект желтого свечения под UI элементом с пульсацией
/// </summary>
public class GlowEffect : MonoBehaviour
{
    [Header("Glow Settings")]
    [Tooltip("Цвет свечения")]
    public Color glowColor = new Color(1f, 0.92f, 0.016f, 1f); // Желтый
    
    [Tooltip("Минимальная прозрачность свечения")]
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;
    
    [Tooltip("Максимальная прозрачность свечения")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;
    
    [Tooltip("Скорость пульсации")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Размер свечения относительно кнопки (добавляется к размеру)")]
    public Vector2 sizeOffset = new Vector2(20f, 20f);
    
    [Header("References")]
    [Tooltip("Image компонент для свечения (создастся автоматически если не указан)")]
    public Image glowImage;
    
    [Tooltip("Спрайт для свечения (если не указан, используется стандартный UI sprite)")]
    public Sprite glowSprite;
    
    private Coroutine glowCoroutine;
    private bool isGlowing = false;
    private GameObject glowObject;
    
    /// <summary>
    /// Запускает эффект свечения
    /// </summary>
    public void StartGlow()
    {
        if (isGlowing) return;
        
        if (glowImage == null)
        {
            CreateGlowImage();
        }
        
        glowImage.gameObject.SetActive(true);
        isGlowing = true;
        glowCoroutine = StartCoroutine(GlowPulseRoutine());
    }
    
    /// <summary>
    /// Останавливает эффект свечения
    /// </summary>
    public void StopGlow()
    {
        isGlowing = false;
        
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
            glowCoroutine = null;
        }
        
        if (glowImage != null)
        {
            glowImage.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Создает Image объект для свечения программно
    /// </summary>
    private void CreateGlowImage()
    {
        // Создаем дочерний объект для свечения
        glowObject = new GameObject("GlowEffect");
        glowObject.transform.SetParent(transform);
        glowObject.transform.localPosition = Vector3.zero;
        glowObject.transform.localRotation = Quaternion.identity;
        glowObject.transform.localScale = Vector3.one;
        
        // Помещаем за кнопку (первым ребенком), чтобы свечение было под кнопкой
        glowObject.transform.SetAsFirstSibling();
        
        // Добавляем RectTransform
        RectTransform glowRect = glowObject.AddComponent<RectTransform>();
        RectTransform parentRect = GetComponent<RectTransform>();
        
        if (parentRect != null)
        {
            // Устанавливаем размер больше родителя на sizeOffset
            glowRect.sizeDelta = parentRect.sizeDelta + sizeOffset;
        }
        else
        {
            glowRect.sizeDelta = new Vector2(100f, 100f) + sizeOffset;
        }
        
        glowRect.anchoredPosition = Vector2.zero;
        
        // Добавляем Image компонент
        glowImage = glowObject.AddComponent<Image>();
        
        if (glowSprite != null)
        {
            glowImage.sprite = glowSprite;
        }
        
        glowImage.color = glowColor;
        glowImage.raycastTarget = false; // Чтобы не блокировать клики по кнопке
        
        glowObject.SetActive(false);
    }
    
    private IEnumerator GlowPulseRoutine()
    {
        while (isGlowing)
        {
            // Пульсация прозрачности с помощью синусоиды
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            
            if (glowImage != null)
            {
                Color color = glowColor;
                color.a = alpha;
                glowImage.color = color;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Проверяет, активно ли свечение
    /// </summary>
    public bool IsGlowing()
    {
        return isGlowing;
    }
    
    /// <summary>
    /// Обновляет цвет свечения
    /// </summary>
    public void SetGlowColor(Color newColor)
    {
        glowColor = newColor;
        if (glowImage != null && isGlowing)
        {
            glowImage.color = newColor;
        }
    }
    
    void OnDisable()
    {
        StopGlow();
    }
    
    void OnDestroy()
    {
        if (glowObject != null)
        {
            Destroy(glowObject);
        }
    }
}
