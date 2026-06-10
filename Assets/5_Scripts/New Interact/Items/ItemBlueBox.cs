using UnityEngine;

public class ItemBlueBox : ItemBase
{
    public override void OnThrow(Vector3 velocity)
    {
        base.OnThrow(velocity);
        
        // Синяя коробка летит строго прямо, не падая вниз
        if (rb != null)
        {
            rb.useGravity = false;
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        
        // При ударе обо что-то можно вернуть гравитацию, чтобы она нормально упала на пол
        if (rb != null && !rb.useGravity && holder == null)
        {
            rb.useGravity = true;
        }
    }
}
