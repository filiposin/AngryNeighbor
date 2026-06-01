using UnityEngine;

public class DeleteBogdana : MonoBehaviour
{
    public void DeleteBogdan()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
    }
        public void DeleteFPS()
    {
        GameObject[] z = GameObject.FindGameObjectsWithTag("FpsCounter");

        foreach (GameObject v in z)
        {
            Destroy(v);
        }
    }
}
