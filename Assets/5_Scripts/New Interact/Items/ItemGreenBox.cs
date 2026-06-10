using System.Collections;
using UnityEngine;

public class ItemGreenBox : ItemBase
{
    private Collider[] myCols;

    protected override void Awake()
    {
        base.Awake();
        myCols = GetComponentsInChildren<Collider>(true);
    }

    public void OnPlaced(GameObject playerObj)
    {
        // Убираем гравитацию и делаем кинематичным, чтобы он висел в воздухе (как было без rigidbody)
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Временно выключаем коллизию с игроком, чтобы не выталкивало физикой
        var playerCols = playerObj.GetComponentsInChildren<Collider>();
        foreach (var pCol in playerCols)
        {
            foreach (var mCol in myCols)
            {
                if (pCol != null && mCol != null)
                    Physics.IgnoreCollision(pCol, mCol, true);
            }
        }

        StartCoroutine(RestoreCollisionRoutine(playerObj.transform, playerCols));
    }

    private IEnumerator RestoreCollisionRoutine(Transform playerTr, Collider[] playerCols)
    {
        float boxTopY = transform.position.y + 0.5f;
        float boxRadius = 0.5f; // Примерный радиус коробки по XZ
        
        if (myCols.Length > 0 && myCols[0] != null)
        {
            boxTopY = myCols[0].bounds.max.y;
            boxRadius = Mathf.Max(myCols[0].bounds.extents.x, myCols[0].bounds.extents.z);
        }

        // Ждем пока игрок поднимется выше коробки ИЛИ отойдет в сторону
        while (playerTr != null)
        {
            bool isAboveBox = playerTr.position.y >= (boxTopY - 0.1f);
            
            // Считаем дистанцию только по XZ (игнорируем высоту)
            Vector2 playerXZ = new Vector2(playerTr.position.x, playerTr.position.z);
            Vector2 boxXZ = new Vector2(transform.position.x, transform.position.z);
            bool isOutsideBox = Vector2.Distance(playerXZ, boxXZ) > (boxRadius + 0.5f); // 0.5f - запас на радиус игрока

            if (isAboveBox || isOutsideBox)
            {
                break;
            }

            yield return null;
        }

        // Возвращаем коллизию
        if (playerTr != null)
        {
            foreach (var pCol in playerCols)
            {
                foreach (var mCol in myCols)
                {
                    if (pCol != null && mCol != null)
                        Physics.IgnoreCollision(pCol, mCol, false);
                }
            }
        }
    }
}
