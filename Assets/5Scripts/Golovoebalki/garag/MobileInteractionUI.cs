using UnityEngine;
using UnityEngine.UI;

public class MobileInteractionUI : MonoBehaviour
{
    public bool isAndroid = false;
    [Header("UI")]
    [SerializeField] private Button actionButton; 
    [SerializeField] private GameObject buttonVisuals; 

    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 3f; 
    [SerializeField] private LayerMask interactLayer; 
    [SerializeField] private float rayInterval = 0.1f; 

    private Camera cam;
    private HydraulicJackItem currentTargetJack;
    private float timer;

    private void Start()
    {
        actionButton.onClick.AddListener(OnActionButtonClick);
        buttonVisuals.SetActive(false);

        // Определяем платформу
#if UNITY_ANDROID && !UNITY_EDITOR
        isAndroid = true;
#else
        // Если тестируем в редакторе, можно переключать галочку isAndroid вручную,
        // но по дефолту считаем, что ПК
        isAndroid = false; 
#endif
    }

    private void Update()
    {
        if (PlayerItemHandler.inst == null) return;
        cam = PlayerItemHandler.inst.playerCamera;

        timer += Time.deltaTime;
        if (timer >= rayInterval)
        {
            timer = 0f;
            PerformRaycast();
        }

        // Логика для ПК (Mouse0) работает независимо от видимости кнопки
        if (currentTargetJack != null && Input.GetKeyDown(KeyCode.Mouse0) && !isAndroid)
        {
            currentTargetJack.PumpIt();
        }
    }

    private void PerformRaycast()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        bool foundTarget = false;

        if (Physics.Raycast(ray, out hit, rayDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent<HydraulicJackItem>(out var jack))
            {
                if (jack.IsPlacedInSocket() && jack.holder == null)
                {
                    currentTargetJack = jack; // Цель запоминаем всегда (и для ПК, и для Андроид)
                    foundTarget = true;
                }
            }
        }

        // === ИЗМЕНЕНИЕ ЗДЕСЬ ===
        if (foundTarget)
        {
            // Показываем кнопку ТОЛЬКО если это Андроид
            if (isAndroid)
            {
                if (!buttonVisuals.activeSelf) buttonVisuals.SetActive(true);
            }
            // Если ПК (isAndroid == false), кнопка останется выключенной (SetActive(true) не сработает)
        }
        else
        {
            // Если цель потеряли — скрываем кнопку и сбрасываем цель
            if (buttonVisuals.activeSelf) buttonVisuals.SetActive(false);
            currentTargetJack = null;
        }
    }

    private void OnActionButtonClick()
    {
        if (currentTargetJack != null)
        {
            currentTargetJack.PumpIt();
        }
    }
}