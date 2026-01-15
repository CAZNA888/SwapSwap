using UnityEngine;

[System.Serializable]
public class LevelImageData
{
    [Tooltip("Название картинки для отладки")]
    public string name;
    
    [Tooltip("Спрайт для использования в Unity режиме (когда useAddressables = false)")]
    public Sprite sprite;
    
    [Tooltip("Ключ для загрузки через Addressables (автогенерируется из имени спрайта, если пусто)")]
    public string addressableKey;
    
    /// <summary>
    /// Получает ключ Addressable для этой картинки
    /// Автогенерируется из имени спрайта, если addressableKey пуст
    /// </summary>
    public string GetAddressableKey()
    {
        if (!string.IsNullOrEmpty(addressableKey))
        {
            return addressableKey;
        }
        
        if (sprite != null && !string.IsNullOrEmpty(sprite.name))
        {
            return "LevelImage_" + sprite.name;
        }
        
        if (!string.IsNullOrEmpty(name))
        {
            return "LevelImage_" + name;
        }
        
        return "LevelImage_Unknown";
    }
}







