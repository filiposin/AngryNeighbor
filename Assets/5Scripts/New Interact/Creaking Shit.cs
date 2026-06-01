using UnityEngine;

public class CreakingShit : MonoBehaviour
{
    [Header("Settings")]
    // Если галочка стоит - этот пол будет работать ТОЛЬКО в режиме ExtraLoud
    public bool isExtraFloor = false; 
    
    // Радиус звука вынесли в переменную, чтобы менять через настройки
    public float soundRadius = 15f; 

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip creakingClip;

    public float cooldown = 2f; 
    private float lastPlayTime = -Mathf.Infinity;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayCreakingSound();
        }
    }

    public void PlayCreakingSound()
    {
        // Проверка кулдауна
        if (Time.time - lastPlayTime < cooldown)
            return;

        if (audioSource != null && creakingClip != null)
        {
            audioSource.PlayOneShot(creakingClip);
            
            // Используем переменную soundRadius вместо хардкода 15f
            // (Если у тебя есть класс AISoundManager, это сработает)
            AISoundManager.MakeSound(transform.position, soundRadius);
            
            lastPlayTime = Time.time;
        }
    }
}