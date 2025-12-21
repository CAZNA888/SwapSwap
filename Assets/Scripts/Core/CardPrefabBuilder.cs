using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode] // Позволяет работать в режиме редактора
public class CardPrefabBuilder : MonoBehaviour
{
    [Header("Спрайты (перетащите сюда)")]
    [Tooltip("Обратная сторона карточки - определяет размер карточки (368×512 пикселей)")]
    public Sprite cardBackSprite;
    
    [Tooltip("Спрайт рамки (с прозрачной серединой, 368×512 пикселей)")]
    public Sprite borderSprite;
    
    [Header("Автоматические настройки")]
    [Tooltip("Размер карточки (определяется автоматически из CardBack)")]
    [SerializeField] private Vector2 cardSize;
    
    [ContextMenu("1. Автоматическая настройка ВСЕГО")]
    public void AutoSetupEverything()
    {
        Debug.Log("=== НАЧАЛО АВТОМАТИЧЕСКОЙ НАСТРОЙКИ ===");
        
        // Шаг 1: Определяем размер из CardBack
        if (cardBackSprite == null)
        {
            Debug.LogError("❌ Card Back Sprite не установлен! Перетащите спрайт в поле Card Back Sprite");
            return;
        }
        
        cardSize = new Vector2(
            cardBackSprite.bounds.size.x,
            cardBackSprite.bounds.size.y
        );
        
        Debug.Log($"✓ Размер карточки определен: {cardSize.x:F2} x {cardSize.y:F2} единиц");
        
        // Шаг 2: Настраиваем SpriteRenderer
        SetupSpriteRenderer();
        
        // Шаг 3: Настраиваем Collider
        SetupCollider();
        
        // Шаг 4: Настраиваем Rigidbody
        SetupRigidbody();
        
        // Шаг 5: Создаем рамки
        if (borderSprite != null)
        {
            CreateBorders();
        }
        else
        {
            Debug.LogWarning("⚠ Border Sprite не установлен. Рамки не будут созданы.");
        }
        
        Debug.Log("=== НАСТРОЙКА ЗАВЕРШЕНА ===");
    }
    
    private void SetupSpriteRenderer()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
        
        sr.sprite = cardBackSprite;
        sr.sortingOrder = 0;
        
        Debug.Log("✓ SpriteRenderer настроен");
    }
    
    private void SetupCollider()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        collider.size = cardSize;
        
        Debug.Log($"✓ BoxCollider2D настроен, размер: {collider.size.x:F2} x {collider.size.y:F2}");
    }
    
    private void SetupRigidbody()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.isKinematic = true;
        rb.gravityScale = 0;
        
        Debug.Log("✓ Rigidbody2D настроен (Kinematic)");
    }
    
    [ContextMenu("2. Создать рамки")]
    public void CreateBorders()
    {
        if (borderSprite == null)
        {
            Debug.LogError("❌ Border Sprite не установлен!");
            return;
        }
        
        if (cardSize.x == 0 || cardSize.y == 0)
        {
            Debug.LogError("❌ Размер карточки не определен! Сначала запустите '1. Автоматическая настройка ВСЕГО'");
            return;
        }
        
        // Удаляем старые рамки
        ClearBorders();
        
        // Создаем контейнер для рамок
        GameObject borderContainer = transform.Find("BorderContainer")?.gameObject;
        if (borderContainer == null)
        {
            borderContainer = new GameObject("BorderContainer");
            borderContainer.transform.SetParent(transform);
            // Устанавливаем позицию Z ближе к камере, чтобы рамки были поверх карточки
            borderContainer.transform.localPosition = new Vector3(0, 0, -0.01f);
            borderContainer.transform.localRotation = Quaternion.identity;
            borderContainer.transform.localScale = Vector3.one;
            
            borderContainer.AddComponent<BorderRenderer>();
        }
        else
        {
            // Обновляем позицию Z для существующего контейнера
            Vector3 pos = borderContainer.transform.localPosition;
            pos.z = -0.01f;
            borderContainer.transform.localPosition = pos;
        }
        
        borderContainer = transform.Find("BorderContainer").gameObject;
        BorderRenderer borderRenderer = borderContainer.GetComponent<BorderRenderer>();
        
        // ВАЖНО: Используем rect спрайта, а не всю текстуру!
        // Если спрайт обрезан через Sprite Editor, rect покажет только область спрайта
        Rect spriteRect = borderSprite.rect; // Область обрезанного спрайта в текстуре
        int spriteWidth = Mathf.RoundToInt(spriteRect.width);  // 368 (не 512!)
        int spriteHeight = Mathf.RoundToInt(spriteRect.height); // 512
        int spriteX = Mathf.RoundToInt(spriteRect.x);
        int spriteY = Mathf.RoundToInt(spriteRect.y);
        
        Debug.Log($"Спрайт рамки: rect = {spriteRect}, размер в текстуре: {spriteWidth}×{spriteHeight}");
        Debug.Log($"Полная текстура: {borderSprite.texture.width}×{borderSprite.texture.height}");
        
        // Получаем читаемую текстуру
        Texture2D fullTexture = GetReadableTexture(borderSprite.texture);
        
        // Вырезаем только область спрайта из текстуры (368×512, а не 512×512)
        Texture2D spriteTexture = new Texture2D(spriteWidth, spriteHeight);
        Color[] pixels = fullTexture.GetPixels(spriteX, spriteY, spriteWidth, spriteHeight);
        spriteTexture.SetPixels(pixels);
        spriteTexture.Apply();
        
        // Масштабируем под размер карточки если нужно
        float pixelsPerUnit = borderSprite.pixelsPerUnit;
        int targetWidth = Mathf.RoundToInt(cardSize.x * pixelsPerUnit);
        int targetHeight = Mathf.RoundToInt(cardSize.y * pixelsPerUnit);
        
        Texture2D scaledTexture = spriteTexture;
        if (spriteWidth != targetWidth || spriteHeight != targetHeight)
        {
            scaledTexture = ScaleTexture(spriteTexture, targetWidth, targetHeight);
            #if UNITY_EDITOR
            DestroyImmediate(spriteTexture); // Удаляем промежуточную текстуру
            #else
            Destroy(spriteTexture);
            #endif
        }
        
        int width = scaledTexture.width;
        int height = scaledTexture.height;
        
        Debug.Log($"Разрезание рамки {width}×{height} (из обрезанного спрайта {spriteWidth}×{spriteHeight}) по диагоналям");
        
        // Создаем 4 треугольника
        // Верхний треугольник (верхняя часть)
        CreateTriangleBorder("TopBorder", scaledTexture, width, height, 
            TriangleType.Top, new Vector3(0, cardSize.y / 2, 0), 
            new Vector2(cardSize.x, cardSize.y / 2), borderContainer);
        
        // Нижний треугольник (нижняя часть)
        CreateTriangleBorder("BottomBorder", scaledTexture, width, height, 
            TriangleType.Bottom, new Vector3(0, -cardSize.y / 2, 0), 
            new Vector2(cardSize.x, cardSize.y / 2), borderContainer);
        
        // Левый треугольник (левая часть)
        CreateTriangleBorder("LeftBorder", scaledTexture, width, height, 
            TriangleType.Left, new Vector3(-cardSize.x / 2, 0, 0), 
            new Vector2(cardSize.x / 2, cardSize.y), borderContainer);
        
        // Правый треугольник (правая часть)
        CreateTriangleBorder("RightBorder", scaledTexture, width, height, 
            TriangleType.Right, new Vector3(cardSize.x / 2, 0, 0), 
            new Vector2(cardSize.x / 2, cardSize.y), borderContainer);
        
        // Инициализируем BorderRenderer
        GameObject top = borderContainer.transform.Find("TopBorder")?.gameObject;
        GameObject bottom = borderContainer.transform.Find("BottomBorder")?.gameObject;
        GameObject left = borderContainer.transform.Find("LeftBorder")?.gameObject;
        GameObject right = borderContainer.transform.Find("RightBorder")?.gameObject;
        
        if (top != null && bottom != null && left != null && right != null)
        {
            borderRenderer.Initialize(top, bottom, left, right);
            Debug.Log("✓ Рамки созданы из треугольников");
            
            // ВАЖНО: Сохраняем изменения в префаб
            #if UNITY_EDITOR
            SavePrefabChanges();
            #endif
        }
        
        // Очищаем временные текстуры
        // ВАЖНО: НЕ удаляем scaledTexture и spriteTexture здесь!
        // Они используются спрайтами треугольников, которые создали свои копии
        // Удаляем только fullTexture если это была копия
        if (fullTexture != borderSprite.texture)
        {
            #if UNITY_EDITOR
            DestroyImmediate(fullTexture);
            #else
            Destroy(fullTexture);
            #endif
        }
        
        // Примечание: scaledTexture и spriteTexture будут удалены сборщиком мусора
        // когда спрайты, использующие их копии, будут уничтожены
    }
    
    enum TriangleType
    {
        Top,    // Верхний треугольник
        Bottom, // Нижний треугольник
        Left,   // Левый треугольник
        Right   // Правый треугольник
    }
    
    private void CreateTriangleBorder(string name, Texture2D source, int width, int height, 
        TriangleType triangleType, Vector3 position, Vector2 targetSize, GameObject parent)
    {
        // Создаем текстуру для треугольника
        Texture2D triangleTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // Определяем маску треугольника
        // Диагональ 1: от (width, height) до (0, 0) - y = height - (height/width) * x
        // Диагональ 2: от (0, height) до (width, 0) - y = (height/width) * x
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isInTriangle = false;
                
                // Определяем, находится ли пиксель в нужном треугольнике
                float diag1Y = height - (float)height / width * x; // Диагональ от (width, height) до (0, 0)
                float diag2Y = (float)height / width * x; // Диагональ от (0, height) до (width, 0)
                
                switch (triangleType)
                {
                    case TriangleType.Top:
                        // Верхний: выше обеих диагоналей
                        isInTriangle = (y > diag1Y) && (y > diag2Y);
                        break;
                        
                    case TriangleType.Bottom:
                        // Нижний: ниже обеих диагоналей
                        isInTriangle = (y < diag1Y) && (y < diag2Y);
                        break;
                        
                    case TriangleType.Left:
                        // Левый: левее центра и между диагоналями
                        bool aboveDiag1 = y > diag1Y;
                        bool aboveDiag2 = y > diag2Y;
                        isInTriangle = x < width / 2 && (aboveDiag1 != aboveDiag2);
                        break;
                        
                    case TriangleType.Right:
                        // Правый: правее центра и между диагоналями
                        bool aboveDiag1Right = y > diag1Y;
                        bool aboveDiag2Right = y > diag2Y;
                        isInTriangle = x >= width / 2 && (aboveDiag1Right != aboveDiag2Right);
                        break;
                }
                
                if (isInTriangle)
                {
                    // Копируем пиксель из исходной текстуры
                    triangleTexture.SetPixel(x, y, source.GetPixel(x, y));
                }
                else
                {
                    // Прозрачный пиксель
                    triangleTexture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        triangleTexture.Apply();
        
        // СОЗДАЕМ КВАДРАТНУЮ ТЕКСТУРУ ДЛЯ ЛУЧШЕГО СЖАТИЯ В АТЛАСЫ
        int squareSize = Mathf.Max(width, height); // 512 для 368x512
        Texture2D squareTexture = new Texture2D(squareSize, squareSize, TextureFormat.RGBA32, false);
        
        // Заполняем квадратную текстуру прозрачным цветом
        Color[] transparentPixels = new Color[squareSize * squareSize];
        for (int i = 0; i < transparentPixels.Length; i++)
        {
            transparentPixels[i] = Color.clear;
        }
        squareTexture.SetPixels(transparentPixels);
        
        // Вычисляем смещение для размещения треугольника по центру
        int offsetX = (squareSize - width) / 2;  // (512 - 368) / 2 = 72
        int offsetY = (squareSize - height) / 2; // (512 - 512) / 2 = 0
        
        // Копируем треугольник в центр квадратной текстуры
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = triangleTexture.GetPixel(x, y);
                squareTexture.SetPixel(offsetX + x, offsetY + y, pixel);
            }
        }
        squareTexture.Apply();
        
        // Удаляем временную текстуру треугольника
        #if UNITY_EDITOR
        DestroyImmediate(triangleTexture);
        #else
        Destroy(triangleTexture);
        #endif
        
        // СОХРАНЯЕМ КВАДРАТНУЮ ТЕКСТУРУ КАК АССЕТ В ПРОЕКТЕ
        Sprite triangleSprite = null;
        
        #if UNITY_EDITOR
        // Создаем папку для сохранения спрайтов треугольников
        string borderSpritesPath = "Assets/Sprites/Borders/Triangles";
        if (!AssetDatabase.IsValidFolder(borderSpritesPath))
        {
            // Создаем папку если её нет
            string[] folders = borderSpritesPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
        
        // Имя файла для сохранения (используем имя префаба для уникальности)
        string prefabName = gameObject.name;
        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = "PuzzlePiece";
        }
        string texturePath = $"{borderSpritesPath}/{prefabName}_{name}_Texture.png";
        
        // Сохраняем квадратную текстуру как PNG
        byte[] pngData = squareTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(texturePath, pngData);
        AssetDatabase.ImportAsset(texturePath);
        
        // Настраиваем импорт как спрайт
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = borderSprite.pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        
        // Загружаем сохраненный спрайт
        Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        
        if (loadedSprite == null)
        {
            Debug.LogWarning($"Не удалось загрузить спрайт из {texturePath}, создаем временный спрайт");
            // Fallback - создаем спрайт в памяти из квадратной текстуры
            triangleSprite = Sprite.Create(
                squareTexture,
                new Rect(offsetX, offsetY, width, height), // Rect указывает на область треугольника в квадрате
                new Vector2(0.5f, 0.5f),
                borderSprite.pixelsPerUnit
            );
        }
        else
        {
            // Создаем спрайт с правильным rect, чтобы использовать только область треугольника
            Texture2D loadedTexture = loadedSprite.texture;
            triangleSprite = Sprite.Create(
                loadedTexture,
                new Rect(offsetX, offsetY, width, height), // Rect указывает на область треугольника в квадрате
                new Vector2(0.5f, 0.5f),
                borderSprite.pixelsPerUnit
            );
            
            // Удаляем временную квадратную текстуру, так как теперь используем сохраненный спрайт
            DestroyImmediate(squareTexture);
            Debug.Log($"  ✓ Квадратный спрайт ({squareSize}×{squareSize}) сохранен в {texturePath}");
        }
        #else
        // В рантайме просто создаем спрайт в памяти из квадратной текстуры
        triangleSprite = Sprite.Create(
            squareTexture,
            new Rect(offsetX, offsetY, width, height), // Rect указывает на область треугольника в квадрате
            new Vector2(0.5f, 0.5f),
            borderSprite.pixelsPerUnit
        );
        #endif
        
        // Создаем GameObject для границы
        GameObject borderObj = new GameObject(name);
        borderObj.transform.SetParent(parent.transform);
        // Устанавливаем позицию Z ближе к камере, чтобы рамки были поверх карточки
        Vector3 borderPosition = position;
        borderPosition.z = -0.01f; // Ближе к камере, чем карточка (которая имеет z=0)
        borderObj.transform.localPosition = borderPosition;
        borderObj.transform.localRotation = Quaternion.identity;
        
        // Масштабируем под нужный размер
        Vector2 spriteSize = triangleSprite.bounds.size;
        float scaleX = targetSize.x / spriteSize.x;
        float scaleY = targetSize.y / spriteSize.y;
        borderObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        
        SpriteRenderer sr = borderObj.AddComponent<SpriteRenderer>();
        sr.sprite = triangleSprite;
        sr.sortingOrder = 2; // Должно быть выше карточки (которая имеет 0), чтобы рамки были поверх
        
        // ВАЖНО: Убеждаемся, что спрайт установлен
        if (sr.sprite == null)
        {
            Debug.LogError($"Sprite не установлен для {name}!");
        }
        
        // ВАЖНО: Убеждаемся, что объект активен
        borderObj.SetActive(true);
        
        #if UNITY_EDITOR
        string savedInfo = triangleSprite != null && AssetDatabase.Contains(triangleSprite) 
            ? $", сохранен в проекте" 
            : "";
        Debug.Log($"  ✓ {name} создан из треугольника, позиция: {position}, sprite={sr.sprite != null}, active={borderObj.activeSelf}, sortingOrder={sr.sortingOrder}{savedInfo}");
        #else
        Debug.Log($"  ✓ {name} создан из треугольника, позиция: {position}, sprite={sr.sprite != null}, active={borderObj.activeSelf}, sortingOrder={sr.sortingOrder}");
        #endif
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
    
    [ContextMenu("3. Удалить рамки")]
    public void ClearBorders()
    {
        Transform borderContainer = transform.Find("BorderContainer");
        if (borderContainer != null)
        {
            #if UNITY_EDITOR
            DestroyImmediate(borderContainer.gameObject);
            #else
            Destroy(borderContainer.gameObject);
            #endif
            Debug.Log("✓ Рамки удалены");
        }
    }
    
    [ContextMenu("Проверить размеры спрайтов")]
    public void CheckSpriteSizes()
    {
        Debug.Log("=== ПРОВЕРКА РАЗМЕРОВ СПРАЙТОВ ===");
        
        if (cardBackSprite != null)
        {
            Vector2 cardSize = new Vector2(
                cardBackSprite.bounds.size.x,
                cardBackSprite.bounds.size.y
            );
            Debug.Log($"CardBack размер: {cardSize.x:F2} x {cardSize.y:F2} (мировых единиц)");
            Debug.Log($"CardBack пиксели: {cardBackSprite.texture.width} x {cardBackSprite.texture.height}");
            Debug.Log($"Pixels Per Unit: {cardBackSprite.pixelsPerUnit}");
            Debug.Log($"Соотношение: {cardSize.x / cardSize.y:F2}:1");
            
            // Проверка на правильный размер (368x512 при Pixels Per Unit = 100)
            float expectedWidth = 368f / cardBackSprite.pixelsPerUnit;
            float expectedHeight = 512f / cardBackSprite.pixelsPerUnit;
            
            Debug.Log($"Ожидаемый размер (368×512 / {cardBackSprite.pixelsPerUnit}): {expectedWidth:F2} x {expectedHeight:F2}");
            
            if (Mathf.Abs(cardSize.x - expectedWidth) < 0.01f && 
                Mathf.Abs(cardSize.y - expectedHeight) < 0.01f)
            {
                Debug.Log("✓ Размеры правильные!");
            }
            else
            {
                Debug.LogWarning($"⚠ Несоответствие размеров!");
                Debug.LogWarning($"  Ожидалось: {expectedWidth:F2} x {expectedHeight:F2}");
                Debug.LogWarning($"  Получено: {cardSize.x:F2} x {cardSize.y:F2}");
                Debug.LogWarning("  Проверьте Pixels Per Unit спрайта! Должно быть 100 для размера 368×512");
            }
        }
        else
        {
            Debug.LogError("❌ Card Back Sprite не установлен!");
        }
        
        if (borderSprite != null)
        {
            Vector2 borderSize = new Vector2(
                borderSprite.bounds.size.x,
                borderSprite.bounds.size.y
            );
            Debug.Log($"Border размер: {borderSize.x:F2} x {borderSize.y:F2} (мировых единиц)");
            Debug.Log($"Border пиксели: {borderSprite.texture.width} x {borderSprite.texture.height}");
            Debug.Log($"Pixels Per Unit: {borderSprite.pixelsPerUnit}");
            
            if (cardBackSprite != null)
            {
                Vector2 cardSize = new Vector2(
                    cardBackSprite.bounds.size.x,
                    cardBackSprite.bounds.size.y
                );
                
                if (Mathf.Abs(borderSize.x - cardSize.x) < 0.01f && 
                    Mathf.Abs(borderSize.y - cardSize.y) < 0.01f)
                {
                    Debug.Log("✓ Размеры Border и CardBack совпадают!");
                }
                else
                {
                    Debug.LogWarning("⚠ Размеры Border и CardBack не совпадают!");
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠ Border Sprite не установлен");
        }
    }
    
    [ContextMenu("Показать размер карточки")]
    public void ShowCardSize()
    {
        if (cardBackSprite != null)
        {
            Vector2 size = new Vector2(
                cardBackSprite.bounds.size.x,
                cardBackSprite.bounds.size.y
            );
            Debug.Log($"Размер карточки: {size.x:F2} x {size.y:F2} (мировых единиц)");
            Debug.Log($"Соотношение сторон: {size.x / size.y:F2}:1");
            Debug.Log($"Пиксели: {cardBackSprite.texture.width} x {cardBackSprite.texture.height}");
        }
        else
        {
            Debug.LogWarning("Card Back Sprite не установлен!");
        }
    }
    
    #if UNITY_EDITOR
    private void SavePrefabChanges()
    {
        // Отмечаем объект как измененный
        EditorUtility.SetDirty(gameObject);
        
        // Проверяем, редактируем ли мы префаб в Prefab Mode
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            // Мы в Prefab Mode - сохраняем напрямую
            string prefabPath = AssetDatabase.GetAssetPath(gameObject);
            if (!string.IsNullOrEmpty(prefabPath))
            {
                PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"✓ Префаб сохранен: {prefabPath}");
                return;
            }
        }
        
        // Проверяем, является ли это экземпляром префаба
        if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            // Применяем изменения к префабу
            PrefabUtility.ApplyPrefabInstance(gameObject, InteractionMode.AutomatedAction);
            Debug.Log("✓ Изменения применены к префабу");
            return;
        }
        
        // Это не префаб, просто отмечаем изменения
        EditorUtility.SetDirty(gameObject);
        Debug.Log("✓ Изменения отмечены (не префаб)");
    }
    #endif
}

