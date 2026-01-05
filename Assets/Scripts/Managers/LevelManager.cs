using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Image Loading")]
    [Tooltip("Использовать Addressables для загрузки картинок. Если false - использовать спрайты напрямую из Unity")]
    public bool useAddressables = false;
    
    [Tooltip("Список картинок для уровней. Заполняется один раз в инспекторе")]
    public List<LevelImageData> levelImages = new List<LevelImageData>();
    
    [Header("Grid Size Settings")]
    [Tooltip("Стартовая размерность сетки")]
    public int startGridSize = 3;
    
    [Tooltip("Периодичность увеличения размерности (k). Каждые k уровней размерность увеличивается на 1")]
    public int gridSizeIncreasePeriod = 5;
    
    [Tooltip("Максимальная размерность сетки (k_max)")]
    public int maxGridSize = 8;
    
    [Header("Difficult Level Settings")]
    [Tooltip("Периодичность сложных уровней (k_d). Каждые k_d уровней выпадает сложный уровень")]
    public int difficultLevelPeriod = 3;
    
    [Tooltip("GameObject для показа в начале сложного уровня")]
    public GameObject difficultLevelUIObject;
    
    [Tooltip("Длительность показа GameObject в сложном уровне (l секунд)")]
    public float difficultLevelDisplayDuration = 3f;
    
    private int currentLevel;
    private const string LEVEL_KEY = "CurrentLevel";
    
    // Singleton pattern для легкого доступа
    private static LevelManager instance;
    public static LevelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<LevelManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("LevelManager");
                    instance = go.AddComponent<LevelManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        Debug.Log($"=== LevelManager Awake ===");
        Debug.Log($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"levelImages count: {(levelImages != null ? levelImages.Count : 0)}");
        
        if (instance == null)
        {
            instance = this;
            // НЕ используем DontDestroyOnLoad - LevelManager должен уничтожаться при переходе в другую сцену
            // Прогресс уровня сохраняется через PlayerPrefs, поэтому данные не потеряются
            
            // Проверяем, что список levelImages заполнен
            if (levelImages == null || levelImages.Count == 0)
            {
                Debug.LogError("LevelManager: levelImages list is empty! Please fill it in Inspector!");
            }
            else
            {
                Debug.Log($"LevelManager: Instance created with {levelImages.Count} level images");
            }
            
            LoadLevel();
        }
        else if (instance != this)
        {
            Debug.LogWarning($"LevelManager: Duplicate found in scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}. Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"LevelManager: Already instance exists. levelImages count: {instance.levelImages.Count}");
        }
    }
    
    void OnDestroy()
    {
        // Сохраняем прогресс уровня перед уничтожением
        if (instance == this)
        {
            SaveLevel();
            Debug.Log("LevelManager: Saving level progress before destruction");
        }
    }
    
    /// <summary>
    /// Загружает текущий уровень из PlayerPrefs
    /// </summary>
    public void LoadLevel()
    {
        currentLevel = PlayerPrefs.GetInt(LEVEL_KEY, 0);
    }
    
    /// <summary>
    /// Сохраняет текущий уровень в PlayerPrefs
    /// </summary>
    public void SaveLevel()
    {
        PlayerPrefs.SetInt(LEVEL_KEY, currentLevel);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Возвращает текущий номер уровня
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    /// <summary>
    /// Увеличивает уровень после прохождения и сохраняет в PlayerPrefs
    /// </summary>
    public void IncrementLevel()
    {
        currentLevel++;
        PlayerPrefs.SetInt(LEVEL_KEY, currentLevel);
        PlayerPrefs.Save();
        Debug.Log($"Level incremented to {currentLevel}");
    }
    
    /// <summary>
    /// Устанавливает уровень напрямую (для тестирования)
    /// </summary>
    public void SetLevel(int level)
    {
        currentLevel = level;
        PlayerPrefs.SetInt(LEVEL_KEY, currentLevel);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Рассчитывает размерность сетки для текущего уровня
    /// </summary>
    public int CalculateGridSize()
    {
        return CalculateGridSize(currentLevel);
    }
    
    /// <summary>
    /// Рассчитывает размерность сетки для указанного уровня
    /// </summary>
    public int CalculateGridSize(int level)
    {
        int baseSize;
        int increase;
        int gridSize;
        int finalSize;
        
        // Специальная логика для startGridSize = 2
        if (startGridSize == 2)
        {
            // Первые 3 уровня (0, 1, 2) имеют размерность 2
            if (level < 3)
            {
                finalSize = 2;
                Debug.Log($"LevelManager.CalculateGridSize: level={level}, special case for startGridSize=2, finalSize={finalSize}");
                return finalSize;
            }
            
            // Начиная с уровня 3, размерность становится 3, и дальше применяется обычная логика
            // Но нужно скорректировать расчет, так как мы уже "потратили" 3 уровня на размерность 2
            // Уровень 3 должен быть размерностью 3, поэтому считаем как будто startGridSize = 3
            // и level начинается с 3, но для расчета используем level - 3
            int adjustedLevel = level - 3;
            baseSize = 3; // Начинаем с 3 после первых 3 уровней
            increase = adjustedLevel / gridSizeIncreasePeriod;
            gridSize = baseSize + increase;
            
            if (IsDifficultLevel(level))
            {
                gridSize += 1; // Дополнительное увеличение для сложного уровня
            }
            
            finalSize = Mathf.Min(gridSize, maxGridSize);
            
            Debug.Log($"LevelManager.CalculateGridSize: level={level}, startGridSize=2 special case, adjustedLevel={adjustedLevel}, baseSize={baseSize}, increase={increase}, gridSize={gridSize}, isDifficult={IsDifficultLevel(level)}, finalSize={finalSize} (max={maxGridSize})");
            
            return finalSize;
        }
        
        // Обычная логика для остальных случаев
        baseSize = startGridSize;
        increase = level / gridSizeIncreasePeriod;
        gridSize = baseSize + increase;
        
        if (IsDifficultLevel(level))
        {
            gridSize += 1; // Дополнительное увеличение для сложного уровня
        }
        
        finalSize = Mathf.Min(gridSize, maxGridSize);
        
        // Логирование для диагностики
        Debug.Log($"LevelManager.CalculateGridSize: level={level}, baseSize={baseSize}, increase={increase} (level/{gridSizeIncreasePeriod}), gridSize={gridSize}, isDifficult={IsDifficultLevel(level)}, finalSize={finalSize} (max={maxGridSize})");
        
        return finalSize;
    }
    
    /// <summary>
    /// Проверяет, является ли текущий уровень сложным
    /// </summary>
    public bool IsDifficultLevel()
    {
        return IsDifficultLevel(currentLevel);
    }
    
    /// <summary>
    /// Проверяет, является ли указанный уровень сложным
    /// </summary>
    public bool IsDifficultLevel(int level)
    {
        if (difficultLevelPeriod <= 0) return false;
        return level > 0 && level % difficultLevelPeriod == 0;
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
    /// Загружает картинку для текущего уровня
    /// </summary>
    public Sprite GetLevelImage()
    {
        if (levelImages == null || levelImages.Count == 0)
        {
            Debug.LogError("LevelManager: levelImages list is empty!");
            return null;
        }
        
        int imageIndex = currentLevel % levelImages.Count;
        LevelImageData imageData = levelImages[imageIndex];
        
        if (!useAddressables)
        {
            // Unity режим - используем спрайт напрямую
            if (imageData.sprite == null)
            {
                Debug.LogError($"LevelManager: Sprite is null for level {currentLevel}, image index {imageIndex}");
                return null;
            }
            return imageData.sprite;
        }
        else
        {
            // Addressables режим - проверяем доступность
            if (!IsAddressablesAvailable())
            {
                Debug.LogError("LevelManager: Addressables are not available! Set useAddressables to false or install Addressables package.");
                return null;
            }
            
            // Addressables режим - нужно загрузить асинхронно
            // Для синхронного доступа возвращаем null и предупреждение
            Debug.LogWarning("LevelManager: Addressables mode requires async loading. Use LoadLevelImageAsync() instead.");
            return null;
        }
    }
    
    /// <summary>
    /// Асинхронно загружает картинку для текущего уровня через Addressables
    /// </summary>
    public IEnumerator LoadLevelImageAsync(System.Action<Sprite> onComplete)
    {
        if (levelImages == null || levelImages.Count == 0)
        {
            Debug.LogError("LevelManager: levelImages list is empty!");
            onComplete?.Invoke(null);
            yield break;
        }
        
        int imageIndex = currentLevel % levelImages.Count;
        LevelImageData imageData = levelImages[imageIndex];
        
        if (!useAddressables)
        {
            // Unity режим - возвращаем спрайт сразу
            onComplete?.Invoke(imageData.sprite);
            yield break;
        }
        
        // Addressables режим - проверяем наличие во время выполнения
        if (!IsAddressablesAvailable())
        {
            Debug.LogError("LevelManager: Addressables are not installed! Please install Addressables package (Window > Package Manager > Addressables) or set useAddressables to false.");
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
            Debug.LogError($"LevelManager: Failed to load image from Addressables with key '{addressableKey}'");
            onComplete?.Invoke(null);
        }
        
        // Не освобождаем handle здесь, так как спрайт может использоваться
        // Можно добавить систему управления ресурсами при необходимости
#else
        // Fallback для случая, когда символ не определен
        // Проверка IsAddressablesAvailable() уже выполнилась выше, но если мы здесь,
        // значит Addressables недоступны или символ не определен
        Debug.LogError("LevelManager: Addressables code is not compiled. Please add 'UNITY_ADDRESSABLES' to Scripting Define Symbols in Player Settings (Edit > Project Settings > Player > Other Settings > Scripting Define Symbols) or set useAddressables to false.");
        onComplete?.Invoke(null);
        yield break;
#endif
    }
    
    /// <summary>
    /// Показывает UI для сложного уровня на заданное время
    /// </summary>
    public IEnumerator ShowDifficultLevelUI()
    {
        if (difficultLevelUIObject != null && IsDifficultLevel())
        {
            difficultLevelUIObject.SetActive(true);
            yield return new WaitForSeconds(difficultLevelDisplayDuration);
            difficultLevelUIObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Получает информацию о текущем уровне (для отладки)
    /// </summary>
    public string GetLevelInfo()
    {
        int gridSize = CalculateGridSize();
        bool isDifficult = IsDifficultLevel();
        return $"Level: {currentLevel}, Grid Size: {gridSize}x{gridSize}, Difficult: {isDifficult}";
    }
}

