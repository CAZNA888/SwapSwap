# Инструкция по созданию префаба карточки PuzzlePiece

## Структура префаба

```
PuzzlePiece (GameObject)
├── PuzzlePiece (Component) - основной скрипт
├── PuzzlePieceSetup (Component) - настройка префаба
├── SpriteRenderer (Component) - отображение карточки
├── BoxCollider2D (Component) - коллайдер для свайпов
├── Rigidbody2D (Component) - kinematic, для коллайдера
└── BorderContainer (GameObject) - создается автоматически
    ├── BorderRenderer (Component)
    ├── TopBorder (GameObject)
    │   └── SpriteRenderer
    ├── BottomBorder (GameObject)
    │   └── SpriteRenderer
    ├── LeftBorder (GameObject)
    │   └── SpriteRenderer
    └── RightBorder (GameObject)
        └── SpriteRenderer
```

## Пошаговая инструкция

### Шаг 1: Создание GameObject

1. Откройте сцену или создайте новую
2. Создайте пустой GameObject: `GameObject → Create Empty`
3. Назовите его `PuzzlePiece`

### Шаг 2: Добавление компонентов

Добавьте следующие компоненты на `PuzzlePiece`:

1. **PuzzlePiece** (Scripts/Core/PuzzlePiece.cs)
2. **PuzzlePieceSetup** (Scripts/Core/PuzzlePieceSetup.cs)
3. **SpriteRenderer**
4. **BoxCollider2D**
5. **Rigidbody2D**

### Шаг 3: Настройка PuzzlePieceSetup

В инспекторе компонента `PuzzlePieceSetup`:

1. **Card Size**:
   - Если у вас есть CardBack спрайт, установите размер из него
   - Например, для соотношения 3:4: X = 1, Y = 1.33
   - Или используйте размер из `cardBackSprite.bounds.size`

2. **Components** (перетащите ссылки):
   - `Card Renderer` → перетащите SpriteRenderer компонент
   - `Card Collider` → перетащите BoxCollider2D компонент
   - `Card Rigidbody` → перетащите Rigidbody2D компонент
   - `Border Renderer` → будет создан автоматически

3. **Border Settings**:
   - `Border Sprite` → перетащите спрайт рамки из `Sprites/Borders/`
   - `Border Thickness Percent` → 0.05 (5% от размера карточки)

4. Нажмите правой кнопкой на компоненте `PuzzlePieceSetup`:
   - Выберите `Setup Card` - это настроит все компоненты
   - Выберите `Create Borders` - это создаст границы

### Шаг 4: Настройка SpriteRenderer

1. В компоненте `SpriteRenderer`:
   - `Sprite` → перетащите `Card Back Sprite` (обратная сторона карточки)
   - `Sorting Layer` → Default
   - `Order in Layer` → 0

### Шаг 5: Настройка BoxCollider2D

1. В компоненте `BoxCollider2D`:
   - `Size` → должен автоматически установиться из `Card Size` в PuzzlePieceSetup
   - Если нет - установите вручную (например, X: 1, Y: 1.33)

### Шаг 6: Настройка Rigidbody2D

1. В компоненте `Rigidbody2D`:
   - `Body Type` → Kinematic
   - `Gravity Scale` → 0
   - `Collision Detection` → Discrete

### Шаг 7: Проверка структуры

После выполнения `Create Borders` должна появиться структура:

- `BorderContainer` (дочерний объект)
  - `TopBorder`
  - `BottomBorder`
  - `LeftBorder`
  - `RightBorder`

### Шаг 8: Визуальная проверка

1. Убедитесь, что:
   - Карточка имеет правильный размер (соотношение 3:4)
   - Рамки точно покрывают карточку
   - Все границы видны и правильно расположены

2. Если нужно изменить размер:
   - Измените `Card Size` в PuzzlePieceSetup
   - Нажмите `Create Borders` снова (старые удалятся автоматически)

### Шаг 9: Сохранение префаба

1. Перетащите GameObject `PuzzlePiece` в папку `Assets/Prefabs/`
2. Unity создаст префаб
3. Удалите объект со сцены (префаб останется в папке)

### Шаг 10: Использование в GameManager

1. Откройте GameManager на сцене
2. В инспекторе найдите поле `Puzzle Piece Prefab`
3. Перетащите созданный префаб из `Assets/Prefabs/` в это поле

## Автоматическая настройка размера

Если в GameManager установлен `Card Back Sprite`, размер карточки будет автоматически обновляться из этого спрайта при создании карточек.

## Полезные команды Context Menu

В компоненте `PuzzlePieceSetup` доступны команды:

- **Setup Card** - настраивает все компоненты
- **Create Borders** - создает границы рамки
- **Clear Borders** - удаляет все границы

## Решение проблем

### Проблема: Рамки не создаются

**Решение:**
- Убедитесь, что `Border Sprite` установлен в PuzzlePieceSetup
- Проверьте, что `Card Size` установлен правильно
- Нажмите `Create Borders` снова

### Проблема: Рамки не покрывают карточку

**Решение:**
- Проверьте `Card Size` - должен соответствовать размеру CardBack
- Увеличьте `Border Thickness Percent` если рамки слишком тонкие
- Убедитесь, что Border Sprite имеет правильное соотношение сторон

### Проблема: Размер карточки неправильный

**Решение:**
- Установите правильный `Card Size` в PuzzlePieceSetup
- Или используйте `UpdateSizeFromCardBack()` в коде
- Проверьте размер CardBack спрайта в инспекторе

## Готово!

Теперь префаб готов к использованию. Все карточки будут создаваться с правильными размерами и рамками.

