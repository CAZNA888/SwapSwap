using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;
using PlayerPrefs = RedefineYG.PlayerPrefs;
public class AdManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] GameObject noadsicon;
    [SerializeField] GameObject TimerAd;
    void Start()
    {
        noads = PlayerPrefs.GetInt("noads");




        //////////////
        ///////////////

        /////////////

        ///////

        ///////////////

        ///////////////////////////////////////////////




        if (noads == 1)
        {
            noadsicon.SetActive(false);

         if (TimerAd)  
                TimerAd.SetActive(false);   
        }   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnEnable()
    {
        YG2.onPurchaseSuccess += SuccessPurchased;
        YG2.onRewardAdv += OnReward;
    }

    private void OnDisable()
    {
        YG2.onPurchaseSuccess -= SuccessPurchased;
        YG2.onRewardAdv -= OnReward;
    }

    private void SuccessPurchased(string id)
    {



        if (id == "1")
        {
            noads = 1;
            noadsicon.SetActive(false);
            PlayerPrefs.SetInt("noads", noads);

            if (TimerAd)
                TimerAd.SetActive(false);
        }
      
    }
    public int noads;

    public void ShowFullAD()
    {
        if (noads == 0)
        {
            YG2.InterstitialAdvShow();
        }
    }

    public void OpenMenu() 
    {
        SceneManager.LoadScene(0);
    }




    [SerializeField] HintManager HintManager;
 
    // Вызов рекламы за вознаграждение
    public void MyRewardAdvShow(string id)
    {
        YG2.RewardedAdvShow(id);
    }

    // Метод подписан на событие OnReward (ивент вознаграждения)
    private void OnReward(string id)
    {
        // Проверяем ID вознаграждения. Если совпадает с тем ID, с которым вызывали рекламу, то вознаграждаем.
        if (id == "1")
        {
            if (HintManager)
                HintManager.ShowHint();
        }
    }
}
