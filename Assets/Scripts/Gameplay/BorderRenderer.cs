using UnityEngine;

public class BorderRenderer : MonoBehaviour
{
    [Header("Border Parts")]
    public GameObject topBorder;
    public GameObject bottomBorder;
    public GameObject leftBorder;
    public GameObject rightBorder;
    
    private PuzzlePiece parentPiece;
    
    void Awake()
    {
        parentPiece = GetComponentInParent<PuzzlePiece>();
    }
    
    public void Initialize(GameObject top, GameObject bottom, GameObject left, GameObject right)
    {
        topBorder = top;
        bottomBorder = bottom;
        leftBorder = left;
        rightBorder = right;
    }
    
    public void SetBorderActive(int side, bool active)
    {
        // side: 0=верх, 1=низ, 2=лево, 3=право
        GameObject border = null;
        
        switch (side)
        {
            case 0: border = topBorder; break;
            case 1: border = bottomBorder; break;
            case 2: border = leftBorder; break;
            case 3: border = rightBorder; break;
        }
        
        if (border != null)
        {
            border.SetActive(active);
        }
    }
    
    public void SetBordersSortingOrder(int sortingOrder)
    {
        if (topBorder != null)
        {
            SpriteRenderer sr = topBorder.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = sortingOrder;
        }
        if (bottomBorder != null)
        {
            SpriteRenderer sr = bottomBorder.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = sortingOrder;
        }
        if (leftBorder != null)
        {
            SpriteRenderer sr = leftBorder.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = sortingOrder;
        }
        if (rightBorder != null)
        {
            SpriteRenderer sr = rightBorder.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = sortingOrder;
        }
    }
    
    public void UpdateBorders(bool[] connections, bool isFlipped)
    {
        // Если карточка не перевернута (CardBack) - скрываем все рамки
        if (!isFlipped)
        {
            for (int i = 0; i < 4; i++)
            {
                SetBorderActive(i, false); // Скрываем все рамки на обратной стороне
            }
            return;
        }
        
        // Если перевернута - показываем/скрываем в зависимости от connections
        // connections[0] = верх, connections[1] = низ, connections[2] = лево, connections[3] = право
        // Если соединена, скрываем границу (не показываем)
        
        // Проверка наличия рамок
        if (topBorder == null || bottomBorder == null || leftBorder == null || rightBorder == null)
        {
            Debug.LogWarning($"UpdateBorders: Рамки не инициализированы! top={topBorder != null}, bottom={bottomBorder != null}, left={leftBorder != null}, right={rightBorder != null}");
            return;
        }
        
        for (int i = 0; i < 4; i++)
        {
            bool shouldShow = !connections[i]; // Показываем если НЕ соединена
            SetBorderActive(i, shouldShow);
        }
    }
    
    public void CreateBorderParts(Sprite borderSprite, Vector2 cardSize)
    {
        if (borderSprite == null) return;
        
        // Получаем читаемую текстуру рамки
        Texture2D borderTexture = GetReadableTexture(borderSprite.texture);
        
        // Вычисляем нужный размер рамки (точно под карточку)
        // Используем pixelsPerUnit для правильного масштабирования
        float pixelsPerUnit = borderSprite.pixelsPerUnit;
        int targetWidth = Mathf.RoundToInt(cardSize.x * pixelsPerUnit);
        int targetHeight = Mathf.RoundToInt(cardSize.y * pixelsPerUnit);
        
        // Масштабируем текстуру рамки под размер карточки
        Texture2D scaledBorderTexture = ScaleTexture(borderTexture, targetWidth, targetHeight);
        
        int width = scaledBorderTexture.width;
        int height = scaledBorderTexture.height;
        
        // Толщина рамки (примерно 5-10% от размера, минимум 4 пикселя)
        int borderThickness = Mathf.Max(4, Mathf.Min(width / 10, height / 10));
        
        // Создаем 4 части точно по краям карточки
        // Верхняя граница
        CreateBorderPart(scaledBorderTexture, 0, height - borderThickness, width, borderThickness, 
            "TopBorder", new Vector3(0, cardSize.y / 2, 0), new Vector2(cardSize.x, borderThickness / pixelsPerUnit));
        
        // Нижняя граница
        CreateBorderPart(scaledBorderTexture, 0, 0, width, borderThickness, 
            "BottomBorder", new Vector3(0, -cardSize.y / 2, 0), new Vector2(cardSize.x, borderThickness / pixelsPerUnit));
        
        // Левая граница
        CreateBorderPart(scaledBorderTexture, 0, 0, borderThickness, height, 
            "LeftBorder", new Vector3(-cardSize.x / 2, 0, 0), new Vector2(borderThickness / pixelsPerUnit, cardSize.y));
        
        // Правая граница
        CreateBorderPart(scaledBorderTexture, width - borderThickness, 0, borderThickness, height, 
            "RightBorder", new Vector3(cardSize.x / 2, 0, 0), new Vector2(borderThickness / pixelsPerUnit, cardSize.y));
        
        // Очищаем временные текстуры
        if (scaledBorderTexture != borderTexture)
        {
            Destroy(scaledBorderTexture);
        }
        if (borderTexture != borderSprite.texture)
        {
            Destroy(borderTexture);
        }
    }
    
    private void CreateBorderPart(Texture2D source, int x, int y, int width, int height, 
        string name, Vector3 localPosition, Vector2 targetSize)
    {
        Texture2D partTexture = new Texture2D(width, height);
        Color[] pixels = source.GetPixels(x, y, width, height);
        partTexture.SetPixels(pixels);
        partTexture.Apply();
        
        // Создаем спрайт с правильным pixelsPerUnit
        float pixelsPerUnit = 100f; // Стандартное значение
        Sprite partSprite = Sprite.Create(partTexture, new Rect(0, 0, width, height), 
            new Vector2(0.5f, 0.5f), pixelsPerUnit);
        
        GameObject borderObj = new GameObject(name);
        borderObj.transform.SetParent(transform);
        borderObj.transform.localPosition = localPosition;
        borderObj.transform.localRotation = Quaternion.identity;
        
        // Масштабируем под нужный размер (чтобы точно покрывал карточку)
        Vector2 spriteSize = partSprite.bounds.size;
        float scaleX = targetSize.x / spriteSize.x;
        float scaleY = targetSize.y / spriteSize.y;
        borderObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        
        SpriteRenderer sr = borderObj.AddComponent<SpriteRenderer>();
        sr.sprite = partSprite;
        sr.sortingOrder = 1; // Поверх карточки
        
        // Сохраняем ссылку
        switch (name)
        {
            case "TopBorder": topBorder = borderObj; break;
            case "BottomBorder": bottomBorder = borderObj; break;
            case "LeftBorder": leftBorder = borderObj; break;
            case "RightBorder": rightBorder = borderObj; break;
        }
    }
    
    // Масштабирует текстуру
    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D readableTexture = new Texture2D(targetWidth, targetHeight);
        readableTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return readableTexture;
    }
    
    private Texture2D GetReadableTexture(Texture2D source)
    {
        try
        {
            source.GetPixels(0, 0, 1, 1);
            return source;
        }
        catch
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            
            Texture2D readableTexture = new Texture2D(source.width, source.height);
            readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTexture.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            
            return readableTexture;
        }
    }
}


