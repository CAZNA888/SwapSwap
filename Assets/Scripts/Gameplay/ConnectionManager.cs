using UnityEngine;
using System.Collections.Generic;

public class ConnectionManager : MonoBehaviour
{
    private PuzzleGrid grid;
    private Dictionary<Vector2Int, PuzzlePiece> piecesOnGrid;
    
    public void Initialize(PuzzleGrid puzzleGrid)
    {
        grid = puzzleGrid;
        piecesOnGrid = new Dictionary<Vector2Int, PuzzlePiece>();
    }
    
    public void UpdatePieceOnGrid(PuzzlePiece piece, Vector2Int oldPosition, Vector2Int newPosition)
    {
        if (oldPosition.x >= 0 && oldPosition.y >= 0)
        {
            piecesOnGrid.Remove(oldPosition);
        }
        
        if (newPosition.x >= 0 && newPosition.y >= 0)
        {
            piecesOnGrid[newPosition] = piece;
        }
    }
    
    public void CheckAllConnections()
    {
        foreach (var kvp in piecesOnGrid)
        {
            CheckPieceConnections(kvp.Value);
        }
    }
    
    public void CheckPieceConnections(PuzzlePiece piece)
    {
        if (piece == null || grid == null) return;
        
        bool[] connections = new bool[4]; // 0=верх, 1=низ, 2=лево, 3=право
        
        Vector2Int originalPos = piece.GetOriginalPosition(grid.gridCols);
        
        // Проверяем все 4 стороны
        // Верх (row - 1)
        if (originalPos.x > 0)
        {
            int neighborIndex = (originalPos.x - 1) * grid.gridCols + originalPos.y;
            connections[0] = CheckConnection(piece, neighborIndex, -1, 0);
        }
        
        // Низ (row + 1)
        if (originalPos.x < grid.gridRows - 1)
        {
            int neighborIndex = (originalPos.x + 1) * grid.gridCols + originalPos.y;
            connections[1] = CheckConnection(piece, neighborIndex, 1, 0);
        }
        
        // Лево (col - 1)
        if (originalPos.y > 0)
        {
            int neighborIndex = originalPos.x * grid.gridCols + (originalPos.y - 1);
            connections[2] = CheckConnection(piece, neighborIndex, 0, -1);
        }
        
        // Право (col + 1)
        if (originalPos.y < grid.gridCols - 1)
        {
            int neighborIndex = originalPos.x * grid.gridCols + (originalPos.y + 1);
            connections[3] = CheckConnection(piece, neighborIndex, 0, 1);
        }
        
        // Обновляем границы
        piece.isConnected = connections;
        UpdateBorderVisibility(piece);
    }
    
    private bool CheckConnection(PuzzlePiece piece, int neighborOriginalIndex, int rowOffset, int colOffset)
    {
        // Ищем соседа с таким originalIndex на поле
        Vector2Int neighborGridPos = new Vector2Int(
            piece.currentGridRow + rowOffset,
            piece.currentGridCol + colOffset
        );
        
        if (!grid.IsValidGridPosition(neighborGridPos.x, neighborGridPos.y))
        {
            return false;
        }
        
        if (piecesOnGrid.TryGetValue(neighborGridPos, out PuzzlePiece neighbor))
        {
            bool isConnected = neighbor.originalIndex == neighborOriginalIndex;
            if (isConnected)
            {
                Debug.Log($"[ConnectionManager] CheckConnection: {piece.name} (originalIndex={piece.originalIndex}) соединена с {neighbor.name} (originalIndex={neighbor.originalIndex}) на позиции ({neighborGridPos.x}, {neighborGridPos.y})");
            }
            return isConnected;
        }
        else
        {
            Debug.LogWarning($"[ConnectionManager] CheckConnection: не найдена карточка в piecesOnGrid для позиции ({neighborGridPos.x}, {neighborGridPos.y})");
        }
        
        return false;
    }
    
    private void UpdateBorderVisibility(PuzzlePiece piece)
    {
        BorderRenderer borderRenderer = piece.GetComponentInChildren<BorderRenderer>();
        if (borderRenderer != null)
        {
            borderRenderer.UpdateBorders(piece.isConnected, piece.isFlipped);
        }
    }
    
    public void RegisterPiece(PuzzlePiece piece)
    {
        Vector2Int gridPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
        if (grid.IsValidGridPosition(gridPos.x, gridPos.y))
        {
            piecesOnGrid[gridPos] = piece;
        }
    }
    
    public void UnregisterPiece(PuzzlePiece piece)
    {
        Vector2Int gridPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
        piecesOnGrid.Remove(gridPos);
    }
    
    // Синхронизирует piecesOnGrid с переданным словарем occupiedCells
    public void SyncWithOccupiedCells(Dictionary<Vector2Int, PuzzlePiece> occupiedCells)
    {
        // Очищаем старые данные
        piecesOnGrid.Clear();
        
        // Копируем данные из occupiedCells
        foreach (var kvp in occupiedCells)
        {
            piecesOnGrid[kvp.Key] = kvp.Value;
        }
        
        Debug.Log($"[ConnectionManager] SyncWithOccupiedCells: синхронизировано {piecesOnGrid.Count} карточек");
    }
}


