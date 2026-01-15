using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MenuProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Родительский объект для сетки карточек (GridLayoutGroup)")]
    public Transform cardsGridParent;
    
    [Tooltip("Префаб карточки меню")]
    public GameObject menuCardPrefab;
    
    [Tooltip("Спрайт обратной стороны карточки (закрытая)")]
    public Sprite cardBackSprite;
    
    [Header("Settings")]
    [Tooltip("Размер сетки (по умолчанию 5x5)")]
    public int gridSize = 5;
    
    [Tooltip("Толщина рамки в пикселях (для UI отображения)")]
    public float borderThickness = 8f;
    
    [Tooltip("Длительность задержки между анимациями карточек")]
    public float cardAnimationDelay = 0.05f;
    
    [Header("Border Offsets")]
    [Tooltip("Смещение верхней рамки (X, Y)")]
    public Vector2 topBorderOffset = Vector2.zero;
    
    [Tooltip("Смещение нижней рамки (X, Y)")]
    public Vector2 bottomBorderOffset = Vector2.zero;
    
    [Tooltip("Смещение левой рамки (X, Y)")]
    public Vector2 leftBorderOffset = Vector2.zero;
    
    [Tooltip("Смещение правой рамки (X, Y)")]
    public Vector2 rightBorderOffset = Vector2.zero;
    
    private MenuManager menuManager;
    private ImageSlicer imageSlicer;
    private List<MenuCard> menuCards = new List<MenuCard>();
    private int currentImageIndex = -1;
    private bool isInitialized = false;
    private bool shouldAnimateNewCard = false;
    
    private const string LAST_UNLOCKED_COUNT_KEY = "LastUnlockedCardCount";
    private const string LAST_IMAGE_INDEX_KEY = "LastImageIndex";
    
    void Start()
    {
        menuManager = MenuManager.Instance;
        if (menuManager == null)
        {
            Debug.LogError("MenuProgressUI: MenuManager not found!");
            return;
        }
        
        // Создаем ImageSlicer для разрезания картинок
        imageSlicer = gameObject.AddComponent<ImageSlicer>();
        
        Initialize();
    }
    
    /// <summary>
    /// Инициализирует сетку карточек
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        if (cardsGridParent == null)
        {
            Debug.LogError("MenuProgressUI: cardsGridParent is not set!");
            return;
        }
        
        // Настраиваем GridLayoutGroup, если он есть
        GridLayoutGroup gridLayout = cardsGridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = gridSize;
        }
        
        // Создаем карточки
        int totalCards = gridSize * gridSize;
        for (int i = 0; i < totalCards; i++)
        {
            GameObject cardObj;
            
            if (menuCardPrefab != null)
            {
                cardObj = Instantiate(menuCardPrefab, cardsGridParent);
            }
            else
            {
                // Создаем карточку программно, если нет префаба
                cardObj = new GameObject($"MenuCard_{i}");
                cardObj.transform.SetParent(cardsGridParent);
                
                // Добавляем RectTransform
                RectTransform rectTransform = cardObj.AddComponent<RectTransform>();
                rectTransform.localScale = Vector3.one;
                
                // Добавляем Image
                Image image = cardObj.AddComponent<Image>();
                image.preserveAspect = true;
            }
            
            // Получаем или создаем MenuCard компонент
            MenuCard menuCard = cardObj.GetComponent<MenuCard>();
            if (menuCard == null)
            {
                menuCard = cardObj.AddComponent<MenuCard>();
            }
            
            // Если карточка была создана программно, устанавливаем Image
            if (menuCard.cardImage == null)
            {
                Image image = cardObj.GetComponent<Image>();
                if (image != null)
                {
                    menuCard.cardImage = image;
                }
            }
            
            // Устанавливаем спрайт обратной стороны
            if (cardBackSprite != null)
            {
                menuCard.cardBackSprite = cardBackSprite;
            }
            
            menuCards.Add(menuCard);
        }
        
        isInitialized = true;
        
        // Загружаем текущую картинку
        LoadCurrentMenuImage();
    }
    
    /// <summary>
    /// Загружает текущую картинку меню
    /// </summary>
    public void LoadCurrentMenuImage()
    {
        if (menuManager == null) return;
        
        int imageIndex = menuManager.GetCurrentMenuImageIndex();
        StartCoroutine(LoadMenuImage(imageIndex));
    }
    
    /// <summary>
    /// Загружает и разрезает картинку меню
    /// </summary>
    private IEnumerator LoadMenuImage(int imageIndex)
    {
        if (currentImageIndex == imageIndex && menuCards.Count > 0 && menuCards[0].IsUnlocked())
        {
            // Картинка уже загружена, проверяем новую карточку
            CheckAndAnimateNewCard();
            yield break;
        }
        
        currentImageIndex = imageIndex;
        
        // Загружаем картинку через MenuManager
        Sprite menuImage = null;
        yield return StartCoroutine(menuManager.LoadMenuImageAsync(imageIndex, (sprite) => {
            menuImage = sprite;
        }));
        
        if (menuImage == null)
        {
            Debug.LogError($"MenuProgressUI: Failed to load menu image at index {imageIndex}");
            yield break;
        }
        
        // Разрезаем картинку на части
        List<Sprite> slicedSprites = imageSlicer.SliceImage(menuImage, gridSize, gridSize);
        
        if (slicedSprites == null || slicedSprites.Count != gridSize * gridSize)
        {
            Debug.LogError($"MenuProgressUI: Failed to slice image! Expected {gridSize * gridSize} sprites, got {(slicedSprites != null ? slicedSprites.Count : 0)}");
            yield break;
        }
        
        // Устанавливаем спрайты для карточек
        for (int i = 0; i < menuCards.Count && i < slicedSprites.Count; i++)
        {
            menuCards[i].SetCardSprite(slicedSprites[i]);
        }
        
        // Проверяем новую карточку ПЕРЕД обновлением прогресса
        CheckAndAnimateNewCard();
        
        // Обновляем рамки после загрузки карточек из сохранений
        // (CheckAndAnimateNewCard вызывает UpdateProgress, который уже обновляет рамки,
        // но вызываем еще раз для гарантии после полной загрузки)
        UpdateAllBorders();
    }
    
    /// <summary>
    /// Проверяет, нужно ли показать анимацию новой карточки
    /// </summary>
    private void CheckAndAnimateNewCard()
    {
        if (menuManager == null || !isInitialized) return;
        
        int imageIndex = menuManager.GetCurrentMenuImageIndex();
        
        // Получаем текущее количество открытых карточек ДО обновления прогресса
        int previousUnlockedCount = menuManager.GetUnlockedCardsCount(imageIndex);
        
        // Загружаем сохраненное значение из PlayerPrefs
        int savedImageIndex = PlayerPrefs.GetInt(LAST_IMAGE_INDEX_KEY, -1);
        int savedUnlockedCount = PlayerPrefs.GetInt(LAST_UNLOCKED_COUNT_KEY, 0);
        
        // Проверяем, была ли открыта новая карточка
        bool hasNewCard = menuManager.HasNewCardUnlocked();
        
        // Если это та же картинка и количество увеличилось, показываем анимацию
        bool shouldAnimate = (savedImageIndex == imageIndex && previousUnlockedCount > savedUnlockedCount) || hasNewCard;
        
        Debug.Log($"MenuProgressUI: savedImageIndex={savedImageIndex}, currentImageIndex={imageIndex}, savedUnlocked={savedUnlockedCount}, currentUnlocked={previousUnlockedCount}, shouldAnimate={shouldAnimate}");
        
        // Обновляем прогресс в MenuManager
        menuManager.UpdateProgress();
        
        // Получаем новое количество открытых карточек после обновления
        int unlockedCount = menuManager.GetUnlockedCardsCount(imageIndex);
        
        // ПРОВЕРЯЕМ: заполнена ли текущая картинка
        bool isCurrentImageCompleted = menuManager.IsImageCompleted(imageIndex);
        
        if (isCurrentImageCompleted)
        {
            // Картинка заполнена - проверяем, есть ли следующая
            int nextImageIndex = imageIndex + 1;
            if (nextImageIndex < menuManager.menuImages.Count)
            {
                Debug.Log($"MenuProgressUI: Current image {imageIndex} is completed, switching to next image {nextImageIndex}");
                
                // Переключаемся на следующую картинку
                // Убеждаемся, что уровень установлен на начало следующей картинки
                int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
                int targetLevel = nextImageIndex * menuManager.cardsPerImage;
                
                // Устанавливаем уровень только если он меньше целевого (чтобы не откатывать прогресс)
                if (currentLevel < targetLevel)
                {
                    PlayerPrefs.SetInt("CurrentLevel", targetLevel);
                    PlayerPrefs.Save();
                }
                
                // Обновляем индекс картинки после установки уровня
                int newImageIndex = menuManager.GetCurrentMenuImageIndex();
                
                // Сбрасываем сохраненные значения для новой картинки
                PlayerPrefs.SetInt(LAST_IMAGE_INDEX_KEY, newImageIndex);
                PlayerPrefs.SetInt(LAST_UNLOCKED_COUNT_KEY, 0);
                PlayerPrefs.Save();
                
                // Закрываем все карточки перед загрузкой новой картинки
                for (int i = 0; i < menuCards.Count; i++)
                {
                    menuCards[i].SetUnlocked(false);
                }
                
                // Загружаем новую картинку
                LoadCurrentMenuImage();
                return;
            }
            else
            {
                Debug.Log($"MenuProgressUI: Current image {imageIndex} is completed, but no more images available");
            }
        }
        
        if (shouldAnimate && unlockedCount > 0 && unlockedCount <= menuCards.Count)
        {
            Debug.Log($"MenuProgressUI: Will animate new card at index {unlockedCount - 1}");
            // Устанавливаем все карточки в правильное состояние, но последнюю оставляем закрытой
            UpdateProgress(skipLastCard: true);
            
            // Запускаем анимацию переворота последней карточки
            StartCoroutine(AnimateNewCardOnReturn());
        }
        else
        {
            // Просто обновляем прогресс без анимации
            UpdateProgress();
        }
        
        // Сохраняем текущие значения
        PlayerPrefs.SetInt(LAST_IMAGE_INDEX_KEY, imageIndex);
        PlayerPrefs.SetInt(LAST_UNLOCKED_COUNT_KEY, unlockedCount);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Обновляет состояние всех карточек на основе прогресса
    /// </summary>
    public void UpdateProgress(bool skipLastCard = false)
    {
        if (menuManager == null || !isInitialized) return;
        
        int imageIndex = menuManager.GetCurrentMenuImageIndex();
        int unlockedCount = menuManager.GetUnlockedCardsCount(imageIndex);
        
        for (int i = 0; i < menuCards.Count; i++)
        {
            bool isUnlocked = i < unlockedCount;
            
            // Если skipLastCard = true и это последняя карточка, оставляем её закрытой
            if (skipLastCard && i == unlockedCount - 1 && unlockedCount > 0)
            {
                menuCards[i].SetUnlocked(false); // Оставляем закрытой для анимации
                shouldAnimateNewCard = true;
            }
            else
            {
                menuCards[i].SetUnlocked(isUnlocked);
            }
        }
        
        // Обновляем рамки после изменения состояния карточек
        UpdateAllBorders();
    }
    
    /// <summary>
    /// Обновляет рамки для всех карточек в зависимости от их состояния и соседей
    /// </summary>
    private void UpdateAllBorders()
    {
        for (int i = 0; i < menuCards.Count; i++)
        {
            UpdateCardBorders(i);
        }
    }
    
    /// <summary>
    /// Обновляет рамки для конкретной карточки
    /// </summary>
    private void UpdateCardBorders(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= menuCards.Count) return;
        
        MenuCard menuCard = menuCards[cardIndex];
        if (menuCard == null) return;
        
        // Находим BorderRenderer в префабе (может быть в дочерних объектах)
        BorderRenderer borderRenderer = menuCard.GetComponentInChildren<BorderRenderer>();
        if (borderRenderer == null)
        {
            // Если нет BorderRenderer, значит это не префаб с рамками - пропускаем
            return;
        }
        
        // Настраиваем UI рамки для каждого border объекта
        if (menuCard.cardImage != null && menuCard.cardImage.rectTransform != null)
        {
            SetupBorderAsUI(borderRenderer.topBorder, menuCard.cardImage.rectTransform, 0); // Top
            SetupBorderAsUI(borderRenderer.bottomBorder, menuCard.cardImage.rectTransform, 1); // Bottom
            SetupBorderAsUI(borderRenderer.leftBorder, menuCard.cardImage.rectTransform, 2); // Left
            SetupBorderAsUI(borderRenderer.rightBorder, menuCard.cardImage.rectTransform, 3); // Right
        }
        
        // Проверяем, открыта ли карточка
        bool isUnlocked = menuCard.IsUnlocked();
        
        // Если карточка закрыта - скрываем все рамки
        if (!isUnlocked)
        {
            SetBordersActive(borderRenderer, new bool[4] { false, false, false, false });
            return;
        }
        
        // Вычисляем позицию карточки в сетке
        int row = cardIndex / gridSize;
        int col = cardIndex % gridSize;
        
        // Проверяем соседей: 0=верх, 1=низ, 2=лево, 3=право
        bool[] connections = new bool[4];
        
        // Верхний сосед
        int topIndex = cardIndex - gridSize;
        connections[0] = (row > 0 && topIndex >= 0 && topIndex < menuCards.Count && menuCards[topIndex].IsUnlocked());
        
        // Нижний сосед
        int bottomIndex = cardIndex + gridSize;
        connections[1] = (row < gridSize - 1 && bottomIndex < menuCards.Count && menuCards[bottomIndex].IsUnlocked());
        
        // Левый сосед
        int leftIndex = cardIndex - 1;
        connections[2] = (col > 0 && leftIndex >= 0 && leftIndex < menuCards.Count && menuCards[leftIndex].IsUnlocked());
        
        // Правый сосед
        int rightIndex = cardIndex + 1;
        connections[3] = (col < gridSize - 1 && rightIndex < menuCards.Count && menuCards[rightIndex].IsUnlocked());
        
        // Обновляем видимость рамок: показываем там, где нет соединения
        bool[] shouldShow = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            shouldShow[i] = !connections[i]; // Показываем если НЕ соединена
        }
        SetBordersActive(borderRenderer, shouldShow);
    }
    
    /// <summary>
    /// Настраивает border объект как UI элемент (добавляет Image и настраивает RectTransform)
    /// Использует спрайт, который уже есть на объекте (из SpriteRenderer)
    /// </summary>
    private void SetupBorderAsUI(GameObject borderObj, RectTransform cardRect, int side)
    {
        if (borderObj == null || cardRect == null) return;
        
        // Получаем спрайт из существующего SpriteRenderer
        SpriteRenderer spriteRenderer = borderObj.GetComponent<SpriteRenderer>();
        Sprite borderSprite = null;
        
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            borderSprite = spriteRenderer.sprite;
            // Отключаем SpriteRenderer, но не удаляем (для игры)
            spriteRenderer.enabled = false;
        }
        else
        {
            // Если нет спрайта, пропускаем
            return;
        }
        
        // Добавляем RectTransform, если его нет
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        if (borderRect == null)
        {
            borderRect = borderObj.AddComponent<RectTransform>();
        }
        
        // Делаем borderObj дочерним объектом карточки (если еще не является)
        if (borderObj.transform.parent != cardRect)
        {
            borderObj.transform.SetParent(cardRect);
        }
        
        // Рассчитываем правильный размер на основе реального размера спрайта
        // Конвертируем из Unity units в пиксели UI (обычно 1 unit = 100 pixels для Canvas)
        Vector2 spriteSizeInPixels = new Vector2(
            borderSprite.rect.width / borderSprite.pixelsPerUnit * 100f,
            borderSprite.rect.height / borderSprite.pixelsPerUnit * 100f
        );
        
        // Настраиваем RectTransform в зависимости от стороны
        borderRect.localRotation = Quaternion.identity;
        borderRect.localPosition = Vector3.zero;
        
        // Коэффициент коррекции для верхних и нижних рамок (примерно 16% больше)
        const float topBottomScaleCorrection = 1.16f;
        
        switch (side)
        {
            case 0: // Top
                borderRect.anchorMin = new Vector2(0, 1);
                borderRect.anchorMax = new Vector2(1, 1);
                // НЕ устанавливаем pivot - оставляем как в префабе
                borderRect.anchoredPosition = topBorderOffset; // Используем смещение
                borderRect.sizeDelta = new Vector2(0, spriteSizeInPixels.y);
                borderRect.localScale = new Vector3(topBottomScaleCorrection, topBottomScaleCorrection, topBottomScaleCorrection);
                break;
                
            case 1: // Bottom
                borderRect.anchorMin = new Vector2(0, 0);
                borderRect.anchorMax = new Vector2(1, 0);
                // НЕ устанавливаем pivot - оставляем как в префабе
                borderRect.anchoredPosition = bottomBorderOffset; // Используем смещение
                borderRect.sizeDelta = new Vector2(0, spriteSizeInPixels.y);
                borderRect.localScale = new Vector3(topBottomScaleCorrection, topBottomScaleCorrection, topBottomScaleCorrection);
                break;
                
            case 2: // Left
                borderRect.anchorMin = new Vector2(0, 0);
                borderRect.anchorMax = new Vector2(0, 1);
                // НЕ устанавливаем pivot - оставляем как в префабе
                borderRect.anchoredPosition = leftBorderOffset; // Используем смещение
                borderRect.sizeDelta = new Vector2(spriteSizeInPixels.x, 0);
                borderRect.localScale = Vector3.one;
                break;
                
            case 3: // Right
                borderRect.anchorMin = new Vector2(1, 0);
                borderRect.anchorMax = new Vector2(1, 1);
                // НЕ устанавливаем pivot - оставляем как в префабе
                borderRect.anchoredPosition = rightBorderOffset; // Используем смещение
                borderRect.sizeDelta = new Vector2(spriteSizeInPixels.x, 0);
                borderRect.localScale = Vector3.one;
                break;
        }
        
        // Добавляем или получаем Image компонент
        Image borderImage = borderObj.GetComponent<Image>();
        if (borderImage == null)
        {
            borderImage = borderObj.AddComponent<Image>();
        }
        
        // Устанавливаем тот же спрайт, что был в SpriteRenderer
        borderImage.sprite = borderSprite;
        borderImage.type = Image.Type.Simple; // ИСПРАВЛЕНО: Simple вместо Sliced для предотвращения искажений
        borderImage.preserveAspect = true; // ИСПРАВЛЕНО: Сохраняем пропорции спрайта
        borderImage.raycastTarget = false; // Не блокируем клики
        
        // Убеждаемся, что Image включен
        borderImage.enabled = true;
    }
    
    /// <summary>
    /// Устанавливает видимость рамок через Image компоненты (изолированно от BorderRenderer)
    /// </summary>
    private void SetBordersActive(BorderRenderer borderRenderer, bool[] shouldShow)
    {
        if (borderRenderer == null || shouldShow == null || shouldShow.Length < 4) return;
        
        // Устанавливаем видимость для каждой рамки через Image
        SetBorderImageActive(borderRenderer.topBorder, shouldShow[0]);
        SetBorderImageActive(borderRenderer.bottomBorder, shouldShow[1]);
        SetBorderImageActive(borderRenderer.leftBorder, shouldShow[2]);
        SetBorderImageActive(borderRenderer.rightBorder, shouldShow[3]);
    }
    
    /// <summary>
    /// Устанавливает видимость рамки через Image компонент
    /// </summary>
    private void SetBorderImageActive(GameObject borderObj, bool active)
    {
        if (borderObj == null) return;
        
        // Используем Image для UI
        Image borderImage = borderObj.GetComponent<Image>();
        if (borderImage != null)
        {
            borderImage.enabled = active;
        }
        // Если Image нет, используем SetActive как fallback
        else
        {
            borderObj.SetActive(active);
        }
    }
    
    /// <summary>
    /// Анимирует переворот новой карточки
    /// </summary>
    public void AnimateNewCard()
    {
        if (menuManager == null || !isInitialized) return;
        
        int imageIndex = menuManager.GetCurrentMenuImageIndex();
        int unlockedCount = menuManager.GetUnlockedCardsCount(imageIndex);
        
        // Проверяем, есть ли новая карточка для анимации
        if (unlockedCount > 0 && unlockedCount <= menuCards.Count)
        {
            int cardIndex = unlockedCount - 1;
            MenuCard newCard = menuCards[cardIndex];
            
            if (!newCard.IsUnlocked() && !newCard.IsAnimating())
            {
                StartCoroutine(AnimateCardFlip(newCard));
            }
        }
    }
    
    /// <summary>
    /// Корутина для анимации переворота карточки
    /// </summary>
    private IEnumerator AnimateCardFlip(MenuCard card)
    {
        card.FlipCard(() => {
            Debug.Log("MenuProgressUI: Card flip animation completed");
            // Обновляем рамки после переворота
            UpdateAllBorders();
        });
        
        yield return new WaitForSeconds(cardAnimationDelay);
    }
    
    /// <summary>
    /// Обновляет прогресс и анимирует новую карточку, если нужно
    /// </summary>
    public void RefreshProgress()
    {
        if (menuManager == null) return;
        
        // Обновляем прогресс в MenuManager
        menuManager.UpdateProgress();
        
        // Обновляем визуальное состояние
        UpdateProgress();
        
        // Проверяем, нужно ли анимировать новую карточку
        if (menuManager.HasNewCardUnlocked())
        {
            AnimateNewCard();
        }
    }
    
    /// <summary>
    /// Вызывается при возврате из уровня в меню
    /// Обновляет прогресс и анимирует новую карточку, если нужно
    /// </summary>
    public void OnReturnFromLevel()
    {
        if (menuManager == null) return;
        
        // Проверяем, была ли открыта новая карточка ДО обновления прогресса
        bool hasNewCard = menuManager.HasNewCardUnlocked();
        int imageIndex = menuManager.GetCurrentMenuImageIndex();
        int currentUnlocked = menuManager.GetUnlockedCardsCount(imageIndex);
        
        // Обновляем прогресс в MenuManager
        menuManager.UpdateProgress();
        
        // Получаем новое количество открытых карточек после обновления
        int unlockedCount = menuManager.GetUnlockedCardsCount(imageIndex);
        
        // ПРОВЕРЯЕМ: заполнена ли текущая картинка
        bool isCurrentImageCompleted = menuManager.IsImageCompleted(imageIndex);
        
        if (isCurrentImageCompleted)
        {
            // Картинка заполнена - проверяем, есть ли следующая
            int nextImageIndex = imageIndex + 1;
            if (nextImageIndex < menuManager.menuImages.Count)
            {
                Debug.Log($"MenuProgressUI: Current image {imageIndex} is completed, switching to next image {nextImageIndex}");
                
                // Переключаемся на следующую картинку
                // Убеждаемся, что уровень установлен на начало следующей картинки
                int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
                int targetLevel = nextImageIndex * menuManager.cardsPerImage;
                
                // Устанавливаем уровень только если он меньше целевого (чтобы не откатывать прогресс)
                if (currentLevel < targetLevel)
                {
                    PlayerPrefs.SetInt("CurrentLevel", targetLevel);
                    PlayerPrefs.Save();
                }
                
                // Обновляем индекс картинки после установки уровня
                int newImageIndex = menuManager.GetCurrentMenuImageIndex();
                
                // Сбрасываем сохраненные значения для новой картинки
                PlayerPrefs.SetInt(LAST_IMAGE_INDEX_KEY, newImageIndex);
                PlayerPrefs.SetInt(LAST_UNLOCKED_COUNT_KEY, 0);
                PlayerPrefs.Save();
                
                // Закрываем все карточки перед загрузкой новой картинки
                for (int i = 0; i < menuCards.Count; i++)
                {
                    menuCards[i].SetUnlocked(false);
                }
                
                // Загружаем новую картинку
                LoadCurrentMenuImage();
                return;
            }
            else
            {
                Debug.Log($"MenuProgressUI: Current image {imageIndex} is completed, but no more images available");
                // Просто обновляем прогресс без анимации
                UpdateProgress();
            }
        }
        else if (hasNewCard && unlockedCount > 0 && unlockedCount <= menuCards.Count)
        {
            // Устанавливаем все карточки в правильное состояние, но последнюю оставляем закрытой
            UpdateProgress(skipLastCard: true);
            
            // Запускаем анимацию переворота последней карточки
            StartCoroutine(AnimateNewCardOnReturn());
        }
        else
        {
            // Просто обновляем прогресс без анимации
            UpdateProgress();
        }
        
        // Проверяем, нужно ли загрузить новую картинку (на случай, если переключение произошло выше)
        int finalImageIndex = menuManager.GetCurrentMenuImageIndex();
        if (finalImageIndex != imageIndex)
        {
            LoadCurrentMenuImage();
        }
    }
    
    /// <summary>
    /// Корутина для анимации новой карточки при возврате из уровня
    /// </summary>
    private IEnumerator AnimateNewCardOnReturn()
    {
        if (menuManager == null || !isInitialized) yield break;
        
        int imageIndex = menuManager.GetCurrentMenuImageIndex();
        int unlockedCount = menuManager.GetUnlockedCardsCount(imageIndex);
        
        if (unlockedCount > 0 && unlockedCount <= menuCards.Count)
        {
            int cardIndex = unlockedCount - 1;
            MenuCard newCard = menuCards[cardIndex];
            
            // Убеждаемся, что карточка закрыта перед анимацией
            if (newCard.IsUnlocked())
            {
                newCard.SetUnlocked(false);
            }
            
            // Небольшая задержка перед анимацией (чтобы игрок увидел закрытую карточку)
            yield return new WaitForSeconds(0.5f);
            
            // Анимируем переворот (используется та же анимация, что и в GameManager)
            newCard.FlipCard(() => {
                Debug.Log("MenuProgressUI: New card flip animation completed after level completion");
                // Обновляем рамки после переворота
                UpdateAllBorders();
            });
        }
        
        shouldAnimateNewCard = false;
    }
    
    void OnEnable()
    {
        // При активации проверяем новую карточку
        if (isInitialized)
        {
            // Небольшая задержка, чтобы убедиться, что все загружено
            StartCoroutine(DelayedCheckForNewCard());
        }
    }
    
    /// <summary>
    /// Корутина для отложенной проверки новой карточки
    /// </summary>
    private IEnumerator DelayedCheckForNewCard()
    {
        // Ждем один кадр, чтобы убедиться, что все компоненты инициализированы
        yield return null;
        
        // Проверяем новую карточку
        CheckAndAnimateNewCard();
    }
    
    /// <summary>
    /// Открывает все карточки текущей картинки и обновляет UI
    /// </summary>
    public void UnlockAllCardsForCurrentImage()
    {
        if (menuManager == null || !isInitialized) return;
        
        int imageIndex = menuManager.GetCurrentMenuImageIndex();
        
        // Открываем все карточки в MenuManager
        menuManager.UnlockAllCardsForImage(imageIndex);
        
        // Обновляем визуальное отображение
        UpdateProgress();
        
        Debug.Log($"MenuProgressUI: Unlocked all cards for image {imageIndex}");
    }
    
    /// <summary>
    /// Открывает все карточки для указанной картинки и обновляет UI (если это текущая картинка)
    /// </summary>
    public void UnlockAllCardsForImage(int imageIndex)
    {
        if (menuManager == null || !isInitialized) return;
        
        // Открываем все карточки в MenuManager
        menuManager.UnlockAllCardsForImage(imageIndex);
        
        // Обновляем визуальное отображение только если это текущая картинка
        int currentImageIndex = menuManager.GetCurrentMenuImageIndex();
        if (imageIndex == currentImageIndex)
        {
            UpdateProgress();
            Debug.Log($"MenuProgressUI: Unlocked all cards for current image {imageIndex}");
        }
        else
        {
            Debug.Log($"MenuProgressUI: Unlocked all cards for image {imageIndex} (not current, UI not updated)");
        }
    }
}

