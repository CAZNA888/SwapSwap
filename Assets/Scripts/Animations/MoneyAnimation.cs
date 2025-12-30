using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MoneyAnimation : MonoBehaviour
{
    [Header("Settings")]
    public float animationDuration = 1f;
    public Ease moveEase = Ease.InOutQuad; // Изменено на InOutQuad для плавной остановки
    
    private Vector2 targetPosition;
    private bool isUIElement = false;
    
    public void Initialize(Vector2 target)
    {
        targetPosition = target;
        // Проверяем, является ли это UI элементом
        RectTransform rectTransform = GetComponent<RectTransform>();
        isUIElement = (rectTransform != null);
    }
    
    public void AnimateToTarget()
    {
        if (isUIElement)
        {
            // Для UI элементов используем RectTransform.anchoredPosition
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.DOAnchorPos(targetPosition, animationDuration)
                    .SetEase(moveEase)
                    .OnComplete(() => {
                        // Убеждаемся, что монетка точно остановилась на финишной точке
                        rectTransform.anchoredPosition = targetPosition;
                        Destroy(gameObject);
                    });
            }
        }
        else
        {
            // Для обычных объектов используем transform.position
            transform.DOMove(targetPosition, animationDuration)
                .SetEase(moveEase)
                .OnComplete(() => {
                    // Убеждаемся, что монетка точно остановилась на финишной точке
                    transform.position = targetPosition;
                    Destroy(gameObject);
                });
        }
    }
}


