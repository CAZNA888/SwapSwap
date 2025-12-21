using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class CardDealer : MonoBehaviour
{
    [Header("Settings")]
    public float dealDuration = 0.5f;
    public float dealDelay = 0.1f;
    public Ease moveEase = Ease.OutQuad;
    
    private AudioManager audioManager;
    
    public void Initialize(AudioManager audio)
    {
        audioManager = audio;
    }
    
    public IEnumerator DealCards(List<PuzzlePiece> pieces, PuzzleGrid grid)
    {
        int index = 0;
        
        foreach (PuzzlePiece piece in pieces)
        {
            // Позиция на сетке (слева направо, сверху вниз)
            int row = index / grid.gridCols;
            int col = index % grid.gridCols;
            
            Vector2 targetPosition = grid.GetWorldPosition(row, col);
            
            // Устанавливаем только координаты сетки БЕЗ перемещения
            // Карточка уже находится в позиции колоды
            piece.SetGridCoordinates(row, col);
            
            // Плавное перемещение из текущей позиции (колоды) на сетку
            piece.transform.DOMove(targetPosition, dealDuration)
                .SetEase(moveEase)
                .OnStart(() => {
                    if (audioManager != null)
                    {
                        audioManager.PlayCardDeal();
                    }
                });
            
            index++;
            
            // Задержка перед следующей карточкой
            yield return new WaitForSeconds(dealDelay);
        }
        
        // Ждем завершения всех анимаций
        yield return new WaitForSeconds(dealDuration);
    }
}


