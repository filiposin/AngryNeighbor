using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private float maxPitch = 45f;
    [SerializeField] private float maxYaw = 90f;
    private float lerpSpeed = 5f;
    private float gizmoDistance = 5f;
    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.rotation;
        if (targetCamera == null) targetCamera = Camera.main.transform;
    }
#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            initialRotation = transform.rotation;
    }
#endif

    void Update()
    {
        if (!Application.isPlaying)
        {
            if (transform.rotation != initialRotation)
                transform.rotation = initialRotation;
            return;
        }

        Vector3 dirToCam = (targetCamera.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(dirToCam);

        Quaternion deltaQuat = Quaternion.Inverse(initialRotation) * lookRot;
        Vector3 deltaEuler = deltaQuat.eulerAngles;
        deltaEuler.x = NormalizeAngle(deltaEuler.x);
        deltaEuler.y = NormalizeAngle(deltaEuler.y);

        bool okPitch = Mathf.Abs(deltaEuler.x) <= maxPitch;
        bool okYaw   = Mathf.Abs(deltaEuler.y) <= maxYaw;

        Quaternion targetRot = (okPitch && okYaw)
            ? initialRotation * Quaternion.Euler(deltaEuler)
            : initialRotation;

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, lerpSpeed * Time.deltaTime);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < 4; i++)
        {
            float yaw   = (i < 2) ? -maxYaw : maxYaw;
            float pitch = (i % 2 == 0) ? -maxPitch : maxPitch;
            Vector3 dir = initialRotation * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
            Gizmos.DrawRay(transform.position, dir * gizmoDistance);
        }

        if (targetCamera)
        {
            Gizmos.color = Color.yellow;
            Vector3 camDir = (targetCamera.position - transform.position).normalized;
            Gizmos.DrawRay(transform.position, camDir * gizmoDistance);
        }

        Gizmos.color = Color.red;
        Vector3 forward = initialRotation * Vector3.forward;
        Gizmos.DrawRay(transform.position, forward * gizmoDistance);
    }
}
