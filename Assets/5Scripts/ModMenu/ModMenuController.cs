using UnityEngine;
using Pathfinding; // A* Pathfinding
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO; // Для работы с файлами
using System;

public class ModMenuController : MonoBehaviour
{
    [Header("Настройки")]
    public bool forceEnableDevMode = false;
    public string allowedSceneName = "World";
    
    // --- НОВОЕ: Ссылка на материал ---
    [Header("Материалы")]
    public Material buildingMaterial; // Сюда нужно перетащить материал в инспекторе

    // --- Состояния меню ---
    private bool showMenu = false;
    private int currentTab = 0; 
    private string[] tabNames = { "MAIN", "STATS", "FUN", "SPAWNER", "ITEMS", "EDITOR" }; 
    private Vector2 scrollPosition;

    // --- MAIN ---
    private bool godMode = false;
    private bool flyMode = false; 
    private bool flyUpMode = false; 
    private bool espEnabled = false;
    private bool invisibleMode = false;
    private bool freezeEnemy = false;
    private bool muteEnemyAudio = false;
    private float uiOpacity = 1.0f; 

    // --- STATS ---
    private float customWalkSpeed = 6.0f;
    private float customJumpForce = 8.0f;
    private float customEnemyHeight = 1.0f;
    private float customEnemyWidth = 1.0f;
    private float customEnemySpeed = 6.0f; 

    // --- FUN ---
    private bool spinBotMode = false; 
    private bool wideMode = false;
    private bool upsideDownCamera = false; 
    private float customFOV = 90f;
    private string customScreenText = ""; 
    private bool showScreenText = false;

    // --- EDITOR ---
    private GameObject currentHitObject; 
    private GameObject lockedObject;     
    private bool isObjectLocked = false;
    private float editorMoveStep = 0.5f; 
    private float editorRotStep = 45f;   
    private float editorScaleStep = 0.5f;
    
    private string saveFileName = "map_01";
    private string lastSaveMessage = "";

    // --- ITEMS ---
    private List<GameObject> itemPrefabs = new List<GameObject>();
    private List<GameObject> keyPrefabs = new List<GameObject>();
    private List<GameObject> meleePrefabs = new List<GameObject>();
    private List<GameObject> gunPrefabs = new List<GameObject>(); 

    // --- Ссылки ---
    private FP_Controller playerController;
    private CanvasGroup playerCanvasGroup;
    private RichAI_EnemyController mainEnemy;
    private GameObject enemyPrefabTemplate;
    private Camera mainCam;

    // --- GUI ---
    private Rect windowRect;
    private Texture2D boxTexture;
    private GUIStyle bigTextStyle; 
    public static ModMenuController Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        boxTexture = new Texture2D(1, 1);
        boxTexture.SetPixel(0, 0, Color.green);
        boxTexture.Apply();
    }

    void Start()
    {
        float width = Mathf.Min(Screen.width * 0.9f, 650); 
        float height = Mathf.Min(Screen.height * 0.85f, 800);
        windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);

        LoadItemsFromResources();
        FindReferences();
    }

    private bool IsModMenuUnlocked()
    {
        return forceEnableDevMode || PlayerPrefs.GetInt("NightmareCompleted", 0) == 1;
    }

    void LoadItemsFromResources()
    {
        itemPrefabs.AddRange(Resources.LoadAll<GameObject>("Items/Items"));
        keyPrefabs.AddRange(Resources.LoadAll<GameObject>("Items/Keys"));
        meleePrefabs.AddRange(Resources.LoadAll<GameObject>("Items/Melee"));
        gunPrefabs.AddRange(Resources.LoadAll<GameObject>("Items/Guns"));
    }

    void Update()
    {
        if (!IsModMenuUnlocked()) return;
        if (SceneManager.GetActiveScene().name != allowedSceneName) return;

        if (playerController == null) FindReferences();
        
        if (showMenu && !isObjectLocked && mainCam != null)
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                currentHitObject = hit.collider.gameObject;
            }
            else
            {
                currentHitObject = null;
            }
        }

        ApplyContinuousEffects();
    }

    private void FindReferences()
    {
        playerController = FindFirstObjectByType<FP_Controller>();
        mainCam = Camera.main;

        if (playerController != null)
        {
            playerCanvasGroup = playerController.GetComponentInChildren<CanvasGroup>(true);
            
            if (customWalkSpeed == 0)
            {
                customWalkSpeed = playerController.walkSpeed;
                customJumpForce = playerController.jumpForce;
            }
        }

        if (mainCam != null && customFOV == 0) 
        {
            customFOV = mainCam.fieldOfView;
        }
        
        if (mainEnemy == null)
        {
            mainEnemy = FindFirstObjectByType<RichAI_EnemyController>();
            if (mainEnemy != null)
            {
                customEnemyHeight = mainEnemy.transform.localScale.y;
                customEnemyWidth = mainEnemy.transform.localScale.x;
                customEnemySpeed = mainEnemy.chaseSpeed; 
                enemyPrefabTemplate = mainEnemy.gameObject;
            }
        }
    }

    public bool IsGodModeActive() => godMode;

    void ApplyContinuousEffects()
    {
        if (playerController != null)
        {
            if (flyUpMode)
            {
                if (playerController.controller.enabled) 
                    playerController.controller.enabled = false;
                playerController.transform.position += Vector3.up * 8f * Time.deltaTime;
            }
            else if (!flyMode)
            {
                if (!playerController.controller.enabled) 
                    playerController.controller.enabled = true;
            }
        }

        if (playerCanvasGroup != null) playerCanvasGroup.alpha = uiOpacity;

        var allEnemies = FindObjectsByType<RichAI_EnemyController>(FindObjectsSortMode.None);
        foreach (var enemy in allEnemies)
        {
            if(enemy == null) continue;
            if (enemy.chaseAudio != null) enemy.chaseAudio.mute = muteEnemyAudio;

            var ai = enemy.GetComponent<RichAI>();
            if (ai != null) ai.maxSpeed = customEnemySpeed;
            enemy.chaseSpeed = customEnemySpeed;
            enemy.walkSpeed = Mathf.Min(customEnemySpeed, enemy.walkSpeed); 

            if (spinBotMode && !freezeEnemy) enemy.transform.Rotate(0, 1500f * Time.deltaTime, 0);

            if (wideMode) enemy.transform.localScale = new Vector3(3.0f, 0.5f, 1.0f);
            else enemy.transform.localScale = new Vector3(customEnemyWidth, customEnemyHeight, customEnemyWidth);
        }

        if (mainCam != null)
        {
            mainCam.fieldOfView = customFOV;
            float targetZ = upsideDownCamera ? 180f : 0f;
            mainCam.transform.localEulerAngles = new Vector3(mainCam.transform.localEulerAngles.x, mainCam.transform.localEulerAngles.y, targetZ);
        }
    }

    void OnGUI()
    {
        if (!IsModMenuUnlocked()) return;
        if (SceneManager.GetActiveScene().name != allowedSceneName) return;

        if (bigTextStyle == null)
        {
            bigTextStyle = new GUIStyle(GUI.skin.label);
            bigTextStyle.fontSize = 40;
            bigTextStyle.fontStyle = FontStyle.Bold;
            bigTextStyle.normal.textColor = Color.red;
            bigTextStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (showScreenText && !string.IsNullOrEmpty(customScreenText))
        {
            GUI.Label(new Rect(0, Screen.height * 0.15f, Screen.width, 100), customScreenText, bigTextStyle);
        }

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 20; btnStyle.fixedHeight = 45;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 18; labelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
        tabStyle.fontSize = 12; tabStyle.fixedHeight = 35;

        if (!showMenu)
        {
            if (GUI.Button(new Rect(Screen.width - 90, 20, 70, 70), "MOD", btnStyle))
            {
                showMenu = true;
                if (playerController) playerController.canControl = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (espEnabled) DrawAllEnemiesESP();
            return;
        }

        if (espEnabled) DrawAllEnemiesESP();

        if (currentHitObject != null && !isObjectLocked)
        {
             GUI.Label(new Rect(Screen.width/2 - 100, Screen.height/2 + 20, 200, 30), $"[ {currentHitObject.name} ]", labelStyle);
        }

        GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        windowRect = GUI.Window(0, windowRect, (id) => DrawWindowContent(id, btnStyle, labelStyle, tabStyle), "MOD MENU - " + allowedSceneName);
    }

    void DrawWindowContent(int windowID, GUIStyle btnStyle, GUIStyle labelStyle, GUIStyle tabStyle)
    {
        currentTab = GUILayout.Toolbar(currentTab, tabNames, tabStyle);
        GUILayout.Space(10);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        switch (currentTab)
        {
            case 0: DrawMainTab(btnStyle, labelStyle); break;
            case 1: DrawStatsTab(btnStyle, labelStyle); break;
            case 2: DrawFunTab(btnStyle, labelStyle); break;
            case 3: DrawSpawnerTab(btnStyle, labelStyle); break;
            case 4: DrawItemsTab(btnStyle, labelStyle); break;
            case 5: DrawEditorTab(btnStyle, labelStyle); break;
        }

        GUILayout.EndScrollView();
        GUILayout.FlexibleSpace();
        
        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
        if (GUILayout.Button("CLOSE X", btnStyle))
        {
            showMenu = false;
            if (playerController) playerController.canControl = true;
        }
        GUI.DragWindow();
    }

    // --- TABS IMPL ---
    void DrawMainTab(GUIStyle btnStyle, GUIStyle labelStyle)
    {
        GUI.backgroundColor = godMode ? Color.green : Color.white;
        if (GUILayout.Button($"God Mode: {(godMode ? "ON" : "OFF")}", btnStyle)) godMode = !godMode;

        GUI.backgroundColor = flyMode ? Color.green : Color.white;
        if (GUILayout.Button($"NoClip: {(flyMode ? "ON" : "OFF")}", btnStyle))
        {
            flyMode = !flyMode;
            if (playerController) playerController.isFly = flyMode;
        }

        GUI.backgroundColor = flyUpMode ? Color.cyan : Color.white;
        if (GUILayout.Button($"FLY UP: {(flyUpMode ? "ACTIVE" : "OFF")}", btnStyle)) flyUpMode = !flyUpMode;

        GUI.backgroundColor = espEnabled ? Color.green : Color.white;
        if (GUILayout.Button($"ESP: {(espEnabled ? "ON" : "OFF")}", btnStyle)) espEnabled = !espEnabled;

        GUI.backgroundColor = invisibleMode ? Color.green : Color.white;
        if (GUILayout.Button($"Invisibility: {(invisibleMode ? "ON" : "OFF")}", btnStyle)) { invisibleMode = !invisibleMode; UpdateEnemyVision(); }

        GUI.backgroundColor = Color.white;
        GUILayout.Space(10);
        GUILayout.Label($"UI Opacity: {uiOpacity:F1}", labelStyle);
        uiOpacity = GUILayout.HorizontalSlider(uiOpacity, 0f, 1f);

        GUILayout.Space(5);
        GUI.backgroundColor = freezeEnemy ? Color.green : Color.white;
        if (GUILayout.Button($"Freeze AI: {(freezeEnemy ? "ON" : "OFF")}", btnStyle)) { freezeEnemy = !freezeEnemy; UpdateEnemyFreeze(); }

        GUI.backgroundColor = muteEnemyAudio ? Color.red : Color.white;
        if (GUILayout.Button($"Mute Audio: {(muteEnemyAudio ? "MUTED" : "ON")}", btnStyle)) muteEnemyAudio = !muteEnemyAudio;
        GUI.backgroundColor = Color.white;
    }

    void DrawStatsTab(GUIStyle btnStyle, GUIStyle labelStyle)
    {
        GUILayout.Label("--- PLAYER ---", labelStyle);
        GUILayout.Label($"Walk Speed: {customWalkSpeed:F1}", labelStyle);
        float oldSpeed = customWalkSpeed;
        customWalkSpeed = GUILayout.HorizontalSlider(customWalkSpeed, 1f, 30f);
        if (oldSpeed != customWalkSpeed && playerController) { playerController.walkSpeed = customWalkSpeed; playerController.runSpeed = customWalkSpeed * 1.8f; }

        GUILayout.Label($"Jump Force: {customJumpForce:F1}", labelStyle);
        float oldJump = customJumpForce;
        customJumpForce = GUILayout.HorizontalSlider(customJumpForce, 1f, 30f);
        if (oldJump != customJumpForce && playerController) playerController.jumpForce = customJumpForce;

        GUILayout.Space(10);
        GUILayout.Label("--- ENEMY ---", labelStyle);
        GUILayout.Label($"Height: {customEnemyHeight:F1}x", labelStyle);
        customEnemyHeight = GUILayout.HorizontalSlider(customEnemyHeight, 0.1f, 5.0f);
        GUILayout.Label($"Width: {customEnemyWidth:F1}x", labelStyle);
        customEnemyWidth = GUILayout.HorizontalSlider(customEnemyWidth, 0.1f, 5.0f);
        GUILayout.Label($"Enemy Speed: {customEnemySpeed:F1}", labelStyle);
        customEnemySpeed = GUILayout.HorizontalSlider(customEnemySpeed, 0f, 25f); 

        GUILayout.Space(10);
        if (GUILayout.Button("Reset Defaults", btnStyle))
        {
            customWalkSpeed = 6.0f; customJumpForce = 8.0f;
            customEnemyHeight = 1.0f; customEnemyWidth = 1.0f;
            customEnemySpeed = 6.0f; customFOV = 90f; uiOpacity = 1.0f;
            if(playerController) { playerController.walkSpeed = 6f; playerController.runSpeed = 11f; playerController.jumpForce = 8f; }
        }
    }

    void DrawFunTab(GUIStyle btnStyle, GUIStyle labelStyle)
    {
        GUILayout.Label("--- SCREEN TEXT ---", labelStyle);
        customScreenText = GUILayout.TextField(customScreenText, 50);
        GUI.backgroundColor = showScreenText ? Color.green : Color.white;
        if (GUILayout.Button($"Show Text: {(showScreenText ? "YES" : "NO")}", btnStyle)) showScreenText = !showScreenText;
        GUI.backgroundColor = Color.white;

        GUILayout.Space(15);
        GUILayout.Label("--- FUN ZONE ---", labelStyle);
        GUI.backgroundColor = wideMode ? Color.cyan : Color.white;
        if (GUILayout.Button($"Wide Mode: {(wideMode ? "ON" : "OFF")}", btnStyle)) wideMode = !wideMode;
        GUI.backgroundColor = spinBotMode ? Color.cyan : Color.white;
        if (GUILayout.Button($"SPINBOT Enemy: {(spinBotMode ? "ON" : "OFF")}", btnStyle)) spinBotMode = !spinBotMode;
        GUI.backgroundColor = upsideDownCamera ? Color.cyan : Color.white;
        if (GUILayout.Button($"Upside Down Cam: {(upsideDownCamera ? "ON" : "OFF")}", btnStyle)) upsideDownCamera = !upsideDownCamera;
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(15);
        GUILayout.Label("--- TELEPORTS ---", labelStyle);
        if (GUILayout.Button("TP TO Enemy", btnStyle))
        {
            if (playerController && mainEnemy) {
                playerController.controller.enabled = false;
                playerController.transform.position = mainEnemy.transform.position - mainEnemy.transform.forward * 2f;
                playerController.controller.enabled = true;
            }
        }
        if (GUILayout.Button("TP Enemy HERE", btnStyle))
        {
            if (playerController && mainEnemy) mainEnemy.GetComponent<RichAI>().Teleport(playerController.transform.position + playerController.transform.forward * 3f);
        }
        GUILayout.Space(10);
        GUILayout.Label($"FOV: {customFOV:F0}", labelStyle);
        customFOV = GUILayout.HorizontalSlider(customFOV, 30f, 170f);
    }

    void DrawSpawnerTab(GUIStyle btnStyle, GUIStyle labelStyle)
    {
        GUILayout.Label("--- CLONES ---", labelStyle);
        if (GUILayout.Button("+ Spawn Clone", btnStyle)) SpawnClone();
        if (GUILayout.Button("Delete All Clones", btnStyle)) DeleteAllClones();

        GUILayout.Space(15);
        GUILayout.Label("--- ORIGINAL ---", labelStyle);
        bool isMainActive = mainEnemy != null && mainEnemy.gameObject.activeSelf;
        if (isMainActive) {
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("DELETE (Remove)", btnStyle)) if (mainEnemy) mainEnemy.gameObject.SetActive(false);
        } else {
            GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            if (GUILayout.Button("SPAWN (Restore)", btnStyle)) if (mainEnemy) { mainEnemy.gameObject.SetActive(true); if(playerController) mainEnemy.GetComponent<RichAI>().Teleport(playerController.transform.position + playerController.transform.forward * 5f); }
        }
        GUI.backgroundColor = Color.white;
    }

    void DrawItemsTab(GUIStyle btnStyle, GUIStyle labelStyle)
    {
        GUILayout.Label("--- SPAWN ITEMS ---", labelStyle);
        if (playerController == null) return;

        if (itemPrefabs.Count > 0) {
            GUILayout.Label($"Items ({itemPrefabs.Count})", labelStyle);
            foreach (var prefab in itemPrefabs) if (GUILayout.Button(prefab.name, btnStyle)) SpawnItem(prefab);
        }
        if (keyPrefabs.Count > 0) {
            GUILayout.Space(10);
            GUI.backgroundColor = Color.yellow;
            GUILayout.Label($"Keys ({keyPrefabs.Count})", labelStyle);
            foreach (var prefab in keyPrefabs) if (GUILayout.Button(prefab.name, btnStyle)) SpawnItem(prefab);
            GUI.backgroundColor = Color.white;
        }
        if (meleePrefabs.Count > 0) {
            GUILayout.Space(10);
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            GUILayout.Label($"Melee ({meleePrefabs.Count})", labelStyle);
            foreach (var prefab in meleePrefabs) if (GUILayout.Button(prefab.name, btnStyle)) SpawnItem(prefab);
            GUI.backgroundColor = Color.white;
        }
        if (gunPrefabs.Count > 0) {
            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.6f, 0.6f, 1f);
            GUILayout.Label($"Guns ({gunPrefabs.Count})", labelStyle);
            foreach (var prefab in gunPrefabs) if (GUILayout.Button(prefab.name, btnStyle)) SpawnItem(prefab);
            GUI.backgroundColor = Color.white;
        }
    }

    void DrawEditorTab(GUIStyle btnStyle, GUIStyle labelStyle)
    {
        GUILayout.Label("--- MAP SAVE/LOAD SYSTEM ---", labelStyle);
        GUILayout.Label("File Name:", labelStyle);
        saveFileName = GUILayout.TextField(saveFileName);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("SAVE MAP", btnStyle)) SaveMap();
        if (GUILayout.Button("LOAD MAP", btnStyle)) LoadMap();
        GUILayout.EndHorizontal();

        if(!string.IsNullOrEmpty(lastSaveMessage))
            GUILayout.Label(lastSaveMessage, new GUIStyle(labelStyle){fontSize = 14, normal = {textColor = Color.yellow}});

        GUILayout.Space(15);
        GUILayout.Label("--- SPAWN SHAPES ---", labelStyle);

        // 1. Кнопки спавна разных форм
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cube", btnStyle)) SpawnPrimitive(PrimitiveType.Cube, "Cube");
        if (GUILayout.Button("Sphere", btnStyle)) SpawnPrimitive(PrimitiveType.Sphere, "Sphere");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Capsule", btnStyle)) SpawnPrimitive(PrimitiveType.Capsule, "Capsule");
        if (GUILayout.Button("Cylinder", btnStyle)) SpawnPrimitive(PrimitiveType.Cylinder, "Cylinder");
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("Plane (Floor)", btnStyle)) SpawnPrimitive(PrimitiveType.Plane, "Plane");

        GUILayout.Space(10);

        GameObject target = isObjectLocked ? lockedObject : currentHitObject;

        if (target == null)
        {
            GUILayout.Label("Look at an object to select it...", labelStyle);
            return;
        }

        string status = isObjectLocked ? $"LOCKED: {target.name}" : $"LOOKING AT: {target.name}";
        GUI.backgroundColor = isObjectLocked ? Color.green : Color.yellow;
        if (GUILayout.Button(status, btnStyle))
        {
            if (isObjectLocked) { isObjectLocked = false; lockedObject = null; }
            else { isObjectLocked = true; lockedObject = currentHitObject; }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(10);

        if (isObjectLocked && lockedObject != null)
        {
            // 2. Цвета
            GUILayout.Label("--- COLOR ---", labelStyle);
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.white; if(GUILayout.Button("W", btnStyle)) ChangeObjectColor(Color.white);
            GUI.backgroundColor = Color.red; if(GUILayout.Button("R", btnStyle)) ChangeObjectColor(Color.red);
            GUI.backgroundColor = Color.green; if(GUILayout.Button("G", btnStyle)) ChangeObjectColor(Color.green);
            GUI.backgroundColor = Color.blue; if(GUILayout.Button("B", btnStyle)) ChangeObjectColor(Color.blue);
            GUI.backgroundColor = Color.black; if(GUILayout.Button("Bk", btnStyle)) ChangeObjectColor(Color.black);
            GUI.backgroundColor = Color.gray; if(GUILayout.Button("Gy", btnStyle)) ChangeObjectColor(Color.gray);
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            // 3. Телепорт объекта к игроку
            GUILayout.Space(5);
            if (GUILayout.Button("TP Object TO ME", btnStyle)) TeleportObjectToPlayer();

            GUILayout.Space(10);
            // Трансформация (без изменений)
            GUILayout.Label($"Move Step: {editorMoveStep:F1}", labelStyle);
            editorMoveStep = GUILayout.HorizontalSlider(editorMoveStep, 0.1f, 5.0f);
            
            GUILayout.Label($"Rot Step: {editorRotStep:F0}", labelStyle);
            editorRotStep = GUILayout.HorizontalSlider(editorRotStep, 1f, 90f);

            GUILayout.Label($"Scale Step: {editorScaleStep:F1}", labelStyle);
            editorScaleStep = GUILayout.HorizontalSlider(editorScaleStep, 0.1f, 2.0f);

            GUILayout.Label("--- POSITION ---", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X-", btnStyle)) lockedObject.transform.position += Vector3.left * editorMoveStep;
            if (GUILayout.Button("X+", btnStyle)) lockedObject.transform.position += Vector3.right * editorMoveStep;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Y-", btnStyle)) lockedObject.transform.position += Vector3.down * editorMoveStep;
            if (GUILayout.Button("Y+", btnStyle)) lockedObject.transform.position += Vector3.up * editorMoveStep;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Z-", btnStyle)) lockedObject.transform.position += Vector3.back * editorMoveStep;
            if (GUILayout.Button("Z+", btnStyle)) lockedObject.transform.position += Vector3.forward * editorMoveStep;
            GUILayout.EndHorizontal();

            GUILayout.Label("--- ROTATION ---", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"P-", btnStyle)) lockedObject.transform.Rotate(-editorRotStep, 0, 0);
            if (GUILayout.Button($"P+", btnStyle)) lockedObject.transform.Rotate(editorRotStep, 0, 0);
            if (GUILayout.Button($"Y-", btnStyle)) lockedObject.transform.Rotate(0, -editorRotStep, 0);
            if (GUILayout.Button($"Y+", btnStyle)) lockedObject.transform.Rotate(0, editorRotStep, 0);
            GUILayout.EndHorizontal();

            GUILayout.Label("--- SCALE ---", labelStyle);
            Vector3 s = lockedObject.transform.localScale;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X-", btnStyle)) lockedObject.transform.localScale = new Vector3(s.x - editorScaleStep, s.y, s.z);
            if (GUILayout.Button("X+", btnStyle)) lockedObject.transform.localScale = new Vector3(s.x + editorScaleStep, s.y, s.z);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Y-", btnStyle)) lockedObject.transform.localScale = new Vector3(s.x, s.y - editorScaleStep, s.z);
            if (GUILayout.Button("Y+", btnStyle)) lockedObject.transform.localScale = new Vector3(s.x, s.y + editorScaleStep, s.z);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Z-", btnStyle)) lockedObject.transform.localScale = new Vector3(s.x, s.y, s.z - editorScaleStep);
            if (GUILayout.Button("Z+", btnStyle)) lockedObject.transform.localScale = new Vector3(s.x, s.y, s.z + editorScaleStep);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            Rigidbody rb = lockedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                string physText = rb.isKinematic ? "Physics: OFF" : "Physics: ON";
                GUI.backgroundColor = rb.isKinematic ? Color.gray : Color.cyan;
                if (GUILayout.Button(physText, btnStyle)) rb.isKinematic = !rb.isKinematic;
                GUI.backgroundColor = Color.white;
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("DELETE OBJECT", btnStyle))
            {
                Destroy(lockedObject);
                isObjectLocked = false;
                lockedObject = null;
            }
            GUI.backgroundColor = Color.white;
        }
    }

    // --- EDITOR LOGIC ---

    // 1. Улучшенный спавн примитивов с МАТЕРИАЛОМ
    void SpawnPrimitive(PrimitiveType type, string typeName)
    {
        if (playerController == null) return;
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = $"Editor_{typeName}_{UnityEngine.Random.Range(100,999)}";
        obj.transform.position = playerController.transform.position + playerController.transform.forward * 3f + Vector3.up * 1f;
        
        // --- ВАЖНО: Применяем материал ---
        Renderer r = obj.GetComponent<Renderer>();
        if (buildingMaterial != null && r != null)
        {
            r.material = new Material(buildingMaterial); // Создаем копию, чтобы красить объекты по отдельности
        }
        // ---------------------------------

        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true; 
    }

    // 2. Смена цвета
    void ChangeObjectColor(Color c)
    {
        if (lockedObject != null)
        {
            Renderer r = lockedObject.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }
    }

    // 3. Телепорт объекта к игроку
    void TeleportObjectToPlayer()
    {
        if (lockedObject != null && playerController != null)
        {
            lockedObject.transform.position = playerController.transform.position + playerController.transform.forward * 2f;
        }
    }

    // --- SAVE / LOAD SYSTEM ---

    private string GetSavePath()
    {
        string folderName = "ModMaps";
        string path = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        path = System.IO.Path.Combine("/storage/emulated/0/Download", folderName);
#else
        path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), folderName);
#endif

        if (!Directory.Exists(path))
        {
            try {
                Directory.CreateDirectory(path);
            } catch {
                path = System.IO.Path.Combine(Application.persistentDataPath, folderName);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
        }
        
        return path;
    }

    public void SaveMap()
    {
        if(string.IsNullOrEmpty(saveFileName)) saveFileName = "map_default";

        MapData data = new MapData();
        
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach(var go in allObjects)
        {
            if(go.name.StartsWith("Editor_"))
            {
                SavedObject so = new SavedObject();
                so.name = go.name;
                
                if(go.name.Contains("Cube")) so.type = PrimitiveType.Cube;
                else if(go.name.Contains("Sphere")) so.type = PrimitiveType.Sphere;
                else if(go.name.Contains("Capsule")) so.type = PrimitiveType.Capsule;
                else if(go.name.Contains("Cylinder")) so.type = PrimitiveType.Cylinder;
                else if(go.name.Contains("Plane")) so.type = PrimitiveType.Plane;
                else so.type = PrimitiveType.Cube; 

                so.position = go.transform.position;
                so.rotation = go.transform.rotation;
                so.scale = go.transform.localScale;
                
                Renderer r = go.GetComponent<Renderer>();
                so.color = (r != null) ? r.material.color : Color.white;
                
                Rigidbody rb = go.GetComponent<Rigidbody>();
                so.isKinematic = (rb != null) ? rb.isKinematic : true;

                data.objects.Add(so);
            }
        }

        string json = JsonUtility.ToJson(data, true);
        string fullPath = System.IO.Path.Combine(GetSavePath(), saveFileName + ".json");
        
        try {
            File.WriteAllText(fullPath, json);
            lastSaveMessage = $"Saved to: {fullPath}";
        } catch (Exception e) {
            lastSaveMessage = $"Error: {e.Message}";
        }
    }

    public void LoadMap()
    {
        string fullPath = System.IO.Path.Combine(GetSavePath(), saveFileName + ".json");
        if(!File.Exists(fullPath))
        {
            lastSaveMessage = "File not found!";
            return;
        }

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go.name.StartsWith("Editor_")) Destroy(go);
        }

        try {
            string json = File.ReadAllText(fullPath);
            MapData data = JsonUtility.FromJson<MapData>(json);

            foreach(var so in data.objects)
            {
                GameObject obj = GameObject.CreatePrimitive(so.type);
                obj.name = so.name;
                obj.transform.position = so.position;
                obj.transform.rotation = so.rotation;
                obj.transform.localScale = so.scale;

                // --- ВАЖНО: Применяем материал при загрузке ---
                Renderer r = obj.GetComponent<Renderer>();
                if (r != null) 
                {
                    if (buildingMaterial != null)
                        r.material = new Material(buildingMaterial);
                    
                    r.material.color = so.color; // Красим
                }
                // ----------------------------------------------

                Rigidbody rb = obj.AddComponent<Rigidbody>();
                rb.isKinematic = so.isKinematic;
            }
            lastSaveMessage = $"Loaded: {data.objects.Count} objects.";
        } catch (Exception e) {
            lastSaveMessage = $"Load Error: {e.Message}";
        }
    }

    // HELPERS
    void SpawnItem(GameObject prefab)
    {
        if (playerController == null || prefab == null) return;
        Vector3 pos = playerController.transform.position + playerController.transform.forward * 1.5f + Vector3.up * 1.0f;
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.AddForce(playerController.transform.forward * 2f, ForceMode.Impulse); }
    }
    void SpawnClone() { if (enemyPrefabTemplate && playerController) { Vector3 pos = playerController.transform.position + playerController.transform.forward * 3f + Vector3.up; Instantiate(enemyPrefabTemplate, pos, Quaternion.identity).SetActive(true); } }
    void DeleteAllClones() { var all = FindObjectsByType<RichAI_EnemyController>(FindObjectsSortMode.None); foreach (var e in all) if (e != mainEnemy) Destroy(e.gameObject); }
    void UpdateEnemyVision() { foreach (var e in FindObjectsByType<RichAI_EnemyController>(FindObjectsSortMode.None)) e.detectionRange = invisibleMode ? 0f : 30f; }
    void UpdateEnemyFreeze() { foreach (var e in FindObjectsByType<RichAI_EnemyController>(FindObjectsSortMode.None)) { var ai = e.GetComponent<RichAI>(); if(ai) ai.enabled = !freezeEnemy; e.enabled = !freezeEnemy; } }

    void DrawAllEnemiesESP() { foreach (var enemy in FindObjectsByType<RichAI_EnemyController>(FindObjectsSortMode.None)) { if (enemy && enemy.gameObject.activeSelf) DrawBoxESP(enemy.transform); } }
    void DrawBoxESP(Transform t) {
        float hScale = t.localScale.y; Vector3 foot = t.position; Vector3 head = foot + Vector3.up * 1.9f * hScale;
        Vector3 w2s_f = mainCam.WorldToScreenPoint(foot); Vector3 w2s_h = mainCam.WorldToScreenPoint(head);
        if (w2s_f.z <= 0) return;
        float hY = Screen.height - w2s_h.y; float fY = Screen.height - w2s_f.y; float h = fY - hY; float w = h / 2f * t.localScale.x; 
        DrawRectOutline(new Rect(w2s_f.x - w/2f, hY, w, h), 2f);
    }
    void DrawRectOutline(Rect r, float t) { GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), boxTexture); GUI.DrawTexture(new Rect(r.x, r.y + r.height - t, r.width, t), boxTexture); GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), boxTexture); GUI.DrawTexture(new Rect(r.x + r.width - t, r.y, t, r.height), boxTexture); }
}

// --- КЛАССЫ ДЛЯ СОХРАНЕНИЯ ---

[System.Serializable]
public class MapData
{
    public List<SavedObject> objects = new List<SavedObject>();
}

[System.Serializable]
public class SavedObject
{
    public string name;
    public PrimitiveType type;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public Color color;
    public bool isKinematic;
}
