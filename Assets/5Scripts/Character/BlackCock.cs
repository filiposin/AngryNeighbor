using UnityEngine;

public class BlackCock : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animationName = "onload";
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

    [ContextMenu("Play Animation")]
    public void PlayAnimation()
    {
        if (animator != null)
        {
            animator.Play(animationName, 0, 0);
        }
    }
}
