using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip cardDealClip;
    public AudioClip cardFlipClip;
    public AudioClip swipeClip;
    public AudioClip levelCompleteClip;
    public AudioClip connectionClip;
    public AudioClip coinClip; // Звук монетки
    
    [Header("Settings")]
    public float volume = 1f;
    
    private AudioSource audioSource;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.volume = volume;
    }
    
    public void PlayCardDeal()
    {
        PlaySound(cardDealClip);
    }
    
    public void PlayCardFlip()
    {
        PlaySound(cardFlipClip);
    }
    
    public void PlaySwipe()
    {
        PlaySound(swipeClip);
    }
    
    public void PlayLevelComplete()
    {
        PlaySound(levelCompleteClip);
    }
    
    public void PlayConnection()
    {
        PlaySound(connectionClip);
    }
    
    public void PlayCoin()
    {
        PlaySound(coinClip);
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}


