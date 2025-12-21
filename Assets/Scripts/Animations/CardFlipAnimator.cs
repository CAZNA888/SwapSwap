using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class CardFlipAnimator : MonoBehaviour
{
    [Header("Settings")]
    public float flipDuration = 0.15f;
    public float flipDelay = 0.05f;
    
    private AudioManager audioManager;
    private ConnectionManager connectionManager;
    
    public void Initialize(AudioManager audio, ConnectionManager connectionMgr)
    {
        audioManager = audio;
        connectionManager = connectionMgr;
    }
    
    public IEnumerator FlipAllCards(List<PuzzlePiece> pieces)
    {
        foreach (PuzzlePiece piece in pieces)
        {
            StartCoroutine(FlipCard(piece));
            yield return new WaitForSeconds(flipDelay);
        }
        
        // Ждем завершения всех переворотов
        yield return new WaitForSeconds(flipDuration * 2);
    }
    
    private IEnumerator FlipCard(PuzzlePiece piece)
    {
        Transform cardTransform = piece.transform;
        Vector3 originalScale = cardTransform.localScale;
        
        // Уменьшаем по X до 0
        cardTransform.DOScaleX(0, flipDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                // Меняем спрайт
                piece.Flip();
                
                // Обновляем видимость рамок после переворота
                if (connectionManager != null)
                {
                    connectionManager.CheckPieceConnections(piece);
                }
                
                // Дополнительная проверка - убеждаемся, что рамки показываются
                BorderRenderer br = piece.GetComponentInChildren<BorderRenderer>();
                if (br != null)
                {
                    Debug.Log($"После переворота карточки {piece.originalIndex}: top={br.topBorder != null && br.topBorder.activeSelf}, isFlipped={piece.isFlipped}");
                }
                
                // Увеличиваем обратно
                cardTransform.DOScaleX(originalScale.x, flipDuration)
                    .SetEase(Ease.OutQuad)
                    .OnStart(() => {
                        if (audioManager != null)
                        {
                            audioManager.PlayCardFlip();
                        }
                    });
            });
        
        yield return new WaitForSeconds(flipDuration * 2);
    }
}


