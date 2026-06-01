using UnityEngine;

public class BoxDestroyNew : MonoBehaviour
{
    public string hitId = "";

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Okno")) return;

        var replacer = collision.gameObject.GetComponent<SimpleReplacer>();
        if (replacer != null)
        {
            if (!string.IsNullOrEmpty(hitId))
            {
                replacer.TryReplace(hitId);
            }
            else
            {
                replacer.Replace();
            }
        }
        else
        {
            Destroy(collision.gameObject);
        }
    }
}
