using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Эффект свечения для UI кнопки - повторяет форму кнопки и создает свечение вокруг нее
/// </summary>
public class ButtonGlowEffect : MonoBehaviour
{
    [Header("Glow Settings")]
    [Tooltip("Цвет свечения")]
    public Color glowColor = new Color(1f, 0.92f, 0.016f, 0.8f);

    [Tooltip("Интенсивность свечения")]
    [Range(0.1f, 3f)]
    public float intensity = 1.5f;

    [Tooltip("Спрайт для свечения (если не указан, будет использоваться спрайт кнопки)")]
    public Sprite glowSprite;

    [Tooltip("Тип изображения для свечения")]
    public Image.Type glowImageType = Image.Type.Simple;

    [Header("Glow Size Settings")]
    [Tooltip("Увеличение ширины свечения относительно кнопки (%)")]
    [Range(0f, 100f)]
    public float widthIncreasePercent = 10f;

    [Tooltip("Увеличение высоты свечения относительно кнопки (%)")]
    [Range(0f, 100f)]
    public float heightIncreasePercent = 10f;

    [Tooltip("Дополнительное увеличение в пикселях")]
    public Vector2 additionalSize = Vector2.zero;

    [Tooltip("Использовать абсолютный размер вместо процентного")]
    public bool useAbsoluteSize = false;

    [Tooltip("Абсолютный размер свечения в пикселях (если useAbsoluteSize = true)")]
    public Vector2 absoluteGlowSize = new Vector2(100f, 100f);

    [Header("Glow Appearance")]
    [Tooltip("Мягкость свечения (больше = мягче)")]
    [Range(0.1f, 2f)]
    public float softness = 1f;

    [Tooltip("Толщина свечения в пикселях")]
    [Range(5f, 50f)]
    public float glowThickness = 20f;

    [Header("Pulse Animation")]
    [Tooltip("Включить пульсацию свечения")]
    public bool enablePulse = true;

    [Tooltip("Минимальная интенсивность при пульсации")]
    [Range(0f, 1f)]
    public float minPulseIntensity = 0.4f;

    [Tooltip("Максимальная интенсивность при пульсации")]
    [Range(0f, 2f)]
    public float maxPulseIntensity = 1f;

    [Tooltip("Скорость пульсации")]
    public float pulseSpeed = 2f;

    [Header("Performance")]
    [Tooltip("Качество свечения (больше = лучше, но тяжелее)")]
    [Range(1, 10)]
    public int quality = 4;

    [Tooltip("Обновлять размер автоматически")]
    public bool autoUpdateSize = true;

    private GameObject glowContainer;
    private Image[] glowImages;
    private Coroutine pulseCoroutine;
    private bool isGlowing = false;
    private RectTransform buttonRect;
    private Image buttonImage;
    private Vector2 lastButtonSize;

    void Start()
    {
        buttonRect = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();
        if (buttonRect != null)
        {
            lastButtonSize = buttonRect.rect.size;
        }

        // Автоматический запуск для тестирования
        //StartGlow(); // Раскомментируйте для теста
    }

    /// <summary>
    /// Рассчитывает размер свечения на основе настроек
    /// </summary>
    private Vector2 CalculateGlowSize()
    {
        if (buttonRect == null) return absoluteGlowSize;

        Vector2 buttonSize = buttonRect.rect.size;

        if (useAbsoluteSize)
        {
            // Используем абсолютный размер
            return absoluteGlowSize;
        }
        else
        {
            // Используем процентное увеличение
            float width = buttonSize.x * (1f + widthIncreasePercent / 100f);
            float height = buttonSize.y * (1f + heightIncreasePercent / 100f);

            // Добавляем дополнительный размер
            return new Vector2(width, height) + additionalSize;
        }
    }

    /// <summary>
    /// Запускает эффект свечения
    /// </summary>
    public void StartGlow()
    {
        if (isGlowing) return;

        CreateGlowEffect();

        isGlowing = true;
        glowContainer.SetActive(true);

        if (enablePulse)
        {
            pulseCoroutine = StartCoroutine(PulseRoutine());
        }
        else
        {
            UpdateGlowIntensity(1f);
        }

        Debug.Log("ButtonGlowEffect: Glow started");
    }

    /// <summary>
    /// Останавливает эффект свечения
    /// </summary>
    public void StopGlow()
    {
        if (!isGlowing) return;

        isGlowing = false;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (glowContainer != null)
        {
            glowContainer.SetActive(false);
        }

        Debug.Log("ButtonGlowEffect: Glow stopped");
    }

    /// <summary>
    /// Создаёт эффект свечения вокруг кнопки
    /// </summary>
    private void CreateGlowEffect()
    {
        // Удаляем старый объект если есть
        if (glowContainer != null)
        {
            Destroy(glowContainer);
        }

        if (buttonRect == null) buttonRect = GetComponent<RectTransform>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();

        if (buttonRect == null)
        {
            Debug.LogError("ButtonGlowEffect: No RectTransform found on button!");
            return;
        }

        // Рассчитываем размер свечения
        Vector2 calculatedGlowSize = CalculateGlowSize();

        // Создаём контейнер для свечения
        glowContainer = new GameObject("GlowEffect");

        // Важно: делаем glowContainer дочерним объектом родителя кнопки, а не самой кнопки
        // Это позволяет свечению быть под кнопкой в иерархии
        if (transform.parent != null)
        {
            glowContainer.transform.SetParent(transform.parent);
        }
        else
        {
            glowContainer.transform.SetParent(transform);
        }

        // Устанавливаем позицию и поворот
        glowContainer.transform.localPosition = transform.localPosition;
        glowContainer.transform.localRotation = transform.localRotation;
        glowContainer.transform.localScale = Vector3.one;

        // Помещаем glowContainer ПЕРЕД кнопкой в иерархии (чтобы был под ней при отрисовке)
        int buttonIndex = transform.GetSiblingIndex();
        glowContainer.transform.SetSiblingIndex(buttonIndex);

        // Настраиваем RectTransform glowContainer
        RectTransform glowRect = glowContainer.AddComponent<RectTransform>();

        // Копируем свойства RectTransform кнопки
        glowRect.anchorMin = buttonRect.anchorMin;
        glowRect.anchorMax = buttonRect.anchorMax;
        glowRect.pivot = buttonRect.pivot;
        glowRect.anchoredPosition = buttonRect.anchoredPosition;
        glowRect.sizeDelta = calculatedGlowSize; // Используем рассчитанный размер

        // Добавляем CanvasGroup для управления прозрачностью
        CanvasGroup canvasGroup = glowContainer.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.ignoreParentGroups = true;

        // Создаём несколько слоёв для имитации мягкого свечения
        CreateGlowLayers(glowRect, calculatedGlowSize);

        glowContainer.SetActive(false);

        Debug.Log($"ButtonGlowEffect: Glow effect created with {quality} layers, size: {calculatedGlowSize}");
    }

    /// <summary>
    /// Создаёт слои свечения
    /// </summary>
    private void CreateGlowLayers(RectTransform containerRect, Vector2 baseSize)
    {
        glowImages = new Image[quality];

        for (int i = 0; i < quality; i++)
        {
            GameObject layerObject = new GameObject($"GlowLayer_{i}");
            layerObject.transform.SetParent(containerRect);
            layerObject.transform.localPosition = Vector3.zero;
            layerObject.transform.localRotation = Quaternion.identity;
            layerObject.transform.localScale = Vector3.one;

            // Настраиваем RectTransform слоя
            RectTransform layerRect = layerObject.AddComponent<RectTransform>();
            layerRect.anchorMin = new Vector2(0f, 0f);
            layerRect.anchorMax = new Vector2(1f, 1f);
            layerRect.pivot = new Vector2(0.5f, 0.5f);
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;

            // Добавляем Image компонент
            Image layerImage = layerObject.AddComponent<Image>();
            layerImage.raycastTarget = false;
            layerImage.preserveAspect = false;

            // Используем указанный спрайт или спрайт кнопки
            if (glowSprite != null)
            {
                layerImage.sprite = glowSprite;
                layerImage.type = glowImageType;
            }
            else if (buttonImage != null && buttonImage.sprite != null)
            {
                layerImage.sprite = buttonImage.sprite;
                layerImage.type = buttonImage.type;

                // Если кнопка использует sliced или tiled, копируем настройки
                if (buttonImage.type == Image.Type.Sliced || buttonImage.type == Image.Type.Tiled)
                {
                    layerImage.pixelsPerUnitMultiplier = buttonImage.pixelsPerUnitMultiplier;
                }
            }
            else
            {
                // Если нет спрайта, создаем простой белый квадрат
                layerImage.sprite = CreateSquareSprite();
                layerImage.type = Image.Type.Simple;
            }

            // Настраиваем цвет и размер для этого слоя
            float layerScale = 1f + (i * (softness / quality));
            float layerAlpha = Mathf.Lerp(1f, 0.1f, (float)i / quality);

            // Устанавливаем цвет
            Color layerColor = glowColor;
            layerColor.a *= layerAlpha;
            layerImage.color = layerColor;

            // Рассчитываем размер слоя
            float thicknessMultiplier = glowThickness * i;
            Vector2 layerSize = baseSize + new Vector2(thicknessMultiplier, thicknessMultiplier);

            // Устанавливаем размер слоя
            layerRect.sizeDelta = layerSize;

            glowImages[i] = layerImage;
        }
    }

    /// <summary>
    /// Создаёт простой квадратный спрайт
    /// </summary>
    private Sprite CreateSquareSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[size * size];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    /// <summary>
    /// Корутина пульсации
    /// </summary>
    private IEnumerator PulseRoutine()
    {
        float time = 0f;

        while (isGlowing)
        {
            // Синусоидальная пульсация
            time += Time.deltaTime * pulseSpeed;
            float t = (Mathf.Sin(time) + 1f) / 2f;
            float currentIntensity = Mathf.Lerp(minPulseIntensity, maxPulseIntensity, t);

            UpdateGlowIntensity(currentIntensity);

            yield return null;
        }
    }

    /// <summary>
    /// Обновляет интенсивность свечения
    /// </summary>
    private void UpdateGlowIntensity(float intensityMultiplier)
    {
        if (glowImages == null) return;

        // Обновляем цвета всех слоёв
        for (int i = 0; i < glowImages.Length; i++)
        {
            if (glowImages[i] != null)
            {
                float layerAlpha = Mathf.Lerp(1f, 0.1f, (float)i / glowImages.Length);
                Color layerColor = glowColor * intensity * intensityMultiplier;
                layerColor.a = glowColor.a * layerAlpha * intensityMultiplier;
                glowImages[i].color = layerColor;
            }
        }
    }

    /// <summary>
    /// Обновляет размер и позицию свечения при изменении кнопки
    /// </summary>
    public void UpdateGlowTransform()
    {
        if (glowContainer == null || buttonRect == null) return;

        RectTransform glowRect = glowContainer.GetComponent<RectTransform>();
        if (glowRect != null)
        {
            // Обновляем позицию
            glowRect.anchoredPosition = buttonRect.anchoredPosition;

            // Рассчитываем новый размер
            Vector2 calculatedGlowSize = CalculateGlowSize();
            glowRect.sizeDelta = calculatedGlowSize;

            // Обновляем размеры всех слоёв
            if (glowImages != null)
            {
                for (int i = 0; i < glowImages.Length; i++)
                {
                    if (glowImages[i] != null)
                    {
                        RectTransform layerRect = glowImages[i].GetComponent<RectTransform>();
                        if (layerRect != null)
                        {
                            float thicknessMultiplier = glowThickness * i;
                            Vector2 layerSize = calculatedGlowSize + new Vector2(thicknessMultiplier, thicknessMultiplier);
                            layerRect.sizeDelta = layerSize;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Обновляет цвет свечения
    /// </summary>
    public void SetGlowColor(Color newColor)
    {
        glowColor = newColor;
        if (isGlowing)
        {
            UpdateGlowIntensity(1f);
        }
    }

    /// <summary>
    /// Обновляет спрайт свечения
    /// </summary>
    public void SetGlowSprite(Sprite newSprite)
    {
        glowSprite = newSprite;
        if (isGlowing && glowImages != null)
        {
            foreach (Image layerImage in glowImages)
            {
                if (layerImage != null)
                {
                    layerImage.sprite = newSprite;
                }
            }
        }
    }

    /// <summary>
    /// Пересоздаёт эффект с новыми параметрами
    /// </summary>
    public void RefreshGlowEffect()
    {
        if (isGlowing)
        {
            StopGlow();
            StartGlow();
        }
    }

    /// <summary>
    /// Проверяет, изменился ли размер кнопки
    /// </summary>
    private bool HasButtonSizeChanged()
    {
        if (buttonRect == null) return false;

        Vector2 currentSize = buttonRect.rect.size;
        bool hasChanged = currentSize != lastButtonSize;

        if (hasChanged)
        {
            lastButtonSize = currentSize;
        }

        return hasChanged;
    }

    /// <summary>
    /// Проверяет, активно ли свечение
    /// </summary>
    public bool IsGlowing()
    {
        return isGlowing;
    }

    void Update()
    {
        // Автоматическое обновление трансформа при изменении кнопки
        if (autoUpdateSize && isGlowing && glowContainer != null && HasButtonSizeChanged())
        {
            UpdateGlowTransform();
        }
    }

    void OnDisable()
    {
        StopGlow();
    }

    void OnDestroy()
    {
        // Очищаем созданные ресурсы
        if (glowContainer != null)
        {
            Destroy(glowContainer);
        }
    }

    /// <summary>
    /// Обновляет свечение при изменении параметров в инспекторе
    /// </summary>
    void OnValidate()
    {
        // Ограничиваем значения
        quality = Mathf.Clamp(quality, 1, 10);
        widthIncreasePercent = Mathf.Max(0, widthIncreasePercent);
        heightIncreasePercent = Mathf.Max(0, heightIncreasePercent);

        // Если свечение активно, пересоздаём его с новыми параметрами
        if (isGlowing && Application.isPlaying)
        {
            RefreshGlowEffect();
        }
    }

    // Метод для быстрого тестирования из инспектора
    [ContextMenu("Test Glow")]
    public void TestGlow()
    {
        if (isGlowing)
        {
            StopGlow();
        }
        else
        {
            StartGlow();
        }
    }

    [ContextMenu("Refresh Glow")]
    public void RefreshFromInspector()
    {
        RefreshGlowEffect();
    }

    [ContextMenu("Update Glow Size")]
    public void UpdateSizeFromInspector()
    {
        if (isGlowing)
        {
            UpdateGlowTransform();
        }
    }
}