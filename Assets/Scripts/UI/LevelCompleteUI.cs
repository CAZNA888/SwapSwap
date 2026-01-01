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
    private GameManager gameManager; // Добавляем ссылку на GameManager
    private Vector2 moneyTargetPosition;
    private bool isAnimating = false;
    
    void Start()
    {
        moneyManager = FindObjectOfType<MoneyManager>();
        gameManager = FindObjectOfType<GameManager>(); // Находим GameManager
        
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
        
        // Отключаем кнопку, чтобы предотвратить повторные нажатия
        if (nextButton != null)
        {
            nextButton.interactable = false;
        }
        
        // Скрываем панель сразу, чтобы не мешать загрузке
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
        
        // Сразу вызываем загрузку сцены через GameManager
        // GameManager сам обработает анимацию монет и начисление денег
        if (gameManager != null)
        {
            Debug.Log($"Loading next scene. Current scene index: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex}");
            gameManager.ReloadCurrentSceneWithAnimation();
        }
        else
        {
            Debug.LogError("GameManager not found! Cannot reload scene.");
            // Fallback - перезагружаем текущую сцену по имени
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}


