using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject levelCompletePanel;
    public TextMeshProUGUI levelCompleteText;
    public Button nextButton;
    public GameObject coinPrefab;
    public Transform coinSpawnPoint; // Позиция над кнопкой Next
    
    [Header("Settings")]
    public int coinsToSpawn = 15;
    public float coinSpawnDelay = 0.05f;
    public float coinAnimationDuration = 1f;
    
    private MoneyManager moneyManager;
    private Vector2 moneyTargetPosition;
    private bool isAnimating = false;
    
    void Start()
    {
        moneyManager = FindObjectOfType<MoneyManager>();
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClick);
        }
        
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }
    
    public void SetMoneyTargetPosition(Vector2 target)
    {
        moneyTargetPosition = target;
    }
    
    public void ShowLevelComplete()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        
        // Можно добавить fade-in анимацию
        CanvasGroup canvasGroup = levelCompletePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            // Используем DOTween.To для анимации alpha
            DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, 0.5f)
                .SetEase(Ease.OutQuad);
        }
    }
    
    private void OnNextButtonClick()
    {
        if (isAnimating) return;
        
        isAnimating = true;
        StartCoroutine(AnimateCoins());
    }
    
    private IEnumerator AnimateCoins()
    {
        if (coinPrefab == null || coinSpawnPoint == null)
        {
            Debug.LogWarning("Coin prefab or spawn point not set!");
            yield break;
        }
        
        List<GameObject> coins = new List<GameObject>();
        
        // Создаем монетки
        for (int i = 0; i < coinsToSpawn; i++)
        {
            Vector3 spawnPos = coinSpawnPoint.position;
            spawnPos += Random.insideUnitSphere * 0.2f; // Небольшое разброс
            spawnPos.z = 0;
            
            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity, transform);
            coins.Add(coin);
            
            // Анимируем монетку к цели
            MoneyAnimation moneyAnim = coin.GetComponent<MoneyAnimation>();
            if (moneyAnim == null)
            {
                moneyAnim = coin.AddComponent<MoneyAnimation>();
            }
            
            moneyAnim.Initialize(moneyTargetPosition);
            moneyAnim.AnimateToTarget();
            
            yield return new WaitForSeconds(coinSpawnDelay);
        }
        
        // Ждем завершения анимаций
        yield return new WaitForSeconds(coinAnimationDuration);
        
        // Начисляем деньги
        if (moneyManager != null)
        {
            moneyManager.AddMoney(coinsToSpawn);
        }
        
        isAnimating = false;
    }
}


