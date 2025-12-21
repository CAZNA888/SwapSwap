using UnityEngine;
using System.Collections.Generic;

public class PuzzleGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridRows = 3;
    public int gridCols = 3;
    public float fieldWidth = 10f;
    public float fieldHeight = 10f;
    public float cardSpacing = 0.1f;
    
    [Header("Deck Position")]
    public Vector2 deckPosition = new Vector2(5f, -5f);
    
    private Vector2 cardSize;
    private Vector2 fieldStartPosition; // Левый верхний угол поля
    
    void Awake()
    {
        CalculateCardSize();
        CalculateFieldStartPosition();
    }
    
    public void Initialize(int rows, int cols, float width, float height, float spacing)
    {
        gridRows = rows;
        gridCols = cols;
        fieldWidth = width;
        fieldHeight = height;
        cardSpacing = spacing;
        
        CalculateCardSize();
        CalculateFieldStartPosition();
    }
    
    private void CalculateCardSize()
    {
        float cardWidth = (fieldWidth / gridCols) - cardSpacing;
        float cardHeight = (fieldHeight / gridRows) - cardSpacing;
        cardSize = new Vector2(cardWidth, cardHeight);
    }
    
    private void CalculateFieldStartPosition()
    {
        // Левый верхний угол поля
        // Если центр поля в (0,0), то левый верхний угол = (-fieldWidth/2, fieldHeight/2)
        fieldStartPosition = new Vector2(-fieldWidth / 2f, fieldHeight / 2f);
    }
    
    public Vector2 GetCardSize()
    {
        return cardSize;
    }
    
    // Получает размер карточки из спрайта CardBack
    public Vector2 GetCardSizeFromSprite(Sprite cardBackSprite)
    {
        if (cardBackSprite != null)
        {
            // Получаем размер спрайта в мировых единицах
            float width = cardBackSprite.bounds.size.x;
            float height = cardBackSprite.bounds.size.y;
            return new Vector2(width, height);
        }
        // Fallback на расчетный размер
        return GetCardSize();
    }
    
    public Vector2 GetWorldPosition(int row, int col)
    {
        // Расчет позиции от левого верхнего угла
        float x = fieldStartPosition.x + (col * (cardSize.x + cardSpacing)) + (cardSize.x / 2f);
        float y = fieldStartPosition.y - (row * (cardSize.y + cardSpacing)) - (cardSize.y / 2f);
        return new Vector2(x, y);
    }
    
    public Vector2Int GetGridPosition(Vector2 worldPos)
    {
        // Обратное преобразование
        Vector2 localPos = worldPos - fieldStartPosition;
        int col = Mathf.FloorToInt((localPos.x) / (cardSize.x + cardSpacing));
        int row = Mathf.FloorToInt((fieldStartPosition.y - localPos.y) / (cardSize.y + cardSpacing));
        
        // Ограничение границами
        col = Mathf.Clamp(col, 0, gridCols - 1);
        row = Mathf.Clamp(row, 0, gridRows - 1);
        
        return new Vector2Int(row, col);
    }
    
    public Vector2Int FindNearestEmptyCell(Vector2 worldPosition, Dictionary<Vector2Int, PuzzlePiece> occupiedCells)
    {
        Vector2Int gridPos = GetGridPosition(worldPosition);
        
        // Если ячейка свободна, возвращаем её
        if (!occupiedCells.ContainsKey(gridPos))
        {
            return gridPos;
        }
        
        // Ищем ближайшую свободную ячейку (BFS)
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(gridPos);
        visited.Add(gridPos);
        
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            if (!occupiedCells.ContainsKey(current))
            {
                return current;
            }
            
            for (int i = 0; i < 4; i++)
            {
                Vector2Int next = new Vector2Int(current.x + dx[i], current.y + dy[i]);
                
                if (IsValidCell(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }
        
        return gridPos; // Если все занято, возвращаем исходную
    }
    
    public List<Vector2Int> GetEmptyCells(Dictionary<Vector2Int, PuzzlePiece> occupiedCells)
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();
        
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Vector2Int cell = new Vector2Int(row, col);
                if (!occupiedCells.ContainsKey(cell))
                {
                    emptyCells.Add(cell);
                }
            }
        }
        
        return emptyCells;
    }
    
    private bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridRows && cell.y >= 0 && cell.y < gridCols;
    }
    
    public Vector2 GetDeckPosition()
    {
        return deckPosition;
    }
    
    public bool IsValidGridPosition(int row, int col)
    {
        return row >= 0 && row < gridRows && col >= 0 && col < gridCols;
    }
}


