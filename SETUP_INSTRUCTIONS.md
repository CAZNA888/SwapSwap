# Инструкция по настройке и запуску игры SwapSwap

## Предварительные требования

- Unity 2022.3 или новее
- DOTween (уже установлен в проекте)
- Unity 2D Package (уже установлен)

## Важно: Вертикальная ориентация экрана

Игра разработана для **вертикальной ориентации** с разрешением **1080×1920** (портретная ориентация).

## Шаг 1: Подготовка ресурсов

### 1.1 Изображения

1. **Исходное изображение для пазла:**

   - Поместите изображение в `Assets/Sprites/PuzzleImages/`
   - Импортируйте как Sprite (2D)
   - Рекомендуемый размер: квадратное изображение (например, 1024×1024)
   - В настройках импорта: Texture Type = Sprite (2D and UI)

2. **Обратная сторона карточки:**

   - Поместите спрайт в `Assets/Sprites/CardBack/`
   - Импортируйте как Sprite (2D)
   - Размер должен соответствовать размеру карточки

3. **Рамка карточки:**

   - Поместите спрайт в `Assets/Sprites/Borders/`
   - Импортируйте как Sprite (2D)
   - **Важно**: Рамка должна иметь прозрачную середину и рамку по краям
   - Размер должен соответствовать размеру карточки

4. **Спрайт монетки:**
   - Поместите спрайт в `Assets/Sprites/UI/`
   - Импортируйте как Sprite (2D)
   - Будет использован для анимации монеток

### 1.2 Звуки

Поместите MP3 файлы в `Assets/Audio/`:

- `CardDeal.mp3` - звук раздачи карты
- `CardFlip.mp3` - звук переворота карты
- `Swipe.mp3` - звук свайпа
- `LevelComplete.mp3` - звук победы

**Настройки импорта аудио:**

- Compression Format: Compressed
- Quality: 70% (для оптимизации)
- Load Type: Compressed In Memory

## Шаг 2: Настройка сцены

### 2.1 Основная камера

1. Откройте сцену `Assets/Scenes/SampleScene.unity`
2. Убедитесь, что камера настроена:
   - **Projection**: Orthographic
   - **Size**: 5-6 (подберите под размер поля для вертикального экрана)
   - **Background**: Выберите цвет фона
   - **Aspect**: Убедитесь, что камера настроена для портретной ориентации

### 2.2 Создание GameManager

1. Создайте пустой GameObject: `GameObject → Create Empty`
2. Назовите его `GameManager`
3. Добавьте компонент `GameManager` (Scripts/Core/GameManager.cs)

### 2.3 Настройка GameManager в инспекторе

**Puzzle Settings:**

- `Source Image` - перетащите исходное изображение из `Sprites/PuzzleImages/`
- `Card Back Sprite` - перетащите обратную сторону из `Sprites/CardBack/`
- `Border Sprite` - перетащите рамку из `Sprites/Borders/`
- `Grid Rows` - количество строк (например, 3)
- `Grid Cols` - количество столбцов (например, 3)
- `Field Width` - ширина поля в мировых единицах (например, 10)
- `Field Height` - высота поля в мировых единицах (например, 10)
- `Card Spacing` - отступ между карточками (например, 0.1)

**Positions:**

- `Deck Position` - позиция колоды (X: 5, Y: -5)
- `Money Target Position` - точка для монеток (X: -8, Y: 4)

**Timing:**

- `Deal Delay` - задержка между раздачей карточек (0.1)
- `Flip Delay` - задержка между переворотами (0.05)

**Prefabs (опционально):**

- `Puzzle Piece Prefab` - можно оставить пустым (создастся автоматически)
- `Border Part Prefab` - можно оставить пустым

### 2.4 Создание AudioManager

1. Создайте пустой GameObject: `GameObject → Create Empty`
2. Назовите его `AudioManager`
3. Добавьте компонент `AudioManager` (Scripts/Managers/AudioManager.cs)
4. Добавьте компонент `Audio Source` (автоматически добавится)
5. В инспекторе AudioManager:
   - Перетащите звуковые клипы из `Assets/Audio/`
   - `Card Deal Clip` → CardDeal.mp3
   - `Card Flip Clip` → CardFlip.mp3
   - `Swipe Clip` → Swipe.mp3
   - `Level Complete Clip` → LevelComplete.mp3
   - `Volume` = 1.0

### 2.5 Создание MoneyManager

1. Создайте пустой GameObject: `GameObject → Create Empty`
2. Назовите его `MoneyManager`
3. Добавьте компонент `MoneyManager` (Scripts/Managers/MoneyManager.cs)

### 2.6 Создание UI Canvas

1. Создайте Canvas: `GameObject → UI → Canvas`
2. Настройки Canvas:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler**: Scale With Screen Size
   - **Reference Resolution**: **1080×1920** (вертикальная ориентация!)
   - **Match**: Width or Height (0.5)
   - **Screen Match Mode**: Match Width Or Height

### 2.7 Создание MoneyDisplay

1. На Canvas создайте TextMeshPro: `GameObject → UI → Text - TextMeshPro`
2. Назовите его `MoneyDisplay`
3. Добавьте компонент `MoneyDisplay` (Scripts/UI/MoneyDisplay.cs)
4. В инспекторе MoneyDisplay:
   - Перетащите TextMeshProUGUI компонент в поле `Money Text`
   - Настройте позицию (например, верхний левый угол)
   - Настройте стиль текста

### 2.8 Создание LevelCompleteUI

1. На Canvas создайте панель: `GameObject → UI → Panel`
2. Назовите ее `LevelCompletePanel`
3. Добавьте компонент `Canvas Group` для fade эффекта
4. Добавьте компонент `LevelCompleteUI` (Scripts/UI/LevelCompleteUI.cs)
5. Создайте дочерние элементы:

   - **Text "Level Completed"**: `GameObject → UI → Text - TextMeshPro`
   - **Button "Next"**: `GameObject → UI → Button - TextMeshPro`
   - **Coin Spawn Point**: `GameObject → Create Empty` (для позиции спавна монеток)

6. В инспекторе LevelCompleteUI:
   - `Level Complete Panel` → перетащите панель
   - `Level Complete Text` → перетащите текст
   - `Next Button` → перетащите кнопку
   - `Coin Prefab` → создайте префаб монетки (см. ниже)
   - `Coin Spawn Point` → перетащите точку спавна
   - `Coins To Spawn` = 15

### 2.9 Создание префаба монетки

1. Создайте GameObject: `GameObject → Create Empty`
2. Назовите его `Coin`
3. Добавьте компонент `SpriteRenderer`
4. Перетащите спрайт монетки в SpriteRenderer
5. Добавьте компонент `MoneyAnimation` (Scripts/Animations/MoneyAnimation.cs)
6. Перетащите объект в `Assets/Prefabs/` для создания префаба
7. Удалите объект со сцены (префаб останется)

### 2.10 Создание ConfettiEffect (опционально)

1. Создайте пустой GameObject: `GameObject → Create Empty`
2. Назовите его `ConfettiEffect`
3. Добавьте компонент `ConfettiEffect` (Scripts/Managers/ConfettiEffect.cs)

## Шаг 3: Настройка физики и коллайдеров

### 3.1 EventSystem

1. Убедитесь, что на сцене есть EventSystem:
   - `GameObject → UI → Event System`
   - Если нет - создайте автоматически при создании Canvas

### 3.2 Physics2D настройки

1. Проверьте настройки Physics2D:
   - `Edit → Project Settings → Physics 2D`
   - Убедитесь, что `Queries Start In Colliders` включено

## Шаг 4: Тестирование

### 4.1 Первый запуск

1. Нажмите **Play** в Unity Editor
2. Проверьте:
   - Изображение разрезается на части
   - Карточки раздаются из колоды
   - Карточки переворачиваются
   - Можно свайпать карточки
   - Звуки воспроизводятся

### 4.2 Проверка соединений

1. Переместите карточки так, чтобы они были рядом и в правильном порядке
2. Проверьте, что границы рамок исчезают при соединении

### 4.3 Проверка победы

1. Соберите пазл правильно
2. Проверьте:
   - Появляется UI "Level Completed"
   - Воспроизводится звук победы
   - Кнопка Next работает
   - Монетки анимируются
   - Деньги начисляются

## Шаг 5: Оптимизация (опционально)

### 5.1 Sprite Atlas

1. Создайте Sprite Atlas: `Assets → Create → Sprite Atlas`
2. Добавьте все спрайты карточек в Atlas
3. Это уменьшит количество draw calls

### 5.2 Object Pooling

Можно добавить object pooling для карточек и монеток для лучшей производительности.

### 5.3 Сжатие аудио

Убедитесь, что все аудио файлы сжаты для WebGL билда.

## Шаг 6: Сборка для WebGL

### 6.1 Настройки билда

1. `File → Build Settings`
2. Выберите платформу **WebGL**
3. Настройки:
   - **Compression Format**: Gzip
   - **Memory Size**: 32 MB (или больше при необходимости)

### 6.2 Оптимизация для веба

1. Уменьшите разрешение спрайтов если нужно
2. Используйте сжатые аудио форматы
3. Минимизируйте количество объектов на сцене

## Частые проблемы и решения

### Проблема: Карточки не появляются

**Решение:**

- Проверьте, что `Source Image` установлен в GameManager (это изображение будет разрезано)
- Проверьте размеры сетки (gridRows, gridCols)
- Проверьте размеры поля (fieldWidth, fieldHeight)
- Проверьте консоль Unity на наличие ошибок при разрезании изображения

### Проблема: Карточки появляются сразу на сетке, а не в колоде

**Решение:**

- Убедитесь, что в `PuzzlePiece.cs` есть методы `SetDeckPosition()` и `SetGridCoordinates()`
- Проверьте, что в `GameManager.CreatePuzzlePieces()` вызывается `piece.SetDeckPosition(deckPos)`
- Проверьте, что в `CardDealer.DealCards()` используется `SetGridCoordinates()` вместо `SetPosition()`
- Убедитесь, что все скрипты сохранены и скомпилированы

### Проблема: Свайпы не работают

**Решение:**

- Убедитесь, что на карточках есть Collider2D (добавляется автоматически)
- Проверьте, что EventSystem присутствует на сцене
- Проверьте, что камера видит карточки

### Проблема: Звуки не воспроизводятся

**Решение:**

- Проверьте, что AudioManager создан и настроен
- Проверьте, что AudioSource компонент присутствует
- Проверьте громкость в AudioManager

### Проблема: Монетки не анимируются

**Решение:**

- Проверьте, что Coin Prefab установлен в LevelCompleteUI
- Проверьте, что moneyTargetPosition установлен в GameManager
- Проверьте, что LevelCompleteUI.SetMoneyTargetPosition() вызван

### Проблема: Соединения не работают

**Решение:**

- Проверьте, что BorderRenderer создан на карточках
- Проверьте, что borderSprite установлен в GameManager
- Проверьте логику проверки соединений в ConnectionManager

## Дополнительные настройки

### Изменение размера сетки

1. Измените `Grid Rows` и `Grid Cols` в GameManager
2. Подберите `Field Width` и `Field Height` под новый размер (учитывайте вертикальную ориентацию)
3. Перезапустите игру

### Настройка для вертикального экрана (1080×1920)

**GameManager:**

- `Field Width`: 5-7 (ширина поля в мировых единицах)
- `Field Height`: 9-11 (высота поля в мировых единицах)
- `Deck Position`: X: 4-5, Y: -8 до -9 (правый нижний угол)
- `Money Target Position`: X: -4 до -5, Y: 8-9 (верхняя часть экрана, где счет денег)

**Camera:**

- `Orthographic Size`: 5-6 (подберите так, чтобы поле помещалось на экран)

**Canvas:**

- `Reference Resolution`: 1080×1920 (обязательно вертикальная ориентация!)

### Изменение скорости анимаций

1. Измените `Deal Delay` и `Flip Delay` в GameManager
2. Измените `Deal Duration` в CardDealer
3. Измените `Flip Duration` в CardFlipAnimator

### Изменение количества монеток

1. Измените `Coins To Spawn` в LevelCompleteUI
2. Измените вызов `AddMoney()` в LevelCompleteUI.OnNextButtonClick()

## Готово!

Игра должна быть полностью настроена и готова к запуску. Если возникнут проблемы, проверьте логи Unity Console и убедитесь, что все компоненты правильно настроены в инспекторе.
