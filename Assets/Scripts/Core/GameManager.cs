using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

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
    
    void Start()
    {
        InitializeComponents();
        StartGame();
    }
    
    private void InitializeComponents()
    {
        // Инициализация всех компонентов
        puzzleGrid = GetComponent<PuzzleGrid>();
        if (puzzleGrid == null)
        {
            puzzleGrid = gameObject.AddComponent<PuzzleGrid>();
        }
        puzzleGrid.Initialize(gridRows, gridCols, fieldWidth, fieldHeight, cardSpacing);
        puzzleGrid.deckPosition = deckPosition;
        puzzleGrid.CreateGridCells(); // Создаем ячейки с коллайдерами
        
        imageSlicer = GetComponent<ImageSlicer>();
        if (imageSlicer == null)
        {
            imageSlicer = gameObject.AddComponent<ImageSlicer>();
        }
        
        cardDealer = GetComponent<CardDealer>();
        if (cardDealer == null)
        {
            cardDealer = gameObject.AddComponent<CardDealer>();
        }
        cardDealer.dealDelay = dealDelay;
        
        cardFlipAnimator = GetComponent<CardFlipAnimator>();
        if (cardFlipAnimator == null)
        {
            cardFlipAnimator = gameObject.AddComponent<CardFlipAnimator>();
        }
        cardFlipAnimator.Initialize(audioManager, connectionManager);
        cardFlipAnimator.flipDelay = flipDelay;
        
        connectionManager = GetComponent<ConnectionManager>();
        if (connectionManager == null)
        {
            connectionManager = gameObject.AddComponent<ConnectionManager>();
        }
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
        {
            levelCompleteUI.SetMoneyTargetPosition(moneyTargetPosition);
        }
        
        confettiEffect = FindObjectOfType<ConfettiEffect>();
        if (confettiEffect == null)
        {
            GameObject confettiObj = new GameObject("ConfettiEffect");
            confettiEffect = confettiObj.AddComponent<ConfettiEffect>();
        }
        
        // Инициализация LevelManager
        levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            // Получаем размерность сетки из LevelManager
            int calculatedGridSize = levelManager.CalculateGridSize();
            if (gridRows == 0) gridRows = calculatedGridSize;
            if (gridCols == 0) gridCols = calculatedGridSize;
            
            Debug.Log($"LevelManager: {levelManager.GetLevelInfo()}");
        }
        else
        {
            Debug.LogWarning("LevelManager not found! Using default values.");
            if (gridRows == 0) gridRows = 3;
            if (gridCols == 0) gridCols = 3;
        }
        
        occupiedCells = new Dictionary<Vector2Int, PuzzlePiece>();
        puzzlePieces = new List<PuzzlePiece>();
    }
    
    private void StartGame()
    {
        StartCoroutine(GameSequence());
    }
    
    private IEnumerator GameSequence()
    {
        // 0. Показываем UI сложного уровня, если это сложный уровень
        if (levelManager != null && levelManager.IsDifficultLevel())
        {
            yield return StartCoroutine(levelManager.ShowDifficultLevelUI());
        }
        
        // 1. Загружаем картинку из LevelManager, если она не установлена
        if (sourceImage == null && levelManager != null)
        {
            if (levelManager.useAddressables)
            {
                // Асинхронная загрузка через Addressables
                Sprite loadedSprite = null;
                yield return StartCoroutine(levelManager.LoadLevelImageAsync((sprite) => {
                    loadedSprite = sprite;
                }));
                sourceImage = loadedSprite;
            }
            else
            {
                // Синхронная загрузка из Unity
                sourceImage = levelManager.GetLevelImage();
            }
        }
        
        // Проверяем наличие картинки
        if (sourceImage == null)
        {
            Debug.LogError("Source image is not set and LevelManager failed to load it!");
            yield break;
        }
        
        slicedSprites = imageSlicer.SliceImage(sourceImage, gridRows, gridCols);
        
        if (slicedSprites == null || slicedSprites.Count != gridRows * gridCols)
        {
            Debug.LogError("Failed to slice image!");
            yield break;
        }
        
        // 2. Создаем карточки
        CreatePuzzlePieces();
        
        // 3. Перемешиваем индексы
        ShufflePieces();
        
        // 4. Раздаем карточки
        yield return StartCoroutine(cardDealer.DealCards(puzzlePieces, puzzleGrid));
        
        // 5. Переворачиваем карточки
        yield return StartCoroutine(cardFlipAnimator.FlipAllCards(puzzlePieces));
        
        // 6. Инициализируем свайп-хендлер
        InitializeSwipeHandler();
        
        // 7. Проверяем соединения
        connectionManager.CheckAllConnections();
    }
    
    private void CreatePuzzlePieces()
    {
        puzzlePieces.Clear();
        occupiedCells.Clear();
        
        // Получаем размер карточки из CardBack спрайта (соотношение 3:4)
        Vector2 cardSize;
        if (cardBackSprite != null)
        {
            cardSize = new Vector2(
                cardBackSprite.bounds.size.x,
                cardBackSprite.bounds.size.y
            );
        }
        else
        {
            cardSize = puzzleGrid.GetCardSize();
        }
        
        for (int i = 0; i < gridRows * gridCols; i++)
        {
            GameObject pieceObj;
            
            if (puzzlePiecePrefab != null)
            {
                pieceObj = Instantiate(puzzlePiecePrefab);
                
                // Если есть PuzzlePieceSetup, обновляем размер из CardBack
                PuzzlePieceSetup setup = pieceObj.GetComponent<PuzzlePieceSetup>();
                if (setup != null && cardBackSprite != null)
                {
                    setup.UpdateSizeFromCardBack(cardBackSprite);
                    cardSize = setup.cardSize; // Обновляем cardSize из префаба
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
            collider.size = cardSize;
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
            
            // Масштабируем frontSprite под размер карточки (из CardBack)
            Sprite scaledFrontSprite = ScaleSpriteToSize(frontSprite, cardSize);
            
            piece.Initialize(i, scaledFrontSprite, cardBackSprite, puzzleGrid);
            
            // Устанавливаем размер карточки под размер CardBack
            piece.SetCardSize(cardSize);
            
            // Устанавливаем спрайт обратной стороны
            SpriteRenderer sr = pieceObj.GetComponent<SpriteRenderer>();
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
                
                Debug.Log($"Карточка {i}: Используются рамки из префаба");
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
                        Debug.Log($"Карточка {i}: Рамки созданы через CardPrefabBuilder");
                    }
                    else
                    {
                        Debug.LogWarning($"Карточка {i}: Не удалось создать рамки через CardPrefabBuilder");
                    }
                }
                else if (borderSprite != null)
                {
                    // Fallback - создаем старым способом (только если нет CardPrefabBuilder)
                    CreateBorders(pieceObj, piece, borderSprite, cardSize);
                    Debug.Log($"Карточка {i}: Рамки созданы программно (fallback)");
                }
                else
                {
                    Debug.LogWarning($"Карточка {i}: BorderSprite не установлен, рамки не будут созданы!");
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
                    Debug.LogWarning($"Карточка {i}: BorderRenderer не полностью инициализирован! top={br.topBorder != null}, bottom={br.bottomBorder != null}, left={br.leftBorder != null}, right={br.rightBorder != null}");
                }
            }
            else
            {
                Debug.LogWarning($"Карточка {i}: BorderRenderer не найден!");
            }
            
            puzzlePieces.Add(piece);
        }
        
        // Диагностика после создания всех карточек
        Debug.Log($"=== ДИАГНОСТИКА РАМОК ===");
        int bordersFound = 0;
        int bordersMissing = 0;
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            BorderRenderer br = piece.GetComponentInChildren<BorderRenderer>();
            if (br != null)
            {
                bool allBorders = br.topBorder != null && br.bottomBorder != null && 
                                 br.leftBorder != null && br.rightBorder != null;
                if (allBorders)
                {
                    bordersFound++;
                }
                else
                {
                    bordersMissing++;
                    Debug.LogWarning($"Карточка {piece.originalIndex}: Рамки неполные - top={br.topBorder != null}, bottom={br.bottomBorder != null}, left={br.leftBorder != null}, right={br.rightBorder != null}");
                }
            }
            else
            {
                bordersMissing++;
                Debug.LogWarning($"Карточка {piece.originalIndex}: BorderRenderer не найден!");
            }
        }
        Debug.Log($"Рамки найдены: {bordersFound}, отсутствуют: {bordersMissing}");
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
        
        Debug.Log($"Перемешано {puzzlePieces.Count} карточек");
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
    
    private void OnLevelComplete()
    {
        isGameComplete = true;
        
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
    }
    
    // Public method to check if game is complete
    public bool IsGameComplete()
    {
        return isGameComplete;
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
    
    // Загрузка сцены по индексу с анимацией монеток
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            StartCoroutine(LoadSceneWithCoinAnimation(sceneIndex));
        }
        else
        {
            Debug.LogWarning($"Scene index {sceneIndex} is out of range! Available scenes: 0-{SceneManager.sceneCountInBuildSettings - 1}");
        }
    }
    
    // Корутина: анимация монеток -> добавление денег -> загрузка сцены
    private IEnumerator LoadSceneWithCoinAnimation(int sceneIndex)
    {
        // Проверяем наличие необходимых объектов
        if (coinStartPoint == null || coinFinishPoint == null || coinPrefab == null)
        {
            Debug.LogWarning("Coin animation parameters not set! Loading scene without animation.");
            yield return new WaitForSeconds(1f);
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
            Debug.LogWarning("Canvas not found! Loading scene without animation.");
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene(sceneIndex);
            yield break;
        }
        
        List<GameObject> coins = new List<GameObject>();
        
        // Создаем монетки в точке старта
        RectTransform canvasRect = canvas.transform as RectTransform;
        
        if (canvasRect == null)
        {
            Debug.LogWarning("Canvas must have RectTransform component!");
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene(sceneIndex);
            yield break;
        }
        
        for (int i = 0; i < coinsCount; i++)
        {
            // Используем RectTransform для UI элементов
            RectTransform startRect = coinStartPoint as RectTransform;
            RectTransform finishRect = coinFinishPoint as RectTransform;
            
            if (startRect == null || finishRect == null)
            {
                Debug.LogWarning("Coin start/finish points must have RectTransform component!");
                break;
            }
            
            // Создаем монетку как дочерний объект Canvas
            GameObject coin = Instantiate(coinPrefab, canvas.transform);
            RectTransform coinRect = coin.GetComponent<RectTransform>();
            
            if (coinRect == null)
            {
                Debug.LogWarning("Coin prefab must have RectTransform component!");
                Destroy(coin);
                continue;
            }
            
            // Конвертируем позицию старта в локальные координаты Canvas
            Vector2 startScreenPos = RectTransformUtility.WorldToScreenPoint(null, startRect.position);
            Vector2 startLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPos, null, out startLocalPos);
            
            // Устанавливаем начальную позицию с небольшим разбросом
            coinRect.anchoredPosition = startLocalPos;
            coinRect.anchoredPosition += Random.insideUnitCircle * 20f; // Небольшой разброс в пикселях
            
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
        
        // Удаляем все монетки (на случай, если они еще не удалились)
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
        
        // Ждем 1 секунду перед загрузкой сцены
        yield return new WaitForSeconds(1f);
        
        // Загружаем сцену
        SceneManager.LoadScene(sceneIndex);
    }
    
    // Загрузка следующей сцены (использует nextSceneIndex)
    public void LoadNextScene()
    {
        LoadSceneByIndex(nextSceneIndex);
    }
    
    // Перезагрузка текущей сцены
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Масштабирует спрайт до нужного размера
    private Sprite ScaleSpriteToSize(Sprite sourceSprite, Vector2 targetSize)
    {
        if (sourceSprite == null) return null;
        
        // Получаем текущий размер спрайта в мировых единицах
        Vector2 currentSize = sourceSprite.bounds.size;
        
        // Вычисляем масштаб
        float scaleX = targetSize.x / currentSize.x;
        float scaleY = targetSize.y / currentSize.y;
        
        // Вычисляем новый размер текстуры в пикселях
        int newWidth = Mathf.RoundToInt(sourceSprite.texture.width * scaleX);
        int newHeight = Mathf.RoundToInt(sourceSprite.texture.height * scaleY);
        
        // Масштабируем текстуру
        Texture2D scaledTexture = ScaleTexture(GetReadableTexture(sourceSprite.texture), newWidth, newHeight);
        
        // Создаем новый спрайт
        Sprite scaledSprite = Sprite.Create(
            scaledTexture,
            new Rect(0, 0, scaledTexture.width, scaledTexture.height),
            new Vector2(0.5f, 0.5f),
            sourceSprite.pixelsPerUnit
        );
        
        scaledSprite.name = sourceSprite.name + "_Scaled";
        
        return scaledSprite;
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

