using UnityEngine;
using System.Collections.Generic;

public class ImageSlicer : MonoBehaviour
{
    public List<Sprite> SliceImage(Sprite sourceSprite, int rows, int cols, float? targetPixelsPerUnit = null)
    {
        if (sourceSprite == null)
        {
            Debug.LogError("Source sprite is null!");
            return null;
        }
        
        List<Sprite> slicedSprites = new List<Sprite>();
        
        // ВАЖНО: Используем rect спрайта, а не всю текстуру!
        // Если спрайт обрезан через Sprite Editor, rect покажет только область спрайта
        Rect spriteRect = sourceSprite.rect;
        int spriteWidth = Mathf.RoundToInt(spriteRect.width);  // 368 (не 512!)
        int spriteHeight = Mathf.RoundToInt(spriteRect.height); // 512
        int spriteX = Mathf.RoundToInt(spriteRect.x);
        int spriteY = Mathf.RoundToInt(spriteRect.y);
        
        Debug.Log($"Нарезание изображения: спрайт rect = {spriteRect}, размер: {spriteWidth}×{spriteHeight}");
        Debug.Log($"Полная текстура: {sourceSprite.texture.width}×{sourceSprite.texture.height}");
        
        // Получаем читаемую текстуру
        Texture2D fullTexture = GetReadableTexture(sourceSprite.texture);
        
        // Вырезаем только область спрайта из текстуры
        Texture2D spriteTexture = new Texture2D(spriteWidth, spriteHeight, TextureFormat.RGBA32, false);
        // Используем GetPixels напрямую для сохранения максимального качества (float precision)
        Color[] spritePixels = fullTexture.GetPixels(spriteX, spriteY, spriteWidth, spriteHeight);
        spriteTexture.SetPixels(spritePixels);
        
        // КРИТИЧНО: Устанавливаем point filtering ДО Apply() для предотвращения размытия
        spriteTexture.filterMode = FilterMode.Point;
        spriteTexture.wrapMode = TextureWrapMode.Clamp;
        spriteTexture.Apply();
        
        // Вычисляем размеры кусочков с учетом остатков
        int sliceWidth = spriteWidth / cols;
        int sliceHeight = spriteHeight / rows;
        int widthRemainder = spriteWidth % cols;
        int heightRemainder = spriteHeight % rows;
        
        Debug.Log($"Размер каждого кусочка: {sliceWidth}×{sliceHeight} пикселей");
        if (widthRemainder > 0 || heightRemainder > 0)
        {
            Debug.LogWarning($"Остатки при делении: ширина {widthRemainder}, высота {heightRemainder}. Последние кусочки будут немного больше.");
        }
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Вычисляем координаты для вырезки (относительно обрезанного спрайта)
                int x = col * sliceWidth;
                int y = (rows - 1 - row) * sliceHeight; // Инвертируем Y для Unity координат
                
                // Для последних кусочков учитываем остаток
                int currentSliceWidth = sliceWidth;
                int currentSliceHeight = sliceHeight;
                
                if (col == cols - 1 && widthRemainder > 0)
                {
                    currentSliceWidth += widthRemainder;
                }
                if (row == rows - 1 && heightRemainder > 0)
                {
                    currentSliceHeight += heightRemainder;
                }
                
                // Создаем новую текстуру для кусочка с явным форматом
                Texture2D sliceTexture = new Texture2D(currentSliceWidth, currentSliceHeight, TextureFormat.RGBA32, false);
                
                // Используем GetPixels напрямую для сохранения максимального качества
                Color[] pixels = spriteTexture.GetPixels(x, y, currentSliceWidth, currentSliceHeight);
                sliceTexture.SetPixels(pixels);
                
                // КРИТИЧНО: Устанавливаем point filtering ДО Apply() для предотвращения размытия и артефактов
                sliceTexture.filterMode = FilterMode.Point;
                sliceTexture.wrapMode = TextureWrapMode.Clamp;
                sliceTexture.Apply();
                
                // ВАЖНО: Вычисляем pixelsPerUnit так, чтобы размер нарезанного спрайта в мировых единицах
                // совпадал с размером исходного спрайта (и back спрайта)
                float pixelsPerUnitToUse;
                
                if (targetPixelsPerUnit.HasValue)
                {
                    // Вычисляем соотношение размеров: размер кусочка / размер исходного спрайта
                    float sizeRatioX = (float)currentSliceWidth / spriteWidth;
                    float sizeRatioY = (float)currentSliceHeight / spriteHeight;
                    
                    // Используем среднее соотношение (обычно они одинаковые для квадратной сетки)
                    // Если кусочек в 2 раза меньше, то PPU должен быть в 2 раза меньше,
                    // чтобы размер в мировых единицах был таким же
                    float sizeRatio = (sizeRatioX + sizeRatioY) / 2f;
                    pixelsPerUnitToUse = targetPixelsPerUnit.Value * sizeRatio;
                    
                    Debug.Log($"Slice {row}x{col}: size={currentSliceWidth}x{currentSliceHeight}, " +
                             $"source={spriteWidth}x{spriteHeight}, ratio={sizeRatio:F2}, " +
                             $"PPU={pixelsPerUnitToUse:F2} (target={targetPixelsPerUnit.Value:F2})");
                }
                else
                {
                    // Если targetPixelsPerUnit не указан, вычисляем на основе исходного спрайта
                    float sizeRatioX = (float)currentSliceWidth / spriteWidth;
                    float sizeRatioY = (float)currentSliceHeight / spriteHeight;
                    float sizeRatio = (sizeRatioX + sizeRatioY) / 2f;
                    pixelsPerUnitToUse = sourceSprite.pixelsPerUnit * sizeRatio;
                }
                
                // Создаем спрайт с правильным pixelsPerUnit
                Sprite sliceSprite = Sprite.Create(
                    sliceTexture,
                    new Rect(0, 0, currentSliceWidth, currentSliceHeight),
                    new Vector2(0.5f, 0.5f), // Pivot в центре
                    pixelsPerUnitToUse
                );
                
                sliceSprite.name = $"Slice_{row}_{col}_Index_{row * cols + col}";
                
                #if UNITY_EDITOR
                // В редакторе принудительно обновляем bounds
                // Это помогает избежать проблем с неправильным bounds.size для динамически созданных спрайтов
                UnityEditor.EditorUtility.SetDirty(sliceSprite);
                #endif
                
                slicedSprites.Add(sliceSprite);
            }
        }
        
        // Очищаем временные текстуры
        Destroy(spriteTexture);
        if (fullTexture != sourceSprite.texture)
        {
            Destroy(fullTexture);
        }
        
        return slicedSprites;
    }
    
    private Texture2D GetReadableTexture(Texture2D source)
    {
        // Если текстура уже читаема, возвращаем её
        try
        {
            source.GetPixels(0, 0, 1, 1);
            return source;
        }
        catch
        {
            // Текстура не читаема, создаем копию
            // Используем sRGB для правильной цветопередачи
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.sRGB
            );
            
            Graphics.Blit(source, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            
            // Создаем текстуру с явным форматом и point filtering
            Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            // КРИТИЧНО: Устанавливаем point filtering ДО Apply()
            readableTexture.filterMode = FilterMode.Point;
            readableTexture.wrapMode = TextureWrapMode.Clamp;
            readableTexture.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            
            return readableTexture;
        }
    }
    
    public Sprite GetSpriteAt(List<Sprite> sprites, int row, int col, int totalCols)
    {
        int index = row * totalCols + col;
        if (index >= 0 && index < sprites.Count)
        {
            return sprites[index];
        }
        return null;
    }
}


