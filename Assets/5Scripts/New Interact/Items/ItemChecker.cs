using UnityEngine;

public class ItemChecker : MonoBehaviour
{
    [SerializeField] private GameObject checkedObject;
    public void DeleteChecked()
    {
        if (checkedObject != null)
        {
            Destroy(checkedObject);
            checkedObject = null;
        }
    }
}
