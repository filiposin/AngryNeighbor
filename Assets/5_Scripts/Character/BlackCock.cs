using UnityEngine;

public class BlackCock : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string darkenAnimationName = "fade_out"; // Название анимации ухода в темноту
    [SerializeField] private string lightenAnimationName = "onload";  // Название анимации выхода из темноты (старая)
    public static BlackCock instance;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of BlackCock detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    [ContextMenu("Play Darken")]
    public void PlayDarkenAnimation()
    {
        if (animator != null)
        {
            animator.Play(darkenAnimationName, 0, 0);
        }
    }

    [ContextMenu("Play Lighten")]
    public void PlayLightenAnimation()
    {
        if (animator != null)
        {
            animator.Play(lightenAnimationName, 0, 0);
        }
    }

    // Оставили для обратной совместимости, если где-то еще вызывается
    [ContextMenu("Play Animation (Legacy)")]
    public void PlayAnimation()
    {
        PlayLightenAnimation();
    }
}
