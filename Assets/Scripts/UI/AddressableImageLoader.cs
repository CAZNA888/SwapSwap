using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Компонент для автоматической загрузки спрайтов из Addressables.
/// Поддерживает как UI Image, так и SpriteRenderer.
/// Работает в WebGL и других платформах.
/// </summary>
public class AddressableImageLoader : MonoBehaviour
{
    [Header("Addressable Settings")]
    [Tooltip("Ключ для загрузки спрайта из Addressables. Обязателен для работы")]
    public string addressableKey = "";
    
    [Header("Component Settings")]
    [Tooltip("Автоматически загружать спрайт при старте")]
    public bool loadOnStart = true;
    
    [Tooltip("Автоматически искать Image компонент, если не указан")]
    public bool autoFindImage = true;
    
    [Tooltip("Автоматически искать SpriteRenderer компонент, если Image не найден")]
    public bool autoFindSpriteRenderer = true;
    
    // Компоненты для отображения
    private Image uiImage;
    private SpriteRenderer spriteRenderer;
    
    // Handle для Addressables (для освобождения ресурсов при необходимости)
#if UNITY_ADDRESSABLES
    private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<Sprite>? addressableHandle;
#else
    private object addressableHandle;
#endif
    
    private void Awake()
    {
        // Ищем компоненты
        if (autoFindImage)
        {
            uiImage = GetComponent<Image>();
        }
        
        if (autoFindSpriteRenderer && uiImage == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
    
    private void Start()
    {
        if (loadOnStart)
        {
            LoadSprite();
        }
    }
    
    /// <summary>
    /// Загружает спрайт из Addressables
    /// </summary>
    public void LoadSprite()
    {
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogError($"AddressableImageLoader: addressableKey не указан для '{gameObject.name}'. Загрузка невозможна.");
            return;
        }
        
        // Запускаем асинхронную загрузку
        StartCoroutine(LoadSpriteAsync());
    }
    
    /// <summary>
    /// Асинхронно загружает спрайт из Addressables
    /// </summary>
    private IEnumerator LoadSpriteAsync()
    {
        // Проверяем доступность Addressables
        if (!IsAddressablesAvailable())
        {
            Debug.LogError($"AddressableImageLoader: Addressables не доступны для '{gameObject.name}'. Загрузка невозможна.");
            yield break;
        }
        
#if UNITY_ADDRESSABLES
        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(addressableKey);
        addressableHandle = handle;
        
        yield return handle;
        
        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            SetSprite(handle.Result);
            Debug.Log($"AddressableImageLoader: Спрайт успешно загружен из Addressables с ключом '{addressableKey}' для '{gameObject.name}'");
        }
        else
        {
            Debug.LogError($"AddressableImageLoader: Не удалось загрузить спрайт из Addressables с ключом '{addressableKey}' для '{gameObject.name}'. Загрузка не выполнена.");
            // Очищаем handle при ошибке
            addressableHandle = null;
        }
#else
        Debug.LogError($"AddressableImageLoader: Addressables код не скомпилирован для '{gameObject.name}'. Загрузка невозможна.");
        yield break;
#endif
    }
    
    /// <summary>
    /// Устанавливает спрайт в соответствующий компонент
    /// </summary>
    private void SetSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"AddressableImageLoader: Попытка установить null спрайт для '{gameObject.name}'");
            return;
        }
        
        if (uiImage != null)
        {
            uiImage.sprite = sprite;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
        else
        {
            Debug.LogError($"AddressableImageLoader: Не найден ни Image, ни SpriteRenderer компонент на '{gameObject.name}'");
        }
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
    /// Устанавливает Image компонент вручную
    /// </summary>
    public void SetImage(Image image)
    {
        uiImage = image;
    }
    
    /// <summary>
    /// Устанавливает SpriteRenderer компонент вручную
    /// </summary>
    public void SetSpriteRenderer(SpriteRenderer renderer)
    {
        spriteRenderer = renderer;
    }
    
    /// <summary>
    /// Освобождает ресурсы Addressables (опционально, вызывать при уничтожении объекта)
    /// </summary>
    public void ReleaseAddressable()
    {
#if UNITY_ADDRESSABLES
        if (addressableHandle.HasValue)
        {
            var handle = addressableHandle.Value;
            // Проверяем, что handle валиден и не был уже освобожден
            if (handle.IsValid())
            {
                UnityEngine.AddressableAssets.Addressables.Release(handle);
            }
            addressableHandle = null;
        }
#endif
    }
    
    private void OnDestroy()
    {
        // Освобождаем ресурсы при уничтожении объекта
        ReleaseAddressable();
    }
}

