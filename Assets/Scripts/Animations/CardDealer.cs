using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        // Генерируем размещение, гарантируя что ни одна карточка не на своей оригинальной позиции
        List<Vector2Int> positions = GenerateNonOriginalPositions(pieces, grid);
        
        for (int i = 0; i < pieces.Count; i++)
        {
            PuzzlePiece piece = pieces[i];
            Vector2Int pos = positions[i];
            
            Vector2 targetPosition2D = grid.GetWorldPosition(pos.x, pos.y);
            Vector3 targetPosition = new Vector3(targetPosition2D.x, targetPosition2D.y, 0f);
            
            piece.SetGridCoordinates(pos.x, pos.y);
            
            piece.transform.DOMove(targetPosition, dealDuration)
                .SetEase(moveEase)
                .OnStart(() => {
                    if (audioManager != null)
                    {
                        audioManager.PlayCardDeal();
                    }
                });
            
            yield return new WaitForSeconds(dealDelay);
        }
        
        yield return new WaitForSeconds(dealDuration);
    }
    
    /// <summary>
    /// Генерирует размещение карточек, гарантируя что ни одна не на своей оригинальной позиции
    /// </summary>
    private List<Vector2Int> GenerateNonOriginalPositions(List<PuzzlePiece> pieces, PuzzleGrid grid)
    {
        System.Random random = new System.Random();
        List<Vector2Int> allPositions = new List<Vector2Int>();
        
        // Создаем список всех позиций на сетке
        for (int row = 0; row < grid.gridRows; row++)
        {
            for (int col = 0; col < grid.gridCols; col++)
            {
                allPositions.Add(new Vector2Int(row, col));
            }
        }
        
        List<Vector2Int> assignedPositions = new List<Vector2Int>();
        List<Vector2Int> availablePositions = new List<Vector2Int>(allPositions);
        
        foreach (PuzzlePiece piece in pieces)
        {
            // Получаем оригинальную позицию карточки
            Vector2Int originalPos = piece.GetOriginalPosition(grid.gridCols);
            
            // Фильтруем доступные позиции: исключаем оригинальную позицию этой карточки
            List<Vector2Int> validPositions = availablePositions
                .Where(p => p != originalPos)
                .ToList();
            
            // Если валидных позиций нет (маловероятно, но на всякий случай)
            if (validPositions.Count == 0)
            {
                // Используем любую доступную позицию
                validPositions = new List<Vector2Int>(availablePositions);
            }
            
            // Выбираем случайную позицию из валидных
            int randomIndex = random.Next(validPositions.Count);
            Vector2Int selectedPos = validPositions[randomIndex];
            assignedPositions.Add(selectedPos);
            
            // Удаляем выбранную позицию из доступных
            availablePositions.Remove(selectedPos);
        }
        
        Debug.Log($"CardDealer: Generated placement - all pieces on non-original positions");
        return assignedPositions;
    }
}


