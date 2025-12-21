using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoneyDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI moneyText;
    public Text moneyTextLegacy; // Для обычного Text, если не используется TMP

    private MoneyManager moneyManager;

    void Start()
    {
        moneyManager = FindObjectOfType<MoneyManager>();
        if (moneyManager != null)
        {
            moneyManager.OnMoneyChanged += UpdateDisplay;
            UpdateDisplay(moneyManager.GetMoney());
        }
    }

    void OnDestroy()
    {
        if (moneyManager != null)
        {
            moneyManager.OnMoneyChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(int amount)
    {
        string moneyString = amount.ToString();

        if (moneyText != null)
        {
            moneyText.text = moneyString;
        }
        else if (moneyTextLegacy != null)
        {
            moneyTextLegacy.text = moneyString;
        }
    }
}


