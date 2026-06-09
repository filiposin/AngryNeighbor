using UnityEngine;

public class ItemMelee : ItemBase
{
    [SerializeField] private ItemDefinition meleeItemDef;
    [SerializeField] private LayerMask hittableMasks;

    public override void OnUse()
    {
        var animController = PlayerItemHandler.inst?.animationController;
        if (animController != null)
        {
            animController.PlayUseAnimation(PerformHit);
            return;
        }

        PerformHit();
    }

    private void PerformHit()
    {
        if (!this || !isActiveAndEnabled || holder == null) return;

        Camera cam = Camera.main;
        if (cam == null || PlayerItemHandler.inst == null) return;
        
        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, PlayerItemHandler.inst.interactDistance, hittableMasks))
        {
            Debug.Log("hit hittable layer object:" + hit.collider.name);
            if (hit.collider.TryGetComponent<IHittable>(out var hittable))
            {
                // лучше использовать definition, но оставил meleeItzemDef если ты инспектором задавал другой деф
                string idToUse = (meleeItemDef != null) ? meleeItemDef.id : (definition != null ? definition.id : null);
                if (!string.IsNullOrEmpty(idToUse))
                    hittable.TryReplace(idToUse);
            }
        }
    }
}
