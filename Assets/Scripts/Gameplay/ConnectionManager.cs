using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using DG.Tweening;

public class ConnectionManager : MonoBehaviour
{
    private PuzzleGrid grid;
    private Dictionary<Vector2Int, PuzzlePiece> piecesOnGrid;
    private AudioManager audioManager;
    
    [Header("Connection Animation")]
    public float scaleIncrease = 0.1f; // 10% increase (more spectacular)
    public float animationDuration = 0.3f;
    public float delayAfterMovement = 0.1f; // Delay before scale animation starts
    
    // Track active animations to prevent conflicts
    private Dictionary<PuzzlePiece, Tween> activeScaleTweens = new Dictionary<PuzzlePiece, Tween>();
    
    // Queue for deferred animations
    private Dictionary<PuzzlePiece, Action> queuedAnimations = new Dictionary<PuzzlePiece, Action>();
    
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
        
        // Animate the entire group together as one unit (with deferred timing)
        AnimateGroupScaleDeferred(connectedGroup);
    }
    
    // Public method for deferred connection checks from SwipeHandler
    public void CheckPieceConnectionsDeferred(PuzzlePiece piece, float delayAfterMovement = 0.1f)
    {
        if (piece == null) return;
        
        // Wait for movement to complete, then check connections
        StartCoroutine(WaitForMovementAndCheck(piece, delayAfterMovement));
    }
    
    private IEnumerator WaitForMovementAndCheck(PuzzlePiece piece, float delay)
    {
        // Wait for any active movement tweens to complete
        while (DOTween.IsTweening(piece.transform))
        {
            yield return null;
        }
        
        // Additional delay for polish
        yield return new WaitForSeconds(delay);
        
        // Now check connections
        CheckPieceConnections(piece);
    }
    
    private void AnimateGroupScaleDeferred(HashSet<PuzzlePiece> group)
    {
        if (group == null || group.Count == 0) return;
        
        // Start coroutine to wait for movement and then animate
        StartCoroutine(WaitForMovementAndAnimate(group));
    }
    
    private IEnumerator WaitForMovementAndAnimate(HashSet<PuzzlePiece> group)
    {
        List<PuzzlePiece> validPieces = new List<PuzzlePiece>();
        foreach (PuzzlePiece piece in group)
        {
            if (piece != null && piece.transform != null)
            {
                validPieces.Add(piece);
            }
        }
        
        if (validPieces.Count == 0) yield break;
        
        // Wait for all movement tweens to complete
        bool anyMoving = true;
        while (anyMoving)
        {
            anyMoving = false;
            foreach (PuzzlePiece piece in validPieces)
            {
                if (DOTween.IsTweening(piece.transform))
                {
                    anyMoving = true;
                    break;
                }
            }
            if (anyMoving)
            {
                yield return null;
            }
        }
        
        // Additional delay for polish
        yield return new WaitForSeconds(delayAfterMovement);
        
        // Now animate with final positions
        AnimateGroupScale(validPieces);
    }
    
    private void AnimateGroupScale(List<PuzzlePiece> validPieces)
    {
        if (validPieces == null || validPieces.Count == 0) return;
        
        // Kill any existing animations for pieces in this group
        foreach (PuzzlePiece piece in validPieces)
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
        
        // Get target positions from grid (final positions, not current)
        List<Vector3> targetPositions = GetTargetWorldPositions(validPieces);
        if (targetPositions.Count != validPieces.Count)
        {
            Debug.LogWarning("[ConnectionManager] AnimateGroupScale: Failed to get target positions");
            return;
        }
        
        // Calculate center using target positions
        Vector3 groupCenter = CalculateGroupCenterFromPositions(targetPositions);
        
        // Create a temporary parent container for the group
        GameObject groupContainer = new GameObject("GroupContainer_Temp");
        groupContainer.transform.position = groupCenter;
        
        // Store original parent, position, and sortingOrder for each piece
        Dictionary<PuzzlePiece, Transform> originalParents = new Dictionary<PuzzlePiece, Transform>();
        Dictionary<PuzzlePiece, Vector3> localPositions = new Dictionary<PuzzlePiece, Vector3>();
        Dictionary<PuzzlePiece, int> originalCardSortingOrders = new Dictionary<PuzzlePiece, int>();
        Dictionary<PuzzlePiece, int> originalBorderSortingOrders = new Dictionary<PuzzlePiece, int>();
        
        // Constants for animation sorting orders
        const int ANIMATION_CARD_SORTING_ORDER = 10;
        const int ANIMATION_BORDER_SORTING_ORDER = 11;
        
        for (int i = 0; i < validPieces.Count; i++)
        {
            PuzzlePiece piece = validPieces[i];
            Vector3 targetPos = targetPositions[i];
            
            originalParents[piece] = piece.transform.parent;
            
            // Store and set card sorting order
            SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                originalCardSortingOrders[piece] = sr.sortingOrder;
                sr.sortingOrder = ANIMATION_CARD_SORTING_ORDER;
            }
            
            // Store and set border sorting order
            BorderRenderer borderRenderer = piece.GetComponentInChildren<BorderRenderer>();
            if (borderRenderer != null)
            {
                // Get original border sorting order (check first border as reference)
                int originalBorderOrder = 1; // default
                if (borderRenderer.topBorder != null)
                {
                    SpriteRenderer borderSr = borderRenderer.topBorder.GetComponent<SpriteRenderer>();
                    if (borderSr != null)
                    {
                        originalBorderOrder = borderSr.sortingOrder;
                    }
                }
                originalBorderSortingOrders[piece] = originalBorderOrder;
                borderRenderer.SetBordersSortingOrder(ANIMATION_BORDER_SORTING_ORDER);
            }
            
            // Use target position (final position from grid)
            piece.transform.SetParent(groupContainer.transform);
            piece.transform.localPosition = groupContainer.transform.InverseTransformPoint(targetPos);
            localPositions[piece] = piece.transform.localPosition;
        }
        
        // Animate the container scale instead of individual cards
        Vector3 originalScale = groupContainer.transform.localScale;
        Sequence groupSequence = DOTween.Sequence();
        
        // More spectacular animation with bounce effect
        groupSequence.Append(groupContainer.transform.DOScale(originalScale * (1f + scaleIncrease), animationDuration / 2f)
            .SetEase(Ease.OutBack)); // Bounce effect for more spectacular feel
        groupSequence.Append(groupContainer.transform.DOScale(originalScale, animationDuration / 2f)
            .SetEase(Ease.InQuad));
        
        groupSequence.OnComplete(() => {
            // Restore original parents, positions, and sorting orders
            for (int i = 0; i < validPieces.Count; i++)
            {
                PuzzlePiece piece = validPieces[i];
                if (piece != null && piece.transform != null)
                {
                    Vector3 worldPos = groupContainer.transform.TransformPoint(localPositions[piece]);
                    piece.transform.SetParent(originalParents[piece]);
                    piece.transform.position = worldPos;
                    
                    // Restore card sorting order
                    if (originalCardSortingOrders.ContainsKey(piece))
                    {
                        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sortingOrder = originalCardSortingOrders[piece];
                        }
                    }
                    
                    // Restore border sorting order
                    if (originalBorderSortingOrders.ContainsKey(piece))
                    {
                        BorderRenderer borderRenderer = piece.GetComponentInChildren<BorderRenderer>();
                        if (borderRenderer != null)
                        {
                            borderRenderer.SetBordersSortingOrder(originalBorderSortingOrders[piece]);
                        }
                    }
                }
                if (activeScaleTweens.ContainsKey(piece))
                {
                    activeScaleTweens.Remove(piece);
                }
            }
            Destroy(groupContainer);
        });
        
        groupSequence.OnKill(() => {
            // Restore everything even if animation is killed
            for (int i = 0; i < validPieces.Count; i++)
            {
                PuzzlePiece piece = validPieces[i];
                if (piece != null && piece.transform != null)
                {
                    Vector3 worldPos = groupContainer.transform.TransformPoint(localPositions[piece]);
                    piece.transform.SetParent(originalParents[piece]);
                    piece.transform.position = worldPos;
                    
                    // Restore card sorting order
                    if (originalCardSortingOrders.ContainsKey(piece))
                    {
                        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sortingOrder = originalCardSortingOrders[piece];
                        }
                    }
                    
                    // Restore border sorting order
                    if (originalBorderSortingOrders.ContainsKey(piece))
                    {
                        BorderRenderer borderRenderer = piece.GetComponentInChildren<BorderRenderer>();
                        if (borderRenderer != null)
                        {
                            borderRenderer.SetBordersSortingOrder(originalBorderSortingOrders[piece]);
                        }
                    }
                }
                if (activeScaleTweens.ContainsKey(piece))
                {
                    activeScaleTweens.Remove(piece);
                }
            }
            Destroy(groupContainer);
        });
        
        // Store the sequence for tracking
        foreach (PuzzlePiece piece in validPieces)
        {
            activeScaleTweens[piece] = groupSequence;
        }
    }
    
    private List<Vector3> GetTargetWorldPositions(List<PuzzlePiece> pieces)
    {
        List<Vector3> positions = new List<Vector3>();
        
        foreach (PuzzlePiece piece in pieces)
        {
            if (piece != null && grid != null)
            {
                // Get final position from grid coordinates
                Vector2 worldPos2D = grid.GetWorldPosition(piece.currentGridRow, piece.currentGridCol);
                Vector3 worldPos = new Vector3(worldPos2D.x, worldPos2D.y, 0f);
                positions.Add(worldPos);
            }
        }
        
        return positions;
    }
    
    private Vector3 CalculateGroupCenter(List<PuzzlePiece> pieces)
    {
        if (pieces == null || pieces.Count == 0) return Vector3.zero;
        
        Vector3 center = Vector3.zero;
        foreach (PuzzlePiece piece in pieces)
        {
            if (piece != null && piece.transform != null)
            {
                center += piece.transform.position;
            }
        }
        return center / pieces.Count;
    }
    
    private Vector3 CalculateGroupCenterFromPositions(List<Vector3> positions)
    {
        if (positions == null || positions.Count == 0) return Vector3.zero;
        
        Vector3 center = Vector3.zero;
        foreach (Vector3 pos in positions)
        {
            center += pos;
        }
        return center / positions.Count;
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


