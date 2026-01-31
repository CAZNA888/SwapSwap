using UnityEngine;

[System.Serializable]
public class LevelImageData
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
            return "LevelImage_" + name;
        }
        
        return "LevelImage_Unknown";
    }
}







