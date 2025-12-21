using UnityEngine;
using System.Collections.Generic;

public class ImageSlicer : MonoBehaviour
{
    public List<Sprite> SliceImage(Sprite sourceSprite, int rows, int cols)
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
        
        // Вырезаем только область спрайта из текстуры (368×512, а не 512×512)
        Texture2D spriteTexture = new Texture2D(spriteWidth, spriteHeight);
        Color[] spritePixels = fullTexture.GetPixels(spriteX, spriteY, spriteWidth, spriteHeight);
        spriteTexture.SetPixels(spritePixels);
        spriteTexture.Apply();
        
        // Теперь нарезаем обрезанный спрайт на кусочки
        int sliceWidth = spriteWidth / cols;  // Используем размер спрайта, а не текстуры!
        int sliceHeight = spriteHeight / rows;
        
        Debug.Log($"Размер каждого кусочка: {sliceWidth}×{sliceHeight} пикселей");
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Вычисляем координаты для вырезки (относительно обрезанного спрайта)
                int x = col * sliceWidth;
                int y = (rows - 1 - row) * sliceHeight; // Инвертируем Y для Unity координат
                
                // Создаем новую текстуру для кусочка
                Texture2D sliceTexture = new Texture2D(sliceWidth, sliceHeight);
                
                // Копируем пиксели из обрезанного спрайта
                Color[] pixels = spriteTexture.GetPixels(x, y, sliceWidth, sliceHeight);
                sliceTexture.SetPixels(pixels);
                sliceTexture.Apply();
                
                // Создаем спрайт
                Sprite sliceSprite = Sprite.Create(
                    sliceTexture,
                    new Rect(0, 0, sliceWidth, sliceHeight),
                    new Vector2(0.5f, 0.5f), // Pivot в центре
                    sourceSprite.pixelsPerUnit
                );
                
                sliceSprite.name = $"Slice_{row}_{col}_Index_{row * cols + col}";
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
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );
            
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


