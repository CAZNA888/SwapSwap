using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using PlayerPrefs = RedefineYG.PlayerPrefs;
public class MenuManager : MonoBehaviour
{
    [Header("Image Loading")]
    [Tooltip("Список картинок для меню. Заполняется один раз в инспекторе. Загрузка выполняется только через Addressables")]
    public List<MenuImageData> menuImages = new List<MenuImageData>();
    
    [Header("Settings")]
    [Tooltip("Количество карточек на одну картинку (по умолчанию 25 для сетки 5x5)")]
    public int cardsPerImage = 25;
    
    [Header("UI")]
    [Tooltip("TextMeshProUGUI для отображения номера уровня (только цифры)")]
    public TextMeshProUGUI levelText;
    
    private const string MENU_PROGRESS_KEY = "MenuProgress_";
    private string currentMenuSceneName;
    
    // Singleton pattern для легкого доступа
    private static MenuManager instance;
    public static MenuManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MenuManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MenuManager");
                    instance = go.AddComponent<MenuManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // НЕ используем DontDestroyOnLoad - MenuManager должен уничтожаться при переходе в другую сцену
            // Прогресс сохраняется через PlayerPrefs, поэтому данные не потеряются
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        UpdateLevelDisplay();
    }
    
    void OnDestroy()
    {
        // Сохраняем прогресс перед уничтожением
        if (instance == this)
        {
            SaveMenuProgress();
            Debug.Log("MenuManager: Saving progress before destruction");
        }
    }
    
    /// <summary>
    /// Вычисляет индекс текущей картинки меню на основе уровня
    /// Каждые 25 уровней = новая картинка
    /// </summary>
    public int GetCurrentMenuImageIndex()
    {
        // Используем PlayerPrefs напрямую, чтобы не создавать LevelManager в меню
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        return currentLevel / cardsPerImage;
    }
    
    /// <summary>
    /// Вычисляет индекс текущей карточки (0-24) на основе уровня
    /// </summary>
    public int GetCurrentCardIndex()
    {
        // Используем PlayerPrefs напрямую, чтобы не создавать LevelManager в меню
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        return currentLevel % cardsPerImage;
    }
    
    /// <summary>
    /// Получает количество открытых карточек для указанной картинки
    /// </summary>
    public int GetUnlockedCardsCount(int imageIndex)
    {
        string key = MENU_PROGRESS_KEY + imageIndex;
        return PlayerPrefs.GetInt(key, 0);
    }
    
    /// <summary>
    /// Устанавливает количество открытых карточек для указанной картинки
    /// </summary>
    public void SetUnlockedCardsCount(int imageIndex, int count)
    {
        string key = MENU_PROGRESS_KEY + imageIndex;
        PlayerPrefs.SetInt(key, Mathf.Clamp(count, 0, cardsPerImage));
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Сохраняет прогресс меню в PlayerPrefs
    /// </summary>
    public void SaveMenuProgress()
    {
        // Прогресс уже сохраняется через SetUnlockedCardsCount при каждом обновлении
        // Этот метод просто гарантирует, что все изменения записаны на диск
        PlayerPrefs.Save();
        Debug.Log("MenuManager: Menu progress saved to PlayerPrefs");
    }
    
    /// <summary>
    /// Проверяет, полностью ли открыта указанная картинка
    /// </summary>
    public bool IsImageCompleted(int imageIndex)
    {
        int unlockedCount = GetUnlockedCardsCount(imageIndex);
        return unlockedCount >= cardsPerImage;
    }
    
    /// <summary>
    /// Обновляет отображение номера уровня
    /// </summary>
    public void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
            // Показываем следующий уровень (текущий + 1), отсчет с 1
            levelText.text = (currentLevel + 1).ToString();
        }
    }
    
    /// <summary>
    /// Обновляет прогресс меню на основе текущего уровня
    /// Вызывается при возврате из уровня
    /// </summary>
    public void UpdateProgress()
    {
        int imageIndex = GetCurrentMenuImageIndex();
        int cardIndex = GetCurrentCardIndex();
        
        // Получаем текущий уровень
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        
        // Количество открытых карточек = количество пройденных уровней в текущей картинке
        // Если currentLevel = 0, то ничего не пройдено, unlockedCount = 0
        // Если currentLevel = 1, то пройден 1 уровень, unlockedCount = 1
        int unlockedCount = 0;
        if (currentLevel > 0)
        {
            // Вычисляем количество пройденных уровней в текущей картинке
            int levelsInCurrentImage = currentLevel - (imageIndex * cardsPerImage);
            unlockedCount = Mathf.Max(0, levelsInCurrentImage);
        }
        
        int currentUnlocked = GetUnlockedCardsCount(imageIndex);
        
        // Обновляем только если прогресс увеличился
        if (unlockedCount > currentUnlocked)
        {
            SetUnlockedCardsCount(imageIndex, unlockedCount);
            Debug.Log($"MenuManager: Updated progress for image {imageIndex}: {unlockedCount}/{cardsPerImage} cards unlocked");
        }
        
        // Обновляем отображение уровня
        UpdateLevelDisplay();
    }
    
    /// <summary>
    /// Проверяет, была ли открыта новая карточка после последнего обновления
    /// </summary>
    public bool HasNewCardUnlocked()
    {
        int imageIndex = GetCurrentMenuImageIndex();
        int cardIndex = GetCurrentCardIndex();
        
        // Получаем текущий уровень
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        
        // Вычисляем ожидаемое количество открытых карточек
        // Если currentLevel = 0, то ничего не пройдено, expectedUnlocked = 0
        int expectedUnlocked = 0;
        if (currentLevel > 0)
        {
            // Вычисляем количество пройденных уровней в текущей картинке
            int levelsInCurrentImage = currentLevel - (imageIndex * cardsPerImage);
            expectedUnlocked = Mathf.Max(0, levelsInCurrentImage);
        }
        
        int currentUnlocked = GetUnlockedCardsCount(imageIndex);
        
        return expectedUnlocked > currentUnlocked;
    }
    
    /// <summary>
    /// Проверяет доступность Addressables во время выполнения
    /// </summary>
    private bool IsAddressablesAvailable()
    {
#if UNITY_ADDRESSABLES
        return true;
#else
        // Проверяем через рефлексию, доступен ли тип Addressables
        System.Type addressablesType = System.Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
        return addressablesType != null;
#endif
    }
    
    /// <summary>
    /// Асинхронно загружает картинку меню через Addressables
    /// </summary>
    public IEnumerator LoadMenuImageAsync(int imageIndex, System.Action<Sprite> onComplete)
    {
        if (menuImages == null || menuImages.Count == 0)
        {
            Debug.LogError("MenuManager: menuImages list is empty!");
            onComplete?.Invoke(null);
            yield break;
        }
        
        if (imageIndex < 0 || imageIndex >= menuImages.Count)
        {
            Debug.LogError($"MenuManager: Image index {imageIndex} is out of range! Available images: 0-{menuImages.Count - 1}");
            onComplete?.Invoke(null);
            yield break;
        }
        
        MenuImageData imageData = menuImages[imageIndex];
        
        // Всегда используем Addressables - проверяем наличие во время выполнения
        if (!IsAddressablesAvailable())
        {
            Debug.LogError("MenuManager: Addressables are not installed! Please install Addressables package (Window > Package Manager > Addressables).");
            onComplete?.Invoke(null);
            yield break;
        }
        
#if UNITY_ADDRESSABLES
        string addressableKey = imageData.GetAddressableKey();
        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(addressableKey);
        
        yield return handle;
        
        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            onComplete?.Invoke(handle.Result);
        }
        else
        {
            Debug.LogError($"MenuManager: Failed to load image from Addressables with key '{addressableKey}'. Загрузка не выполнена.");
            onComplete?.Invoke(null);
        }
        
        // Не освобождаем handle здесь, так как спрайт может использоваться
#else
        Debug.LogError("MenuManager: Addressables code is not compiled. Please add 'UNITY_ADDRESSABLES' to Scripting Define Symbols in Player Settings (Edit > Project Settings > Player > Other Settings > Scripting Define Symbols).");
        onComplete?.Invoke(null);
        yield break;
#endif
    }
    
    /// <summary>
    /// Загружает сцену уровня по имени
    /// </summary>
    public void LoadLevelScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("MenuManager: Scene name is null or empty!");
            return;
        }
        
        // Сохраняем имя текущей сцены меню для возможного возврата
        currentMenuSceneName = SceneManager.GetActiveScene().name;
        
        Debug.Log($"MenuManager: Loading level scene: {sceneName}");
        Debug.Log($"MenuManager: Current scene before load: {SceneManager.GetActiveScene().name}");
        Debug.Log($"MenuManager: Scene exists in build: {SceneExistsInBuild(sceneName)}");
        
        // Используем LoadSceneMode.Single для гарантированной загрузки
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        
        Debug.Log($"MenuManager: Scene load command sent for: {sceneName}");
    }
    
    /// <summary>
    /// Проверяет, существует ли сцена в Build Settings
    /// </summary>
    private bool SceneExistsInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Возвращает имя текущей сцены меню
    /// </summary>
    public string GetCurrentMenuSceneName()
    {
        return currentMenuSceneName;
    }
    
    /// <summary>
    /// Устанавливает имя текущей сцены меню
    /// </summary>
    public void SetCurrentMenuSceneName(string sceneName)
    {
        currentMenuSceneName = sceneName;
    }
    
    /// <summary>
    /// Получает список индексов полностью пройденных картинок
    /// </summary>
    public List<int> GetCompletedImageIndices()
    {
        List<int> completed = new List<int>();
        
        if (menuImages == null || menuImages.Count == 0)
        {
            return completed;
        }
        
        for (int i = 0; i < menuImages.Count; i++)
        {
            if (IsImageCompleted(i))
            {
                completed.Add(i);
            }
        }
        
        return completed;
    }
    
    /// <summary>
    /// Получает информацию о текущем прогрессе меню (для отладки)
    /// </summary>
    public string GetMenuProgressInfo()
    {
        int imageIndex = GetCurrentMenuImageIndex();
        int cardIndex = GetCurrentCardIndex();
        int unlockedCount = GetUnlockedCardsCount(imageIndex);
        bool isCompleted = IsImageCompleted(imageIndex);
        
        return $"Menu Progress: Image {imageIndex}, Card {cardIndex}, Unlocked: {unlockedCount}/{cardsPerImage}, Completed: {isCompleted}";
    }
    
    /// <summary>
    /// Открывает все карточки для указанной картинки
    /// </summary>
    public void UnlockAllCardsForImage(int imageIndex)
    {
        if (imageIndex < 0)
        {
            Debug.LogWarning($"MenuManager: Invalid image index: {imageIndex}");
            return;
        }
        
        SetUnlockedCardsCount(imageIndex, cardsPerImage);
        Debug.Log($"MenuManager: Unlocked all {cardsPerImage} cards for image {imageIndex}");
    }
    
    /// <summary>
    /// Открывает все карточки для текущей картинки
    /// </summary>
    public void UnlockAllCardsForCurrentImage()
    {
        int imageIndex = GetCurrentMenuImageIndex();
        UnlockAllCardsForImage(imageIndex);
    }
    
    /// <summary>
    /// Получает addressable key для указанного индекса картинки
    /// </summary>
    public string GetAddressableKeyForImageIndex(int imageIndex)
    {
        if (menuImages == null || imageIndex < 0 || imageIndex >= menuImages.Count)
        {
            return null;
        }
        
        return menuImages[imageIndex].GetAddressableKey();
    }
    
    /// <summary>
    /// Проверяет, используется ли картинка с указанным индексом в текущем прогрессе
    /// Картинка считается используемой, если она соответствует текущей или будущим картинкам меню
    /// </summary>
    public bool IsImageIndexInUse(int imageIndex)
    {
        if (menuImages == null || imageIndex < 0 || imageIndex >= menuImages.Count)
        {
            return false;
        }
        
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        int currentImageIndex = GetCurrentMenuImageIndex();
        
        // Картинка используется, если она соответствует текущей картинке или будущим
        // Также проверяем, не полностью ли пройдена картинка (если пройдена, она может быть неактуальна)
        if (imageIndex >= currentImageIndex)
        {
            // Это текущая или будущая картинка - она используется
            return true;
        }
        
        // Для прошлых картинок проверяем, не полностью ли они пройдены
        // Если картинка полностью пройдена, она все еще может использоваться в коллекции
        // Но для целей выгрузки считаем, что полностью пройденные картинки можно выгружать
        // если они не являются текущей
        return false;
    }
}

