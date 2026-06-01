using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FunEaseScale : MonoBehaviour
{
    [SerializeField]private RectTransform thisTransform;
    
    [SerializeField]private bool useThisTransform = true;
    [SerializeField] private Vector3 defaultScale = Vector3.one; 
    [SerializeField] private Vector3 eventScale = Vector3.one * 1.2f;
    [SerializeField] private float speed = 5f;

    private Vector3 targetScale;

    private void Awake()
    {
        if(useThisTransform) thisTransform = GetComponent<RectTransform>();
        defaultScale = thisTransform.localScale;
        targetScale = defaultScale;
    }

    private void Update()
    {
        thisTransform.localScale = Vector3.Lerp(
            thisTransform.localScale,
            targetScale,
            Time.deltaTime * speed
        );
    }

    public void ScaleToEvent()
    {
        targetScale = eventScale;
    }

    public void ScaleToDefault() => targetScale = defaultScale;
}