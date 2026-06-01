using UnityEngine;

public class PlaceableItem : ItemBase
{
    [Header("Placement Settings")]
    [SerializeField] private float maxPlaceDistance = 5f;
    [SerializeField] private LayerMask floorLayer;     // Слой поверхностей
    [SerializeField] private Material validMaterial;   // Зеленый мат
    [SerializeField] private Material invalidMaterial; // Красный мат
    
    [Tooltip("Если true - предмет всегда вертикален (по Y). False - липнет к стенам.")]
    [SerializeField] private bool rotateToPlayer = true; 

    [Tooltip("Дополнительное смещение вверх/вниз вручную, если авто-расчет ошибается")]
    [SerializeField] private float manualHeightOffset = 0f;

    [Header("Ghost Settings")]
    [Tooltip("Множитель размера призрака. (1,1,1) - стандарт.")]
    [SerializeField] private Vector3 ghostScaleMultiplier = Vector3.one;

    [Tooltip("Дополнительный поворот призрака/предмета (в градусах).")]
    [SerializeField] private Vector3 placementRotationOffset = Vector3.zero;

    // Внутренние переменные
    private GameObject ghostObject;
    private Renderer[] ghostRenderers;
    private Camera playerCamera;
    private Transform camTransform;
    
    private bool canPlaceCurrentFrame = false;
    private float calculatedBottomOffset = 0f;

    protected override void Awake()
    {
        base.Awake();
        CalculatePivotOffset();
    }

    private void CalculatePivotOffset()
    {
        var cols = GetComponentsInChildren<Collider>();
        if (cols == null || cols.Length == 0) return;

        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (var c in cols)
        {
            if (c.isTrigger) continue;
            if (!hasBounds)
            {
                bounds = c.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        if (hasBounds)
        {
            calculatedBottomOffset = transform.position.y - bounds.min.y;
        }
    }

    private void CreateGhostIfNeeded()
    {
        if (ghostObject != null) return;

        ghostObject = new GameObject($"{gameObject.name}_Ghost");
        
        Vector3 targetScale = Vector3.one;
        if (definition != null && definition.itemPrefab != null)
        {
            targetScale = definition.itemPrefab.transform.localScale;
        }
        else
        {
            targetScale = InitialScale; 
        }

        // Применяем множитель размера
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

    public override void OnPickup(GameObject holder)
    {
        base.OnPickup(holder);
        playerCamera = holder.GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        camTransform = playerCamera.transform;

        CreateGhostIfNeeded();
        ghostObject.SetActive(true);
    }

    public override void OnUse()
    {
        if (canPlaceCurrentFrame)
        {
            PlaceItem();
        }
        else
        {
            Debug.Log("Здесь нельзя поставить!");
        }
    }

    public override void OnDrop()
    {
        base.OnDrop();
        HideGhost();
        FixScaleOnDrop();
    }

    public override void OnThrow(Vector3 velocity)
    {
        base.OnThrow(velocity);
        HideGhost();
        FixScaleOnDrop();
    }

    private void Update()
    {
        if (holder == null || ghostObject == null || !ghostObject.activeSelf) return;

        Ray ray = new Ray(camTransform.position, camTransform.forward);
        RaycastHit hit;

        // Определяем базовый поворот (от игрока или от поверхности)
        Quaternion baseRotation;
        
        bool hitSomething = Physics.Raycast(ray, out hit, maxPlaceDistance, floorLayer);

        if (hitSomething)
        {
            // --- РАСЧЕТ ПОВОРОТА ---
            if (rotateToPlayer)
            {
                baseRotation = Quaternion.Euler(0, holder.transform.eulerAngles.y, 0);
            }
            else
            {
                baseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }

            // Добавляем ручной оффсет поворота
            ghostObject.transform.rotation = baseRotation * Quaternion.Euler(placementRotationOffset);


            // --- РАСЧЕТ ПОЗИЦИИ ---
            Vector3 upDirection = rotateToPlayer ? Vector3.up : hit.normal;
            float totalOffset = calculatedBottomOffset + manualHeightOffset;
            ghostObject.transform.position = hit.point + (upDirection * totalOffset);


            // --- ПРОВЕРКА ВАЛИДНОСТИ ---
            bool isFlatSurface = Vector3.Angle(hit.normal, Vector3.up) < 45f;
            
            if (isFlatSurface)
            {
                SetGhostMaterial(validMaterial);
                canPlaceCurrentFrame = true;
            }
            else
            {
                SetGhostMaterial(invalidMaterial);
                canPlaceCurrentFrame = false;
            }
        }
        else
        {
            // Если луч никуда не попал, держим предмет перед собой
            SetGhostMaterial(invalidMaterial);
            ghostObject.transform.position = ray.GetPoint(maxPlaceDistance);
            
            baseRotation = Quaternion.Euler(0, holder.transform.eulerAngles.y, 0);
            ghostObject.transform.rotation = baseRotation * Quaternion.Euler(placementRotationOffset);
            
            canPlaceCurrentFrame = false;
        }
    }

    private void PlaceItem()
    {
        if (holder != null)
        {
            var handler = holder.GetComponent<PlayerItemHandler>();
            if (handler != null) handler.OnItemPlacedSuccess();
        }

        transform.position = ghostObject.transform.position;
        // Применяем поворот призрака (в нем уже учтен оффсет)
        transform.rotation = ghostObject.transform.rotation;
        
        transform.SetParent(null);
        FixScaleOnDrop();

        holder = null; 
        SetCollidersEnabled(true);
        
        if (rb)
        {
            rb.isKinematic = false; 
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        HideGhost();
    }

    private void FixScaleOnDrop()
    {
        if (definition != null && definition.itemPrefab != null)
        {
            transform.localScale = definition.itemPrefab.transform.localScale;
        }
        else
        {
            transform.localScale = InitialScale;
        }
    }

    private void HideGhost()
    {
        if (ghostObject != null) ghostObject.SetActive(false);
        canPlaceCurrentFrame = false;
    }
    private void ShowGhost()
    {
        if (ghostObject != null) ghostObject.SetActive(true);
        canPlaceCurrentFrame = true;
    }

    private void SetGhostMaterial(Material mat)
    {
        if (ghostRenderers == null || ghostRenderers.Length == 0) return;
        if (ghostRenderers[0].sharedMaterial == mat) return;

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            ghostRenderers[i].sharedMaterial = mat;
        }
    }

    // Добавьте этот метод, чтобы призрак исчезал при скрытии предмета (переключении слотов)
    private void OnDisable()
    {
        HideGhost();
    }
    private void OnEnable()
    {
        ShowGhost();
    }

    private void OnDestroy()
    {
        if (ghostObject) Destroy(ghostObject);
    }
}
