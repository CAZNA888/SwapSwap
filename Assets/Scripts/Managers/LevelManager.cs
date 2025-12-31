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
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLevel();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
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
        int baseSize = startGridSize;
        int increase = level / gridSizeIncreasePeriod;
        int gridSize = baseSize + increase;
        
        if (IsDifficultLevel(level))
        {
            gridSize += 1; // Дополнительное увеличение для сложного уровня
        }
        
        return Mathf.Min(gridSize, maxGridSize);
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

