using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class HintManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    
    [Header("Animation Settings")]
    public float hintScale = 1.2f; // Scale increase for hint animation
    public float hintDuration = 0.5f; // Duration of one pulse
    public int pulseCount = 3; // Number of pulses
    
    private Dictionary<PuzzlePiece, Tween> activeHintAnimations = new Dictionary<PuzzlePiece, Tween>();
    private Dictionary<PuzzlePiece, int> originalCardSortingOrders = new Dictionary<PuzzlePiece, int>();
    private Dictionary<PuzzlePiece, int> originalBorderSortingOrders = new Dictionary<PuzzlePiece, int>();
    private PuzzleGrid grid;
    private Dictionary<Vector2Int, PuzzlePiece> occupiedCells;
    
    // Константы для sorting order во время анимации подсказок
    private const int HINT_FIRST_CARD_SORTING_ORDER = 20;
    private const int HINT_FIRST_BORDER_SORTING_ORDER = 21;
    private const int HINT_SECOND_CARD_SORTING_ORDER = 15;
    private const int HINT_SECOND_BORDER_SORTING_ORDER = 16;
    private const int HINT_OTHER_CARD_SORTING_ORDER = 5;
    private const int HINT_OTHER_BORDER_SORTING_ORDER = 6;
    
    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }
    
    // Проверяет, находится ли карточка в группе (имеет ли соединения)
    private bool IsPieceInGroup(PuzzlePiece piece)
    {
        if (piece == null || piece.isConnected == null) return false;
        return piece.isConnected.Any(connected => connected);
    }
    
    // Находит карточку по originalIndex среди всех карточек
    private PuzzlePiece FindPieceByOriginalIndex(int originalIndex)
    {
        if (gameManager == null) return null;
        
        List<PuzzlePiece> allPieces = gameManager.GetAllPieces();
        if (allPieces == null) return null;
        
        foreach (PuzzlePiece piece in allPieces)
        {
            if (piece != null && piece.originalIndex == originalIndex)
            {
                return piece;
            }
        }
        
        return null;
    }
    
    // Простая логика подсказок: построчный проход по сетке
    // Возвращает true если найдена подсказка для обмена, false если для перемещения группы
    private bool FindSimplePositionHint(out PuzzlePiece piece1, out PuzzlePiece piece2, 
                                        out List<PuzzlePiece> group, out Vector2Int targetPosition)
    {
        piece1 = null;
        piece2 = null;
        group = null;
        targetPosition = Vector2Int.zero;
        
        if (gameManager == null || grid == null || occupiedCells == null)
        {
            Debug.LogWarning("[HintManager] FindSimplePositionHint: gameManager, grid или occupiedCells == null");
            return false;
        }
        
        // Проход по сетке построчно (слева направо, сверху вниз)
        for (int row = 0; row < grid.gridRows; row++)
        {
            for (int col = 0; col < grid.gridCols; col++)
            {
                Vector2Int pos = new Vector2Int(row, col);
                int expectedOriginalIndex = row * grid.gridCols + col;
                
                // Получить карточку на этой позиции
                if (occupiedCells.TryGetValue(pos, out PuzzlePiece currentPiece))
                {
                    // Если карточка правильная - пропустить
                    if (currentPiece.originalIndex == expectedOriginalIndex)
                    {
                        continue;
                    }
                    
                    // Найти карточку, которая должна быть на этой позиции
                    PuzzlePiece correctPiece = FindPieceByOriginalIndex(expectedOriginalIndex);
                    
                    if (correctPiece != null)
                    {
                        Debug.Log($"[HintManager] FindSimplePositionHint: позиция ({row}, {col}) - неправильная карточка {currentPiece.name} (originalIndex={currentPiece.originalIndex}), нужна {correctPiece.name} (originalIndex={expectedOriginalIndex})");
                        
                        // Проверить, находится ли нужная карточка в группе
                        if (IsPieceInGroup(correctPiece))
                        {
                            // Найти группу, в которой находится нужная карточка
                            List<PuzzlePiece> correctPieceGroup = FindConnectedGroup(correctPiece);
                            
                            if (correctPieceGroup != null && correctPieceGroup.Count > 0)
                            {
                                // Вычислить смещение группы, чтобы нужная карточка оказалась на правильной позиции
                                Vector2Int correctPieceCurrentPos = new Vector2Int(correctPiece.currentGridRow, correctPiece.currentGridCol);
                                Vector2Int offset = new Vector2Int(
                                    pos.x - correctPieceCurrentPos.x,
                                    pos.y - correctPieceCurrentPos.y
                                );
                                
                                // Проверить, можно ли разместить группу с этим смещением
                                if (CanPlaceGroupAtOffset(correctPieceGroup, offset))
                                {
                                    Vector2Int mainPos = new Vector2Int(correctPieceGroup[0].currentGridRow, correctPieceGroup[0].currentGridCol);
                                    targetPosition = new Vector2Int(
                                        mainPos.x + offset.x,
                                        mainPos.y + offset.y
                                    );
                                    group = correctPieceGroup;
                                    Debug.Log($"[HintManager] FindSimplePositionHint: найдено перемещение группы из {group.Count} карточек на позицию ({targetPosition.x}, {targetPosition.y})");
                                    return true; // Найдена подсказка для группы
                                }
                                else
                                {
                                    Debug.Log($"[HintManager] FindSimplePositionHint: группа не может быть размещена на позиции ({pos.x}, {pos.y}), пропускаем");
                                    continue; // Пропускаем эту позицию, ищем следующую
                                }
                            }
                        }
                        else
                        {
                            // Простой обмен двух карточек
                            piece1 = currentPiece;
                            piece2 = correctPiece;
                            Debug.Log($"[HintManager] FindSimplePositionHint: найдена подсказка для обмена карточек {currentPiece.originalIndex} и {correctPiece.originalIndex}");
                            return true; // Найдена подсказка для обмена
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[HintManager] FindSimplePositionHint: не найдена карточка с originalIndex={expectedOriginalIndex} для позиции ({row}, {col})");
                    }
                }
            }
        }
        
        Debug.Log("[HintManager] FindSimplePositionHint: не найдено подсказок");
        return false;
    }
    
    // УДАЛЕНО: WouldConnectAfterSwap - больше не используется
    // УДАЛЕНО: WouldConnectAtPosition - больше не используется
    
    // Находит группу соединенных карточек (аналогично SwipeHandler)
    private List<PuzzlePiece> FindConnectedGroup(PuzzlePiece startPiece)
    {
        if (startPiece == null || occupiedCells == null) return new List<PuzzlePiece>();
        
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
    
    // Получает соединенных соседей карточки
    private List<PuzzlePiece> GetConnectedNeighbors(PuzzlePiece piece)
    {
        List<PuzzlePiece> neighbors = new List<PuzzlePiece>();
        
        if (piece == null || piece.isConnected == null || grid == null || occupiedCells == null) 
            return neighbors;
        
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        
        for (int i = 0; i < 4; i++)
        {
            if (!piece.isConnected[i]) continue;
            
            int neighborRow = piece.currentGridRow + dx[i];
            int neighborCol = piece.currentGridCol + dy[i];
            
            if (!grid.IsValidGridPosition(neighborRow, neighborCol)) continue;
            
            Vector2Int neighborPos = new Vector2Int(neighborRow, neighborCol);
            if (occupiedCells.TryGetValue(neighborPos, out PuzzlePiece neighbor))
            {
                // Проверяем, что сосед тоже соединен с обратной стороны
                int oppositeIndex = (i % 2 == 0) ? (i + 1) : (i - 1); // 0<->1, 2<->3
                if (neighbor.isConnected != null && neighbor.isConnected[oppositeIndex])
                {
                    neighbors.Add(neighbor);
                }
            }
        }
        
        return neighbors;
    }
    
    // Находит лучшее место для перемещения группы
    private bool FindBestPositionForGroup(List<PuzzlePiece> group, out Vector2Int targetPosition)
    {
        targetPosition = Vector2Int.zero;
        
        if (group == null || group.Count == 0 || grid == null) return false;
        
        Debug.Log($"[HintManager] FindBestPositionForGroup: ищем позицию для группы из {group.Count} карточек");
        
        // Стратегия 1: Переместить группу так, чтобы карточка оказалась на правильном месте
        foreach (PuzzlePiece wrongPiece in group)
        {
            if (wrongPiece.IsAtCorrectPosition(grid.gridCols)) continue;
            
            // Находим её правильную позицию
            Vector2Int correctPos = wrongPiece.GetOriginalPosition(grid.gridCols);
            
            // Вычисляем смещение группы относительно этой карточки
            Vector2Int currentPiecePos = new Vector2Int(wrongPiece.currentGridRow, wrongPiece.currentGridCol);
            Vector2Int offset = new Vector2Int(
                correctPos.x - currentPiecePos.x,
                correctPos.y - currentPiecePos.y
            );
            
            // Проверяем, поместится ли группа с этим смещением
            if (CanPlaceGroupAtOffset(group, offset))
            {
                Vector2Int mainPos = new Vector2Int(group[0].currentGridRow, group[0].currentGridCol);
                targetPosition = new Vector2Int(
                    mainPos.x + offset.x,
                    mainPos.y + offset.y
                );
                Debug.Log($"[HintManager] FindBestPositionForGroup: найдена позиция через правильное место карточки {wrongPiece.name}");
                return true;
            }
        }
        
        // Стратегия 2: Ищем позиции, где группа может создать соединение с другими карточками
        // Проверяем все возможные смещения в пределах разумного диапазона
        int maxOffset = 3; // Максимальное смещение для поиска
        
        for (int offsetX = -maxOffset; offsetX <= maxOffset; offsetX++)
        {
            for (int offsetY = -maxOffset; offsetY <= maxOffset; offsetY++)
            {
                if (offsetX == 0 && offsetY == 0) continue; // Пропускаем текущую позицию
                
                Vector2Int offset = new Vector2Int(offsetX, offsetY);
                
                // Проверяем, поместится ли группа с этим смещением
                if (CanPlaceGroupAtOffset(group, offset))
                {
                    // Проверяем, создаст ли это смещение новое соединение
                    if (WouldGroupCreateConnection(group, offset))
                    {
                        Vector2Int mainPos = new Vector2Int(group[0].currentGridRow, group[0].currentGridCol);
                        targetPosition = new Vector2Int(
                            mainPos.x + offset.x,
                            mainPos.y + offset.y
                        );
                        Debug.Log($"[HintManager] FindBestPositionForGroup: найдена позиция через создание соединения (offset: {offsetX}, {offsetY})");
                        return true;
                    }
                }
            }
        }
        
        // Стратегия 3: Если не найдено оптимальной позиции, показываем любую валидную позицию
        // (например, перемещение на 1 клетку в любом направлении)
        Debug.Log("[HintManager] FindBestPositionForGroup: Стратегия 3 - ищем любую валидную позицию");
        int[] simpleOffsets = { -1, 0, 1 };
        foreach (int offsetX in simpleOffsets)
        {
            foreach (int offsetY in simpleOffsets)
            {
                if (offsetX == 0 && offsetY == 0) continue;
                
                Vector2Int offset = new Vector2Int(offsetX, offsetY);
                if (CanPlaceGroupAtOffset(group, offset))
                {
                    Vector2Int mainPos = new Vector2Int(group[0].currentGridRow, group[0].currentGridCol);
                    targetPosition = new Vector2Int(
                        mainPos.x + offset.x,
                        mainPos.y + offset.y
                    );
                    Debug.Log($"[HintManager] FindBestPositionForGroup: найдена простая позиция (offset: {offsetX}, {offsetY})");
                    return true;
                }
            }
        }
        
        Debug.Log("[HintManager] FindBestPositionForGroup: не найдено подходящей позиции");
        return false;
    }
    
    // Проверяет, создаст ли перемещение группы новое соединение
    // УДАЛЕНО: WouldConnectAtPosition больше не используется
    private bool WouldGroupCreateConnection(List<PuzzlePiece> group, Vector2Int offset)
    {
        if (group == null || group.Count == 0 || grid == null || occupiedCells == null) return false;
        
        // Проверяем каждую карточку группы на новой позиции
        foreach (PuzzlePiece piece in group)
        {
            Vector2Int newPos = new Vector2Int(
                piece.currentGridRow + offset.x,
                piece.currentGridCol + offset.y
            );
            
            // Дополнительно: проверяем, может ли карточка соединиться с несоединенными карточками
            // после перемещения группы
            if (WouldConnectWithUnconnectedPieces(piece, newPos))
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Проверяет, может ли карточка соединиться с несоединенными карточками
    private bool WouldConnectWithUnconnectedPieces(PuzzlePiece piece, Vector2Int newPos)
    {
        if (piece == null || grid == null || occupiedCells == null) return false;
        
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        
        // Проверяем всех соседей на новой позиции
        for (int i = 0; i < 4; i++)
        {
            int neighborRow = newPos.x + dx[i];
            int neighborCol = newPos.y + dy[i];
            
            if (!grid.IsValidGridPosition(neighborRow, neighborCol)) continue;
            
            Vector2Int neighborPos = new Vector2Int(neighborRow, neighborCol);
            if (occupiedCells.TryGetValue(neighborPos, out PuzzlePiece neighbor))
            {
                // Если сосед не в группе и они должны быть соседями в оригинале
                if (!IsPieceInGroup(neighbor))
                {
                    Vector2Int pieceOriginal = piece.GetOriginalPosition(grid.gridCols);
                    Vector2Int neighborOriginal = neighbor.GetOriginalPosition(grid.gridCols);
                    
                    // Проверяем, были ли они соседями в оригинале
                    int rowDiff = Mathf.Abs(pieceOriginal.x - neighborOriginal.x);
                    int colDiff = Mathf.Abs(pieceOriginal.y - neighborOriginal.y);
                    
                    if ((rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1))
                    {
                        return true; // Могут соединиться!
                    }
                }
            }
        }
        
        return false;
    }
    
    // Проверяет, можно ли разместить группу с указанным смещением
    private bool CanPlaceGroupAtOffset(List<PuzzlePiece> group, Vector2Int offset)
    {
        if (group == null || group.Count == 0 || grid == null || occupiedCells == null) return false;
        
        HashSet<Vector2Int> groupPositions = new HashSet<Vector2Int>();
        
        // Вычисляем все позиции, которые займет группа
        foreach (PuzzlePiece piece in group)
        {
            Vector2Int newPos = new Vector2Int(
                piece.currentGridRow + offset.x,
                piece.currentGridCol + offset.y
            );
            
            if (!grid.IsValidGridPosition(newPos.x, newPos.y))
            {
                return false; // Выходит за границы
            }
            
            groupPositions.Add(newPos);
        }
        
        // Проверяем, что все позиции либо пусты, либо заняты карточками из группы
        foreach (Vector2Int pos in groupPositions)
        {
            if (occupiedCells.TryGetValue(pos, out PuzzlePiece piece))
            {
                if (!group.Contains(piece))
                {
                    return false; // Позиция занята чужой карточкой
                }
            }
        }
        
        return true;
    }
    
    // Called from button click
    public void ShowHint()
    {
        Debug.Log("[HintManager] ShowHint: начало поиска подсказки");
        
        if (gameManager == null)
        {
            Debug.LogWarning("[HintManager] ShowHint: gameManager == null");
            return;
        }
        
        if (gameManager.IsGameComplete())
        {
            Debug.Log("[HintManager] ShowHint: игра завершена, подсказки не показываем");
            return;
        }
        
        // Stop any existing hint animations
        StopAllHints();
        
        // Инициализируем grid и occupiedCells
        grid = gameManager.GetComponent<PuzzleGrid>();
        occupiedCells = gameManager.GetOccupiedCells();
        
        if (grid == null || occupiedCells == null)
        {
            Debug.LogWarning("[HintManager] ShowHint: grid или occupiedCells == null");
            return;
        }
        
        Debug.Log($"[HintManager] ShowHint: grid размер {grid.gridRows}x{grid.gridCols}, занято ячеек {occupiedCells.Count}");
        
        // Простая логика: построчный проход по сетке
        PuzzlePiece piece1, piece2;
        List<PuzzlePiece> group;
        Vector2Int targetPosition;
        
        if (FindSimplePositionHint(out piece1, out piece2, out group, out targetPosition))
        {
            if (group != null && group.Count > 0)
            {
                // Показать подсказку на перемещение группы
                Debug.Log($"[HintManager] ShowHint: найдена подсказка для группы из {group.Count} карточек на позицию ({targetPosition.x}, {targetPosition.y})");
                AnimateGroupHint(group, targetPosition);
            }
            else if (piece1 != null && piece2 != null)
            {
                // Показать подсказку на обмен двух карточек
                Debug.Log($"[HintManager] ShowHint: найдена подсказка для обмена {piece1.name} и {piece2.name}");
                AnimateHint(piece1, piece2);
            }
            return;
        }
        
        // Fallback: показать обмен любых двух соседних карточек
        Debug.Log("[HintManager] ShowHint: fallback - показываем обмен любых соседних карточек");
        List<PuzzlePiece> allPieces = gameManager.GetAllPieces();
        if (allPieces != null && allPieces.Count >= 2)
        {
            foreach (PuzzlePiece piece in allPieces)
            {
                int[] dx = { -1, 1, 0, 0 };
                int[] dy = { 0, 0, -1, 1 };
                
                for (int i = 0; i < 4; i++)
                {
                    int neighborRow = piece.currentGridRow + dx[i];
                    int neighborCol = piece.currentGridCol + dy[i];
                    
                    if (!grid.IsValidGridPosition(neighborRow, neighborCol)) continue;
                    
                    Vector2Int neighborPos = new Vector2Int(neighborRow, neighborCol);
                    if (occupiedCells.TryGetValue(neighborPos, out PuzzlePiece neighbor))
                    {
                        if (piece != neighbor)
                        {
                            Debug.Log($"[HintManager] ShowHint: fallback - показываем обмен {piece.name} и {neighbor.name}");
                            AnimateHint(piece, neighbor);
                            return;
                        }
                    }
                }
            }
            
            // Если соседние не найдены, показываем обмен первых двух карточек
            Debug.Log($"[HintManager] ShowHint: fallback - показываем обмен первых двух карточек");
            AnimateHint(allPieces[0], allPieces[1]);
            return;
        }
        
        Debug.LogWarning("[HintManager] ShowHint: не найдено подходящей подсказки!");
    }
    
    // УДАЛЕНО: FindSimpleHint - больше не используется, заменен на FindSimplePositionHint
    
    // УДАЛЕНО: FindSwapHint - больше не используется, заменен на FindSimplePositionHint
    // УДАЛЕНО: FindAnySwapHint - больше не используется, заменен на FindSimplePositionHint
    
    // Приоритет 2: Найти группу и место для перемещения
    private bool FindGroupMoveHint(out List<PuzzlePiece> group, out Vector2Int targetPosition)
    {
        group = null;
        targetPosition = Vector2Int.zero;
        
        if (gameManager == null || grid == null || occupiedCells == null)
        {
            Debug.LogWarning("[HintManager] FindGroupMoveHint: gameManager, grid или occupiedCells == null");
            return false;
        }
        
        List<PuzzlePiece> allPieces = gameManager.GetAllPieces();
        if (allPieces == null || allPieces.Count == 0)
        {
            Debug.LogWarning("[HintManager] FindGroupMoveHint: нет карточек");
            return false;
        }
        
        Debug.Log($"[HintManager] FindGroupMoveHint: всего карточек {allPieces.Count}");
        
        // Ищем все группы соединенных карточек
        HashSet<PuzzlePiece> processed = new HashSet<PuzzlePiece>();
        int groupCount = 0;
        
        foreach (PuzzlePiece startPiece in allPieces)
        {
            if (processed.Contains(startPiece)) continue;
            
            // Пропускаем карточки без соединений
            if (!IsPieceInGroup(startPiece))
            {
                Debug.Log($"[HintManager] FindGroupMoveHint: карточка {startPiece.name} не в группе, пропускаем");
                continue;
            }
            
            // Находим группу соединенных карточек
            List<PuzzlePiece> connectedGroup = FindConnectedGroup(startPiece);
            if (connectedGroup == null || connectedGroup.Count == 0)
            {
                Debug.LogWarning($"[HintManager] FindGroupMoveHint: не удалось найти группу для {startPiece.name}");
                continue;
            }
            
            groupCount++;
            Debug.Log($"[HintManager] FindGroupMoveHint: найдена группа #{groupCount} из {connectedGroup.Count} карточек");
            
            processed.UnionWith(connectedGroup);
            
            // Пропускаем группы, которые уже полностью собраны
            if (IsGroupComplete(connectedGroup))
            {
                Debug.Log($"[HintManager] FindGroupMoveHint: группа #{groupCount} полностью собрана, пропускаем");
                continue;
            }
            
            // Ищем место, куда можно переместить группу для создания соединения
            Vector2Int targetPos;
            if (FindBestPositionForGroup(connectedGroup, out targetPos))
            {
                Debug.Log($"[HintManager] FindGroupMoveHint: найдена позиция для группы #{groupCount} на ({targetPos.x}, {targetPos.y})");
                group = connectedGroup;
                targetPosition = targetPos;
                return true;
            }
            else
            {
                Debug.Log($"[HintManager] FindGroupMoveHint: не найдена позиция для группы #{groupCount}");
            }
        }
        
        Debug.Log($"[HintManager] FindGroupMoveHint: проверено {groupCount} групп, подходящих не найдено");
        return false;
    }
    
    // Проверяет, полностью ли собрана группа (все карточки на правильных местах)
    private bool IsGroupComplete(List<PuzzlePiece> group)
    {
        if (group == null || grid == null) return false;
        
        foreach (PuzzlePiece piece in group)
        {
            if (!piece.IsAtCorrectPosition(grid.gridCols))
            {
                return false;
            }
        }
        return true;
    }
    
    // Анимация для двух карточек (обмен)
    private void AnimateHint(PuzzlePiece piece1, PuzzlePiece piece2)
    {
        if (piece1 == null || piece2 == null) return;
        
        StopAnimationsForPieces(new[] { piece1, piece2 });
        
        // Сохраняем оригинальные sortingOrder для первой карточки
        SpriteRenderer sr1 = piece1.GetComponent<SpriteRenderer>();
        if (sr1 != null)
        {
            originalCardSortingOrders[piece1] = sr1.sortingOrder;
            sr1.sortingOrder = HINT_FIRST_CARD_SORTING_ORDER; // Самая высокая
        }
        
        BorderRenderer br1 = piece1.GetComponentInChildren<BorderRenderer>();
        if (br1 != null)
        {
            // Получаем оригинальный sortingOrder рамок (берем первую рамку как референс)
            int originalBorderOrder1 = 1;
            if (br1.topBorder != null)
            {
                SpriteRenderer borderSr = br1.topBorder.GetComponent<SpriteRenderer>();
                if (borderSr != null)
                {
                    originalBorderOrder1 = borderSr.sortingOrder;
                }
            }
            originalBorderSortingOrders[piece1] = originalBorderOrder1;
            br1.SetBordersSortingOrder(HINT_FIRST_BORDER_SORTING_ORDER); // Рамки первой карточки выше всех
        }
        
        // Сохраняем оригинальные sortingOrder для второй карточки
        SpriteRenderer sr2 = piece2.GetComponent<SpriteRenderer>();
        if (sr2 != null)
        {
            originalCardSortingOrders[piece2] = sr2.sortingOrder;
            sr2.sortingOrder = HINT_SECOND_CARD_SORTING_ORDER; // Ниже первой, но выше остальных
        }
        
        BorderRenderer br2 = piece2.GetComponentInChildren<BorderRenderer>();
        if (br2 != null)
        {
            // Получаем оригинальный sortingOrder рамок
            int originalBorderOrder2 = 1;
            if (br2.topBorder != null)
            {
                SpriteRenderer borderSr = br2.topBorder.GetComponent<SpriteRenderer>();
                if (borderSr != null)
                {
                    originalBorderOrder2 = borderSr.sortingOrder;
                }
            }
            originalBorderSortingOrders[piece2] = originalBorderOrder2;
            br2.SetBordersSortingOrder(HINT_SECOND_BORDER_SORTING_ORDER); // Рамки второй карточки ниже рамок первой
        }
        
        Sequence hintSequence1 = CreatePulseSequence(piece1.transform, piece1);
        Sequence hintSequence2 = CreatePulseSequence(piece2.transform, piece2);
        
        activeHintAnimations[piece1] = hintSequence1;
        activeHintAnimations[piece2] = hintSequence2;
        
        hintSequence1.Play();
        hintSequence2.Play();
    }
    
    // Анимация для группы (показываем группу и целевую позицию)
    private void AnimateGroupHint(List<PuzzlePiece> group, Vector2Int targetPosition)
    {
        if (group == null || group.Count == 0) return;
        
        StopAnimationsForPieces(group.ToArray());
        
        // Анимируем все карточки группы с правильным sortingOrder
        for (int i = 0; i < group.Count; i++)
        {
            PuzzlePiece piece = group[i];
            
            SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                originalCardSortingOrders[piece] = sr.sortingOrder;
                
                if (i == 0)
                {
                    // Первая карточка группы - самая высокая
                    sr.sortingOrder = HINT_FIRST_CARD_SORTING_ORDER;
                }
                else if (i == 1)
                {
                    // Вторая карточка группы - ниже первой
                    sr.sortingOrder = HINT_SECOND_CARD_SORTING_ORDER;
                }
                else
                {
                    // Остальные карточки - выше обычных, но ниже первых двух
                    sr.sortingOrder = HINT_OTHER_CARD_SORTING_ORDER;
                }
            }
            
            BorderRenderer br = piece.GetComponentInChildren<BorderRenderer>();
            if (br != null)
            {
                // Получаем оригинальный sortingOrder рамок
                int originalBorderOrder = 1;
                if (br.topBorder != null)
                {
                    SpriteRenderer borderSr = br.topBorder.GetComponent<SpriteRenderer>();
                    if (borderSr != null)
                    {
                        originalBorderOrder = borderSr.sortingOrder;
                    }
                }
                originalBorderSortingOrders[piece] = originalBorderOrder;
                
                if (i == 0)
                {
                    // Рамки первой карточки - выше всех
                    br.SetBordersSortingOrder(HINT_FIRST_BORDER_SORTING_ORDER);
                }
                else if (i == 1)
                {
                    // Рамки второй карточки - ниже рамок первой
                    br.SetBordersSortingOrder(HINT_SECOND_BORDER_SORTING_ORDER);
                }
                else
                {
                    // Рамки остальных карточек
                    br.SetBordersSortingOrder(HINT_OTHER_BORDER_SORTING_ORDER);
                }
            }
            
            Sequence hintSequence = CreatePulseSequence(piece.transform, piece);
            activeHintAnimations[piece] = hintSequence;
            hintSequence.Play();
        }
    }
    
    private void StopAnimationsForPieces(PuzzlePiece[] pieces)
    {
        foreach (PuzzlePiece piece in pieces)
        {
            if (activeHintAnimations.ContainsKey(piece))
            {
                activeHintAnimations[piece].Kill();
                activeHintAnimations.Remove(piece);
            }
            
            if (piece != null && piece.transform != null)
            {
                piece.transform.localScale = Vector3.one;
                
                // Восстанавливаем sortingOrder карточки
                if (originalCardSortingOrders.ContainsKey(piece))
                {
                    SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = originalCardSortingOrders[piece];
                    }
                    originalCardSortingOrders.Remove(piece);
                }
                
                // Восстанавливаем sortingOrder рамок
                if (originalBorderSortingOrders.ContainsKey(piece))
                {
                    BorderRenderer br = piece.GetComponentInChildren<BorderRenderer>();
                    if (br != null)
                    {
                        br.SetBordersSortingOrder(originalBorderSortingOrders[piece]);
                    }
                    originalBorderSortingOrders.Remove(piece);
                }
            }
        }
    }
    
    private Sequence CreatePulseSequence(Transform target, PuzzlePiece piece)
    {
        Sequence seq = DOTween.Sequence();
        Vector3 originalScale = target.localScale;
        
        // Create pulse animation (scale up and down)
        for (int i = 0; i < pulseCount; i++)
        {
            seq.Append(target.DOScale(originalScale * hintScale, hintDuration / 2f).SetEase(Ease.OutQuad));
            seq.Append(target.DOScale(originalScale, hintDuration / 2f).SetEase(Ease.InQuad));
        }
        
        seq.OnComplete(() => {
            // Ensure scale is back to original
            target.localScale = originalScale;
            
            // Восстанавливаем оригинальные sortingOrder
            if (piece != null)
            {
                // Восстанавливаем sortingOrder карточки
                if (originalCardSortingOrders.ContainsKey(piece))
                {
                    SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = originalCardSortingOrders[piece];
                    }
                    originalCardSortingOrders.Remove(piece);
                }
                
                // Восстанавливаем sortingOrder рамок
                if (originalBorderSortingOrders.ContainsKey(piece))
                {
                    BorderRenderer br = piece.GetComponentInChildren<BorderRenderer>();
                    if (br != null)
                    {
                        br.SetBordersSortingOrder(originalBorderSortingOrders[piece]);
                    }
                    originalBorderSortingOrders.Remove(piece);
                }
            }
        });
        
        return seq;
    }
    
    private void StopAllHints()
    {
        foreach (var kvp in activeHintAnimations)
        {
            if (kvp.Value != null && kvp.Value.IsActive())
            {
                kvp.Value.Kill();
            }
            
            PuzzlePiece piece = kvp.Key;
            if (piece != null && piece.transform != null)
            {
                piece.transform.localScale = Vector3.one;
                
                // Восстанавливаем sortingOrder карточки
                if (originalCardSortingOrders.ContainsKey(piece))
                {
                    SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = originalCardSortingOrders[piece];
                    }
                    originalCardSortingOrders.Remove(piece);
                }
                
                // Восстанавливаем sortingOrder рамок
                if (originalBorderSortingOrders.ContainsKey(piece))
                {
                    BorderRenderer br = piece.GetComponentInChildren<BorderRenderer>();
                    if (br != null)
                    {
                        br.SetBordersSortingOrder(originalBorderSortingOrders[piece]);
                    }
                    originalBorderSortingOrders.Remove(piece);
                }
            }
        }
        activeHintAnimations.Clear();
    }
}

