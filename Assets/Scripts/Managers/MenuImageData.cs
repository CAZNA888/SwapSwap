using UnityEngine;

[System.Serializable]
public class MenuImageData
{
    [Tooltip("Название картинки для отладки")]
    public string name;
    
    [Tooltip("Ключ для загрузки через Addressables (обязательно должен быть заполнен)")]
    public string addressableKey;
    
    /// <summary>
    /// Получает ключ Addressable для этой картинки
    /// </summary>
    public string GetAddressableKey()
    {
        if (!string.IsNullOrEmpty(addressableKey))
        {
            return addressableKey;
        }
        
        if (!string.IsNullOrEmpty(name))
        {
            return "MenuImage_" + name;
        }
        
        Debug.LogWarning("MenuImageData: Both addressableKey and name are empty! Using default key.");
        return "MenuImage_Unknown";
    }
}









