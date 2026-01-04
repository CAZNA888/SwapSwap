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
        
        #if UNITY_EDITOR
        // ДИАГНОСТИКА: Проверяем размеры
        // ИСПРАВЛЕНО: Используем rect и pixelsPerUnit для консистентности
        if (frontSprite != null && backSprite != null)
        {
            Vector2 frontSize = new Vector2(
                frontSprite.rect.width / frontSprite.pixelsPerUnit,
                frontSprite.rect.height / frontSprite.pixelsPerUnit
            );
            Vector2 backSize = new Vector2(
                backSprite.rect.width / backSprite.pixelsPerUnit,
                backSprite.rect.height / backSprite.pixelsPerUnit
            );
            
            // Для диагностики также показываем bounds.size (может отличаться на разных устройствах)
            Vector2 frontBounds = frontSprite.bounds.size;
            Vector2 backBounds = backSprite.bounds.size;
            
            Debug.Log($"PuzzlePiece {index}: " +
                     $"Front size (rect/PPU)={frontSize}, bounds={frontBounds}, " +
                     $"Back size (rect/PPU)={backSize}, bounds={backBounds}");
        }
        #endif
        
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
            Sprite newSprite = isFlipped ? frontSprite : backSprite;
            spriteRenderer.sprite = newSprite;
            
            // Проверяем, что размеры спрайтов совпадают после переворота
            // ИСПРАВЛЕНО: Используем rect и pixelsPerUnit вместо bounds.size для стабильности
            // Это гарантирует одинаковые размеры на всех устройствах независимо от pixel ratio
            if (newSprite != null && backSprite != null)
            {
                // Используем расчет на основе rect и pixelsPerUnit вместо bounds.size
                Vector2 newSpriteSize = new Vector2(
                    newSprite.rect.width / newSprite.pixelsPerUnit,
                    newSprite.rect.height / newSprite.pixelsPerUnit
                );
                Vector2 backSpriteSize = new Vector2(
                    backSprite.rect.width / backSprite.pixelsPerUnit,
                    backSprite.rect.height / backSprite.pixelsPerUnit
                );
                
                // Вычисляем размер с учетом текущего масштаба
                Vector2 newSpriteSizeScaled = newSpriteSize * new Vector2(transform.localScale.x, transform.localScale.y);
                Vector2 backSpriteSizeScaled = backSpriteSize * new Vector2(transform.localScale.x, transform.localScale.y);
                
                float sizeDiffX = Mathf.Abs(newSpriteSizeScaled.x - backSpriteSizeScaled.x) / Mathf.Max(newSpriteSizeScaled.x, backSpriteSizeScaled.x);
                float sizeDiffY = Mathf.Abs(newSpriteSizeScaled.y - backSpriteSizeScaled.y) / Mathf.Max(newSpriteSizeScaled.y, backSpriteSizeScaled.y);
                
                // Если разница значительная, это указывает на проблему с pixelsPerUnit
                if (sizeDiffX > 0.01f || sizeDiffY > 0.01f)
                {
                    Debug.LogWarning($"PuzzlePiece {originalIndex}: After flip, sprite size mismatch! " +
                                   $"newSprite (scaled): {newSpriteSizeScaled.x:F4}x{newSpriteSizeScaled.y:F4}, " +
                                   $"backSprite (scaled): {backSpriteSizeScaled.x:F4}x{backSpriteSizeScaled.y:F4}, " +
                                   $"Difference: {sizeDiffX*100:F2}%/{sizeDiffY*100:F2}%. " +
                                   $"This indicates a pixelsPerUnit calculation issue in ImageSlicer.");
                    
                    // Корректируем масштаб только если разница критическая (>5%)
                    if (sizeDiffX > 0.05f || sizeDiffY > 0.05f)
                    {
                        float scaleCorrectionX = backSpriteSizeScaled.x / newSpriteSizeScaled.x;
                        float scaleCorrectionY = backSpriteSizeScaled.y / newSpriteSizeScaled.y;
                        Vector3 currentScale = transform.localScale;
                        transform.localScale = new Vector3(
                            currentScale.x * scaleCorrectionX,
                            currentScale.y * scaleCorrectionY,
                            currentScale.z
                        );
                        Debug.LogWarning($"PuzzlePiece {originalIndex}: Applied scale correction: {scaleCorrectionX:F4}x{scaleCorrectionY:F4}");
                    }
                }
            }
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
            // ВАЖНО: Используем расчет на основе rect и pixelsPerUnit вместо bounds.size
            // Это гарантирует стабильные размеры независимо от pixel ratio устройства
            Vector2 baseSize = new Vector2(
                backSprite.rect.width / backSprite.pixelsPerUnit,
                backSprite.rect.height / backSprite.pixelsPerUnit
            );
            
            // Вычисляем масштаб для всего префаба
            float scaleX = targetSize.x / baseSize.x;
            float scaleY = targetSize.y / baseSize.y;
            
            // Применяем масштаб ко всему префабу (включая дочерние элементы - рамки)
            transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }
}
