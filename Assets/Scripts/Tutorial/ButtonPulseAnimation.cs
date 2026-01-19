using UnityEngine;
using System.Collections;

/// <summary>
/// Анимация пульсации кнопки - увеличение/уменьшение scale на заданный процент
/// </summary>
public class ButtonPulseAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Процент увеличения от текущего scale (например, 5 = увеличение на 5%)")]
    [Range(1f, 50f)]
    public float scalePercentage = 5f;
    
    [Tooltip("Длительность одного цикла анимации (увеличение + уменьшение)")]
    public float animationDuration = 0.5f;
    
    [Tooltip("Использовать бесконечный цикл")]
    public bool loop = true;
    
    [Tooltip("Задержка между циклами")]
    public float loopDelay = 0.1f;
    
    private Vector3 originalScale;
    private Coroutine pulseCoroutine;
    private bool isAnimating = false;
    
    void Awake()
    {
        originalScale = transform.localScale;
    }
    
    /// <summary>
    /// Запускает анимацию пульсации
    /// </summary>
    public void StartPulseAnimation()
    {
        if (isAnimating) return;
        
        originalScale = transform.localScale;
        isAnimating = true;
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }
    
    /// <summary>
    /// Останавливает анимацию пульсации
    /// </summary>
    public void StopPulseAnimation()
    {
        isAnimating = false;
        
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Возвращаем к исходному размеру
        transform.localScale = originalScale;
    }
    
    private IEnumerator PulseRoutine()
    {
        // Вычисляем целевой scale на основе процента
        // Если scalePercentage = 5, то scaleFactor = 1.05
        float scaleFactor = 1f + (scalePercentage / 100f);
        Vector3 targetScale = originalScale * scaleFactor;
        
        while (isAnimating)
        {
            // Увеличение
            yield return ScaleTo(targetScale, animationDuration / 2f);
            
            // Уменьшение
            yield return ScaleTo(originalScale, animationDuration / 2f);
            
            if (!loop)
            {
                isAnimating = false;
                break;
            }
            
            if (loopDelay > 0)
            {
                yield return new WaitForSeconds(loopDelay);
            }
        }
    }
    
    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Используем SmoothStep для плавной анимации
            t = t * t * (3f - 2f * t);
            
            transform.localScale = Vector3.Lerp(startScale, target, t);
            yield return null;
        }
        
        transform.localScale = target;
    }
    
    /// <summary>
    /// Проверяет, активна ли анимация
    /// </summary>
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    void OnDisable()
    {
        StopPulseAnimation();
    }
}
