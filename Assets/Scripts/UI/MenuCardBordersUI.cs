using UnityEngine;
using UnityEngine.UI;

public class MenuCardBordersUI : MonoBehaviour
{
    [Header("UI Borders")]
    public Image topBorder;
    public Image bottomBorder;
    public Image leftBorder;
    public Image rightBorder;
    
    [Header("Settings")]
    [Tooltip("Спрайт для рамок")]
    public Sprite borderSprite;
    
    [Tooltip("Толщина рамки в пикселях")]
    public float borderThickness = 8f;
    
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
    }
    
    /// <summary>
    /// Инициализирует или создает UI рамки
    /// </summary>
    public void InitializeBorders(Sprite sprite = null)
    {
        if (sprite != null)
        {
            borderSprite = sprite;
        }
        
        if (borderSprite == null)
        {
            Debug.LogWarning("MenuCardBordersUI: Border sprite is not set!");
            return;
        }
        
        // Создаем рамки, если их нет
        if (topBorder == null) CreateBorder("TopBorder", 0);
        if (bottomBorder == null) CreateBorder("BottomBorder", 1);
        if (leftBorder == null) CreateBorder("LeftBorder", 2);
        if (rightBorder == null) CreateBorder("RightBorder", 3);
        
        // Обновляем размеры рамок
        UpdateBorderSizes();
    }
    
    /// <summary>
    /// Создает UI рамку
    /// </summary>
    private void CreateBorder(string name, int side)
    {
        GameObject borderObj = new GameObject(name);
        borderObj.transform.SetParent(transform);
        borderObj.transform.localScale = Vector3.one;
        borderObj.transform.localRotation = Quaternion.identity;
        
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.sprite = borderSprite;
        borderImage.preserveAspect = false;
        borderImage.type = Image.Type.Sliced; // Для лучшего масштабирования
        
        // Устанавливаем anchors в зависимости от стороны
        switch (side)
        {
            case 0: // Top
                borderRect.anchorMin = new Vector2(0, 1);
                borderRect.anchorMax = new Vector2(1, 1);
                borderRect.pivot = new Vector2(0.5f, 1);
                borderRect.anchoredPosition = Vector2.zero;
                borderRect.sizeDelta = new Vector2(0, borderThickness);
                topBorder = borderImage;
                break;
                
            case 1: // Bottom
                borderRect.anchorMin = new Vector2(0, 0);
                borderRect.anchorMax = new Vector2(1, 0);
                borderRect.pivot = new Vector2(0.5f, 0);
                borderRect.anchoredPosition = Vector2.zero;
                borderRect.sizeDelta = new Vector2(0, borderThickness);
                bottomBorder = borderImage;
                break;
                
            case 2: // Left
                borderRect.anchorMin = new Vector2(0, 0);
                borderRect.anchorMax = new Vector2(0, 1);
                borderRect.pivot = new Vector2(0, 0.5f);
                borderRect.anchoredPosition = Vector2.zero;
                borderRect.sizeDelta = new Vector2(borderThickness, 0);
                leftBorder = borderImage;
                break;
                
            case 3: // Right
                borderRect.anchorMin = new Vector2(1, 0);
                borderRect.anchorMax = new Vector2(1, 1);
                borderRect.pivot = new Vector2(1, 0.5f);
                borderRect.anchoredPosition = Vector2.zero;
                borderRect.sizeDelta = new Vector2(borderThickness, 0);
                rightBorder = borderImage;
                break;
        }
        
        // Устанавливаем порядок отрисовки (поверх карточки)
        borderImage.raycastTarget = false; // Не блокируем клики
    }
    
    /// <summary>
    /// Обновляет размеры рамок под размер карточки
    /// </summary>
    public void UpdateBorderSizes()
    {
        if (rectTransform == null) return;
        
        // Получаем размер из родителя (карточки) или из собственного rectTransform
        Vector2 cardSize = rectTransform.sizeDelta;
        
        // Если размер нулевой, пытаемся получить из родителя
        if (cardSize.x == 0 && cardSize.y == 0 && transform.parent != null)
        {
            RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                cardSize = parentRect.sizeDelta;
            }
        }
        
        // Обновляем размеры рамок
        if (topBorder != null)
        {
            RectTransform rt = topBorder.rectTransform;
            rt.sizeDelta = new Vector2(cardSize.x, borderThickness);
        }
        
        if (bottomBorder != null)
        {
            RectTransform rt = bottomBorder.rectTransform;
            rt.sizeDelta = new Vector2(cardSize.x, borderThickness);
        }
        
        if (leftBorder != null)
        {
            RectTransform rt = leftBorder.rectTransform;
            rt.sizeDelta = new Vector2(borderThickness, cardSize.y);
        }
        
        if (rightBorder != null)
        {
            RectTransform rt = rightBorder.rectTransform;
            rt.sizeDelta = new Vector2(borderThickness, cardSize.y);
        }
    }
    
    /// <summary>
    /// Обновляет размеры рамок с указанным размером карточки
    /// </summary>
    public void UpdateBorderSizes(Vector2 cardSize)
    {
        // Обновляем размеры рамок
        if (topBorder != null)
        {
            RectTransform rt = topBorder.rectTransform;
            rt.sizeDelta = new Vector2(cardSize.x, borderThickness);
        }
        
        if (bottomBorder != null)
        {
            RectTransform rt = bottomBorder.rectTransform;
            rt.sizeDelta = new Vector2(cardSize.x, borderThickness);
        }
        
        if (leftBorder != null)
        {
            RectTransform rt = leftBorder.rectTransform;
            rt.sizeDelta = new Vector2(borderThickness, cardSize.y);
        }
        
        if (rightBorder != null)
        {
            RectTransform rt = rightBorder.rectTransform;
            rt.sizeDelta = new Vector2(borderThickness, cardSize.y);
        }
    }
    
    /// <summary>
    /// Обновляет видимость рамок
    /// connections[0]=верх, [1]=низ, [2]=лево, [3]=право (true = есть сосед, рамку прячем)
    /// </summary>
    public void UpdateBorders(bool[] connections, bool isOpened)
    {
        if (!isOpened)
        {
            SetActiveAll(false);
            return;
        }
        
        if (connections == null || connections.Length < 4)
        {
            SetActiveAll(true);
            return;
        }
        
        // Показать рамку, если НЕТ соседа (false)
        SetBorder(topBorder, !connections[0]);
        SetBorder(bottomBorder, !connections[1]);
        SetBorder(leftBorder, !connections[2]);
        SetBorder(rightBorder, !connections[3]);
    }
    
    private void SetBorder(Image img, bool active)
    {
        if (img != null)
        {
            img.enabled = active;
        }
    }
    
    private void SetActiveAll(bool active)
    {
        SetBorder(topBorder, active);
        SetBorder(bottomBorder, active);
        SetBorder(leftBorder, active);
        SetBorder(rightBorder, active);
    }
}

