using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class SwipeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float swipeThreshold = 0.1f;
    public float moveDuration = 0.3f;
    public Ease moveEase = Ease.OutQuad;
    
    private PuzzleGrid grid;
    private ConnectionManager connectionManager;
    private AudioManager audioManager;
    private GameManager gameManager;
    
    private PuzzlePiece selectedPiece;
    private List<PuzzlePiece> selectedGroup;
    private Vector3 dragOffset;
    private bool isDragging = false;
    private Dictionary<Vector2Int, PuzzlePiece> occupiedCells;
    
    public void Initialize(PuzzleGrid puzzleGrid, ConnectionManager connManager, AudioManager audio, Dictionary<Vector2Int, PuzzlePiece> cells, GameManager gm = null)
    {
        grid = puzzleGrid;
        connectionManager = connManager;
        audioManager = audio;
        occupiedCells = cells;
        gameManager = gm;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDragging) return;
        
        PuzzlePiece clickedPiece = null;
        
        // Сначала пробуем через Physics2D (более надежно для 2D)
        if (Camera.main != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            worldPos.z = 0;
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                clickedPiece = hit.GetComponent<PuzzlePiece>();
            }
        }
        
        // Если не нашли через Physics2D, пробуем через EventSystem
        if (clickedPiece == null && EventSystem.current != null)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (RaycastResult result in results)
            {
                clickedPiece = result.gameObject.GetComponent<PuzzlePiece>();
                if (clickedPiece != null) break;
            }
        }
        
        if (clickedPiece != null)
        {
            selectedPiece = clickedPiece;
            // Находим группу соединенных карточек
            selectedGroup = FindConnectedGroup(selectedPiece);
            
            // Вычисляем смещение от точки нажатия
            Vector3 worldPos = Camera.main != null ? 
                Camera.main.ScreenToWorldPoint(eventData.position) : 
                eventData.position;
            worldPos.z = 0;
            dragOffset = worldPos - selectedPiece.transform.position;
            
            isDragging = true;
            
            // Поднимаем карточки выше (изменяем sortingOrder)
            foreach (PuzzlePiece piece in selectedGroup)
            {
                SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 10; // Выше остальных
                }
            }
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || selectedGroup == null) return;
        
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        
        // Перемещаем всю группу с сохранением относительных позиций
        Vector3 targetPos = worldPos - dragOffset;
        
        foreach (PuzzlePiece piece in selectedGroup)
        {
            Vector3 relativeOffset = piece.transform.position - selectedPiece.transform.position;
            piece.transform.position = targetPos + relativeOffset;
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging || selectedGroup == null) return;
        
        isDragging = false;
        
        // Находим ячейку под курсором
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        Vector2Int targetCell = grid.GetGridPosition(worldPos);
        
        // Проверяем, есть ли карточка в целевой ячейке
        PuzzlePiece targetPiece = null;
        if (occupiedCells.TryGetValue(targetCell, out targetPiece))
        {
            // Если карточка в целевой ячейке не в нашей группе - нужно её переместить
            if (!selectedGroup.Contains(targetPiece))
            {
                // Находим пустые ячейки
                List<Vector2Int> emptyCells = grid.GetEmptyCells(occupiedCells);
                
                // Убираем ячейки, которые будут заняты нашей группой
                foreach (PuzzlePiece piece in selectedGroup)
                {
                    Vector2Int oldPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
                    emptyCells.Remove(oldPos);
                }
                
                if (emptyCells.Count > 0)
                {
                    // Перемещаем целевую карточку в первую пустую ячейку
                    Vector2Int newPosForTarget = emptyCells[0];
                    Vector2 targetWorldPos = grid.GetWorldPosition(newPosForTarget.x, newPosForTarget.y);
                    
                    Vector2Int oldTargetPos = new Vector2Int(targetPiece.currentGridRow, targetPiece.currentGridCol);
                    occupiedCells.Remove(oldTargetPos);
                    
                    targetPiece.transform.DOMove(targetWorldPos, moveDuration).SetEase(moveEase);
                    connectionManager.UpdatePieceOnGrid(targetPiece, oldTargetPos, newPosForTarget);
                    targetPiece.SetPosition(newPosForTarget.x, newPosForTarget.y);
                    occupiedCells[newPosForTarget] = targetPiece;
                }
            }
        }
        
        // Освобождаем старые ячейки группы
        foreach (PuzzlePiece piece in selectedGroup)
        {
            Vector2Int oldPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
            occupiedCells.Remove(oldPos);
        }
        
        // Вычисляем новые позиции для группы с сохранением формы
        Dictionary<PuzzlePiece, Vector2Int> newPositions = CalculateGroupPositions(selectedGroup, targetCell);
        
        // Перемещаем все карточки группы
        List<Tween> tweens = new List<Tween>();
        
        foreach (var kvp in newPositions)
        {
            PuzzlePiece piece = kvp.Key;
            Vector2Int newPos = kvp.Value;
            
            Vector2 targetWorldPos = grid.GetWorldPosition(newPos.x, newPos.y);
            
            Tween tween = piece.transform.DOMove(targetWorldPos, moveDuration)
                .SetEase(moveEase);
            
            tweens.Add(tween);
            
            // Обновляем позицию
            connectionManager.UpdatePieceOnGrid(piece, 
                new Vector2Int(piece.currentGridRow, piece.currentGridCol), 
                newPos);
            
            piece.SetPosition(newPos.x, newPos.y);
            occupiedCells[newPos] = piece;
        }
        
        // Смещаем остальные карточки в пустые места
        ShiftOtherPieces(selectedGroup);
        
        // Возвращаем sortingOrder обратно
        foreach (PuzzlePiece piece in selectedGroup)
        {
            SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 0; // Возвращаем обратно
            }
        }
        
        // Ждем завершения анимаций и проверяем соединения
        Sequence sequence = DOTween.Sequence();
        foreach (var tween in tweens)
        {
            sequence.Join(tween);
        }
        sequence.OnComplete(() => {
            connectionManager.CheckAllConnections();
            if (audioManager != null)
            {
                audioManager.PlaySwipe();
            }
            if (gameManager != null)
            {
                gameManager.OnPieceMoved();
            }
        });
        
        selectedPiece = null;
        selectedGroup = null;
    }
    
    private List<PuzzlePiece> FindConnectedGroup(PuzzlePiece startPiece)
    {
        HashSet<PuzzlePiece> visited = new HashSet<PuzzlePiece>();
        Queue<PuzzlePiece> queue = new Queue<PuzzlePiece>();
        
        queue.Enqueue(startPiece);
        visited.Add(startPiece);
        
        while (queue.Count > 0)
        {
            PuzzlePiece current = queue.Dequeue();
            
            // Находим всех соединенных соседей
            List<PuzzlePiece> neighbors = GetConnectedNeighbors(current);
            
            foreach (PuzzlePiece neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        return visited.ToList();
    }
    
    private List<PuzzlePiece> GetConnectedNeighbors(PuzzlePiece piece)
    {
        List<PuzzlePiece> neighbors = new List<PuzzlePiece>();
        
        // Проверяем все 4 стороны
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        
        for (int i = 0; i < 4; i++)
        {
            int newRow = piece.currentGridRow + dx[i];
            int newCol = piece.currentGridCol + dy[i];
            
            if (grid.IsValidGridPosition(newRow, newCol))
            {
                Vector2Int neighborPos = new Vector2Int(newRow, newCol);
                if (occupiedCells.TryGetValue(neighborPos, out PuzzlePiece neighbor))
                {
                    // Проверяем, соединены ли они в оригинале
                    if (AreConnectedInOriginal(piece, neighbor))
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }
        }
        
        return neighbors;
    }
    
    private bool AreConnectedInOriginal(PuzzlePiece a, PuzzlePiece b)
    {
        Vector2Int aOriginal = a.GetOriginalPosition(grid.gridCols);
        Vector2Int bOriginal = b.GetOriginalPosition(grid.gridCols);
        
        // Проверяем соседство в оригинале
        int rowDiff = Mathf.Abs(aOriginal.x - bOriginal.x);
        int colDiff = Mathf.Abs(aOriginal.y - bOriginal.y);
        
        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }
    
    private Dictionary<PuzzlePiece, Vector2Int> CalculateGroupPositions(List<PuzzlePiece> group, Vector2Int targetCell)
    {
        Dictionary<PuzzlePiece, Vector2Int> positions = new Dictionary<PuzzlePiece, Vector2Int>();
        
        if (group.Count == 0) return positions;
        
        // Находим минимальные координаты группы
        int minRow = group.Min(p => p.currentGridRow);
        int minCol = group.Min(p => p.currentGridCol);
        
        // Вычисляем смещения относительно минимума
        Dictionary<PuzzlePiece, Vector2Int> offsets = new Dictionary<PuzzlePiece, Vector2Int>();
        foreach (PuzzlePiece piece in group)
        {
            offsets[piece] = new Vector2Int(
                piece.currentGridRow - minRow,
                piece.currentGridCol - minCol
            );
        }
        
        // Пытаемся разместить группу начиная с targetCell
        Vector2Int startCell = targetCell;
        
        // Проверяем, помещается ли группа
        bool fits = true;
        foreach (var offset in offsets.Values)
        {
            Vector2Int checkCell = new Vector2Int(startCell.x + offset.x, startCell.y + offset.y);
            if (!grid.IsValidGridPosition(checkCell.x, checkCell.y) || 
                (occupiedCells.ContainsKey(checkCell) && !group.Any(p => p.currentGridRow == checkCell.x && p.currentGridCol == checkCell.y)))
            {
                fits = false;
                break;
            }
        }
        
        // Если не помещается, ищем ближайшее место
        if (!fits)
        {
            startCell = FindBestPositionForGroup(group, offsets);
        }
        
        // Вычисляем финальные позиции
        foreach (PuzzlePiece piece in group)
        {
            Vector2Int offset = offsets[piece];
            Vector2Int newPos = new Vector2Int(startCell.x + offset.x, startCell.y + offset.y);
            positions[piece] = newPos;
        }
        
        return positions;
    }
    
    private Vector2Int FindBestPositionForGroup(List<PuzzlePiece> group, Dictionary<PuzzlePiece, Vector2Int> offsets)
    {
        // Простой поиск: ищем первую свободную область
        for (int row = 0; row < grid.gridRows; row++)
        {
            for (int col = 0; col < grid.gridCols; col++)
            {
                Vector2Int candidate = new Vector2Int(row, col);
                bool fits = true;
                
                foreach (var offset in offsets.Values)
                {
                    Vector2Int checkCell = new Vector2Int(candidate.x + offset.x, candidate.y + offset.y);
                    if (!grid.IsValidGridPosition(checkCell.x, checkCell.y) || 
                        (occupiedCells.ContainsKey(checkCell) && !group.Any(p => p.currentGridRow == checkCell.x && p.currentGridCol == checkCell.y)))
                    {
                        fits = false;
                        break;
                    }
                }
                
                if (fits)
                {
                    return candidate;
                }
            }
        }
        
        // Если ничего не нашли, возвращаем первую позицию группы
        return new Vector2Int(group[0].currentGridRow, group[0].currentGridCol);
    }
    
    private void ShiftOtherPieces(List<PuzzlePiece> movedGroup)
    {
        // Получаем все пустые ячейки
        List<Vector2Int> emptyCells = grid.GetEmptyCells(occupiedCells);
        
        if (emptyCells.Count == 0) return;
        
        // Находим все карточки, которые нужно сдвинуть
        List<PuzzlePiece> piecesToShift = new List<PuzzlePiece>();
        
        foreach (var kvp in occupiedCells)
        {
            if (!movedGroup.Contains(kvp.Value))
            {
                piecesToShift.Add(kvp.Value);
            }
        }
        
        // Сортируем по позиции (сверху вниз, слева направо)
        piecesToShift = piecesToShift.OrderBy(p => p.currentGridRow).ThenBy(p => p.currentGridCol).ToList();
        emptyCells = emptyCells.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
        
        // Перемещаем карточки в пустые места
        int emptyIndex = 0;
        foreach (PuzzlePiece piece in piecesToShift)
        {
            if (emptyIndex < emptyCells.Count)
            {
                Vector2Int newPos = emptyCells[emptyIndex];
                Vector2 targetWorldPos = grid.GetWorldPosition(newPos.x, newPos.y);
                
                Vector2Int oldPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
                occupiedCells.Remove(oldPos);
                
                piece.transform.DOMove(targetWorldPos, moveDuration).SetEase(moveEase);
                
                connectionManager.UpdatePieceOnGrid(piece, oldPos, newPos);
                piece.SetPosition(newPos.x, newPos.y);
                occupiedCells[newPos] = piece;
                
                emptyIndex++;
            }
        }
    }
}

