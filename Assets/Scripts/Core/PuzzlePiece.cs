using UnityEngine;
using System.Collections.Generic;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Piece Data")]
    public int originalIndex; // Индекс в правильном расположении (0..n*k-1)
    public int currentGridRow;
    public int currentGridCol;
    
    [Header("Visual")]
    public Sprite frontSprite;
    public Sprite backSprite;
    public bool isFlipped = false;
    
    [Header("Borders")]
    public GameObject[] borderParts = new GameObject[4]; // 0=верх, 1=низ, 2=лево, 3=право
    public bool[] isConnected = new bool[4]; // Соединения со всех сторон
    
    private SpriteRenderer spriteRenderer;
    private PuzzleGrid grid;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
    
    public void Initialize(int index, Sprite front, Sprite back, PuzzleGrid puzzleGrid)
    {
        originalIndex = index;
        frontSprite = front;
        backSprite = back;
        grid = puzzleGrid;
        
        // Устанавливаем обратную сторону по умолчанию
        if (spriteRenderer != null && backSprite != null)
        {
            spriteRenderer.sprite = backSprite;
        }
    }
    
    // Устанавливает только координаты сетки БЕЗ перемещения карточки
    public void SetGridCoordinates(int row, int col)
    {
        currentGridRow = row;
        currentGridCol = col;
        // НЕ перемещаем карточку визуально!
    }
    
    // Устанавливает позицию карточки в колоде (правый нижний угол)
    public void SetDeckPosition(Vector2 deckPosition)
    {
        transform.position = deckPosition;
        // Не устанавливаем grid координаты, они будут установлены позже при раздаче
    }
    
    // Устанавливает позицию на сетке с визуальным перемещением
    public void SetPosition(int row, int col)
    {
        SetGridCoordinates(row, col);
        if (grid != null)
        {
            Vector2 worldPos = grid.GetWorldPosition(row, col);
            transform.position = worldPos;
        }
    }
    
    public Vector2Int GetOriginalPosition(int gridCols)
    {
        int row = originalIndex / gridCols;
        int col = originalIndex % gridCols;
        return new Vector2Int(row, col);
    }
    
    public void Flip()
    {
        isFlipped = !isFlipped;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isFlipped ? frontSprite : backSprite;
        }
    }
    
    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
    
    public bool IsAtCorrectPosition(int gridCols)
    {
        Vector2Int originalPos = GetOriginalPosition(gridCols);
        return originalPos.x == currentGridRow && originalPos.y == currentGridCol;
    }
    
    // Устанавливает размер карточки через масштабирование префаба
    public void SetCardSize(Vector2 targetSize)
    {
        if (spriteRenderer != null && backSprite != null)
        {
            // Используем размер backSprite (CardBack) как базовый размер префаба
            Vector2 baseSize = backSprite.bounds.size;
            
            // Вычисляем масштаб для всего префаба
            float scaleX = targetSize.x / baseSize.x;
            float scaleY = targetSize.y / baseSize.y;
            
            // Применяем масштаб ко всему префабу (включая дочерние элементы - рамки)
            transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }
}
