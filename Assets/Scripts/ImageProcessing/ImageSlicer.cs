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
        
        // ВАЖНО: Вычисляем размер кусочка с округлением вверх для равномерного распределения
        int sliceWidth = Mathf.CeilToInt((float)spriteWidth / cols);
        int sliceHeight = Mathf.CeilToInt((float)spriteHeight / rows);
        
        // Обрезаем изображение до размера, который делится нацело на количество колонок/строк
        int adjustedWidth = sliceWidth * cols;
        int adjustedHeight = sliceHeight * rows;
        
        // Обрезаем текстуру спрайта с центрированием (обрезаем поровну с обеих сторон)
        int cropX = (spriteWidth - adjustedWidth) / 2;
        int cropY = (spriteHeight - adjustedHeight) / 2;
        
        // ВАЖНО: Проверяем границы, чтобы не выйти за пределы исходной текстуры
        int sourceX = spriteX + cropX;
        int sourceY = spriteY + cropY;
        
        // Проверяем, что координаты не отрицательные
        if (sourceX < 0)
        {
            adjustedWidth += sourceX; // Уменьшаем ширину на величину отрицательного смещения
            sourceX = 0;
            Debug.LogWarning($"Корректировка cropX: смещение было отрицательным, скорректирована ширина до {adjustedWidth}");
        }
        
        if (sourceY < 0)
        {
            adjustedHeight += sourceY; // Уменьшаем высоту на величину отрицательного смещения
            sourceY = 0;
            Debug.LogWarning($"Корректировка cropY: смещение было отрицательным, скорректирована высота до {adjustedHeight}");
        }
        
        // Проверяем, что не выходим за правую границу
        if (sourceX + adjustedWidth > fullTexture.width)
        {
            adjustedWidth = fullTexture.width - sourceX;
            Debug.LogWarning($"Корректировка ширины: обрезано до {adjustedWidth} (граница текстуры: {fullTexture.width})");
        }
        
        // Проверяем, что не выходим за нижнюю границу
        if (sourceY + adjustedHeight > fullTexture.height)
        {
            adjustedHeight = fullTexture.height - sourceY;
            Debug.LogWarning($"Корректировка высоты: обрезано до {adjustedHeight} (граница текстуры: {fullTexture.height})");
        }
        
        // Проверяем валидность финальных размеров
        if (adjustedWidth <= 0 || adjustedHeight <= 0)
        {
            Debug.LogError($"Неверные размеры после обрезки: {adjustedWidth}×{adjustedHeight}. " +
                          $"Исходные: {spriteWidth}×{spriteHeight}, crop: {cropX}×{cropY}, " +
                          $"spritePos: {spriteX}×{spriteY}, textureSize: {fullTexture.width}×{fullTexture.height}");
            return null;
        }
        
        // Пересчитываем размеры кусочков на основе скорректированных размеров
        sliceWidth = adjustedWidth / cols;
        sliceHeight = adjustedHeight / rows;
        adjustedWidth = sliceWidth * cols; // Гарантируем, что делится нацело
        adjustedHeight = sliceHeight * rows;
        
        // Логирование об обрезке
        if (adjustedWidth != spriteWidth || adjustedHeight != spriteHeight)
        {
            Debug.Log($"Изображение обрезано: {spriteWidth}×{spriteHeight} → {adjustedWidth}×{adjustedHeight} " +
                     $"(обрезано {spriteWidth - adjustedWidth}×{spriteHeight - adjustedHeight} пикселей, " +
                     $"смещение: {cropX}×{cropY}, sourcePos: {sourceX}×{sourceY})");
        }
        
        // Вырезаем обрезанную область спрайта из текстуры
        Texture2D spriteTexture = new Texture2D(adjustedWidth, adjustedHeight, TextureFormat.RGBA32, false);
        // Используем GetPixels напрямую для сохранения максимального качества (float precision)
        Color[] spritePixels = fullTexture.GetPixels(sourceX, sourceY, adjustedWidth, adjustedHeight);
        spriteTexture.SetPixels(spritePixels);
        
        // КРИТИЧНО: Устанавливаем point filtering ДО Apply() для предотвращения размытия
        spriteTexture.filterMode = FilterMode.Point;
        spriteTexture.wrapMode = TextureWrapMode.Clamp;
        spriteTexture.Apply();
        
        Debug.Log($"Размер каждого кусочка: {sliceWidth}×{sliceHeight} пикселей (все кусочки одинакового размера)");
        
        // КРИТИЧНО: Вычисляем pixelsPerUnit ОДИН РАЗ для всех кусочков ДО цикла
        // Это гарантирует одинаковый pixelsPerUnit для всех кусочков, независимо от округлений float в WebGL
        float pixelsPerUnitToUse;
        
        if (targetPixelsPerUnit.HasValue)
        {
            // Вычисляем соотношение размеров: размер кусочка / размер обрезанного спрайта
            float sizeRatioX = (float)sliceWidth / adjustedWidth;
            float sizeRatioY = (float)sliceHeight / adjustedHeight;
            
            // Используем среднее соотношение (обычно они одинаковые для квадратной сетки)
            // Если кусочек в 2 раза меньше, то PPU должен быть в 2 раза меньше,
            // чтобы размер в мировых единицах был таким же
            float sizeRatio = (sizeRatioX + sizeRatioY) / 2f;
            pixelsPerUnitToUse = targetPixelsPerUnit.Value * sizeRatio;
        }
        else
        {
            // Если targetPixelsPerUnit не указан, вычисляем на основе исходного спрайта
            float sizeRatioX = (float)sliceWidth / adjustedWidth;
            float sizeRatioY = (float)sliceHeight / adjustedHeight;
            float sizeRatio = (sizeRatioX + sizeRatioY) / 2f;
            pixelsPerUnitToUse = sourceSprite.pixelsPerUnit * sizeRatio;
        }
        
        Debug.Log($"ImageSlicer: pixelsPerUnit для всех кусочков = {pixelsPerUnitToUse:F6} (sizeRatioX={((float)sliceWidth / adjustedWidth):F6}, sizeRatioY={((float)sliceHeight / adjustedHeight):F6})");
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Все кусочки теперь имеют одинаковый размер (sliceWidth × sliceHeight)
                // Вычисляем координаты: базовый размер * индекс
                int x = col * sliceWidth;
                int y = (rows - 1 - row) * sliceHeight; // Инвертируем Y для Unity координат
                
                // Создаем новую текстуру для кусочка с явным форматом
                Texture2D sliceTexture = new Texture2D(sliceWidth, sliceHeight, TextureFormat.RGBA32, false);
                
                // Используем GetPixels напрямую для сохранения максимального качества
                Color[] pixels = spriteTexture.GetPixels(x, y, sliceWidth, sliceHeight);
                sliceTexture.SetPixels(pixels);
                
                // КРИТИЧНО: Устанавливаем point filtering ДО Apply() для предотвращения размытия и артефактов
                sliceTexture.filterMode = FilterMode.Point;
                sliceTexture.wrapMode = TextureWrapMode.Clamp;
                sliceTexture.Apply();
                
                // Используем ОДИНАКОВЫЙ pixelsPerUnit для всех кусочков (вычислен выше)
                // Создаем спрайт с правильным pixelsPerUnit
                Sprite sliceSprite = Sprite.Create(
                    sliceTexture,
                    new Rect(0, 0, sliceWidth, sliceHeight),
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


