using UnityEngine;

public class GridCell : MonoBehaviour
{
    [Header("Cell Data")]
    public int row;
    public int col;
    public PuzzlePiece currentPiece; // Карточка, которая стоит на этой ячейке
    
    private BoxCollider2D cellCollider;
    private PuzzleGrid grid;
    
    public void Initialize(int cellRow, int cellCol, PuzzleGrid puzzleGrid, Vector2 size)
    {
        row = cellRow;
        col = cellCol;
        grid = puzzleGrid;
        
        // Создаем коллайдер для ячейки
        cellCollider = GetComponent<BoxCollider2D>();
        if (cellCollider == null)
        {
            cellCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        cellCollider.size = size;
        cellCollider.isTrigger = true; // Триггер, чтобы не мешать карточкам
        
        // Устанавливаем позицию - ячейки ниже карточек по z
        Vector2 worldPos = grid.GetWorldPosition(row, col);
        transform.position = new Vector3(worldPos.x, worldPos.y, 1f); // z = 1 (дальше от камеры)
        
        gameObject.name = $"GridCell_{row}_{col}";
    }
    
    public void SetPiece(PuzzlePiece piece)
    {
        currentPiece = piece;
    }
    
    public bool IsEmpty()
    {
        return currentPiece == null;
    }
}

