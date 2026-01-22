using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using PlayerPrefs = RedefineYG.PlayerPrefs;
public class MenuCollectionUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Панель коллекции (GameObject для показа/скрытия)")]
    public GameObject collectionPanel;
    
    [Tooltip("Родительский объект для сетки пройденных картинок")]
    public Transform collectionGridParent;
    
    [Tooltip("Префаб для отображения пройденной картинки")]
    public GameObject completedImagePrefab;
    
    [Tooltip("Кнопка закрытия панели коллекции")]
    public Button closeButton;
    
    [Tooltip("GameObject, который показывается когда в коллекции есть хотя бы одна карта")]
    public GameObject collectionIndicator;
    
    [Header("Settings")]
    [Tooltip("Количество колонок в сетке коллекции")]
    public int gridColumns = 3;
    
    private MenuManager menuManager;
    private List<GameObject> collectionItems = new List<GameObject>();
    private bool isInitialized = false;
    
    void OnEnable()
    {
        // Инициализируем menuManager, если еще не инициализирован
        if (menuManager == null)
        {
            menuManager = MenuManager.Instance;
        }
        
        // Проверяем коллекцию и обновляем состояние индикатора
        if (menuManager != null)
        {
            UpdateCollectionIndicator();
        }
    }
    
    void Start()
    {
        menuManager = MenuManager.Instance;
        if (menuManager == null)
        {
            Debug.LogError("MenuCollectionUI: MenuManager not found!");
            return;
        }
        
        // Настраиваем GridLayoutGroup, если он есть
        if (collectionGridParent != null)
        {
            GridLayoutGroup gridLayout = collectionGridParent.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = gridColumns;
            }
        }
        
        // Настраиваем кнопку закрытия
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideCollection);
        }
        
        // Скрываем панель по умолчанию
        if (collectionPanel != null)
        {
            collectionPanel.SetActive(false);
        }
        
        // Проверяем коллекцию и устанавливаем начальное состояние индикатора
        UpdateCollectionIndicator();
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Показывает панель коллекции
    /// </summary>
    public void ShowCollection()
    {
        if (collectionPanel != null)
        {
            collectionPanel.SetActive(true);
            UpdateCollection();
        }
    }
    
    /// <summary>
    /// Скрывает панель коллекции
    /// </summary>
    public void HideCollection()
    {
        if (collectionPanel != null)
        {
            collectionPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Обновляет состояние индикатора коллекции на основе наличия карт
    /// </summary>
    private void UpdateCollectionIndicator()
    {
        if (collectionIndicator == null || menuManager == null) return;
        
        List<int> completedIndices = menuManager.GetCompletedImageIndices();
        bool hasCards = completedIndices.Count >= 1;
        
        collectionIndicator.SetActive(hasCards);
    }
    
    /// <summary>
    /// Обновляет список пройденных картинок
    /// </summary>
    public void UpdateCollection()
    {
        if (menuManager == null || collectionGridParent == null) return;
        
        // Очищаем старые элементы
        ClearCollection();
        
        // Получаем список пройденных картинок
        List<int> completedIndices = menuManager.GetCompletedImageIndices();
        
        // Обновляем состояние индикатора коллекции
        UpdateCollectionIndicator();
        
        if (completedIndices.Count == 0)
        {
            Debug.Log("MenuCollectionUI: No completed images to display");
            return;
        }
        
        // Отображаем пройденные картинки
        DisplayCompletedImages(completedIndices);
    }
    
    /// <summary>
    /// Отображает пройденные картинки в коллекции
    /// </summary>
    public void DisplayCompletedImages(List<int> imageIndices)
    {
        if (collectionGridParent == null)
        {
            Debug.LogError("MenuCollectionUI: collectionGridParent is not set!");
            return;
        }
        
        StartCoroutine(LoadAndDisplayImages(imageIndices));
    }
    
    /// <summary>
    /// Корутина для загрузки и отображения картинок
    /// </summary>
    private IEnumerator LoadAndDisplayImages(List<int> imageIndices)
    {
        foreach (int imageIndex in imageIndices)
        {
            GameObject itemObj;
            
            if (completedImagePrefab != null)
            {
                itemObj = Instantiate(completedImagePrefab, collectionGridParent);
            }
            else
            {
                // Создаем элемент программно, если нет префаба
                itemObj = new GameObject($"CollectionItem_{imageIndex}");
                itemObj.transform.SetParent(collectionGridParent);
                
                RectTransform rectTransform = itemObj.AddComponent<RectTransform>();
                rectTransform.localScale = Vector3.one;
                
                Image image = itemObj.AddComponent<Image>();
                image.preserveAspect = true;
            }
            
            collectionItems.Add(itemObj);
            
            // Загружаем картинку
            Sprite imageSprite = null;
            yield return StartCoroutine(menuManager.LoadMenuImageAsync(imageIndex, (sprite) => {
                imageSprite = sprite;
            }));
            
            if (imageSprite != null)
            {
                // Устанавливаем спрайт
                Image imageComponent = itemObj.GetComponent<Image>();
                if (imageComponent == null)
                {
                    imageComponent = itemObj.AddComponent<Image>();
                }
                imageComponent.sprite = imageSprite;
                imageComponent.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning($"MenuCollectionUI: Failed to load image at index {imageIndex}");
            }
        }
        
        Debug.Log($"MenuCollectionUI: Displayed {collectionItems.Count} completed images");
    }
    
    /// <summary>
    /// Очищает коллекцию
    /// </summary>
    private void ClearCollection()
    {
        foreach (GameObject item in collectionItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        collectionItems.Clear();
    }
    
    /// <summary>
    /// Проверяет, открыта ли панель коллекции
    /// </summary>
    public bool IsCollectionVisible()
    {
        return collectionPanel != null && collectionPanel.activeSelf;
    }
    
    void OnDestroy()
    {
        ClearCollection();
    }
}


