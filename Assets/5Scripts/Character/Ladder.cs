using UnityEngine;

public class Ladder : MonoBehaviour
{
    private FP_Controller currentController = null;

    private void OnTriggerEnter(Collider other)
    {
       if (!other.CompareTag("Player")) return;
       
       if (other.TryGetComponent<FP_Controller>(out FP_Controller controller))
       {
           currentController = controller;
           currentController.OnLadderEnter();
       }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (currentController == null)
        {
            if (other.TryGetComponent<FP_Controller>(out currentController))
            {
                currentController.OnLadderEnter();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
       if (!other.CompareTag("Player")) return;

       FP_Controller controller = other.GetComponent<FP_Controller>();
       
       if (controller != null)
       {
          controller.OnLadderExit();
       }

       if (currentController == controller)
       {
           currentController = null;
       }
    }
}