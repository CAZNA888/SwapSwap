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
        
        // Используем OverlapPointAll чтобы получить все коллайдеры
        if (Camera.main != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            worldPos.z = 0;
            
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
            
            // Ищем PuzzlePiece среди всех коллайдеров, приоритет карточкам с меньшим z
            PuzzlePiece bestPiece = null;
            float closestZ = float.MaxValue;
            
            foreach (Collider2D hit in hits)
            {
                PuzzlePiece piece = hit.GetComponent<PuzzlePiece>();
                if (piece != null)
                {
                    // Выбираем карточку с наименьшим z (ближе к камере)
                    float pieceZ = piece.transform.position.z;
                    if (pieceZ < closestZ)
                    {
                        closestZ = pieceZ;
                        bestPiece = piece;
                    }
                }
            }
            
            clickedPiece = bestPiece;
        }
        
        // Fallback через EventSystem
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
        
        Debug.Log($"[SwipeHandler] OnPointerUp: Начало обработки. Группа из {selectedGroup.Count} карточек");
        
        // Сохраняем старые позиции группы
        Dictionary<PuzzlePiece, Vector2Int> oldPositions = SaveOldPositions(selectedGroup);
        Debug.Log($"[SwipeHandler] Сохранены старые позиции: {string.Join(", ", oldPositions.Select(kvp => $"{kvp.Key.name}->({kvp.Value.x},{kvp.Value.y})"))}");
        
        // Определяем целевую ЯЧЕЙКУ по коллайдеру
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        
        GridCell targetCell = grid.GetCellAtPosition(worldPos);
        
        if (targetCell == null)
        {
            Debug.LogWarning($"[SwipeHandler] Целевая ячейка не найдена! Возвращаем группу на исходные позиции.");
            ReturnGroupToOriginalPositions(selectedGroup, oldPositions);
            ResetGroupState();
            return;
        }
        
        Debug.Log($"[SwipeHandler] Найдена целевая ячейка: ({targetCell.row}, {targetCell.col})");
        
        // ПРОСТОЙ ОБМЕН: если перетаскивается одна карточка
        if (selectedGroup.Count == 1)
        {
            PuzzlePiece draggedPiece = selectedGroup[0];
            Vector2Int draggedOldPos = oldPositions[draggedPiece];
            GridCell draggedOldCell = grid.GetCellAt(draggedOldPos.x, draggedOldPos.y);
            
            if (draggedOldCell == null)
            {
                Debug.LogError($"[SwipeHandler] Не найдена старая ячейка для карточки {draggedPiece.name}");
                ReturnGroupToOriginalPositions(selectedGroup, oldPositions);
                ResetGroupState();
                return;
            }
            
            // Если целевая ячейка занята - обмениваем карточки
            if (!targetCell.IsEmpty())
            {
                PuzzlePiece targetPiece = targetCell.currentPiece;
                
                // ПРОВЕРКА: убеждаемся, что targetPiece действительно на этой ячейке
                if (targetPiece == null)
                {
                    Debug.LogWarning($"[SwipeHandler] targetCell не пуста, но currentPiece == null! Ищем в occupiedCells.");
                    // Исправляем - ищем карточку в occupiedCells
                    Vector2Int targetPos = new Vector2Int(targetCell.row, targetCell.col);
                    if (occupiedCells.TryGetValue(targetPos, out PuzzlePiece foundPiece))
                    {
                        targetPiece = foundPiece;
                        targetCell.SetPiece(foundPiece);
                        Debug.Log($"[SwipeHandler] Найдена карточка {foundPiece.name} в occupiedCells для ячейки ({targetCell.row}, {targetCell.col})");
                    }
                    else
                    {
                        Debug.LogError($"[SwipeHandler] Не найдена карточка в occupiedCells для ячейки ({targetCell.row}, {targetCell.col})");
                        ReturnGroupToOriginalPositions(selectedGroup, oldPositions);
                        ResetGroupState();
                        return;
                    }
                }
                
                // ПРОВЕРКА: убеждаемся, что draggedPiece действительно на старой ячейке
                if (draggedOldCell.currentPiece != draggedPiece)
                {
                    Debug.LogWarning($"[SwipeHandler] draggedOldCell.currentPiece != draggedPiece, исправляем");
                    draggedOldCell.SetPiece(draggedPiece);
                }
                
                Debug.Log($"[SwipeHandler] Обмен: {draggedPiece.name} ({draggedOldPos.x},{draggedOldPos.y}) <-> {targetPiece.name} ({targetCell.row},{targetCell.col})");
                SwapPieces(draggedPiece, draggedOldCell, targetPiece, targetCell);
            }
            else
            {
                // Перемещаем карточку в пустую ячейку
                Debug.Log($"[SwipeHandler] Перемещение: {draggedPiece.name} ({draggedOldPos.x},{draggedOldPos.y}) -> ({targetCell.row},{targetCell.col})");
                MovePieceToCell(draggedPiece, draggedOldCell, targetCell);
            }
            
            ResetGroupState();
            return;
        }
        
        // Перемещение группы с сохранением формы
        if (selectedGroup.Count > 1)
        {
            Debug.Log($"[SwipeHandler] Перемещение группы из {selectedGroup.Count} карточек в ячейку ({targetCell.row}, {targetCell.col})");
            MoveGroupWithShape(selectedGroup, targetCell, oldPositions);
            ResetGroupState();
            return;
        }
    }
    
    private void SwapPieces(PuzzlePiece piece1, GridCell cell1, PuzzlePiece piece2, GridCell cell2)
    {
        // ПРОВЕРКИ: убеждаемся, что карточки действительно на этих ячейках
        if (cell1.currentPiece != piece1)
        {
            Debug.LogWarning($"[SwipeHandler] SwapPieces: cell1.currentPiece != piece1! cell1: {cell1.currentPiece?.name}, piece1: {piece1.name}. Исправляем.");
            cell1.SetPiece(piece1);
        }
        
        if (cell2.currentPiece != piece2)
        {
            Debug.LogWarning($"[SwipeHandler] SwapPieces: cell2.currentPiece != piece2! cell2: {cell2.currentPiece?.name}, piece2: {piece2.name}. Исправляем.");
            cell2.SetPiece(piece2);
        }
        
        // Обновляем позиции карточек (сохраняем z = 0)
        Vector3 pos1 = new Vector3(
            grid.GetWorldPosition(cell2.row, cell2.col).x,
            grid.GetWorldPosition(cell2.row, cell2.col).y,
            0f
        );
        Vector3 pos2 = new Vector3(
            grid.GetWorldPosition(cell1.row, cell1.col).x,
            grid.GetWorldPosition(cell1.row, cell1.col).y,
            0f
        );
        
        piece1.transform.DOMove(pos1, moveDuration).SetEase(moveEase);
        piece2.transform.DOMove(pos2, moveDuration).SetEase(moveEase);
        
        // Убеждаемся, что коллайдеры активны
        BoxCollider2D collider1 = piece1.GetComponent<BoxCollider2D>();
        BoxCollider2D collider2 = piece2.GetComponent<BoxCollider2D>();
        if (collider1 != null) collider1.enabled = true;
        if (collider2 != null) collider2.enabled = true;
        
        // Обновляем координаты СРАЗУ (до анимации)
        Vector2Int oldPos1 = new Vector2Int(cell1.row, cell1.col);
        Vector2Int oldPos2 = new Vector2Int(cell2.row, cell2.col);
        Vector2Int newPos1 = new Vector2Int(cell2.row, cell2.col);
        Vector2Int newPos2 = new Vector2Int(cell1.row, cell1.col);
        
        piece1.SetPosition(newPos1.x, newPos1.y);
        piece2.SetPosition(newPos2.x, newPos2.y);
        
        // ВАЖНО: Сначала очищаем старые позиции в occupiedCells
        occupiedCells.Remove(oldPos1);
        occupiedCells.Remove(oldPos2);
        
        // Затем обновляем ячейки
        cell1.SetPiece(piece2);
        cell2.SetPiece(piece1);
        
        // Затем добавляем новые позиции в occupiedCells
        occupiedCells[newPos1] = piece1;
        occupiedCells[newPos2] = piece2;
        
        connectionManager.UpdatePieceOnGrid(piece1, oldPos1, newPos1);
        connectionManager.UpdatePieceOnGrid(piece2, oldPos2, newPos2);
        
        // Возвращаем sortingOrder
        SpriteRenderer sr1 = piece1.GetComponent<SpriteRenderer>();
        SpriteRenderer sr2 = piece2.GetComponent<SpriteRenderer>();
        if (sr1 != null) sr1.sortingOrder = 0;
        if (sr2 != null) sr2.sortingOrder = 0;
        
        // Проверяем соединения
        Sequence swapSeq = DOTween.Sequence();
        swapSeq.AppendInterval(moveDuration);
        swapSeq.OnComplete(() => {
            connectionManager.CheckAllConnections();
            if (audioManager != null) audioManager.PlaySwipe();
            if (gameManager != null) gameManager.OnPieceMoved();
        });
    }
    
    private void MovePieceToCell(PuzzlePiece piece, GridCell oldCell, GridCell newCell)
    {
        // ПРОВЕРКА: убеждаемся, что новая ячейка действительно пуста
        if (!newCell.IsEmpty())
        {
            Debug.LogError($"[SwipeHandler] MovePieceToCell: новая ячейка ({newCell.row}, {newCell.col}) не пуста! Содержит {newCell.currentPiece?.name}");
            // Очищаем ячейку
            newCell.SetPiece(null);
        }
        
        // ПРОВЕРКА: убеждаемся, что старая ячейка содержит эту карточку
        if (oldCell.currentPiece != piece)
        {
            Debug.LogWarning($"[SwipeHandler] MovePieceToCell: oldCell.currentPiece != piece! Исправляем.");
            oldCell.SetPiece(piece);
        }
        
        Vector2 worldPos = grid.GetWorldPosition(newCell.row, newCell.col);
        Vector3 newPos = new Vector3(worldPos.x, worldPos.y, 0f); // Сохраняем z = 0
        
        piece.transform.DOMove(newPos, moveDuration).SetEase(moveEase);
        
        // Убеждаемся, что коллайдер активен
        BoxCollider2D collider = piece.GetComponent<BoxCollider2D>();
        if (collider != null) collider.enabled = true;
        
        Vector2Int oldPos = new Vector2Int(oldCell.row, oldCell.col);
        Vector2Int newPosInt = new Vector2Int(newCell.row, newCell.col);
        
        // ВАЖНО: Сначала обновляем координаты карточки
        piece.SetPosition(newPosInt.x, newPosInt.y);
        
        // Затем очищаем старую позицию в occupiedCells
        occupiedCells.Remove(oldPos);
        
        // Затем обновляем ячейки
        oldCell.SetPiece(null);
        newCell.SetPiece(piece);
        
        // Затем добавляем новую позицию в occupiedCells
        occupiedCells[newPosInt] = piece;
        
        connectionManager.UpdatePieceOnGrid(piece, oldPos, newPosInt);
        
        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 0;
        
        Sequence moveSeq = DOTween.Sequence();
        moveSeq.AppendInterval(moveDuration);
        moveSeq.OnComplete(() => {
            connectionManager.CheckAllConnections();
            if (audioManager != null) audioManager.PlaySwipe();
            if (gameManager != null) gameManager.OnPieceMoved();
        });
    }
    
    private void MoveGroupWithShape(List<PuzzlePiece> group, GridCell targetCell, Dictionary<PuzzlePiece, Vector2Int> oldPositions)
    {
        // 1. Вычисляем относительные смещения карточек в группе
        Dictionary<PuzzlePiece, Vector2Int> offsets = new Dictionary<PuzzlePiece, Vector2Int>();
        Vector2Int mainOldPos = oldPositions[selectedPiece];
        
        foreach (PuzzlePiece piece in group)
        {
            if (oldPositions.TryGetValue(piece, out Vector2Int oldPos))
            {
                Vector2Int offset = new Vector2Int(
                    oldPos.x - mainOldPos.x,
                    oldPos.y - mainOldPos.y
                );
                offsets[piece] = offset;
            }
        }
        
        // 2. Определяем все целевые ячейки для группы
        HashSet<Vector2Int> targetCells = new HashSet<Vector2Int>();
        List<GridCell> targetGridCells = new List<GridCell>();
        
        foreach (var offset in offsets.Values)
        {
            int targetRow = targetCell.row + offset.x;
            int targetCol = targetCell.col + offset.y;
            
            if (grid.IsValidGridPosition(targetRow, targetCol))
            {
                Vector2Int targetPos = new Vector2Int(targetRow, targetCol);
                targetCells.Add(targetPos);
                
                GridCell cell = grid.GetCellAt(targetRow, targetCol);
                if (cell != null)
                {
                    targetGridCells.Add(cell);
                }
            }
        }
        
        // 3. Проверяем, что все ячейки валидны
        if (targetCells.Count != group.Count)
        {
            Debug.LogWarning($"[SwipeHandler] Группа не помещается в целевую область. Возвращаем на место.");
            ReturnGroupToOriginalPositions(group, oldPositions);
            return;
        }
        
        // 4. Находим карточки, которые нужно переместить (мешают размещению группы)
        List<PuzzlePiece> piecesToMove = new List<PuzzlePiece>();
        HashSet<Vector2Int> freedCells = new HashSet<Vector2Int>();
        
        foreach (var kvp in oldPositions)
        {
            freedCells.Add(kvp.Value); // Старые позиции группы
        }
        
        foreach (Vector2Int targetPos in targetCells)
        {
            GridCell cell = grid.GetCellAt(targetPos.x, targetPos.y);
            if (cell != null && !cell.IsEmpty())
            {
                PuzzlePiece blockingPiece = cell.currentPiece;
                if (!group.Contains(blockingPiece))
                {
                    piecesToMove.Add(blockingPiece);
                }
            }
        }
        
        // 5. Освобождаем старые ячейки группы
        foreach (var kvp in oldPositions)
        {
            Vector2Int oldPos = kvp.Value;
            GridCell oldCell = grid.GetCellAt(oldPos.x, oldPos.y);
            if (oldCell != null)
            {
                oldCell.SetPiece(null);
            }
            occupiedCells.Remove(oldPos);
        }
        
        // 6. Освобождаем ячейки, которые займет группа
        foreach (Vector2Int targetPos in targetCells)
        {
            GridCell cell = grid.GetCellAt(targetPos.x, targetPos.y);
            if (cell != null && !cell.IsEmpty())
            {
                PuzzlePiece blockingPiece = cell.currentPiece;
                if (!group.Contains(blockingPiece))
                {
                    Vector2Int blockingPos = new Vector2Int(blockingPiece.currentGridRow, blockingPiece.currentGridCol);
                    GridCell blockingCell = grid.GetCellAt(blockingPos.x, blockingPos.y);
                    if (blockingCell != null)
                    {
                        blockingCell.SetPiece(null);
                    }
                    occupiedCells.Remove(blockingPos);
                    freedCells.Add(blockingPos);
                }
                // ВАЖНО: Очищаем ячейку, даже если карточка в группе (она будет установлена заново)
                cell.SetPiece(null);
            }
        }
        
        // 7. Перемещаем мешающие карточки в освободившиеся ячейки
        if (piecesToMove.Count > 0 && freedCells.Count > 0)
        {
            List<Vector2Int> sortedFreedCells = freedCells.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
            int cellIndex = 0;
            
            foreach (PuzzlePiece piece in piecesToMove)
            {
                if (cellIndex >= sortedFreedCells.Count) break;
                
                Vector2Int newPos = sortedFreedCells[cellIndex];
                GridCell newCell = grid.GetCellAt(newPos.x, newPos.y);
                
                if (newCell != null)
                {
                    Vector2Int oldPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
                    GridCell oldCell = grid.GetCellAt(oldPos.x, oldPos.y);
                    Vector2 worldPos2D = grid.GetWorldPosition(newPos.x, newPos.y);
                    Vector3 newWorldPos = new Vector3(worldPos2D.x, worldPos2D.y, 0f); // z = 0
                    
                    piece.transform.DOMove(newWorldPos, moveDuration).SetEase(moveEase);
                    
                    // Убеждаемся, что коллайдер активен
                    BoxCollider2D collider = piece.GetComponent<BoxCollider2D>();
                    if (collider != null) collider.enabled = true;
                    
                    // ВАЖНО: Сначала обновляем координаты карточки
                    piece.SetPosition(newPos.x, newPos.y);
                    
                    // Затем очищаем старую позицию в occupiedCells
                    occupiedCells.Remove(oldPos);
                    
                    // Затем обновляем ячейки
                    if (oldCell != null)
                    {
                        oldCell.SetPiece(null);
                    }
                    newCell.SetPiece(piece);
                    
                    // Затем добавляем новую позицию в occupiedCells
                    occupiedCells[newPos] = piece;
                    connectionManager.UpdatePieceOnGrid(piece, oldPos, newPos);
                    
                    cellIndex++;
                }
            }
        }
        
        // 8. Размещаем группу на новом месте с сохранением формы
        List<Tween> tweens = new List<Tween>();
        
        foreach (PuzzlePiece piece in group)
        {
            if (!offsets.TryGetValue(piece, out Vector2Int offset))
                continue;
            
            int newRow = targetCell.row + offset.x;
            int newCol = targetCell.col + offset.y;
            
            if (!grid.IsValidGridPosition(newRow, newCol))
                continue;
            
            Vector2Int oldPos = oldPositions[piece];
            Vector2Int newPos = new Vector2Int(newRow, newCol);
            Vector2 worldPos2D = grid.GetWorldPosition(newRow, newCol);
            Vector3 newWorldPos = new Vector3(worldPos2D.x, worldPos2D.y, 0f); // z = 0
            
            GridCell newCell = grid.GetCellAt(newRow, newCol);
            if (newCell == null) continue;
            
            Tween tween = piece.transform.DOMove(newWorldPos, moveDuration).SetEase(moveEase);
            
            // Убеждаемся, что коллайдер активен
            BoxCollider2D collider = piece.GetComponent<BoxCollider2D>();
            if (collider != null) collider.enabled = true;
            tweens.Add(tween);
            
            // ВАЖНО: Сначала обновляем координаты карточки
            piece.SetPosition(newRow, newCol);
            
            // Затем обновляем ячейку (старая уже очищена на шаге 5, целевая очищена на шаге 6)
            // Но нужно убедиться, что ячейка пуста
            if (!newCell.IsEmpty() && newCell.currentPiece != piece)
            {
                Debug.LogWarning($"[SwipeHandler] MoveGroupWithShape: ячейка ({newRow}, {newCol}) не пуста! Содержит {newCell.currentPiece?.name}. Очищаем.");
                newCell.SetPiece(null);
            }
            newCell.SetPiece(piece);
            
            // Затем добавляем новую позицию в occupiedCells (старая уже удалена на шаге 5)
            occupiedCells[newPos] = piece;
            connectionManager.UpdatePieceOnGrid(piece, oldPos, newPos);
        }
        
        // 9. Возвращаем sortingOrder
        foreach (PuzzlePiece piece in group)
        {
            SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 0;
        }
        
        // 10. Ждем завершения анимаций
        Sequence sequence = DOTween.Sequence();
        foreach (var tween in tweens)
        {
            sequence.Join(tween);
        }
        sequence.OnComplete(() => {
            connectionManager.CheckAllConnections();
            if (audioManager != null) audioManager.PlaySwipe();
            if (gameManager != null) gameManager.OnPieceMoved();
        });
    }
    
    private void ResetGroupState()
    {
        selectedPiece = null;
        selectedGroup = null;
    }
    
    // Определяет, на какую карточку наведен курсор
    private PuzzlePiece GetPieceUnderCursor(PointerEventData eventData)
    {
        PuzzlePiece clickedPiece = null;
        
        // Пробуем через Physics2D
        if (Camera.main != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            worldPos.z = 0;
            
            // Используем OverlapPointAll чтобы получить все карточки под курсором
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
            
            // Фильтруем: исключаем карточки из группы и выбираем с наименьшим sortingOrder
            PuzzlePiece bestCandidate = null;
            int lowestSortingOrder = int.MaxValue;
            
            foreach (Collider2D hit in hits)
            {
                PuzzlePiece piece = hit.GetComponent<PuzzlePiece>();
                if (piece != null && (selectedGroup == null || !selectedGroup.Contains(piece)))
                {
                    SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                    int sortingOrder = sr != null ? sr.sortingOrder : 0;
                    
                    // Выбираем карточку с наименьшим sortingOrder (ниже всех)
                    if (sortingOrder < lowestSortingOrder)
                    {
                        lowestSortingOrder = sortingOrder;
                        bestCandidate = piece;
                    }
                }
            }
            
            clickedPiece = bestCandidate;
            
            if (clickedPiece != null)
            {
                Debug.Log($"[SwipeHandler] GetPieceUnderCursor: Найдена карточка {clickedPiece.name} через Physics2D в позиции {worldPos}, sortingOrder: {lowestSortingOrder}");
            }
        }
        
        // Если не нашли через Physics2D, пробуем через EventSystem
        if (clickedPiece == null && EventSystem.current != null)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (RaycastResult result in results)
            {
                PuzzlePiece piece = result.gameObject.GetComponent<PuzzlePiece>();
                if (piece != null && (selectedGroup == null || !selectedGroup.Contains(piece)))
                {
                    clickedPiece = piece;
                    Debug.Log($"[SwipeHandler] GetPieceUnderCursor: Найдена карточка {clickedPiece.name} через EventSystem");
                    break;
                }
            }
        }
        
        if (clickedPiece == null)
        {
            Debug.Log($"[SwipeHandler] GetPieceUnderCursor: Карточка под курсором не найдена");
        }
        
        return clickedPiece;
    }
    
    // Сохраняет старые позиции группы
    private Dictionary<PuzzlePiece, Vector2Int> SaveOldPositions(List<PuzzlePiece> group)
    {
        Dictionary<PuzzlePiece, Vector2Int> oldPositions = new Dictionary<PuzzlePiece, Vector2Int>();
        foreach (PuzzlePiece piece in group)
        {
            oldPositions[piece] = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
        }
        return oldPositions;
    }
    
    // Возвращает группу на исходные позиции с восстановлением grid координат
    private void ReturnGroupToOriginalPositions(List<PuzzlePiece> group, Dictionary<PuzzlePiece, Vector2Int> oldPositions)
    {
        foreach (PuzzlePiece piece in group)
        {
            if (oldPositions.TryGetValue(piece, out Vector2Int oldPos))
            {
                Vector2 worldPos2D = grid.GetWorldPosition(oldPos.x, oldPos.y);
                Vector3 worldPos = new Vector3(worldPos2D.x, worldPos2D.y, 0f); // z = 0
                piece.transform.DOMove(worldPos, moveDuration).SetEase(moveEase);
                // Восстанавливаем grid координаты
                piece.SetGridCoordinates(oldPos.x, oldPos.y);
                // Восстанавливаем в occupiedCells
                occupiedCells[oldPos] = piece;
                
                // Восстанавливаем ячейку
                GridCell cell = grid.GetCellAt(oldPos.x, oldPos.y);
                if (cell != null)
                {
                    cell.SetPiece(piece);
                }
                
                // Убеждаемся, что коллайдер активен
                BoxCollider2D collider = piece.GetComponent<BoxCollider2D>();
                if (collider != null) collider.enabled = true;
            }
        }
    }
    
    // Проверяет валидность всех ячеек
    private bool AreAllCellsValid(HashSet<Vector2Int> cells)
    {
        foreach (Vector2Int cell in cells)
        {
            if (!grid.IsValidGridPosition(cell.x, cell.y))
            {
                return false;
            }
        }
        return true;
    }
    
    // Вычисляет смещения на основе СТАРЫХ grid координат (до перетаскивания)
    private Dictionary<PuzzlePiece, Vector2Int> CalculateGroupOffsetsFromOldPositions(List<PuzzlePiece> group, Dictionary<PuzzlePiece, Vector2Int> oldPositions)
    {
        Dictionary<PuzzlePiece, Vector2Int> offsets = new Dictionary<PuzzlePiece, Vector2Int>();
        
        if (group.Count == 0 || selectedPiece == null) return offsets;
        
        // Получаем старую позицию главной карточки
        if (!oldPositions.TryGetValue(selectedPiece, out Vector2Int mainOldPos))
        {
            return offsets;
        }
        
        foreach (PuzzlePiece piece in group)
        {
            if (oldPositions.TryGetValue(piece, out Vector2Int pieceOldPos))
            {
                // Вычисляем смещение относительно главной карточки на основе СТАРЫХ позиций
                Vector2Int offset = new Vector2Int(
                    pieceOldPos.x - mainOldPos.x,
                    pieceOldPos.y - mainOldPos.y
                );
                offsets[piece] = offset;
            }
        }
        
        return offsets;
    }
    
    // Вычисляет относительные смещения карточек группы от главной карточки (старый метод, оставлен для совместимости)
    private Dictionary<PuzzlePiece, Vector2Int> CalculateGroupOffsets(List<PuzzlePiece> group)
    {
        Dictionary<PuzzlePiece, Vector2Int> offsets = new Dictionary<PuzzlePiece, Vector2Int>();
        
        if (group.Count == 0 || selectedPiece == null) return offsets;
        
        // Используем selectedPiece как главную карточку (точку отсчета)
        Vector2Int mainPos = new Vector2Int(selectedPiece.currentGridRow, selectedPiece.currentGridCol);
        
        foreach (PuzzlePiece piece in group)
        {
            Vector2Int offset = new Vector2Int(
                piece.currentGridRow - mainPos.x,
                piece.currentGridCol - mainPos.y
            );
            offsets[piece] = offset;
        }
        
        return offsets;
    }
    
    // Вычисляет все ячейки, которые займет группа при размещении в заданной позиции
    private HashSet<Vector2Int> GetGroupTargetCells(Vector2Int mainCell, Dictionary<PuzzlePiece, Vector2Int> offsets)
    {
        HashSet<Vector2Int> targetCells = new HashSet<Vector2Int>();
        
        foreach (var offset in offsets.Values)
        {
            Vector2Int targetCell = new Vector2Int(mainCell.x + offset.x, mainCell.y + offset.y);
            if (grid.IsValidGridPosition(targetCell.x, targetCell.y))
            {
                targetCells.Add(targetCell);
            }
        }
        
        return targetCells;
    }
    
    // Находит карточки, которые мешают размещению группы
    private List<PuzzlePiece> FindConflictingPieces(HashSet<Vector2Int> targetCells, List<PuzzlePiece> group)
    {
        List<PuzzlePiece> conflicting = new List<PuzzlePiece>();
        HashSet<PuzzlePiece> groupSet = new HashSet<PuzzlePiece>(group);
        
        foreach (Vector2Int cell in targetCells)
        {
            if (occupiedCells.TryGetValue(cell, out PuzzlePiece piece))
            {
                // Если карточка не входит в группу, она мешает размещению
                if (!groupSet.Contains(piece))
                {
                    conflicting.Add(piece);
                }
            }
        }
        
        return conflicting;
    }
    
    // Перемещает карточки в освободившиеся ячейки
    private void MovePiecesToFreedCells(List<PuzzlePiece> pieces, HashSet<Vector2Int> freedCells)
    {
        if (pieces.Count == 0 || freedCells.Count == 0)
        {
            Debug.Log($"[SwipeHandler] MovePiecesToFreedCells: Нет карточек или ячеек (pieces: {pieces.Count}, freedCells: {freedCells.Count})");
            return;
        }
        
        List<Vector2Int> sortedFreedCells = freedCells.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
        Debug.Log($"[SwipeHandler] MovePiecesToFreedCells: Перемещаем {pieces.Count} карточек в {sortedFreedCells.Count} ячеек");
        
        int cellIndex = 0;
        foreach (PuzzlePiece piece in pieces)
        {
            if (cellIndex >= sortedFreedCells.Count)
            {
                Debug.LogWarning($"[SwipeHandler] MovePiecesToFreedCells: Недостаточно свободных ячеек для карточки {piece.name}");
                break;
            }
            
            Vector2Int newPos = sortedFreedCells[cellIndex];
            Vector2 targetWorldPos = grid.GetWorldPosition(newPos.x, newPos.y);
            
            Vector2Int oldPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
            occupiedCells.Remove(oldPos);
            
            Debug.Log($"[SwipeHandler] MovePiecesToFreedCells: {piece.name} ({oldPos.x},{oldPos.y}) -> ({newPos.x},{newPos.y})");
            
            piece.transform.DOMove(targetWorldPos, moveDuration).SetEase(moveEase);
            connectionManager.UpdatePieceOnGrid(piece, oldPos, newPos);
            piece.SetPosition(newPos.x, newPos.y);
            occupiedCells[newPos] = piece;
            
            cellIndex++;
        }
    }
    
    // Перемещает мешающие карточки в освободившиеся ячейки
    private void MoveConflictingPieces(List<PuzzlePiece> conflicting, HashSet<Vector2Int> availableCells)
    {
        if (conflicting.Count == 0 || availableCells.Count == 0) return;
        
        // Преобразуем HashSet в отсортированный список
        List<Vector2Int> sortedAvailableCells = availableCells.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
        
        int cellIndex = 0;
        foreach (PuzzlePiece piece in conflicting)
        {
            if (cellIndex >= sortedAvailableCells.Count) break;
            
            Vector2Int newPos = sortedAvailableCells[cellIndex];
            Vector2 targetWorldPos = grid.GetWorldPosition(newPos.x, newPos.y);
            
            Vector2Int oldPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
            occupiedCells.Remove(oldPos);
            
            piece.transform.DOMove(targetWorldPos, moveDuration).SetEase(moveEase);
            connectionManager.UpdatePieceOnGrid(piece, oldPos, newPos);
            piece.SetPosition(newPos.x, newPos.y);
            occupiedCells[newPos] = piece;
            
            cellIndex++;
        }
    }
    
    // Размещает группу на новом месте с анимацией
    private List<Tween> PlaceGroup(List<PuzzlePiece> group, Vector2Int mainCell, Dictionary<PuzzlePiece, Vector2Int> offsets, Dictionary<PuzzlePiece, Vector2Int> oldPositions)
    {
        List<Tween> tweens = new List<Tween>();
        
        Debug.Log($"[SwipeHandler] PlaceGroup: Размещаем группу из {group.Count} карточек, главная ячейка: ({mainCell.x}, {mainCell.y})");
        
        foreach (PuzzlePiece piece in group)
        {
            if (!offsets.TryGetValue(piece, out Vector2Int offset))
            {
                Debug.LogWarning($"[SwipeHandler] PlaceGroup: Нет смещения для карточки {piece.name}");
                continue; // Пропускаем, если нет смещения
            }
            
            Vector2Int newPos = new Vector2Int(mainCell.x + offset.x, mainCell.y + offset.y);
            
            // Проверяем валидность - если невалидна, это ошибка, но мы уже проверили выше
            if (!grid.IsValidGridPosition(newPos.x, newPos.y))
            {
                Debug.LogError($"[SwipeHandler] PlaceGroup: Карточка {piece.name} не может быть размещена в позиции ({newPos.x}, {newPos.y})");
                continue;
            }
            
            Vector2 targetWorldPos = grid.GetWorldPosition(newPos.x, newPos.y);
            
            // Используем сохраненную старую позицию, а не текущую!
            if (!oldPositions.TryGetValue(piece, out Vector2Int oldPos))
            {
                oldPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
            }
            
            Debug.Log($"[SwipeHandler] PlaceGroup: {piece.name} ({oldPos.x},{oldPos.y}) -> ({newPos.x},{newPos.y}), offset: ({offset.x},{offset.y})");
            
            Tween tween = piece.transform.DOMove(targetWorldPos, moveDuration)
                .SetEase(moveEase);
            
            tweens.Add(tween);
            
            // Обновляем позицию СРАЗУ (не ждем анимации) - ВАЖНО для синхронизации occupiedCells
            connectionManager.UpdatePieceOnGrid(piece, oldPos, newPos);
            piece.SetPosition(newPos.x, newPos.y);
            occupiedCells[newPos] = piece; // ВАЖНО: добавляем в occupiedCells
            
            Debug.Log($"[SwipeHandler] PlaceGroup: {piece.name} добавлена в occupiedCells[({newPos.x},{newPos.y})]");
        }
        
        Debug.Log($"[SwipeHandler] PlaceGroup: Создано {tweens.Count} анимаций из {group.Count} карточек");
        return tweens;
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
        int[] connectionIndex = { 0, 1, 2, 3 }; // 0=верх, 1=низ, 2=лево, 3=право
        
        for (int i = 0; i < 4; i++)
        {
            // Проверяем, соединена ли карточка с этой стороны ФИЗИЧЕСКИ на поле
            if (!piece.isConnected[connectionIndex[i]])
            {
                continue; // Не соединена с этой стороны - пропускаем
            }
            
            int newRow = piece.currentGridRow + dx[i];
            int newCol = piece.currentGridCol + dy[i];
            
            if (grid.IsValidGridPosition(newRow, newCol))
            {
                Vector2Int neighborPos = new Vector2Int(newRow, newCol);
                if (occupiedCells.TryGetValue(neighborPos, out PuzzlePiece neighbor))
                {
                    // Проверяем, что сосед тоже соединен с этой карточкой
                    // Для соседа нужно проверить противоположную сторону
                    int oppositeIndex = (i % 2 == 0) ? (i + 1) : (i - 1); // 0<->1, 2<->3
                    if (neighbor.isConnected[oppositeIndex])
                    {
                        // Дополнительно проверяем, что они были соседями в оригинале
                        if (AreConnectedInOriginal(piece, neighbor))
                        {
                            neighbors.Add(neighbor);
                        }
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
    
    private void ShiftOtherPieces(List<PuzzlePiece> movedGroup)
    {
        // Получаем все пустые ячейки (уже учитывая размещенную группу)
        List<Vector2Int> emptyCells = grid.GetEmptyCells(occupiedCells);
        
        if (emptyCells.Count == 0) return;
        
        // Находим все карточки, которые нужно сдвинуть (не входят в перемещенную группу)
        List<PuzzlePiece> piecesToShift = new List<PuzzlePiece>();
        HashSet<PuzzlePiece> movedGroupSet = new HashSet<PuzzlePiece>(movedGroup);
        
        foreach (var kvp in occupiedCells)
        {
            if (!movedGroupSet.Contains(kvp.Value))
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
                
                // Пропускаем, если карточка уже на правильной позиции
                if (piece.currentGridRow == newPos.x && piece.currentGridCol == newPos.y)
                {
                    emptyIndex++;
                    continue;
                }
                
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
    
    // Метод для синхронизации ячеек и occupiedCells (для отладки и исправления)
    private void SyncCellsWithOccupiedCells()
    {
        // Проверяем все ячейки
        for (int row = 0; row < grid.gridRows; row++)
        {
            for (int col = 0; col < grid.gridCols; col++)
            {
                GridCell cell = grid.GetCellAt(row, col);
                Vector2Int pos = new Vector2Int(row, col);
                
                if (cell != null)
                {
                    if (occupiedCells.TryGetValue(pos, out PuzzlePiece piece))
                    {
                        // Ячейка должна содержать эту карточку
                        if (cell.currentPiece != piece)
                        {
                            Debug.LogWarning($"[SwipeHandler] Синхронизация: ячейка ({row},{col}) должна содержать {piece.name}, но содержит {cell.currentPiece?.name}");
                            cell.SetPiece(piece);
                        }
                    }
                    else
                    {
                        // occupiedCells не содержит эту позицию - ячейка должна быть пуста
                        if (cell.currentPiece != null)
                        {
                            Debug.LogWarning($"[SwipeHandler] Синхронизация: ячейка ({row},{col}) должна быть пуста, но содержит {cell.currentPiece.name}");
                            cell.SetPiece(null);
                        }
                    }
                }
            }
        }
    }
}


