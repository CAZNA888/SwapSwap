using UnityEngine;
using YG;
using PlayerPrefs = RedefineYG.PlayerPrefs;
public class MoneyManager : MonoBehaviour
{
    private const string MONEY_KEY = "PlayerMoney";
    
    private int currentMoney = 0;
    
    public System.Action<int> OnMoneyChanged;
    
    void Start()
    {
        LoadMoney();
    }
    
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        SaveMoney();
        OnMoneyChanged?.Invoke(currentMoney);
    }
    
    public int GetMoney()
    {
        return currentMoney;
    }
    
    public void SaveMoney()
    {
        PlayerPrefs.SetInt(MONEY_KEY, currentMoney);
        PlayerPrefs.Save();


        YG2.SetLeaderboard( "money", currentMoney);
    }
    
    public void LoadMoney()
    {
        currentMoney = PlayerPrefs.GetInt(MONEY_KEY, 0);
        OnMoneyChanged?.Invoke(currentMoney);
    }
    
    public void SetMoney(int amount)
    {
        currentMoney = amount;
        SaveMoney();
        OnMoneyChanged?.Invoke(currentMoney);
    }
}


