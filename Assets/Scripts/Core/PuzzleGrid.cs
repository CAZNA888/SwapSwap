using UnityEngine;
using System.Collections.Generic;

public class PuzzleGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridRows = 3;
    public int gridCols = 3;
    public float fieldWidth = 10f;
    public float fieldHeight = 10f;
    public float cardSpacing = 0.1f; // Оставляем для обратной совместимости
    
    [Header("Card Spacing")]
    [Tooltip("Горизонтальное расстояние между карточками (ширина)")]
    public float cardSpan = 0.1f;
    [Tooltip("Вертикальное расстояние между карточками (высота)")]
    public float cardHeight = 0.1f;
    
    [Header("Deck Position")]
    public Vector2 deckPosition = new Vector2(5f, -5f);
    
    [Header("Grid Cells")]
    public GameObject gridCellPrefab; // Опциональный префаб для ячейки
    
    private Vector2 cardSize;
    private Vector2 actualCardSize; // Реальный размер карточки после учета aspect ratio
    private Vector2 fieldStartPosition; // Левый верхний угол поля
    private Dictionary<Vector2Int, GridCell> gridCells = new Dictionary<Vector2Int, GridCell>();
    
    void Awake()
    {
        CalculateCardSize();
        CalculateFieldStartPosition();
    }
    
    public void Initialize(int rows, int cols, float width, float height, float span, float heightSpacing)
    {
        gridRows = rows;
        gridCols = cols;
        fieldWidth = width;
        fieldHeight = height;
        cardSpan = span;
        cardHeight = heightSpacing;
        
        Debug.Log($"PuzzleGrid.Initialize: cardSpan={cardSpan:F2}, cardHeight={cardHeight:F2}");
        
        CalculateCardSize();
        CalculateFieldStartPosition();
    }
    
    private void CalculateCardSize()
    {
        float cardWidth = (fieldWidth / gridCols) - cardSpan;
        float cardHeightValue = (fieldHeight / gridRows) - cardHeight;
        cardSize = new Vector2(cardWidth, cardHeightValue);
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
    
    // Устанавливает реальный размер карточки (после учета aspect ratio)
    public void SetActualCardSize(Vector2 actualSize)
    {
        actualCardSize = actualSize;
        Debug.Log($"PuzzleGrid.SetActualCardSize: actualCardSize={actualSize.x:F2}x{actualSize.y:F2}, calculated cardSize={cardSize.x:F2}x{cardSize.y:F2}");
    }
    
    public Vector2 GetWorldPosition(int row, int col)
    {
        // Используем реальный размер карточки если он установлен, иначе используем расчетный
        Vector2 sizeToUse = actualCardSize != Vector2.zero ? actualCardSize : cardSize;
        
        // Расчет позиции от левого верхнего угла
        // Используем реальный размер карточки + spacing для шага, чтобы карточки правильно позиционировались
        float cellWidth = sizeToUse.x + cardSpan;
        float cellHeight = sizeToUse.y + cardHeight;
        
        float x = fieldStartPosition.x + (col * cellWidth) + (sizeToUse.x / 2f);
        float y = fieldStartPosition.y - (row * cellHeight) - (sizeToUse.y / 2f);
        return new Vector2(x, y);
    }
    
    public Vector2Int GetGridPosition(Vector2 worldPos)
    {
        // Обратное преобразование
        // Используем реальный размер карточки если он установлен
        Vector2 sizeToUse = actualCardSize != Vector2.zero ? actualCardSize : cardSize;
        
        Vector2 localPos = worldPos - fieldStartPosition;
        int col = Mathf.FloorToInt((localPos.x) / (sizeToUse.x + cardSpan));
        int row = Mathf.FloorToInt((fieldStartPosition.y - localPos.y) / (sizeToUse.y + cardHeight));
        
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
    
    public void CreateGridCells()
    {
        // Удаляем старые ячейки если есть
        foreach (var cell in gridCells.Values)
        {
            if (cell != null) Destroy(cell.gameObject);
        }
        gridCells.Clear();
        
        // Создаем ячейки
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                GameObject cellObj;
                if (gridCellPrefab != null)
                {
                    cellObj = Instantiate(gridCellPrefab, transform);
                }
                else
                {
                    cellObj = new GameObject();
                    cellObj.transform.SetParent(transform);
                }
                
                GridCell cell = cellObj.GetComponent<GridCell>();
                if (cell == null)
                {
                    cell = cellObj.AddComponent<GridCell>();
                }
                
                Vector2 cellSize = new Vector2(cardSize.x + cardSpan, cardSize.y + cardHeight);
                cell.Initialize(row, col, this, cellSize);
                
                Vector2Int key = new Vector2Int(row, col);
                gridCells[key] = cell;
            }
        }
    }
    
    public GridCell GetCellAt(int row, int col)
    {
        Vector2Int key = new Vector2Int(row, col);
        gridCells.TryGetValue(key, out GridCell cell);
        return cell;
    }
    
    public GridCell GetCellAtPosition(Vector2 worldPos)
    {
        // Используем коллайдеры для определения ячейки
        // Используем OverlapPointAll чтобы получить все коллайдеры под точкой
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        
        // Ищем GridCell среди всех коллайдеров
        foreach (Collider2D hit in hits)
        {
            GridCell cell = hit.GetComponent<GridCell>();
            if (cell != null)
            {
                return cell;
            }
        }
        
        return null;
    }
}


