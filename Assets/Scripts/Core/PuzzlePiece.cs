using UnityEngine;
using System.Collections.Generic;
using PlayerPrefs = RedefineYG.PlayerPrefs;
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
    
    [Header("Card Sprite")]
    [Tooltip("Дочерний объект со SpriteRenderer для отображения карточки. Если не установлен, создается автоматически.")]
    public GameObject cardSpriteContainer;
    [Tooltip("SpriteRenderer дочернего объекта. Если не установлен, находится автоматически.")]
    public SpriteRenderer cardSpriteRenderer;
    
    // Мультипликаторы масштабирования для front и back sprite
    private float frontSpriteScaleMultiplier = 1.0f;
    private float backSpriteScaleMultiplier = 1.0f;
    
    private PuzzleGrid grid;
    
    void Awake()
    {
        // КРИТИЧНО для WebGL: Инициализируем ссылки на дочерние объекты сразу в Awake
        // В WebGL билде ссылки могут быть null при первом обращении, даже если они указаны в префабе
        EnsureContainerInitialized();
    }
    
    // КРИТИЧНО для WebGL: Гарантированная инициализация контейнера
    // Вызывается в Awake и перед каждым использованием контейнера
    private void EnsureContainerInitialized()
    {
        if (cardSpriteContainer == null)
        {
            // Сначала пытаемся найти по имени (если GameObject переименован)
            cardSpriteContainer = transform.Find("CardSpriteContainer")?.gameObject;
            
            // Если не найдено по имени, ищем первый дочерний объект со SpriteRenderer
            if (cardSpriteContainer == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<SpriteRenderer>() != null && child.name != "BorderContainer")
                    {
                        cardSpriteContainer = child.gameObject;
                        break;
                    }
                }
            }
        }
        
        // Инициализируем cardSpriteRenderer
        if (cardSpriteRenderer == null && cardSpriteContainer != null)
        {
            cardSpriteRenderer = cardSpriteContainer.GetComponent<SpriteRenderer>();
            
            // КРИТИЧНО для WebGL: Если SpriteRenderer не найден, создаем его
            if (cardSpriteRenderer == null)
            {
                cardSpriteRenderer = cardSpriteContainer.AddComponent<SpriteRenderer>();
                Debug.LogWarning($"PuzzlePiece {originalIndex}: SpriteRenderer was missing on {cardSpriteContainer.name}, created automatically");
            }
        }
    }
    
    public void Initialize(int index, Sprite front, Sprite back, PuzzleGrid puzzleGrid)
    {
        originalIndex = index;
        frontSprite = front;
        backSprite = back;
        grid = puzzleGrid;
        
        // КРИТИЧНО для WebGL: Гарантируем инициализацию контейнера ПЕРЕД установкой спрайта
        // В WebGL билде дочерние объекты могут быть не готовы в Awake
        EnsureContainerInitialized();
        
        #if UNITY_EDITOR
        // ДИАГНОСТИКА: Проверяем размеры
        // КРИТИЧНО для WebGL: Используем GetSpriteSize() с fallback для надежности
        if (frontSprite != null && backSprite != null)
        {
            Vector2 frontSize = GetSpriteSize(frontSprite);
            Vector2 backSize = GetSpriteSize(backSprite);
            
            // Для диагностики также показываем bounds.size (может отличаться на разных устройствах)
            Vector2 frontBounds = frontSprite.bounds.size;
            Vector2 backBounds = backSprite.bounds.size;
            
            // Показываем источник размера для диагностики
            string frontSource = frontSprite.rect.width > 0 ? "rect/PPU" : 
                                (frontSprite.texture != null ? "texture/PPU" : "bounds");
            string backSource = backSprite.rect.width > 0 ? "rect/PPU" : 
                               (backSprite.texture != null ? "texture/PPU" : "bounds");
            
            Debug.Log($"PuzzlePiece {index}: " +
                     $"Front size ({frontSource})={frontSize}, bounds={frontBounds}, " +
                     $"Back size ({backSource})={backSize}, bounds={backBounds}");
        }
        #endif
        
        // Устанавливаем обратную сторону по умолчанию
        // КРИТИЧНО: После EnsureContainerInitialized() cardSpriteRenderer должен быть найден
        if (cardSpriteRenderer == null)
        {
            EnsureContainerInitialized(); // Пробуем еще раз
        }
        
        if (cardSpriteRenderer != null && backSprite != null)
        {
            cardSpriteRenderer.sprite = backSprite;
            cardSpriteRenderer.sortingOrder = 0; // По умолчанию
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"PuzzlePiece {index}: Sprite set on container '{cardSpriteContainer?.name}' (renderer: {cardSpriteRenderer != null})");
            #endif
        }
        else
        {
            Debug.LogError($"PuzzlePiece {index}: FAILED to set sprite! cardSpriteRenderer={cardSpriteRenderer != null}, backSprite={backSprite != null}, cardSpriteContainer={cardSpriteContainer != null}, children={transform.childCount}");
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
    
    // Синхронизирует визуальную позицию с grid координатами
    // Используется для исправления рассинхронизации после анимаций
    public void SyncPositionWithGrid()
    {
        if (grid != null)
        {
            Vector2 worldPos = grid.GetWorldPosition(currentGridRow, currentGridCol);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        }
    }
    
    public Vector2Int GetOriginalPosition(int gridCols)
    {
        int row = originalIndex / gridCols;
        int col = originalIndex % gridCols;
        return new Vector2Int(row, col);
    }
    
    // КРИТИЧНО для WebGL: Безопасное получение размера спрайта с fallback
    // В WebGL билде rect.width может быть 0 для динамически созданных спрайтов
    // Использует texture.width/height как fallback
    private Vector2 GetSpriteSize(Sprite sprite)
    {
        if (sprite == null)
        {
            return Vector2.zero;
        }
        
        // Приоритет 1: Используем rect и pixelsPerUnit (работает в редакторе)
        if (sprite.rect.width > 0 && sprite.rect.height > 0 && sprite.pixelsPerUnit > 0)
        {
            return new Vector2(
                sprite.rect.width / sprite.pixelsPerUnit,
                sprite.rect.height / sprite.pixelsPerUnit
            );
        }
        
        // Приоритет 2 (WebGL fallback): Используем texture.width/height
        // Это надежно работает в WebGL, так как текстура всегда имеет правильные размеры
        if (sprite.texture != null && sprite.texture.width > 0 && sprite.texture.height > 0 && sprite.pixelsPerUnit > 0)
        {
            return new Vector2(
                sprite.texture.width / sprite.pixelsPerUnit,
                sprite.texture.height / sprite.pixelsPerUnit
            );
        }
        
        // Приоритет 3: Используем bounds.size (последний fallback)
        // Может быть менее точным из-за pixel ratio, но всегда доступен
        return sprite.bounds.size;
    }
    
    public void Flip()
    {
        isFlipped = !isFlipped;
        
        // КРИТИЧНО для WebGL: Гарантируем инициализацию контейнера перед переворотом
        EnsureContainerInitialized();
        
        if (cardSpriteRenderer != null)
        {
            Sprite newSprite = isFlipped ? frontSprite : backSprite;
            
            // КРИТИЧНО для WebGL: Проверяем, что спрайт не null перед установкой
            if (newSprite == null)
            {
                Debug.LogError($"PuzzlePiece {originalIndex}: Cannot flip! newSprite is null (isFlipped: {isFlipped}, frontSprite: {frontSprite != null}, backSprite: {backSprite != null})");
                return;
            }
            
            cardSpriteRenderer.sprite = newSprite;
            
            // Применяем правильный мультипликатор масштабирования в зависимости от стороны
            float multiplier = isFlipped ? frontSpriteScaleMultiplier : backSpriteScaleMultiplier;
            if (cardSpriteContainer != null)
            {
                cardSpriteContainer.transform.localScale = Vector3.one * multiplier;
            }
            
            // Проверяем, что размеры спрайтов совпадают после переворота
            // КРИТИЧНО для WebGL: Используем GetSpriteSize() с fallback на texture.width/height
            if (newSprite != null && backSprite != null)
            {
                // Используем безопасный метод получения размеров с fallback для WebGL
                Vector2 newSpriteSize = GetSpriteSize(newSprite);
                Vector2 backSpriteSize = GetSpriteSize(backSprite);
                
                // Вычисляем размер с учетом текущего масштаба (главного объекта и дочернего)
                Vector2 mainScale = new Vector2(transform.localScale.x, transform.localScale.y);
                Vector2 containerScale = cardSpriteContainer != null ? 
                    new Vector2(cardSpriteContainer.transform.localScale.x, cardSpriteContainer.transform.localScale.y) : 
                    Vector2.one;
                Vector2 totalScale = new Vector2(mainScale.x * containerScale.x, mainScale.y * containerScale.y);
                
                Vector2 newSpriteSizeScaled = newSpriteSize * totalScale;
                Vector2 backSpriteSizeScaled = backSpriteSize * totalScale;
                
                // КРИТИЧНО: Проверяем на ноль перед делением, чтобы избежать NaN
                float maxX = Mathf.Max(Mathf.Abs(newSpriteSizeScaled.x), Mathf.Abs(backSpriteSizeScaled.x));
                float maxY = Mathf.Max(Mathf.Abs(newSpriteSizeScaled.y), Mathf.Abs(backSpriteSizeScaled.y));
                
                float sizeDiffX = maxX > 0.0001f ? 
                    Mathf.Abs(newSpriteSizeScaled.x - backSpriteSizeScaled.x) / maxX : 0f;
                float sizeDiffY = maxY > 0.0001f ? 
                    Mathf.Abs(newSpriteSizeScaled.y - backSpriteSizeScaled.y) / maxY : 0f;
                
                // Если разница значительная, это указывает на проблему с pixelsPerUnit
                if (sizeDiffX > 0.01f || sizeDiffY > 0.01f)
                {
                    Debug.LogWarning($"PuzzlePiece {originalIndex}: After flip, sprite size mismatch! " +
                                   $"newSprite (scaled): {newSpriteSizeScaled.x:F4}x{newSpriteSizeScaled.y:F4}, " +
                                   $"backSprite (scaled): {backSpriteSizeScaled.x:F4}x{backSpriteSizeScaled.y:F4}, " +
                                   $"Difference: {sizeDiffX*100:F2}%/{sizeDiffY*100:F2}%. " +
                                   $"This indicates a pixelsPerUnit calculation issue in ImageSlicer.");
                    
                    // Корректируем масштаб только если разница критическая (>5%) И размеры валидны
                    if ((sizeDiffX > 0.05f || sizeDiffY > 0.05f) && maxX > 0.0001f && maxY > 0.0001f)
                    {
                        float scaleCorrectionX = maxX > 0.0001f ? backSpriteSizeScaled.x / newSpriteSizeScaled.x : 1f;
                        float scaleCorrectionY = maxY > 0.0001f ? backSpriteSizeScaled.y / newSpriteSizeScaled.y : 1f;
                        
                        // Проверяем на валидность перед применением
                        if (!float.IsNaN(scaleCorrectionX) && !float.IsInfinity(scaleCorrectionX) &&
                            !float.IsNaN(scaleCorrectionY) && !float.IsInfinity(scaleCorrectionY) &&
                            scaleCorrectionX > 0.01f && scaleCorrectionX < 100f &&
                            scaleCorrectionY > 0.01f && scaleCorrectionY < 100f)
                        {
                            Vector3 currentScale = transform.localScale;
                            transform.localScale = new Vector3(
                                currentScale.x * scaleCorrectionX,
                                currentScale.y * scaleCorrectionY,
                                currentScale.z
                            );
                            Debug.LogWarning($"PuzzlePiece {originalIndex}: Applied scale correction: {scaleCorrectionX:F4}x{scaleCorrectionY:F4}");
                        }
                        else
                        {
                            Debug.LogError($"PuzzlePiece {originalIndex}: Invalid scale correction values: {scaleCorrectionX:F4}x{scaleCorrectionY:F4}. Skipping correction.");
                        }
                    }
                }
            }
        }
    }
    
    public void SetSprite(Sprite sprite)
    {
        if (cardSpriteRenderer != null)
        {
            cardSpriteRenderer.sprite = sprite;
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
        if (cardSpriteRenderer != null && backSprite != null)
        {
            // КРИТИЧНО для WebGL: Используем GetSpriteSize() с fallback для надежности
            // Это гарантирует стабильные размеры независимо от pixel ratio устройства и платформы
            Vector2 baseSize = GetSpriteSize(backSprite);
            
            // Проверяем валидность размера перед вычислением масштаба
            if (baseSize.x > 0.0001f && baseSize.y > 0.0001f)
            {
                // Вычисляем масштаб для всего префаба
                float scaleX = targetSize.x / baseSize.x;
                float scaleY = targetSize.y / baseSize.y;
                
                // Проверяем валидность масштаба
                if (!float.IsNaN(scaleX) && !float.IsInfinity(scaleX) &&
                    !float.IsNaN(scaleY) && !float.IsInfinity(scaleY) &&
                    scaleX > 0.01f && scaleX < 100f &&
                    scaleY > 0.01f && scaleY < 100f)
                {
                    // Применяем масштаб ко всему префабу (включая дочерние элементы - рамки)
                    transform.localScale = new Vector3(scaleX, scaleY, 1f);
                }
                else
                {
                    Debug.LogError($"PuzzlePiece {originalIndex}: Invalid scale calculated: {scaleX:F4}x{scaleY:F4} for targetSize: {targetSize}, baseSize: {baseSize}");
                }
            }
            else
            {
                Debug.LogError($"PuzzlePiece {originalIndex}: Invalid baseSize: {baseSize} for backSprite. Cannot set card size.");
            }
        }
    }
    
    // Возвращает SpriteRenderer из дочернего объекта
    public SpriteRenderer GetCardSpriteRenderer()
    {
        // КРИТИЧНО для WebGL: Гарантируем инициализацию перед возвратом
        EnsureContainerInitialized();
        
        return cardSpriteRenderer;
    }
    
    // Устанавливает sortingOrder в дочернем SpriteRenderer
    public void SetCardSortingOrder(int order)
    {
        if (cardSpriteRenderer != null)
        {
            cardSpriteRenderer.sortingOrder = order;
        }
    }
    
    // Возвращает sortingOrder из дочернего SpriteRenderer
    public int GetCardSortingOrder()
    {
        return cardSpriteRenderer != null ? cardSpriteRenderer.sortingOrder : 0;
    }
    
    // Устанавливает мультипликатор масштабирования для front sprite
    public void SetFrontSpriteScale(float multiplier)
    {
        frontSpriteScaleMultiplier = multiplier;
        // Применяем только если карточка сейчас показывает front sprite
        if (cardSpriteContainer != null && isFlipped)
        {
            cardSpriteContainer.transform.localScale = Vector3.one * multiplier;
        }
    }
    
    // Устанавливает мультипликатор масштабирования для back sprite
    public void SetBackSpriteScale(float multiplier)
    {
        backSpriteScaleMultiplier = multiplier;
        // Применяем только если карточка сейчас показывает back sprite
        if (cardSpriteContainer != null && !isFlipped)
        {
            cardSpriteContainer.transform.localScale = Vector3.one * multiplier;
        }
    }
    
    // Устанавливает оба мультипликатора одновременно
    public void SetSpriteScales(float frontMultiplier, float backMultiplier)
    {
        frontSpriteScaleMultiplier = frontMultiplier;
        backSpriteScaleMultiplier = backMultiplier;
        
        // КРИТИЧНО для WebGL: Гарантируем инициализацию контейнера
        EnsureContainerInitialized();
        
        // Применяем правильный мультипликатор в зависимости от текущего состояния
        if (cardSpriteContainer != null)
        {
            float multiplier = isFlipped ? frontMultiplier : backMultiplier;
            cardSpriteContainer.transform.localScale = Vector3.one * multiplier;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"PuzzlePiece {originalIndex}: Applied sprite scale multiplier: {multiplier} (isFlipped: {isFlipped}, container: {cardSpriteContainer.name})");
            #endif
        }
        else
        {
            Debug.LogError($"PuzzlePiece {originalIndex}: cardSpriteContainer is null! Cannot apply sprite scale multiplier. GameObject: {gameObject.name}, Children count: {transform.childCount}");
        }
    }
}
