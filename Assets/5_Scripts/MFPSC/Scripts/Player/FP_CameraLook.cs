using UnityEngine;
using System.Collections;

[RequireComponent(typeof(FP_Controller))]
[RequireComponent(typeof(FP_Input))]
public class FP_CameraLook : MonoBehaviour
{
    public Transform PlayerHead;
    public float LookSensitivity = 2.0F;
    public float ShootSensitivity = 1.0F;
    [Range(-35, -90)]
    public float minimumY = -60.0F;
    [Range(35, 90)]
    public float maximumY = 60.0F;
    public float Smooth = 25;
    public float lookAtSpeed = 5f;

    private Vector2 lookAt;
    private float sensitivity;
    [HideInInspector]
    public float rotationY = 0.0F;

    private float InputX, InputY;

    private FP_Input playerInput;
    private FP_Controller playerController;

    private Transform targetToLook;
    private bool isForcedLooking = false;

    void Awake()
    {
        if (PlayerHead == null)
        {
            Debug.LogError("<color=yellow>[FP_CameraLook]</color> Add player camera pls");
        }
    }

    void Start()
    {
        playerInput = GetComponent<FP_Input>();
        playerController = GetComponent<FP_Controller>();
        if (PlayerPrefs.HasKey("PlayerSensitivity"))
        {
            LookSensitivity = PlayerPrefs.GetFloat("PlayerSensitivity");
        }
    }

    public void LookTo(Transform target)
    {
        targetToLook = target;
        isForcedLooking = true;
    }

    public void StopLook()
    {
        isForcedLooking = false;
        targetToLook = null;
        
        Vector3 currentAngles = PlayerHead.localEulerAngles;
        rotationY = -currentAngles.x;
        if (rotationY < -180) rotationY += 360;
    }

    void Update()
    {
        if (PlayerHead == null)
            return;

        PlayerHead.localPosition = Vector3.Lerp(PlayerHead.localPosition, new Vector3(
            PlayerHead.localPosition.x,
            playerController.controller.center.y + playerController.controller.height / 2 - 0.25F,
            PlayerHead.localPosition.z), 15 * Time.deltaTime);

        if (isForcedLooking && targetToLook != null)
        {
            Vector3 direction = (targetToLook.position - PlayerHead.position).normalized;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                Vector3 bodyEuler = transform.eulerAngles;
                float targetBodyY = targetRotation.eulerAngles.y;
                float newBodyY = Mathf.LerpAngle(bodyEuler.y, targetBodyY, lookAtSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, newBodyY, 0);

                Vector3 headEuler = PlayerHead.localEulerAngles;
                float targetHeadX = targetRotation.eulerAngles.x;
                if (targetHeadX > 180) targetHeadX -= 360;
                
                float currentHeadX = -rotationY; 
                
                float newHeadX = Mathf.LerpAngle(currentHeadX, targetHeadX, lookAtSpeed * Time.deltaTime);
                
                rotationY = -newHeadX;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                
                PlayerHead.localEulerAngles = new Vector3(-rotationY, 0, 0);
            }
            return;
        }

        if (playerController.canControl && !playerController.IsCursorForcedUnlocked)
        {
            switch (playerInput.UseMobileInput)
            {
                case true:
                    InputX = playerInput.LookInput().x;
                    InputY = playerInput.LookInput().y;
                    break;
                case false:
                    InputX = Input.GetAxis("Mouse X") * 10;
                    InputY = Input.GetAxis("Mouse Y") * 10;
                    break;
            }

            sensitivity = LookSensitivity;

            if (!playerInput.UseMobileInput)
            {
                lookAt.x = InputX;
                lookAt.y = InputY;
            }
            else
            {
                lookAt.x = Mathf.Lerp(lookAt.x, InputX, Smooth * Time.deltaTime);
                lookAt.y = Mathf.Lerp(lookAt.y, InputY, Smooth * Time.deltaTime);
            }

            transform.Rotate(0.0F, lookAt.x * (sensitivity / 10), 0.0F);

            rotationY += lookAt.y * (sensitivity / 10);
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
            PlayerHead.localEulerAngles = new Vector3(-rotationY, PlayerHead.localEulerAngles.y, 0.0F);
        }
    }
}