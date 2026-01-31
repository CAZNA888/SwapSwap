using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using PlayerPrefs = RedefineYG.PlayerPrefs;
public class GameManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [Tooltip("Устанавливается автоматически через LevelManager. Оставьте пустым для автоматической настройки.")]
    public Sprite sourceImage;
    public Sprite cardBackSprite;
    public Sprite borderSprite;
    [Tooltip("Устанавливается автоматически через LevelManager. Оставьте 0 для автоматической настройки.")]
    public int gridRows = 0;
    [Tooltip("Устанавливается автоматически через LevelManager. Оставьте 0 для автоматической настройки.")]
    public int gridCols = 0;
    public float fieldWidth = 10f;
    public float fieldHeight = 10f;
    public float cardSpacing = 0.1f;
    
    [Header("Settings")]
    [Tooltip("Горизонтальное расстояние между карточками (ширина)")]
    public float cardSpan = 0.1f;
    [Tooltip("Вертикальное расстояние между карточками (высота)")]
    public float cardHeight = 0.1f;
    
    [Header("Collider Settings")]
    [Tooltip("Базовая ширина спрайта карточки в единицах Unity (обычно 3.68 для спрайта 368×512 при Pixels Per Unit = 100)")]
    public float spriteBaseWidth = 3.68f;
    [Tooltip("Эффективная ширина коллайдера в единицах Unity (обычно 3.6 для учета прозрачных краев спрайта)")]
    public float colliderEffectiveWidth = 3.6f;
    
    [Header("Positions")]
    public Vector2 deckPosition = new Vector2(5f, -5f);
    public Vector2 moneyTargetPosition = new Vector2(-8f, 4f);
    
    [Header("Timing")]
    public float dealDelay = 0.1f;
    public float flipDelay = 0.05f;
    
    [Header("Prefabs")]
    public GameObject puzzlePiecePrefab;
    public GameObject borderPartPrefab;
    
    [Header("Level Complete Effects")]
    public GameObject levelCompleteUIObject; // GameObject to turn on at level completion
    public List<ParticleSystem> levelCompleteParticles = new List<ParticleSystem>();
    
    [Header("Scene Management")]
    public int nextSceneIndex = 0; // Индекс следующей сцены для загрузки
    public Transform coinStartPoint; // UI объект старта для монеток
    public Transform coinFinishPoint; // UI объект финиша для монеток
    public GameObject coinPrefab; // Префаб монетки
    public int coinsCount = 15; // Количество монеток (k)
    public int moneyAmount = 15; // Количество денег для добавления (j)
    public float coinSpawnDelay = 0.05f; // Задержка между созданием монеток
    public float coinAnimationDuration = 1f; // Длительность анимации монеток
    
    [Header("Level Display")]
    [Tooltip("TextMeshProUGUI для отображения номера уровня (только число)")]
    public TextMeshProUGUI levelNumberText;
    
    // Components
    private PuzzleGrid puzzleGrid;
    private ImageSlicer imageSlicer;
    private CardDealer cardDealer;
    private CardFlipAnimator cardFlipAnimator;
    private SwipeHandler swipeHandler;
    private ConnectionManager connectionManager;
    private AudioManager audioManager;
    private MoneyManager moneyManager;
    private LevelCompleteUI levelCompleteUI;
    private ConfettiEffect confettiEffect;
    private LevelManager levelManager;
    
    // Game State
    private List<PuzzlePiece> puzzlePieces;
    private Dictionary<Vector2Int, PuzzlePiece> occupiedCells;
    private List<Sprite> slicedSprites;
    private bool isGameComplete = false;
    private bool isLoadingScene = false; // Флаг для предотвращения повторных вызовов
    private bool isDealingOrFlipping = false; // Флаг для блокировки подсказок во время раздачи и переворота карт
    private Vector3 originalCardScale = Vector3.one; // Store original card scale (all cards have same size on same level)
    
    void Awake()
    {
    }
    
    void OnEnable()
    {
    }
    
    void OnDisable()
    {
    }
    
    void Start()
    {
        if (this == null || gameObject == null)
            return;
        
        GameManager[] allGameManagers = FindObjectsOfType<GameManager>();
        if (allGameManagers.Length > 1)
        {
            // Multiple GameManagers - may cause issues
        }
        
        if (SceneManager.GetActiveScene().name != "Level")
        {
            // Start() called in wrong scene
        }
        
        if (!gameObject.activeInHierarchy)
            return;
        
        InitializeComponents();
        StartGame();
    }
    
    void OnDestroy()
    {
        CleanupBeforeSceneLoad();
    }
    
    private void InitializeComponents()
    {
        if (gameObject == null)
            return;
        
        if (!gameObject.activeInHierarchy)
            return;
        
        levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            int calculatedGridSize = levelManager.CalculateGridSize();
            gridRows = calculatedGridSize;
            gridCols = calculatedGridSize;
        }
        else
        {
            if (gridRows == 0) gridRows = 3;
            if (gridCols == 0) gridCols = 3;
            gridRows = gridCols = Mathf.Max(gridRows, gridCols);
        }
        
        gridCols = gridRows;
        
        puzzleGrid = GetComponent<PuzzleGrid>();
        if (puzzleGrid == null)
            puzzleGrid = gameObject.AddComponent<PuzzleGrid>();
        
        puzzleGrid.Initialize(gridRows, gridCols, fieldWidth, fieldHeight, cardSpan, cardHeight);
        puzzleGrid.deckPosition = deckPosition;
        puzzleGrid.CreateGridCells();
        
        imageSlicer = GetComponent<ImageSlicer>();
        if (imageSlicer == null)
            imageSlicer = gameObject.AddComponent<ImageSlicer>();
        
        cardDealer = GetComponent<CardDealer>();
        if (cardDealer == null)
            cardDealer = gameObject.AddComponent<CardDealer>();
        cardDealer.dealDelay = dealDelay;
        
        cardFlipAnimator = GetComponent<CardFlipAnimator>();
        if (cardFlipAnimator == null)
            cardFlipAnimator = gameObject.AddComponent<CardFlipAnimator>();
        cardFlipAnimator.Initialize(audioManager, connectionManager);
        cardFlipAnimator.flipDelay = flipDelay;
        
        connectionManager = GetComponent<ConnectionManager>();
        if (connectionManager == null)
            connectionManager = gameObject.AddComponent<ConnectionManager>();
        connectionManager.Initialize(puzzleGrid);
        
        audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null)
        {
            GameObject audioObj = new GameObject("AudioManager");
            audioManager = audioObj.AddComponent<AudioManager>();
        }
        
        // Pass AudioManager to ConnectionManager
        connectionManager.SetAudioManager(audioManager);
        
        cardDealer.Initialize(audioManager);
        cardFlipAnimator.Initialize(audioManager, connectionManager);
        
        moneyManager = FindObjectOfType<MoneyManager>();
        if (moneyManager == null)
        {
            GameObject moneyObj = new GameObject("MoneyManager");
            moneyManager = moneyObj.AddComponent<MoneyManager>();
        }
        
        levelCompleteUI = FindObjectOfType<LevelCompleteUI>();
        if (levelCompleteUI != null)
            levelCompleteUI.SetMoneyTargetPosition(moneyTargetPosition);
        
        confettiEffect = FindObjectOfType<ConfettiEffect>();
        if (confettiEffect == null)
        {
            GameObject confettiObj = new GameObject("ConfettiEffect");
            confettiEffect = confettiObj.AddComponent<ConfettiEffect>();
        }
        
        occupiedCells = new Dictionary<Vector2Int, PuzzlePiece>();
        puzzlePieces = new List<PuzzlePiece>();
    }
    
    private void StartGame()
    {
        UpdateLevelNumberDisplay();
        StartCoroutine(GameSequence());
    }
    
    private void UpdateLevelNumberDisplay()
    {
        if (levelNumberText != null && levelManager != null)
            levelNumberText.text = (levelManager.GetCurrentLevel() + 1).ToString();
    }
    
    private IEnumerator GameSequence()
    {
        if (levelManager != null && levelManager.IsDifficultLevel())
            yield return StartCoroutine(levelManager.ShowDifficultLevelUI());
        
        if (sourceImage == null && levelManager != null)
        {
            const int maxRetries = 3;
            const float retryDelaySeconds = 0.5f;
            Sprite loadedSprite = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                loadedSprite = null;
                yield return StartCoroutine(levelManager.LoadLevelImageAsync((sprite) => {
                    loadedSprite = sprite;
                }));
                sourceImage = loadedSprite;

                if (sourceImage != null)
                    break;

                if (attempt < maxRetries)
                    yield return new WaitForSeconds(retryDelaySeconds);
            }

            if (sourceImage != null && levelManager != null)
                levelManager.PreloadNextLevelImages();
        }
        else if (sourceImage != null && levelManager != null)
        {
            levelManager.PreloadNextLevelImages();
        }
        
        if (sourceImage == null)
            yield break;
        
        
        // Используем тот же pixelsPerUnit, что и у cardBackSprite
        float? targetPixelsPerUnit = cardBackSprite != null ? cardBackSprite.pixelsPerUnit : null;
        if (targetPixelsPerUnit.HasValue)
        {
                    }
        
        slicedSprites = imageSlicer.SliceImage(sourceImage, gridRows, gridCols, targetPixelsPerUnit);
        
        if (slicedSprites == null || slicedSprites.Count != gridRows * gridCols)
        {
            yield break;
        }
        
        // 2. Создаем карточки
        CreatePuzzlePieces();
        
        // 3. Перемешиваем индексы
        ShufflePieces();
        
        // 4. Раздаем карточки
        isDealingOrFlipping = true;
        yield return StartCoroutine(cardDealer.DealCards(puzzlePieces, puzzleGrid));
        
        // 5. Переворачиваем карточки
        yield return StartCoroutine(cardFlipAnimator.FlipAllCards(puzzlePieces));
        isDealingOrFlipping = false;
        
        // 6. Инициализируем свайп-хендлер
        InitializeSwipeHandler();
        
        // 7. Проверяем соединения
        connectionManager.CheckAllConnections();
        
        // 8. Запускаем автоподсказки для tutorial (первый уровень)
        HintManager hintManager = FindObjectOfType<HintManager>();
        if (hintManager != null)
        {
            hintManager.StartAutoHints();
        }
        
    }
    
    private void CreatePuzzlePieces()
    {
        puzzlePieces.Clear();
        occupiedCells.Clear();
        
        // ГИБРИДНОЕ РЕШЕНИЕ: Размер из сетки с сохранением соотношения сторон карточки
        // Получаем размер ячейки из сетки (зависит от gridRows и gridCols)
        Vector2 cellSize = puzzleGrid.GetCardSize();
        
        // Получаем соотношение сторон из CardBack (если есть)
        float cardAspectRatio = 1f; // По умолчанию квадрат
        if (cardBackSprite != null)
        {
            // ИСПРАВЛЕНО: Используем rect и pixelsPerUnit вместо bounds.size
            // Это гарантирует одинаковые размеры на всех устройствах независимо от pixel ratio
            Vector2 cardBackSize = new Vector2(
                cardBackSprite.rect.width / cardBackSprite.pixelsPerUnit,
                cardBackSprite.rect.height / cardBackSprite.pixelsPerUnit
            );
            cardAspectRatio = cardBackSize.x / cardBackSize.y;
        }
        
        // Проверяем, есть ли отрицательный spacing (для перекрытия карточек)
        bool hasNegativeSpacing = (cardSpan < 0 || cardHeight < 0);
        
        // Вычисляем размер карточки с сохранением соотношения сторон
        Vector2 cardSize;
        
        if (hasNegativeSpacing)
        {
            // При отрицательном spacing используем размер из сетки напрямую
            // (он уже учитывает отрицательный spacing и делает карточки больше)
            cardSize = cellSize;
        }
        else
        {
            // При положительном spacing ограничиваем размером ячейки с сохранением aspect ratio
            float cellAspectRatio = cellSize.x / cellSize.y;
            
            if (cardAspectRatio > cellAspectRatio)
            {
                // Ширина ограничивает - используем ширину ячейки
                cardSize = new Vector2(cellSize.x, cellSize.x / cardAspectRatio);
            }
            else
            {
                // Высота ограничивает - используем высоту ячейки
                cardSize = new Vector2(cellSize.y * cardAspectRatio, cellSize.y);
            }
        }
        
        // Применяем мультипликатор размера всего префаба (если установлен)
        if (levelManager != null)
        {
            float prefabMultiplier = levelManager.GetCardPrefabMultiplier();
            if (prefabMultiplier != 1.0f)
                cardSize *= prefabMultiplier;
        }
        
        // Передаем реальный размер карточки в PuzzleGrid для правильного позиционирования
        puzzleGrid.SetActualCardSize(cardSize);
        
        for (int i = 0; i < gridRows * gridCols; i++)
        {
            GameObject pieceObj;
            
            if (puzzlePiecePrefab != null)
            {
                pieceObj = Instantiate(puzzlePiecePrefab);
                
                // Если есть PuzzlePieceSetup, обновляем размер из CardBack (для внутренней логики префаба)
                PuzzlePieceSetup setup = pieceObj.GetComponent<PuzzlePieceSetup>();
                if (setup != null && cardBackSprite != null)
                {
                    setup.UpdateSizeFromCardBack(cardBackSprite);
                    // НЕ переопределяем cardSize из префаба - используем наш расчет
                }
            }
            else
            {
                // Fallback - создание без префаба
                pieceObj = new GameObject($"PuzzlePiece_{i}");
                pieceObj.AddComponent<SpriteRenderer>();
            }
            
            PuzzlePiece piece = pieceObj.GetComponent<PuzzlePiece>();
            if (piece == null)
            {
                piece = pieceObj.AddComponent<PuzzlePiece>();
            }
            
            // Добавляем коллайдер для свайпов и физики (если нет в префабе)
            BoxCollider2D collider = pieceObj.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = pieceObj.AddComponent<BoxCollider2D>();
            }
            
            // ВАЖНО: Размер коллайдера должен быть установлен в базовый размер спрайта (до масштабирования)
            // После масштабирования через SetCardSize() коллайдер автоматически масштабируется вместе с transform
            // ИСПРАВЛЕНО: Используем rect и pixelsPerUnit вместо bounds.size для стабильности на всех устройствах
            // Корректируем ширину коллайдера: используем colliderEffectiveWidth вместо spriteBaseWidth для точного соответствия визуальному размеру
            Vector2 baseColliderSize = cardBackSprite != null ? new Vector2(
                (cardBackSprite.rect.width / cardBackSprite.pixelsPerUnit) * (colliderEffectiveWidth / spriteBaseWidth), // Корректируем ширину коллайдера
                cardBackSprite.rect.height / cardBackSprite.pixelsPerUnit
            ) : cardSize; // Fallback если нет спрайта
            
            collider.size = baseColliderSize;
            // Убеждаемся, что коллайдер всегда активен и не триггер
            collider.enabled = true;
            collider.isTrigger = false; // НЕ триггер для карточек
            
            // Добавляем Rigidbody2D для корректной работы коллайдера (kinematic)
            if (pieceObj.GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D rb = pieceObj.AddComponent<Rigidbody2D>();
                rb.isKinematic = true; // Не используем физику, только для коллайдера
                rb.gravityScale = 0;
            }
            
            // Устанавливаем z-позицию карточки выше ячеек
            pieceObj.transform.position = new Vector3(pieceObj.transform.position.x, pieceObj.transform.position.y, 0f);
            
            // Инициализируем карточку
            Sprite frontSprite = slicedSprites[i];
            
            // НЕ масштабируем спрайт - используем оригинальный
            // Размер будет применен через SetCardSize к transform префаба (масштабирует весь префаб)
            piece.Initialize(i, frontSprite, cardBackSprite, puzzleGrid);
            
            // КРИТИЧНО для WebGL: Инициализируем cardSpriteContainer ПЕРЕД применением масштаба
            // В WebGL билде ссылки на дочерние объекты могут не инициализироваться автоматически
            SpriteRenderer sr = piece.GetCardSpriteRenderer();
            
            // ВАЖНО: Сначала применяем мультипликаторы размера для front и back sprite к контейнеру,
            // а потом устанавливаем размер всего префаба. Это гарантирует правильный итоговый масштаб.
            if (levelManager != null)
            {
                float frontMultiplier = levelManager.GetFrontSpriteMultiplier();
                float backMultiplier = levelManager.GetBackSpriteMultiplier();
                piece.SetSpriteScales(frontMultiplier, backMultiplier);
            }
            
            // Устанавливаем размер карточки - это масштабирует весь префаб (включая рамки)
            // Вызывается ПОСЛЕ SetFrontSpriteScale, чтобы итоговый масштаб контейнера был правильным
            piece.SetCardSize(cardSize);
            
            // Store original scale from first card (all cards have same size on same level)
            if (i == 0)
                originalCardScale = piece.transform.localScale;
            
            // Устанавливаем спрайт обратной стороны (cardSpriteContainer уже инициализирован выше)
            if (sr != null)
            {
                sr.sprite = cardBackSprite; // Обратная сторона
                sr.sortingOrder = 0;
            }
            
            // Проверяем наличие BorderRenderer в префабе
            BorderRenderer borderRenderer = pieceObj.GetComponentInChildren<BorderRenderer>();
            if (borderRenderer != null && borderRenderer.topBorder != null && 
                borderRenderer.bottomBorder != null && borderRenderer.leftBorder != null && 
                borderRenderer.rightBorder != null)
            {
                // Используем существующие границы из префаба (треугольники)
                piece.borderParts[0] = borderRenderer.topBorder;
                piece.borderParts[1] = borderRenderer.bottomBorder;
                piece.borderParts[2] = borderRenderer.leftBorder;
                piece.borderParts[3] = borderRenderer.rightBorder;
            }
            else
            {
                // Если рамок нет в префабе - создаем их через CardPrefabBuilder
                CardPrefabBuilder cardBuilder = pieceObj.GetComponent<CardPrefabBuilder>();
                if (cardBuilder != null && borderSprite != null)
                {
                    // Используем CardPrefabBuilder для создания рамок (треугольниками)
                    cardBuilder.borderSprite = borderSprite;
                    cardBuilder.CreateBorders(); // Это создаст треугольники
                    
                    // Получаем BorderRenderer после создания
                    borderRenderer = pieceObj.GetComponentInChildren<BorderRenderer>();
                    if (borderRenderer != null && borderRenderer.topBorder != null)
                    {
                        piece.borderParts[0] = borderRenderer.topBorder;
                        piece.borderParts[1] = borderRenderer.bottomBorder;
                        piece.borderParts[2] = borderRenderer.leftBorder;
                        piece.borderParts[3] = borderRenderer.rightBorder;
                    }
                    else
                    {
                    }
                }
                else if (borderSprite != null)
                {
                    // Fallback - создаем старым способом (только если нет CardPrefabBuilder)
                    CreateBorders(pieceObj, piece, borderSprite, cardSize);
                }
                else
                {
                }
            }
            
            // Устанавливаем позицию колоды для всех карточек (правый нижний угол)
            Vector2 deckPos = puzzleGrid.GetDeckPosition();
            piece.SetDeckPosition(deckPos);
            
            // Скрываем все рамки при создании (карточки еще не перевернуты)
            BorderRenderer br = pieceObj.GetComponentInChildren<BorderRenderer>();
            if (br != null)
            {
                br.UpdateBorders(new bool[4] { false, false, false, false }, false);
                
                // Проверка инициализации
                if (br.topBorder == null || br.bottomBorder == null || 
                    br.leftBorder == null || br.rightBorder == null)
                {
                }
            }
            else
            {
            }
            
            puzzlePieces.Add(piece);
        }
    }
    
    private void CreateBorders(GameObject pieceObj, PuzzlePiece piece, Sprite borderSprite, Vector2 cardSize)
    {
        if (borderSprite == null) return;
        
        BorderRenderer borderRenderer = pieceObj.GetComponent<BorderRenderer>();
        if (borderRenderer == null)
        {
            borderRenderer = pieceObj.AddComponent<BorderRenderer>();
        }
        
        borderRenderer.CreateBorderParts(borderSprite, cardSize);
        piece.borderParts[0] = borderRenderer.topBorder;
        piece.borderParts[1] = borderRenderer.bottomBorder;
        piece.borderParts[2] = borderRenderer.leftBorder;
        piece.borderParts[3] = borderRenderer.rightBorder;
    }
    
    private void ShufflePieces()
    {
        // Перемешиваем сами карточки в списке (алгоритм Fisher-Yates)
        System.Random random = new System.Random();
        
        for (int i = puzzlePieces.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            
            // Меняем местами карточки
            PuzzlePiece temp = puzzlePieces[i];
            puzzlePieces[i] = puzzlePieces[j];
            puzzlePieces[j] = temp;
        }
    }
    
    private void InitializeSwipeHandler()
    {
        // Добавляем Physics2DRaycaster на камеру для работы с 2D коллайдерами
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
        {
            mainCamera.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
        }
        
        // Регистрируем все карточки в connection manager и обновляем ячейки
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            Vector2Int gridPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
            occupiedCells[gridPos] = piece;
            connectionManager.RegisterPiece(piece);
            
            // Обновляем ячейку - привязываем карточку к ячейке
            GridCell cell = puzzleGrid.GetCellAt(gridPos.x, gridPos.y);
            if (cell != null)
            {
                cell.SetPiece(piece);
            }
        }
        
        // Создаем SwipeHandler на каждой карточке
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            SwipeHandler handler = piece.GetComponent<SwipeHandler>();
            if (handler == null)
            {
                handler = piece.gameObject.AddComponent<SwipeHandler>();
            }
            handler.Initialize(puzzleGrid, connectionManager, audioManager, occupiedCells, this);
        }
    }
    
    public void CheckWinCondition()
    {
        if (isGameComplete) return;
        
        bool allCorrect = true;
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (!piece.IsAtCorrectPosition(gridCols))
            {
                allCorrect = false;
                break;
            }
        }
        
        if (allCorrect)
        {
            OnLevelComplete();
        }
    }
    [SerializeField] GameObject hint, noads, exit,rr;
    private void OnLevelComplete()
    {
        isGameComplete = true;
        
        // Завершаем tutorial если это был первый уровень
        HintManager hintManager = FindObjectOfType<HintManager>();
        if (hintManager != null)
        {
            hintManager.CompleteTutorial();
        }
        
        // Disable all piece interactions by disabling colliders
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            BoxCollider2D collider = piece.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.enabled = false; // Disable collider to prevent clicks
            }
        }
        
        // Звук победы
        if (audioManager != null)
        {
            audioManager.PlayLevelComplete();
        }
        
        // Включаем GameObject UI завершения
        if (levelCompleteUIObject != null)
        {
            levelCompleteUIObject.SetActive(true);
            hint.SetActive(false);
            noads.SetActive(false); 
            exit.SetActive(false);
            rr.SetActive(false);
        }
        
        // Показываем UI завершения (через LevelCompleteUI компонент, если используется)
        if (levelCompleteUI != null)
        {
            levelCompleteUI.ShowLevelComplete();
        }
        
        // Запускаем все Particle Systems
        if (levelCompleteParticles != null && levelCompleteParticles.Count > 0)
        {
            foreach (ParticleSystem particleSystem in levelCompleteParticles)
            {
                if (particleSystem != null)
                {
                    particleSystem.Play();
                }
            }
        }
        
        // Конфетти
        if (confettiEffect != null)
        {
            confettiEffect.PlayConfetti();
        }
        
        // Увеличиваем уровень через LevelManager
        if (levelManager != null)
        {
            levelManager.IncrementLevel();
        }
        
        // НЕ обновляем прогресс меню здесь - это будет сделано в MenuProgressUI
        // чтобы можно было правильно определить новую карточку и показать анимацию
        // MenuManager menuManager = MenuManager.Instance;
        // if (menuManager != null)
        // {
        //     menuManager.UpdateProgress();
        // }
    }
    
    // Public method to check if game is complete
    public bool IsGameComplete()
    {
        return isGameComplete;
    }
    
    // Public method to check if cards are being dealt or flipped
    public bool IsDealingOrFlipping()
    {
        return isDealingOrFlipping;
    }
    
    // Public method to get all puzzle pieces (for hint system)
    public List<PuzzlePiece> GetAllPieces()
    {
        return puzzlePieces;
    }
    
    // Public method to get occupied cells (for hint system)
    public Dictionary<Vector2Int, PuzzlePiece> GetOccupiedCells()
    {
        return occupiedCells;
    }
    
    // Вызывается после каждого хода для проверки победы
    public void OnPieceMoved()
    {
        CheckWinCondition();
    }
    
    /// <summary>
    /// Мгновенно завершает уровень, размещая все карточки в правильные позиции
    /// </summary>
    public void CompleteLevelInstantly()
    {
        if (isGameComplete)
            return;
        
        if (puzzlePieces == null || puzzlePieces.Count == 0)
            return;
        
        if (puzzleGrid == null)
            return;
        
        occupiedCells.Clear();
        
        // Размещаем каждую карточку в правильную позицию
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            // Вычисляем правильную позицию
            Vector2Int correctPos = piece.GetOriginalPosition(gridCols);
            int correctRow = correctPos.x;
            int correctCol = correctPos.y;
            
            // Получаем ячейку для правильной позиции
            GridCell targetCell = puzzleGrid.GetCellAt(correctRow, correctCol);
            if (targetCell == null)
                continue;
            
            // Получаем текущую ячейку карточки
            GridCell currentCell = puzzleGrid.GetCellAt(piece.currentGridRow, piece.currentGridCol);
            
            // Перемещаем карточку в правильную позицию (без анимации для мгновенного завершения)
            Vector2 worldPos = puzzleGrid.GetWorldPosition(correctRow, correctCol);
            piece.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            piece.SetPosition(correctRow, correctCol);
            
            // Restore original scale (all cards must be brought to their starting size)
            if (piece != null && piece.transform != null)
            {
                piece.transform.localScale = originalCardScale;
            }
            
            // Обновляем ячейки
            if (currentCell != null)
            {
                currentCell.SetPiece(null);
            }
            targetCell.SetPiece(piece);
            
            // Обновляем occupiedCells
            Vector2Int oldPos = new Vector2Int(piece.currentGridRow, piece.currentGridCol);
            Vector2Int newPos = new Vector2Int(correctRow, correctCol);
            
            if (oldPos != newPos)
            {
                occupiedCells.Remove(oldPos);
            }
            occupiedCells[newPos] = piece;
            
            // Обновляем connectionManager
            if (connectionManager != null)
            {
                connectionManager.UpdatePieceOnGrid(piece, oldPos, newPos);
            }
        }
        
        // Обновляем все соединения
        if (connectionManager != null)
        {
            connectionManager.CheckAllConnections();
        }
        
        CheckWinCondition();
    }
    
    // Загрузка сцены по индексу с анимацией монеток
    public void LoadSceneByIndex(int sceneIndex)
    {
        // Проверяем, не запущена ли уже загрузка сцены
        if (isLoadingScene)
            return;
        
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            isLoadingScene = true;
            if (sceneIndex == currentSceneIndex)
            {
                string sceneName = SceneManager.GetActiveScene().name;
                StartCoroutine(LoadSceneWithCoinAnimationByName(sceneName));
            }
            else
            {
                StartCoroutine(LoadSceneWithCoinAnimation(sceneIndex));
            }
        }
    }
    
    // Корутина: анимация монеток -> добавление денег -> загрузка сцены
    private IEnumerator LoadSceneWithCoinAnimation(int sceneIndex)
    {
        // Проверяем наличие необходимых объектов
        if (coinStartPoint == null || coinFinishPoint == null || coinPrefab == null)
        {
            if (moneyManager != null)
            {
                moneyManager.AddMoney(moneyAmount);
            }
            yield return new WaitForSeconds(0.5f);
            isLoadingScene = false;
            SceneManager.LoadScene(sceneIndex);
            yield break;
        }
        
        // Находим Canvas для создания UI элементов
        Canvas canvas = coinStartPoint.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        if (canvas == null)
        {
            if (moneyManager != null)
            {
                moneyManager.AddMoney(moneyAmount);
            }
            yield return new WaitForSeconds(0.5f);
            isLoadingScene = false;
            SceneManager.LoadScene(sceneIndex);
            yield break;
        }
        
        List<GameObject> coins = new List<GameObject>();
        RectTransform canvasRect = canvas.transform as RectTransform;
        
        if (canvasRect == null)
        {
            if (moneyManager != null)
            {
                moneyManager.AddMoney(moneyAmount);
            }
            yield return new WaitForSeconds(0.5f);
            isLoadingScene = false;
            SceneManager.LoadScene(sceneIndex);
            yield break;
        }
        
        for (int i = 0; i < coinsCount; i++)
        {
            RectTransform startRect = coinStartPoint as RectTransform;
            RectTransform finishRect = coinFinishPoint as RectTransform;
            
            if (startRect == null || finishRect == null)
                break;
            
            GameObject coin = Instantiate(coinPrefab, canvas.transform);
            RectTransform coinRect = coin.GetComponent<RectTransform>();
            
            if (coinRect == null)
            {
                Destroy(coin);
                continue;
            }
            
            // Конвертируем позицию старта в локальные координаты Canvas
            Vector2 startScreenPos = RectTransformUtility.WorldToScreenPoint(null, startRect.position);
            Vector2 startLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPos, null, out startLocalPos);
            
            // Устанавливаем начальную позицию с небольшим разбросом
            coinRect.anchoredPosition = startLocalPos;
            coinRect.anchoredPosition += Random.insideUnitCircle * 20f;
            
            coins.Add(coin);
            
            // Проигрываем звук при создании монетки
            if (audioManager != null)
            {
                audioManager.PlayCoin();
            }
            
            // Анимируем монетку к точке финиша
            MoneyAnimation moneyAnim = coin.GetComponent<MoneyAnimation>();
            if (moneyAnim == null)
            {
                moneyAnim = coin.AddComponent<MoneyAnimation>();
            }
            
            // Конвертируем позицию финиша в локальные координаты Canvas
            Vector2 finishScreenPos = RectTransformUtility.WorldToScreenPoint(null, finishRect.position);
            Vector2 finishLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, finishScreenPos, null, out finishLocalPos);
            
            // Передаем финальную позицию в локальных координатах Canvas
            moneyAnim.Initialize(finishLocalPos);
            moneyAnim.animationDuration = coinAnimationDuration;
            moneyAnim.AnimateToTarget();
            
            yield return new WaitForSeconds(coinSpawnDelay);
        }
        
        // Ждем завершения анимаций (но не слишком долго)
        yield return new WaitForSeconds(coinAnimationDuration);
        
        // Удаляем все монетки
        foreach (GameObject coin in coins)
        {
            if (coin != null)
            {
                Destroy(coin);
            }
        }
        
        // Добавляем деньги
        if (moneyManager != null)
        {
            moneyManager.AddMoney(moneyAmount);
        }
        
        // Минимальная задержка перед загрузкой сцены
        yield return new WaitForSeconds(0.3f);
        
        // НЕ вызываем CleanupBeforeSceneLoad() здесь, так как он останавливает корутины!
        // Unity сам уничтожит все объекты при загрузке новой сцены
        
        // Сбрасываем флаг перед загрузкой сцены
        isLoadingScene = false;
        
        // Загружаем сцену
        SceneManager.LoadScene(sceneIndex);
    }
    
    // Корутина для загрузки сцены по имени с анимацией монеток
    private IEnumerator LoadSceneWithCoinAnimationByName(string sceneName)
    {
        if (coinStartPoint == null || coinFinishPoint == null || coinPrefab == null)
        {
            if (moneyManager != null)
            {
                moneyManager.AddMoney(moneyAmount);
            }
            yield return new WaitForSeconds(0.5f);
            isLoadingScene = false;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            yield break;
        }
        
        // Находим Canvas для создания UI элементов
        Canvas canvas = coinStartPoint.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        if (canvas == null)
        {
            if (moneyManager != null)
            {
                moneyManager.AddMoney(moneyAmount);
            }
            yield return new WaitForSeconds(0.5f);
            isLoadingScene = false;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            yield break;
        }
        
        List<GameObject> coins = new List<GameObject>();
        RectTransform canvasRect = canvas.transform as RectTransform;
        
        if (canvasRect == null)
        {
            if (moneyManager != null)
            {
                moneyManager.AddMoney(moneyAmount);
            }
            yield return new WaitForSeconds(0.5f);
            isLoadingScene = false;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            yield break;
        }
        
        for (int i = 0; i < coinsCount; i++)
        {
            RectTransform startRect = coinStartPoint as RectTransform;
            RectTransform finishRect = coinFinishPoint as RectTransform;
            
            if (startRect == null || finishRect == null)
                break;
            
            GameObject coin = Instantiate(coinPrefab, canvas.transform);
            RectTransform coinRect = coin.GetComponent<RectTransform>();
            
            if (coinRect == null)
            {
                Destroy(coin);
                continue;
            }
            
            Vector2 startScreenPos = RectTransformUtility.WorldToScreenPoint(null, startRect.position);
            Vector2 startLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPos, null, out startLocalPos);
            
            // Устанавливаем начальную позицию с небольшим разбросом
            coinRect.anchoredPosition = startLocalPos;
            coinRect.anchoredPosition += Random.insideUnitCircle * 20f;
            
            coins.Add(coin);
            
            // Проигрываем звук при создании монетки
            if (audioManager != null)
            {
                audioManager.PlayCoin();
            }
            
            // Анимируем монетку к точке финиша
            MoneyAnimation moneyAnim = coin.GetComponent<MoneyAnimation>();
            if (moneyAnim == null)
            {
                moneyAnim = coin.AddComponent<MoneyAnimation>();
            }
            
            // Конвертируем позицию финиша в локальные координаты Canvas
            Vector2 finishScreenPos = RectTransformUtility.WorldToScreenPoint(null, finishRect.position);
            Vector2 finishLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, finishScreenPos, null, out finishLocalPos);
            
            // Передаем финальную позицию в локальных координатах Canvas
            moneyAnim.Initialize(finishLocalPos);
            moneyAnim.animationDuration = coinAnimationDuration;
            moneyAnim.AnimateToTarget();
            
            yield return new WaitForSeconds(coinSpawnDelay);
        }
        
        // Ждем завершения анимаций
        yield return new WaitForSeconds(coinAnimationDuration);
        
        // Удаляем все монетки
        foreach (GameObject coin in coins)
        {
            if (coin != null)
            {
                Destroy(coin);
            }
        }
        
        // Добавляем деньги
        if (moneyManager != null)
        {
            moneyManager.AddMoney(moneyAmount);
        }
        
        // Минимальная задержка перед загрузкой сцены
        yield return new WaitForSeconds(0.3f);
        
        // НЕ вызываем CleanupBeforeSceneLoad() здесь, так как он останавливает корутины!
        // Unity сам уничтожит все объекты при загрузке новой сцены
        
        // Сбрасываем флаг перед загрузкой сцены
        isLoadingScene = false;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    
    // Метод для очистки всех объектов перед перезагрузкой сцены
    private void CleanupBeforeSceneLoad()
    {
        // Проверяем, что мы действительно в игровой сцене, а не в меню
        // Если нет puzzle pieces, значит это первая загрузка или не игровая сцена
        if (puzzlePieces == null || puzzlePieces.Count == 0)
        {
            // Если нет puzzle pieces, значит это первая загрузка или не игровая сцена
            return;
        }
        
        // НЕ останавливаем корутины здесь, так как это остановит корутину загрузки сцены!
        // StopAllCoroutines(); // УБРАНО - останавливает корутину загрузки сцены!
        // При загрузке новой сцены Unity автоматически остановит все корутины
        
        // Уничтожаем все puzzle pieces
        if (puzzlePieces != null)
        {
            foreach (PuzzlePiece piece in puzzlePieces)
            {
                if (piece != null && piece.gameObject != null)
                {
                    Destroy(piece.gameObject);
                }
            }
            puzzlePieces.Clear();
        }
        
        // Очищаем словарь
        if (occupiedCells != null)
        {
            occupiedCells.Clear();
        }
        
        // Очищаем список спрайтов
        if (slicedSprites != null)
        {
            slicedSprites.Clear();
        }
        
        // Сбрасываем флаг завершения игры
        isGameComplete = false;
        
        // Уничтожаем все объекты PuzzlePiece, которые могут остаться в сцене
        PuzzlePiece[] allPieces = FindObjectsOfType<PuzzlePiece>();
        foreach (PuzzlePiece piece in allPieces)
        {
            if (piece != null && piece.gameObject != null)
            {
                Destroy(piece.gameObject);
            }
        }
        
        // Также ищем по имени на случай, если что-то осталось
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.name.Contains("PuzzlePiece"))
            {
                Destroy(obj);
            }
        }
    }
    
    // Загрузка следующей сцены (использует nextSceneIndex)
    public void LoadNextScene()
    {
        LoadSceneByIndex(nextSceneIndex);
    }
    
    // Перезагрузка текущей сцены по имени (гарантированно работает)
    public void ReloadCurrentScene()
    {
        // Очищаем все объекты перед перезагрузкой
        CleanupBeforeSceneLoad();
        
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    
    // Перезагрузка с анимацией монеток
    public void ReloadCurrentSceneWithAnimation()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        LoadSceneByIndex(currentIndex);
    }
    
    // Масштабирует текстуру с использованием высококачественного алгоритма
    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        // Если размеры совпадают, просто копируем пиксели с максимальным качеством
        if (source.width == targetWidth && source.height == targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels();
            result.SetPixels(pixels);
            result.filterMode = FilterMode.Point;
            result.wrapMode = TextureWrapMode.Clamp;
            result.Apply();
            return result;
        }
        
        // Используем RenderTexture с bilinear filtering для высококачественного масштабирования
        // Но применяем point filtering к результату для предотвращения артефактов на краях
        RenderTexture rt = RenderTexture.GetTemporary(
            targetWidth, 
            targetHeight, 
            0, 
            RenderTextureFormat.ARGB32, 
            RenderTextureReadWrite.sRGB
        );
        
        // Используем bilinear для плавного масштабирования
        rt.filterMode = FilterMode.Bilinear;
        
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        // Копируем исходную текстуру с масштабированием
        Graphics.Blit(source, rt);
        
        Texture2D resultTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        resultTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        
        // Устанавливаем point filtering для финальной текстуры
        resultTexture.filterMode = FilterMode.Point;
        resultTexture.wrapMode = TextureWrapMode.Clamp;
        resultTexture.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        
        return resultTexture;
    }
    
    // Получает читаемую текстуру с point filtering
    private Texture2D GetReadableTexture(Texture2D source)
    {
        // Если текстура уже читаема, возвращаем её
        try
        {
            source.GetPixels(0, 0, 1, 1);
            return source;
        }
        catch
        {
            // Текстура не читаема, создаем копию
            // Используем sRGB для правильной цветопередачи
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                source.width, 
                source.height, 
                0, 
                RenderTextureFormat.Default, 
                RenderTextureReadWrite.sRGB
            );
            
            // Устанавливаем point filtering для RenderTexture
            renderTexture.filterMode = FilterMode.Point;
            
            Graphics.Blit(source, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            
            // Создаем текстуру с явным форматом и point filtering
            Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            
            // КРИТИЧНО: Устанавливаем point filtering ДО Apply()
            readableTexture.filterMode = FilterMode.Point;
            readableTexture.wrapMode = TextureWrapMode.Clamp;
            readableTexture.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            
            return readableTexture;
        }
    }
}

