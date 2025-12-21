using UnityEngine;
using DG.Tweening;

public class MoneyAnimation : MonoBehaviour
{
    [Header("Settings")]
    public float animationDuration = 1f;
    public Ease moveEase = Ease.InQuad;
    
    private Vector2 targetPosition;
    
    public void Initialize(Vector2 target)
    {
        targetPosition = target;
    }
    
    public void AnimateToTarget()
    {
        transform.DOMove(targetPosition, animationDuration)
            .SetEase(moveEase)
            .OnComplete(() => {
                Destroy(gameObject);
            });
    }
}


