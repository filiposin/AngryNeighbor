using UnityEngine;

public class SocketPlaceableItem : ItemBase
{
    [Header("Socket Detection")]
    [SerializeField] private float reachDistance = 4f;
    [SerializeField] private float magnetRadius = 0.5f; 
    [SerializeField] private LayerMask socketLayer;     

    [Header("Visuals")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;
    
    [Header("Ghost Settings")]
    [SerializeField] private Vector3 ghostScaleMultiplier = Vector3.one;

    // --- Внутренние переменные ---
    private GameObject ghostObject;
    private Renderer[] ghostRenderers;
    private Camera playerCamera;
    
    // Ссылка на потенциальный слот (куда смотрим сейчас)
    private PlacementSocket targetSocket;
    
    // Ссылка на слот, в котором ПРЕДМЕТ УЖЕ СТОИТ (если стоит)
    internal PlacementSocket currentInstalledSocket; 

    private bool canPlace = false;

    public override void OnPickup(GameObject holder)
    {
        // === ИСПРАВЛЕНИЕ ===
        // Если предмет стоял в сокете, нужно освободить сокет перед тем как взять предмет
        if (currentInstalledSocket != null)
        {
            currentInstalledSocket.RemoveItem();
            currentInstalledSocket = null;
        }

        // Включаем физику обратно (на всякий случай, хотя ItemBase обычно это делает)
        if (rb) rb.isKinematic = false;

        base.OnPickup(holder); // Вызываем базовую логику подбора
        
        // Инициализация камеры и призрака
        playerCamera = holder.GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        CreateGhostIfNeeded();
        ShowGhost();
    }

    public override void OnDrop()
    {
        base.OnDrop();
        HideGhost();
        ResetPhysicalProperties();
        // При обычном выбрасывании предмет ни к чему не привязан
        currentInstalledSocket = null;
    }

    public override void OnThrow(Vector3 velocity)
    {
        base.OnThrow(velocity);
        HideGhost();
        ResetPhysicalProperties();
        currentInstalledSocket = null;
    }

    public override void OnUse()
    {
        if (canPlace && targetSocket != null)
        {
            PlaceInSocket();
        }
        else
        {
            // Здесь можно добавить звук "ошибки"
            Debug.Log("Некуда ставить!");
        }
    }

    protected virtual void Update()
    {
        // Логика поиска работает только когда предмет в руках
        if (holder == null || ghostObject == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        targetSocket = null;
        canPlace = false;

        // Ищем сокеты сферой
        bool hitSomething = Physics.SphereCast(ray, magnetRadius, out hit, reachDistance, socketLayer);

        if (hitSomething)
        {
            PlacementSocket socket = hit.collider.GetComponent<PlacementSocket>();
            if (socket == null) socket = hit.collider.GetComponentInParent<PlacementSocket>();

            if (socket != null)
            {
                string myId = (definition != null) ? definition.id : "";
                
                // Проверяем, свободен ли сокет и подходит ли предмет
                if (socket.CanAcceptItem(myId))
                {
                    targetSocket = socket;
                    canPlace = true;
                }
            }
        }

        UpdateGhostVisuals(ray);
    }

    private void UpdateGhostVisuals(Ray ray)
    {
        if (!ghostObject.activeSelf) ghostObject.SetActive(true);

        if (canPlace && targetSocket != null)
        {
            // Магнитизм призрака к слоту
            ghostObject.transform.position = targetSocket.snapPoint.position;
            ghostObject.transform.rotation = targetSocket.snapPoint.rotation;
            SetGhostMaterial(validMaterial);
        }
        else
        {
            // Призрак висит перед игроком (нельзя поставить)
            ghostObject.transform.position = ray.GetPoint(2f);
            ghostObject.transform.rotation = Quaternion.LookRotation(playerCamera.transform.forward);
            SetGhostMaterial(invalidMaterial);
        }
    }

    private void PlaceInSocket()
    {
        if (holder != null)
        {
            var handler = holder.GetComponent<PlayerItemHandler>();
            if (handler != null) handler.OnItemPlacedSuccess();
        }

        // 1. Ставим предмет
        transform.position = targetSocket.snapPoint.position;
        transform.rotation = targetSocket.snapPoint.rotation;
        transform.SetParent(null);

        // 2. Отключаем физику
        if (rb)
        {
            rb.isKinematic = true; 
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        SetCollidersEnabled(true);

        // 3. Регистрируем в сокете
        targetSocket.PlaceItem(this.gameObject);
        
        // === ВАЖНО: Запоминаем текущий сокет ===
        currentInstalledSocket = targetSocket;

        // 4. Сброс переменных руки
        holder = null;
        HideGhost();
        
        if (definition != null && definition.itemPrefab != null)
            transform.localScale = definition.itemPrefab.transform.localScale;
    }

    private void ResetPhysicalProperties()
    {
        if (rb) rb.isKinematic = false;
        if (definition != null && definition.itemPrefab != null)
            transform.localScale = definition.itemPrefab.transform.localScale;
    }

    // --- Ghost Helpers ---

    private void CreateGhostIfNeeded()
    {
        if (ghostObject != null) return;

        ghostObject = new GameObject($"{gameObject.name}_SocketGhost");
        Vector3 targetScale = Vector3.one;
        if (definition != null && definition.itemPrefab != null)
            targetScale = definition.itemPrefab.transform.localScale;
        else
            targetScale = transform.localScale;

        targetScale = Vector3.Scale(targetScale, ghostScaleMultiplier);

        MeshFilter[] sourceFilters = GetComponentsInChildren<MeshFilter>();
        foreach (var sourceFilter in sourceFilters)
        {
            GameObject ghostPart = new GameObject(sourceFilter.name);
            ghostPart.transform.SetParent(ghostObject.transform);
            ghostPart.transform.localPosition = sourceFilter.transform.localPosition;
            ghostPart.transform.localRotation = sourceFilter.transform.localRotation;
            ghostPart.transform.localScale = sourceFilter.transform.localScale;

            MeshFilter mf = ghostPart.AddComponent<MeshFilter>();
            mf.sharedMesh = sourceFilter.sharedMesh;
            ghostPart.AddComponent<MeshRenderer>();
        }
        
        ghostObject.transform.localScale = targetScale;
        ghostRenderers = ghostObject.GetComponentsInChildren<Renderer>();
        ghostObject.SetActive(false);
    }

    private void SetGhostMaterial(Material mat)
    {
        if (ghostRenderers == null) return;
        foreach (var r in ghostRenderers)
        {
            if (r.sharedMaterial != mat) r.sharedMaterial = mat;
        }
    }

    private void HideGhost()
    {
        if (ghostObject) ghostObject.SetActive(false);
        canPlace = false;
        targetSocket = null;
    }

    private void ShowGhost()
    {
        if (ghostObject) ghostObject.SetActive(true);
    }

    private void OnDisable() => HideGhost();
    private void OnEnable()
    {
        if (holder != null) ShowGhost();
    }
    private void OnDestroy() { if (ghostObject) Destroy(ghostObject); }
}
