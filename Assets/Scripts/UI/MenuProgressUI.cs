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
    
    [Tooltip("Длительность задержки между анимациями карточек")]
    public float cardAnimationDelay = 0.05f;
    
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

