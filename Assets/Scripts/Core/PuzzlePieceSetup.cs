using UnityEngine;

[System.Serializable]
public class PuzzlePieceSetup : MonoBehaviour
{
    [Header("Card Settings")]
    [Tooltip("Размер карточки в мировых единицах (берется из CardBack)")]
    public Vector2 cardSize = new Vector2(1f, 1.33f); // 3:4 соотношение
    
    [Header("Components")]
    public SpriteRenderer cardRenderer;
    public BoxCollider2D cardCollider;
    public Rigidbody2D cardRigidbody;
    public BorderRenderer borderRenderer;
    
    [Header("Border Settings")]
    public Sprite borderSprite;
    [Tooltip("Толщина рамки в процентах от размера карточки (0.05 = 5%)")]
    [Range(0.01f, 0.2f)]
    public float borderThicknessPercent = 0.05f;
    
    [Header("Child Objects")]
    public GameObject topBorderObj;
    public GameObject bottomBorderObj;
    public GameObject leftBorderObj;
    public GameObject rightBorderObj;
    
    void Awake()
    {
        SetupComponents();
    }
    
    [ContextMenu("Setup Card")]
    public void SetupComponents()
    {
        // Настраиваем SpriteRenderer
        if (cardRenderer == null)
        {
            cardRenderer = GetComponent<SpriteRenderer>();
            if (cardRenderer == null)
            {
                cardRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        // Настраиваем Collider
        if (cardCollider == null)
        {
            cardCollider = GetComponent<BoxCollider2D>();
            if (cardCollider == null)
            {
                cardCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }
        cardCollider.size = cardSize;
        
        // Настраиваем Rigidbody2D
        if (cardRigidbody == null)
        {
            cardRigidbody = GetComponent<Rigidbody2D>();
            if (cardRigidbody == null)
            {
                cardRigidbody = gameObject.AddComponent<Rigidbody2D>();
            }
        }
        cardRigidbody.isKinematic = true;
        cardRigidbody.gravityScale = 0;
        
        // Настраиваем BorderRenderer
        if (borderRenderer == null)
        {
            borderRenderer = GetComponentInChildren<BorderRenderer>();
            if (borderRenderer == null)
            {
                GameObject borderContainer = new GameObject("BorderContainer");
                borderContainer.transform.SetParent(transform);
                borderContainer.transform.localPosition = Vector3.zero;
                borderContainer.transform.localRotation = Quaternion.identity;
                borderContainer.transform.localScale = Vector3.one;
                borderRenderer = borderContainer.AddComponent<BorderRenderer>();
            }
        }
    }
    
    [ContextMenu("Create Borders")]
    public void CreateBorders()
    {
        if (borderSprite == null)
        {
            Debug.LogWarning("Border Sprite не установлен!");
            return;
        }
        
        // Удаляем старые границы
        ClearBorders();
        
        Transform borderContainer = borderRenderer.transform;
        float borderThickness = Mathf.Min(cardSize.x, cardSize.y) * borderThicknessPercent;
        
        // Создаем 4 границы
        topBorderObj = CreateBorderPart("TopBorder", 
            new Vector3(0, cardSize.y / 2, 0), 
            new Vector2(cardSize.x, borderThickness), 
            borderContainer);
        
        bottomBorderObj = CreateBorderPart("BottomBorder", 
            new Vector3(0, -cardSize.y / 2, 0), 
            new Vector2(cardSize.x, borderThickness), 
            borderContainer);
        
        leftBorderObj = CreateBorderPart("LeftBorder", 
            new Vector3(-cardSize.x / 2, 0, 0), 
            new Vector2(borderThickness, cardSize.y), 
            borderContainer);
        
        rightBorderObj = CreateBorderPart("RightBorder", 
            new Vector3(cardSize.x / 2, 0, 0), 
            new Vector2(borderThickness, cardSize.y), 
            borderContainer);
        
        // Инициализируем BorderRenderer
        borderRenderer.Initialize(topBorderObj, bottomBorderObj, leftBorderObj, rightBorderObj);
    }
    
    private GameObject CreateBorderPart(string name, Vector3 position, Vector2 size, Transform parent)
    {
        GameObject borderObj = new GameObject(name);
        borderObj.transform.SetParent(parent);
        borderObj.transform.localPosition = position;
        borderObj.transform.localRotation = Quaternion.identity;
        borderObj.transform.localScale = Vector3.one;
        
        SpriteRenderer sr = borderObj.AddComponent<SpriteRenderer>();
        sr.sprite = borderSprite;
        sr.sortingOrder = 1;
        
        // Масштабируем спрайт под нужный размер
        if (borderSprite != null)
        {
            // ИСПРАВЛЕНО: Используем rect и pixelsPerUnit вместо bounds.size для стабильности
            Vector2 spriteSize = new Vector2(
                borderSprite.rect.width / borderSprite.pixelsPerUnit,
                borderSprite.rect.height / borderSprite.pixelsPerUnit
            );
            float scaleX = size.x / spriteSize.x;
            float scaleY = size.y / spriteSize.y;
            borderObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        
        return borderObj;
    }
    
    [ContextMenu("Clear Borders")]
    public void ClearBorders()
    {
        if (topBorderObj != null) DestroyImmediate(topBorderObj);
        if (bottomBorderObj != null) DestroyImmediate(bottomBorderObj);
        if (leftBorderObj != null) DestroyImmediate(leftBorderObj);
        if (rightBorderObj != null) DestroyImmediate(rightBorderObj);
        
        topBorderObj = null;
        bottomBorderObj = null;
        leftBorderObj = null;
        rightBorderObj = null;
    }
    
    // Обновляет размер карточки из CardBack спрайта
    public void UpdateSizeFromCardBack(Sprite cardBack)
    {
        if (cardBack != null)
        {
            // ИСПРАВЛЕНО: Используем rect и pixelsPerUnit вместо bounds.size для стабильности
            cardSize = new Vector2(
                cardBack.rect.width / cardBack.pixelsPerUnit,
                cardBack.rect.height / cardBack.pixelsPerUnit
            );
            
            // Обновляем коллайдер
            if (cardCollider != null)
            {
                cardCollider.size = cardSize;
            }
            
            // Пересоздаем границы
            if (borderSprite != null)
            {
                CreateBorders();
            }
        }
    }
}

