using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FunEasePos : MonoBehaviour
{
    private RectTransform thisTransform;

    [SerializeField] private Vector2 defaultPos; 
    [SerializeField] private Vector2 eventPos;
    [SerializeField] private float speed = 5f;

    private Vector2 targetPos;

    private void Awake()
    {
        thisTransform = GetComponent<RectTransform>();
        defaultPos = thisTransform.anchoredPosition;
        targetPos = defaultPos;
    }

    private void Update()
    {
        thisTransform.anchoredPosition = Vector2.Lerp(
            thisTransform.anchoredPosition, 
            targetPos, 
            Time.deltaTime * speed
        );
    }

    public void MoveToEvent() => targetPos = eventPos;

    public void MoveToDefault() => targetPos = defaultPos;
}