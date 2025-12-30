using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class ConnectionManager : MonoBehaviour
{
    private PuzzleGrid grid;
    private Dictionary<Vector2Int, PuzzlePiece> piecesOnGrid;
    private AudioManager audioManager;
    
    [Header("Connection Animation")]
    public float scaleIncrease = 0.075f; // 7.5% increase (between 5-10%)
    public float animationDuration = 0.3f;
    
    // Track active animations to prevent conflicts
    private Dictionary<PuzzlePiece, Tween> activeScaleTweens = new Dictionary<PuzzlePiece, Tween>();
    
    public void Initialize(PuzzleGrid puzzleGrid)
    {
        grid = puzzleGrid;
        piecesOnGrid = new Dictionary<Vector2Int, PuzzlePiece>();
    }
    
    public void SetAudioManager(AudioManager audioMgr)
    {
        audioManager = audioMgr;
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
        
        // Store old connection state
        bool[] oldConnections = new bool[4];
        if (piece.isConnected != null && piece.isConnected.Length == 4)
        {
            System.Array.Copy(piece.isConnected, oldConnections, 4);
        }
        
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
        
        // Check if new connections were made
        bool hasNewConnections = false;
        if (piece.isConnected != null && piece.isConnected.Length == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                if (connections[i] && !oldConnections[i])
                {
                    hasNewConnections = true;
                    break;
                }
            }
        }
        else
        {
            // First time checking connections - check if any connections exist
            for (int i = 0; i < 4; i++)
            {
                if (connections[i])
                {
                    hasNewConnections = true;
                    break;
                }
            }
        }
        
        // Обновляем границы
        piece.isConnected = connections;
        UpdateBorderVisibility(piece);
        
        // If new connections were made, animate the connected group
        if (hasNewConnections)
        {
            AnimateConnectedGroup(piece);
        }
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
    
    private void AnimateConnectedGroup(PuzzlePiece startPiece)
    {
        // Find all connected pieces in the group
        HashSet<PuzzlePiece> connectedGroup = FindConnectedGroup(startPiece);
        
        if (connectedGroup.Count == 0) return;
        
        // Play sound when connection is made
        if (audioManager != null)
        {
            audioManager.PlayConnection();
        }
        
        // Animate the entire group together as one unit
        AnimateGroupScale(connectedGroup);
    }
    
    private void AnimateGroupScale(HashSet<PuzzlePiece> group)
    {
        if (group == null || group.Count == 0) return;
        
        // Kill any existing animations for pieces in this group
        foreach (PuzzlePiece piece in group)
        {
            if (activeScaleTweens.ContainsKey(piece))
            {
                Tween existingTween = activeScaleTweens[piece];
                if (existingTween != null && existingTween.IsActive())
                {
                    existingTween.Kill();
                }
                activeScaleTweens.Remove(piece);
            }
        }
        
        // Store original scales for all pieces
        Dictionary<PuzzlePiece, Vector3> originalScales = new Dictionary<PuzzlePiece, Vector3>();
        List<PuzzlePiece> validPieces = new List<PuzzlePiece>();
        
        foreach (PuzzlePiece piece in group)
        {
            if (piece != null && piece.transform != null)
            {
                originalScales[piece] = piece.transform.localScale;
                validPieces.Add(piece);
            }
        }
        
        if (validPieces.Count == 0) return;
        
        // Create a single sequence that animates all pieces together
        Sequence groupSequence = DOTween.Sequence();
        
        // Scale up all pieces together - Append first, then Join the rest
        bool isFirst = true;
        foreach (PuzzlePiece piece in validPieces)
        {
            Vector3 targetScale = originalScales[piece] * (1f + scaleIncrease);
            Tween scaleUpTween = piece.transform.DOScale(targetScale, animationDuration / 2f)
                .SetEase(Ease.OutQuad);
            
            if (isFirst)
            {
                groupSequence.Append(scaleUpTween);
                isFirst = false;
            }
            else
            {
                groupSequence.Join(scaleUpTween);
            }
        }
        
        // Scale back down all pieces together - Append first, then Join the rest
        isFirst = true;
        foreach (PuzzlePiece piece in validPieces)
        {
            Tween scaleDownTween = piece.transform.DOScale(originalScales[piece], animationDuration / 2f)
                .SetEase(Ease.InQuad);
            
            if (isFirst)
            {
                groupSequence.Append(scaleDownTween);
                isFirst = false;
            }
            else
            {
                groupSequence.Join(scaleDownTween);
            }
        }
        
        // Ensure all scales are reset on complete or kill
        groupSequence.OnComplete(() => {
            foreach (PuzzlePiece piece in validPieces)
            {
                if (piece != null && piece.transform != null && originalScales.ContainsKey(piece))
                {
                    piece.transform.localScale = originalScales[piece];
                }
                if (activeScaleTweens.ContainsKey(piece))
                {
                    activeScaleTweens.Remove(piece);
                }
            }
        });
        
        groupSequence.OnKill(() => {
            foreach (PuzzlePiece piece in validPieces)
            {
                if (piece != null && piece.transform != null && originalScales.ContainsKey(piece))
                {
                    piece.transform.localScale = originalScales[piece];
                }
                if (activeScaleTweens.ContainsKey(piece))
                {
                    activeScaleTweens.Remove(piece);
                }
            }
        });
        
        // Store the sequence for each piece (so we can track it)
        foreach (PuzzlePiece piece in validPieces)
        {
            activeScaleTweens[piece] = groupSequence;
        }
    }
    
    private HashSet<PuzzlePiece> FindConnectedGroup(PuzzlePiece startPiece)
    {
        HashSet<PuzzlePiece> visited = new HashSet<PuzzlePiece>();
        Queue<PuzzlePiece> queue = new Queue<PuzzlePiece>();
        
        queue.Enqueue(startPiece);
        visited.Add(startPiece);
        
        while (queue.Count > 0)
        {
            PuzzlePiece current = queue.Dequeue();
            
            if (current == null || current.isConnected == null) continue;
            
            Vector2Int currentPos = new Vector2Int(current.currentGridRow, current.currentGridCol);
            
            // Check all 4 directions for connected neighbors
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(-1, 0),  // Top
                new Vector2Int(1, 0),   // Bottom
                new Vector2Int(0, -1),  // Left
                new Vector2Int(0, 1)    // Right
            };
            
            for (int i = 0; i < 4; i++)
            {
                if (current.isConnected[i])
                {
                    Vector2Int neighborPos = currentPos + directions[i];
                    
                    if (piecesOnGrid.TryGetValue(neighborPos, out PuzzlePiece neighbor))
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
        
        return visited;
    }
    
    // Method to reset all active animations (call this if needed)
    public void ResetAllAnimations()
    {
        foreach (var kvp in activeScaleTweens)
        {
            if (kvp.Value != null && kvp.Value.IsActive())
            {
                kvp.Value.Kill();
            }
            
            // Reset scale to original
            if (kvp.Key != null && kvp.Key.transform != null)
            {
                kvp.Key.transform.localScale = Vector3.one;
            }
        }
        
        activeScaleTweens.Clear();
    }
}


