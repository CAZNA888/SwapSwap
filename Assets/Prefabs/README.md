# Prefabs

Префабы для игры:

## PuzzlePiece.prefab (опционально)
Префаб карточки пазла. Если не создан, GameManager создаст карточки автоматически.

**Компоненты:**
- PuzzlePiece
- SpriteRenderer
- BoxCollider2D
- Rigidbody2D (isKinematic = true)
- BorderRenderer
- SwipeHandler

## BorderPart.prefab (опционально)
Префаб части рамки. Если не создан, BorderRenderer создаст части автоматически.

**Компоненты:**
- SpriteRenderer

## Coin.prefab (обязательно)
Префаб монетки для анимации начисления денег.

**Компоненты:**
- SpriteRenderer (со спрайтом монетки)
- MoneyAnimation

**Создание:**
1. Создайте GameObject с SpriteRenderer
2. Добавьте спрайт монетки
3. Добавьте компонент MoneyAnimation
4. Сохраните как префаб в эту папку

