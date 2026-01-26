using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayerPrefs = RedefineYG.PlayerPrefs;
public class LevelManager : MonoBehaviour
{
    [Header("Image Loading")]
    [Tooltip("Список картинок для уровней. Заполняется один раз в инспекторе. Загрузка выполняется только через Addressables")]
    public List<LevelImageData> levelImages = new List<LevelImageData>();

    [Header("Grid Size Settings")]
    [Tooltip("Стартовая размерность сетки")]
    public int startGridSize = 3;

    [Tooltip("Периодичность увеличения размерности (k). Каждые k уровней размерность увеличивается на 1")]
    public int gridSizeIncreasePeriod = 5;

    [Tooltip("Периодичность увеличения размерности с 3 на 4. Используется только для первого перехода с 3 на 4")]
    public int gridSizeIncreasePeriod3To4 = 5;

    [Tooltip("Периодичность увеличения размерности с 4 на 5. Используется только для первого перехода с 4 на 5")]
    public int gridSizeIncreasePeriod4To5 = 5;

    [Tooltip("Максимальная размерность сетки (k_max)")]
    public int maxGridSize = 8;

    [Header("Difficult Level Settings")]
    [Tooltip("Периодичность сложных уровней (k_d). Каждые k_d уровней выпадает сложный уровень")]
    public int difficultLevelPeriod = 3;

    [Tooltip("GameObject для показа в начале сложного уровня")]
    public GameObject difficultLevelUIObject;

    [Tooltip("Длительность показа GameObject в сложном уровне (l секунд)")]
    public float difficultLevelDisplayDuration = 3f;

    [Header("Front Sprite Settings")]
    [Tooltip("Мультипликатор размера front sprite (переднего спрайта карточки). 1.0 = обычный размер, <1.0 = уменьшение, >1.0 = увеличение")]
    public float frontSpriteSizeMultiplier = 1.0f;

    [Header("Back Sprite Settings")]
    [Tooltip("Мультипликатор размера back sprite (обратного спрайта карточки). 1.0 = обычный размер, <1.0 = уменьшение, >1.0 = увеличение")]
    public float backSpriteSizeMultiplier = 1.0f;

    [Header("Card Prefab Settings")]
    [Tooltip("Мультипликатор размера всего префаба карточки. 1.0 = обычный размер, <1.0 = уменьшение, >1.0 = увеличение. Применяется ко всему префабу (включая рамки)")]
    public float cardPrefabSizeMultiplier = 1.0f;

    private int currentLevel;
    private const string LEVEL_KEY = "CurrentLevel";

#if UNITY_ADDRESSABLES
    // Система управления ресурсами Addressables
    private Dictionary<string, UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<Sprite>> loadedHandles = new Dictionary<string, UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<Sprite>>();
    private Dictionary<string, int> referenceCounts = new Dictionary<string, int>();
#endif

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
            
            // Выгружаем все загруженные ресурсы при уничтожении
#if UNITY_ADDRESSABLES
            foreach (var kvp in loadedHandles)
            {
                if (kvp.Value.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(kvp.Value);
                }
            }
            loadedHandles.Clear();
            referenceCounts.Clear();
            Debug.Log("LevelManager: Released all loaded Addressables resources");
#endif
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
        
        // Выгружаем неиспользуемые картинки пройденных уровней
        UnloadUnusedImages();
        
        // Предзагружаем картинки на следующие уровни
        PreloadNextLevelImages();
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
    /// Рассчитывает размерность сетки для startGridSize=3 с отдельными периодами для переходов 3→4 и 4→5
    /// </summary>
    private int CalculateGridSizeForStartSize3(int adjustedLevel, int originalLevel)
    {
        int baseSize;
        int increase;
        int gridSize;
        int finalSize;

        // Логика с отдельными периодами для переходов 3→4 и 4→5
        if (adjustedLevel < gridSizeIncreasePeriod3To4)
        {
            // Уровни 0 до gridSizeIncreasePeriod3To4 - 1: размер 3
            baseSize = 3;
            increase = 0;
            gridSize = baseSize;
        }
        else if (adjustedLevel < gridSizeIncreasePeriod3To4 + gridSizeIncreasePeriod4To5)
        {
            // Уровни gridSizeIncreasePeriod3To4 до gridSizeIncreasePeriod3To4 + gridSizeIncreasePeriod4To5 - 1: размер 4
            baseSize = 4;
            increase = 0;
            gridSize = baseSize;
        }
        else
        {
            // Уровни начиная с gridSizeIncreasePeriod3To4 + gridSizeIncreasePeriod4To5: размер 5 и далее
            // Используем стандартный gridSizeIncreasePeriod для дальнейших переходов
            baseSize = 5;
            int levelAfterTransitions = adjustedLevel - (gridSizeIncreasePeriod3To4 + gridSizeIncreasePeriod4To5);
            increase = levelAfterTransitions / gridSizeIncreasePeriod;
            gridSize = baseSize + increase;
        }

        // Учитываем сложный уровень
        if (IsDifficultLevel(originalLevel))
        {
            gridSize += 1; // Дополнительное увеличение для сложного уровня
        }

        finalSize = Mathf.Min(gridSize, maxGridSize);

        // Логирование для диагностики
        string periodInfo = adjustedLevel < gridSizeIncreasePeriod3To4 
            ? $"period 3→4 (level < {gridSizeIncreasePeriod3To4})"
            : adjustedLevel < gridSizeIncreasePeriod3To4 + gridSizeIncreasePeriod4To5
            ? $"period 4→5 (level < {gridSizeIncreasePeriod3To4 + gridSizeIncreasePeriod4To5})"
            : $"standard period (level >= {gridSizeIncreasePeriod3To4 + gridSizeIncreasePeriod4To5})";
        
        Debug.Log($"LevelManager.CalculateGridSize: level={originalLevel}, adjustedLevel={adjustedLevel}, {periodInfo}, baseSize={baseSize}, increase={increase}, gridSize={gridSize}, isDifficult={IsDifficultLevel(originalLevel)}, finalSize={finalSize} (max={maxGridSize})");

        return finalSize;
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

            // Начиная с уровня 3, размерность становится 3, и дальше применяется логика для startGridSize=3
            // Но нужно скорректировать расчет, так как мы уже "потратили" 3 уровня на размерность 2
            int adjustedLevel = level - 3;
            // Используем логику для startGridSize=3 с adjustedLevel
            return CalculateGridSizeForStartSize3(adjustedLevel, level);
        }

        // Специальная логика для startGridSize = 3 с отдельными периодами для переходов 3→4 и 4→5
        if (startGridSize == 3)
        {
            return CalculateGridSizeForStartSize3(level, level);
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
        // Addressables режим - нужно загрузить асинхронно
        // Для синхронного доступа возвращаем null и предупреждение
        Debug.LogWarning("LevelManager: Addressables mode requires async loading. Use LoadLevelImageAsync() instead.");
        return null;
    }

    /// <summary>
    /// Асинхронно загружает картинку для текущего уровня через Addressables
    /// </summary>
    public IEnumerator LoadLevelImageAsync(System.Action<Sprite> onComplete)
    {
        yield return StartCoroutine(LoadLevelImageAsync(currentLevel, onComplete));
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

    /// <summary>
    /// Возвращает мультипликатор размера front sprite для текущего уровня
    /// </summary>
    public float GetFrontSpriteMultiplier()
    {
        return frontSpriteSizeMultiplier;
    }

    /// <summary>
    /// Возвращает мультипликатор размера back sprite для текущего уровня
    /// </summary>
    public float GetBackSpriteMultiplier()
    {
        return backSpriteSizeMultiplier;
    }

    /// <summary>
    /// Возвращает мультипликатор размера всего префаба карточки для текущего уровня
    /// </summary>
    public float GetCardPrefabMultiplier()
    {
        return cardPrefabSizeMultiplier;
    }

    /// <summary>
    /// Вычисляет индекс картинки для указанного уровня
    /// </summary>
    public int GetImageIndexForLevel(int level)
    {
        if (levelImages == null || levelImages.Count == 0)
        {
            return -1;
        }
        return level % levelImages.Count;
    }

    /// <summary>
    /// Получает addressable key для указанного уровня
    /// </summary>
    private string GetAddressableKeyForLevel(int level)
    {
        int imageIndex = GetImageIndexForLevel(level);
        if (imageIndex < 0 || imageIndex >= levelImages.Count)
        {
            return null;
        }
        return levelImages[imageIndex].GetAddressableKey();
    }

    /// <summary>
    /// Вычисляет следующий уровень, где будет использоваться картинка с указанным индексом
    /// </summary>
    private int GetNextUsageLevel(int imageIndex)
    {
        if (levelImages == null || levelImages.Count == 0)
        {
            return int.MaxValue;
        }

        // Картинка используется циклически: imageIndex, imageIndex + levelImages.Count, imageIndex + 2*levelImages.Count, ...
        // Находим следующий уровень после currentLevel, где будет использоваться эта картинка
        int cycleSize = levelImages.Count;
        int remainder = currentLevel % cycleSize;
        
        // Если текущий уровень уже использует эту картинку, следующий будет через cycleSize уровней
        if (remainder == imageIndex)
        {
            return currentLevel + cycleSize;
        }
        
        // Вычисляем уровень в текущем цикле для этой картинки
        int currentCycle = currentLevel / cycleSize;
        int levelInCurrentCycle = (currentCycle * cycleSize) + imageIndex;
        
        // Если уровень в текущем цикле больше currentLevel, то это следующий уровень использования
        if (levelInCurrentCycle > currentLevel)
        {
            return levelInCurrentCycle;
        }
        
        // Иначе картинка будет использоваться в следующем цикле
        return ((currentCycle + 1) * cycleSize) + imageIndex;
    }

    /// <summary>
    /// Проверяет, используется ли картинка в MenuManager
    /// </summary>
    private bool IsImageUsedInMenuManager(string addressableKey)
    {
        MenuManager menuManager = MenuManager.Instance;
        if (menuManager == null)
        {
            return false;
        }

        // Проверяем все картинки в MenuManager
        if (menuManager.menuImages == null)
        {
            return false;
        }

        for (int i = 0; i < menuManager.menuImages.Count; i++)
        {
            string menuKey = menuManager.menuImages[i].GetAddressableKey();
            if (menuKey == addressableKey)
            {
                // Проверяем, используется ли эта картинка в текущем прогрессе
                if (menuManager.IsImageIndexInUse(i))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Асинхронно загружает картинку для указанного уровня через Addressables
    /// </summary>
    public IEnumerator LoadLevelImageAsync(int level, System.Action<Sprite> onComplete)
    {
        if (levelImages == null || levelImages.Count == 0)
        {
            Debug.LogError("LevelManager: levelImages list is empty!");
            onComplete?.Invoke(null);
            yield break;
        }

        int imageIndex = GetImageIndexForLevel(level);
        if (imageIndex < 0 || imageIndex >= levelImages.Count)
        {
            Debug.LogError($"LevelManager: Invalid image index {imageIndex} for level {level}");
            onComplete?.Invoke(null);
            yield break;
        }

        LevelImageData imageData = levelImages[imageIndex];
        string addressableKey = imageData.GetAddressableKey();

        // Проверяем, не загружена ли уже картинка
#if UNITY_ADDRESSABLES
        if (loadedHandles.ContainsKey(addressableKey))
        {
            var existingHandle = loadedHandles[addressableKey];
            if (existingHandle.IsValid() && existingHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                // Увеличиваем счетчик ссылок
                if (referenceCounts.ContainsKey(addressableKey))
                {
                    referenceCounts[addressableKey]++;
                }
                else
                {
                    referenceCounts[addressableKey] = 1;
                }
                onComplete?.Invoke(existingHandle.Result);
                Debug.Log($"LevelManager: Image '{addressableKey}' already loaded, using cached version");
                yield break;
            }
        }
#endif

        // Всегда используем Addressables - проверяем наличие во время выполнения
        if (!IsAddressablesAvailable())
        {
            Debug.LogError("LevelManager: Addressables are not installed! Please install Addressables package (Window > Package Manager > Addressables).");
            onComplete?.Invoke(null);
            yield break;
        }

#if UNITY_ADDRESSABLES
        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(addressableKey);

        yield return handle;

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            // Сохраняем handle и увеличиваем счетчик ссылок
            loadedHandles[addressableKey] = handle;
            if (referenceCounts.ContainsKey(addressableKey))
            {
                referenceCounts[addressableKey]++;
            }
            else
            {
                referenceCounts[addressableKey] = 1;
            }
            onComplete?.Invoke(handle.Result);
            Debug.Log($"LevelManager: Image '{addressableKey}' loaded successfully for level {level}");
        }
        else
        {
            Debug.LogError($"LevelManager: Failed to load image from Addressables with key '{addressableKey}' for level {level}.");
            onComplete?.Invoke(null);
        }
#else
        Debug.LogError("LevelManager: Addressables code is not compiled. Please add 'UNITY_ADDRESSABLES' to Scripting Define Symbols in Player Settings (Edit > Project Settings > Player > Other Settings > Scripting Define Symbols).");
        onComplete?.Invoke(null);
        yield break;
#endif
    }

    /// <summary>
    /// Предзагружает картинки на 1-2 уровня вперед
    /// </summary>
    public void PreloadNextLevelImages()
    {
        StartCoroutine(PreloadNextLevelImagesCoroutine());
    }

    private IEnumerator PreloadNextLevelImagesCoroutine()
    {
        if (levelImages == null || levelImages.Count == 0)
        {
            yield break;
        }

        // Загружаем картинку для следующего уровня (обязательно)
        int nextLevel = currentLevel + 1;
        string nextLevelKey = GetAddressableKeyForLevel(nextLevel);
        
        if (!string.IsNullOrEmpty(nextLevelKey))
        {
#if UNITY_ADDRESSABLES
            // Проверяем, не загружена ли уже
            if (!loadedHandles.ContainsKey(nextLevelKey))
            {
                Debug.Log($"LevelManager: Preloading image for level {nextLevel}");
                yield return StartCoroutine(LoadLevelImageAsync(nextLevel, (sprite) => {
                    if (sprite != null)
                    {
                        Debug.Log($"LevelManager: Successfully preloaded image for level {nextLevel}");
                    }
                }));
            }
            else
            {
                Debug.Log($"LevelManager: Image for level {nextLevel} already loaded");
            }
#endif
        }

        // Если успеваем, загружаем картинку для уровня +2 (опционально)
        int nextNextLevel = currentLevel + 2;
        string nextNextLevelKey = GetAddressableKeyForLevel(nextNextLevel);
        
        if (!string.IsNullOrEmpty(nextNextLevelKey) && nextNextLevelKey != nextLevelKey)
        {
#if UNITY_ADDRESSABLES
            // Проверяем, не загружена ли уже
            if (!loadedHandles.ContainsKey(nextNextLevelKey))
            {
                Debug.Log($"LevelManager: Preloading image for level {nextNextLevel}");
                yield return StartCoroutine(LoadLevelImageAsync(nextNextLevel, (sprite) => {
                    if (sprite != null)
                    {
                        Debug.Log($"LevelManager: Successfully preloaded image for level {nextNextLevel}");
                    }
                }));
            }
            else
            {
                Debug.Log($"LevelManager: Image for level {nextNextLevel} already loaded");
            }
#endif
        }
    }

    /// <summary>
    /// Выгружает картинки пройденных уровней, если они больше не будут использоваться
    /// </summary>
    public void UnloadUnusedImages()
    {
        StartCoroutine(UnloadUnusedImagesCoroutine());
    }

    private IEnumerator UnloadUnusedImagesCoroutine()
    {
#if UNITY_ADDRESSABLES
        if (levelImages == null || levelImages.Count == 0)
        {
            yield break;
        }

        List<string> keysToUnload = new List<string>();

        // Проверяем все пройденные уровни (от 0 до currentLevel - 1)
        for (int level = 0; level < currentLevel; level++)
        {
            int imageIndex = GetImageIndexForLevel(level);
            if (imageIndex < 0 || imageIndex >= levelImages.Count)
            {
                continue;
            }

            string addressableKey = levelImages[imageIndex].GetAddressableKey();
            if (string.IsNullOrEmpty(addressableKey))
            {
                continue;
            }

            // Проверяем, будет ли картинка использоваться в следующих уровнях
            int nextUsageLevel = GetNextUsageLevel(imageIndex);
            bool willBeUsedInLevelManager = nextUsageLevel <= currentLevel + 2; // Оставляем запас на предзагруженные уровни

            // Проверяем использование в MenuManager
            bool isUsedInMenuManager = IsImageUsedInMenuManager(addressableKey);

            // Если картинка не будет использоваться ни в LevelManager, ни в MenuManager
            if (!willBeUsedInLevelManager && !isUsedInMenuManager)
            {
                if (loadedHandles.ContainsKey(addressableKey))
                {
                    keysToUnload.Add(addressableKey);
                }
            }
        }

        // Выгружаем найденные картинки
        foreach (string key in keysToUnload)
        {
            if (loadedHandles.ContainsKey(key))
            {
                var handle = loadedHandles[key];
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                    Debug.Log($"LevelManager: Unloaded unused image '{key}'");
                }
                loadedHandles.Remove(key);
                referenceCounts.Remove(key);
            }
        }

        if (keysToUnload.Count > 0)
        {
            Debug.Log($"LevelManager: Unloaded {keysToUnload.Count} unused images");
        }
#else
        yield break;
#endif
    }
}

