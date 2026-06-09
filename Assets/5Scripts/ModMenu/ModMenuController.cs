using UnityEngine;
using System.Collections.Generic;

public class ModMenuController : MonoBehaviour
{
    [Header("Настройки")]
    public bool canUseModMenu = false;
    
    [Header("Материалы")]
    public Material buildingMaterial;

    // --- Состояния меню ---
    private bool showMenu = false;
    private int currentTab = 0; 
    private string[] tabNames = { "Player Settings", "Enemy Settings", "Fun & Memes", "Item Spawner", "Object Editor", "Menu Manager" }; 
    private Vector2 scrollPosition;

    // --- Переменные функций ---
    private bool godMode = false;
    private bool flyMode = false; 
    private bool flyUpMode = false; 
    private bool espEnabled = false;
    private bool invisibleMode = false;
    private bool freezeEnemy = false;
    private bool freezePlayer = false;
    private bool muteEnemyAudio = false;
    private float uiOpacity = 1.0f; 
    private bool espLineEnabled = false; 

    private float customWalkSpeed = 6.0f;
    private float customJumpForce = 8.0f;
    private float customEnemyHeight = 1.0f;
    private float customEnemyWidth = 1.0f;
    private float customEnemySpeed = 6.0f; 
    private float playerTpStep = 1.0f; 

    // --- ФАН И МЕМЫ ---
    private bool spinBotMode = false; 
    private bool wideMode = false;
    private bool slenderMode = false;
    private bool pancakeMode = false; // Режим блинчика
    private bool upsideDownCamera = false; 
    private bool drunkMode = false;
    private bool acidMode = false;
    private bool earthquakeMode = false;
    private float customFOV = 80f;
    private string customScreenText = ""; 
    private bool showScreenText = false;
    private float customTimeScale = 1.0f;
    private bool discoMode = false;
    private float customPlayerScale = 1.0f;
    private bool lowGravity = false;
    private Color originalAmbientLight;
    
    // --- КАСТОМИЗАЦИЯ МЕНЮ ---
    private string menuTitle = "Hacked By Mongabox [Шутка, By Filiposin]";
    private Color currentBgColor = new Color(0.07f, 0.05f, 0.15f, 0.95f);
    private bool hideModButtonVisually = false;
    private float menuScale = 1.0f;

    // --- Редактор объектов ---
    private GameObject currentHitObject; 
    private GameObject lockedObject;     
    private bool isObjectLocked = false;
    private float editorMoveStep = 0.5f; 
    private float editorRotStep = 45f;   
    private float editorScaleStep = 0.5f;
    private bool objectEspEnabled = false;
    private Vector2 hierarchyScrollPosition;
    private List<GameObject> sceneObjects = new List<GameObject>();
    private HashSet<GameObject> expandedObjects = new HashSet<GameObject>();

    // --- Списки предметов ---
    private List<GameObject> itemPrefabs = new List<GameObject>();
    private List<GameObject> keyPrefabs = new List<GameObject>();
    private List<GameObject> meleePrefabs = new List<GameObject>();
    private List<GameObject> gunPrefabs = new List<GameObject>(); 

    // --- Ссылки ---
    private FP_Controller playerController;
    private float originalPlayerGravity = 20f;
    private CanvasGroup playerCanvasGroup;
    private node_AIMovement mainEnemy;
    private GameObject enemyPrefabTemplate;
    private Camera mainCam;

    // --- GUI ---
    private Rect windowRect;
    private Texture2D bgTex, whiteTex, transparentTex, activeTabTex;
    private GUIStyle contentStyle, btnStyle, titleStyle, textStyle, tabStyle, closeBtnStyle;
    public static ModMenuController Instance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        bgTex = MakeTex(1, 1, currentBgColor);
        whiteTex = MakeTex(1, 1, Color.white);
        transparentTex = MakeTex(1, 1, Color.clear);
        activeTabTex = MakeTex(1, 1, new Color(1f, 1f, 1f, 0.15f)); 
    }

    void Start()
    {
        float width = 850f; float height = 450f;
        if (Application.isMobilePlatform)
            windowRect = new Rect((1280f - width) / 2, (720f - height) / 2, width, height);
        else
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
            
        originalAmbientLight = RenderSettings.ambientLight;

        LoadItemsFromResources();
        FindReferences();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix); result.Apply(); return result;
    }

    private void ChangeBgColor(Color newColor)
    {
        currentBgColor = newColor;
        bgTex = MakeTex(1, 1, currentBgColor);
    }

    public void EnableModMenu() { canUseModMenu = true; }
    public bool IsGodModeActive() { return godMode; }

    void LoadItemsFromResources()
    {
        itemPrefabs.AddRange(Resources.LoadAll<GameObject>("Items/Items"));
        keyPrefabs.AddRange(Resources.LoadAll<GameObject>("Items/Keys"));
        meleePrefabs.AddRange(Resources.LoadAll<GameObject>("Items/Melee"));
        gunPrefabs.AddRange(Resources.LoadAll<GameObject>("Items/Guns"));
    }

    void Update()
    {
        if (!canUseModMenu) return;
        if (playerController == null) FindReferences();
        
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showMenu = !showMenu;
            if (playerController) 
            {
                playerController.ForceCursorUnlock(showMenu);
            }
            else
            {
                if (showMenu) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                else if (!Application.isMobilePlatform) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            }
        }

        if (showMenu && !isObjectLocked && mainCam != null && currentTab == 4)
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 50f)) currentHitObject = hit.collider.gameObject;
            else currentHitObject = null;
        }

        ApplyContinuousEffects();
    }

    private void FindReferences()
    {
        playerController = GetComponent<FP_Controller>();
        if (playerController == null) playerController = FindFirstObjectByType<FP_Controller>();
        mainCam = Camera.main;

        if (playerController != null)
        {
            playerCanvasGroup = playerController.GetComponentInChildren<CanvasGroup>(true);
            if (customWalkSpeed == 0) { customWalkSpeed = playerController.walkSpeed; customJumpForce = playerController.jumpForce; }
            if (originalPlayerGravity == 20f) originalPlayerGravity = playerController.gravity; // Сохраняем начальную гравитацию FP_Controller
            if (freezePlayer) playerController.canControl = false; // Применяем состояние заморозки
        }

        if (mainCam != null && customFOV == 0) customFOV = mainCam.fieldOfView;
        
        if (mainEnemy == null)
        {
            mainEnemy = FindFirstObjectByType<node_AIMovement>();
            if (mainEnemy != null)
            {
                customEnemyHeight = mainEnemy.transform.localScale.y;
                customEnemyWidth = mainEnemy.transform.localScale.x;
                customEnemySpeed = mainEnemy.speedSettings.chaseSpeed; 
                enemyPrefabTemplate = mainEnemy.gameObject;
            }
        }
    }

    void ApplyContinuousEffects()
    {
        // ВРЕМЯ
        Time.timeScale = customTimeScale;

        // ГРАВИТАЦИЯ (И для мира, и для игрока)
        Physics.gravity = lowGravity ? new Vector3(0, -2f, 0) : new Vector3(0, -9.81f, 0);

        // ДИСКО МОД
        if (discoMode) RenderSettings.ambientLight = Color.HSVToRGB(Mathf.PingPong(Time.unscaledTime * 2f, 1f), 1f, 1f);
        else RenderSettings.ambientLight = originalAmbientLight;

        if (playerController != null)
        {
            playerController.transform.localScale = Vector3.one * customPlayerScale;
            
            // Фикс Moon Gravity для FP_Controller
            playerController.gravity = lowGravity ? 3f : originalPlayerGravity;

            if (flyUpMode)
            {
                if (playerController.controller.enabled) playerController.controller.enabled = false;
                playerController.transform.position += Vector3.up * 8f * Time.unscaledDeltaTime;
            }
            else if (!flyMode && !playerController.controller.enabled) playerController.controller.enabled = true;
        }

        if (playerCanvasGroup != null) playerCanvasGroup.alpha = uiOpacity;

        // МЕМЫ С СОСЕДОМ
        var allEnemies = FindObjectsByType<node_AIMovement>(FindObjectsSortMode.None);
        foreach (var enemy in allEnemies)
        {
            if(enemy == null) continue;
            if (enemy.chaseAudio != null) enemy.chaseAudio.mute = muteEnemyAudio;

            if (spinBotMode && !freezeEnemy) enemy.transform.Rotate(0, 1500f * Time.unscaledDeltaTime, 0);

            if (pancakeMode) enemy.transform.localScale = new Vector3(3.0f, 0.1f, 3.0f);
            else if (wideMode) enemy.transform.localScale = new Vector3(3.0f, 0.5f, 1.0f);
            else if (slenderMode) enemy.transform.localScale = new Vector3(0.3f, 3.5f, 0.3f);
        }

        // МЕМЫ С КАМЕРОЙ
        if (mainCam != null)
        {
            if (acidMode) customFOV = 80f + Mathf.Sin(Time.time * 5f) * 50f; // Угарная пульсация FOV
            
            mainCam.fieldOfView = customFOV;
            
            float targetZ = upsideDownCamera ? 180f : 0f;
            if (drunkMode) targetZ += Mathf.Sin(Time.time * 3f) * 15f; // Пьяное покачивание
            
            mainCam.transform.localEulerAngles = new Vector3(mainCam.transform.localEulerAngles.x, mainCam.transform.localEulerAngles.y, targetZ);

            if (earthquakeMode) mainCam.transform.localEulerAngles += (Vector3)UnityEngine.Random.insideUnitCircle * 5f; // Тряска
        }
    }

    void ResetCamera()
    {
        customFOV = 80f;
        upsideDownCamera = false;
        drunkMode = false;
        acidMode = false;
        earthquakeMode = false;
        if (mainCam != null)
        {
            mainCam.fieldOfView = customFOV;
            mainCam.transform.localEulerAngles = Vector3.zero;
        }
    }

    void InitStyles()
    {
        if (btnStyle != null) return;
        btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.normal.background = transparentTex; btnStyle.normal.textColor = Color.white;
        btnStyle.hover.textColor = new Color(0.8f, 0.8f, 1f);
        btnStyle.alignment = TextAnchor.MiddleCenter; btnStyle.fontSize = 13;
        btnStyle.margin = new RectOffset(2, 2, 2, 2);
        btnStyle.padding = new RectOffset(2, 2, 2, 2);

        tabStyle = new GUIStyle(btnStyle);
        tabStyle.alignment = TextAnchor.MiddleLeft; tabStyle.padding = new RectOffset(15, 0, 0, 0); tabStyle.fontSize = 14;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.normal.textColor = Color.red; titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold; titleStyle.fontSize = 16;

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.normal.textColor = Color.white; textStyle.alignment = TextAnchor.MiddleLeft; textStyle.fontSize = 13;

        closeBtnStyle = new GUIStyle(btnStyle);
        closeBtnStyle.normal.textColor = Color.red; closeBtnStyle.fontSize = 20; closeBtnStyle.fontStyle = FontStyle.Bold;
    }

    void OnGUI()
    {
        if (!canUseModMenu) return;
        
        float sw = Screen.width / menuScale; float sh = Screen.height / menuScale;
        if (Application.isMobilePlatform)
        {
            sw = 1280f / menuScale; sh = 720f / menuScale;
            float rx = (Screen.width / 1280f) * menuScale;
            float ry = (Screen.height / 720f) * menuScale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));
        }
        else
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(menuScale, menuScale, 1));
        }

        InitStyles();

        if (showScreenText && !string.IsNullOrEmpty(customScreenText))
        {
            GUIStyle bigTxt = new GUIStyle(titleStyle) { fontSize = 50, normal = { textColor = Color.red } };
            GUI.Label(new Rect(0, sh * 0.15f, sw, 100), customScreenText, bigTxt);
        }

        if (!showMenu)
        {
            GUIStyle openBtn = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            if (hideModButtonVisually)
            {
                openBtn.normal.background = transparentTex;
                openBtn.hover.background = transparentTex;
                openBtn.active.background = transparentTex;
                openBtn.normal.textColor = Color.clear;
                openBtn.hover.textColor = Color.clear;
                openBtn.active.textColor = Color.clear;
            }

            if (GUI.Button(new Rect(sw - 90, 20, 70, 70), hideModButtonVisually ? "" : "MOD", openBtn))
            {
                showMenu = true;
                if (playerController) 
                {
                    playerController.ForceCursorUnlock(true);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                }
            }
            if (espEnabled) DrawAllEnemiesESP(sw, sh);
            if (objectEspEnabled && lockedObject != null) DrawBoxESP(lockedObject.transform, sh, true);
            return;
        }

        if (espEnabled) DrawAllEnemiesESP(sw, sh);
        if (objectEspEnabled && lockedObject != null) DrawBoxESP(lockedObject.transform, sh, true);

        if (currentHitObject != null && !isObjectLocked && currentTab == 4)
             GUI.Label(new Rect(sw/2 - 100, sh/2 + 20, 200, 30), $"[ {currentHitObject.name} ]", textStyle);

        GUI.backgroundColor = Color.clear;
        windowRect.x = Mathf.Clamp(windowRect.x, 0, Mathf.Max(0, sw - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Mathf.Max(0, sh - windowRect.height));
        windowRect = GUI.Window(0, windowRect, DrawModernWindow, "", GUIStyle.none);
    }

    void DrawModernWindow(int windowID)
    {
        GUI.DrawTexture(new Rect(0, 0, windowRect.width, windowRect.height), bgTex);
        DrawRectOutline(new Rect(0, 0, windowRect.width, windowRect.height), 2f, whiteTex);

        // --- КНОПКА ЗАКРЫТИЯ В УГЛУ ---
        if (GUI.Button(new Rect(windowRect.width - 40, 5, 35, 35), "X", closeBtnStyle))
        {
            showMenu = false;
            if (playerController) 
            {
                playerController.ForceCursorUnlock(false);
            }
            else if (!Application.isMobilePlatform)
            {
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            }
        }

        GUILayout.Space(10);
        GUILayout.Label(menuTitle, titleStyle); // Используем кастомный заголовок
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(180));
        for (int i = 0; i < tabNames.Length; i++)
        {
            Rect btnRect = GUILayoutUtility.GetRect(new GUIContent(tabNames[i]), tabStyle, GUILayout.Height(35));
            if (currentTab == i) GUI.DrawTexture(btnRect, activeTabTex);
            if (GUI.Button(btnRect, tabNames[i], tabStyle)) currentTab = i;
        }
        GUILayout.EndVertical();

        Rect sepRect = GUILayoutUtility.GetRect(2, windowRect.height - 40, GUILayout.Width(2));
        GUI.DrawTexture(sepRect, whiteTex);
        GUILayout.Space(10);

        GUILayout.BeginVertical();
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.Space(5);

        switch (currentTab)
        {
            case 0: DrawPlayerSettings(); break;
            case 1: DrawEnemySettings(); break;
            case 2: DrawFunSettings(); break;
            case 3: DrawItemSpawner(); break;
            case 4: DrawObjectEditor(); break;
            case 5: DrawMenuManager(); break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, windowRect.width, 40));
    }

    void DrawStepper(string label, ref float value, float step, float min, float max, string format = "F1")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value.ToString(format)}", textStyle, GUILayout.Width(110));
        if (GUILayout.Button("-", btnStyle, GUILayout.Width(25))) value = Mathf.Max(min, value - step);
        if (GUILayout.Button("+", btnStyle, GUILayout.Width(25))) value = Mathf.Min(max, value + step);
        GUILayout.EndHorizontal();
    }

    void DrawGridButton(string text, ref bool state, float width = 160)
    {
        string label = $"{text}: {(state ? "ON" : "OFF")}";
        if (GUILayout.Button(label, btnStyle, GUILayout.Width(width))) state = !state;
    }

    // --- ВКЛАДКИ ---

    void DrawPlayerSettings()
    {
        GUILayout.BeginHorizontal();
        DrawGridButton("God Mode", ref godMode);
        float oldSpeed = customWalkSpeed; DrawStepper("Speed", ref customWalkSpeed, 1f, 1f, 30f);
        if (oldSpeed != customWalkSpeed && playerController) { playerController.walkSpeed = customWalkSpeed; playerController.runSpeed = customWalkSpeed * 1.8f; }
        if (GUILayout.Button("Teleport To Enemy", btnStyle, GUILayout.Width(160)))
        {
            if (playerController && mainEnemy) {
                playerController.controller.enabled = false;
                playerController.transform.position = mainEnemy.transform.position - mainEnemy.transform.forward * 2f;
                playerController.controller.enabled = true;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        bool oldFly = flyMode; DrawGridButton("No Clip", ref flyMode);
        if (oldFly != flyMode && playerController) playerController.isFly = flyMode;
        float oldJump = customJumpForce; DrawStepper("Jump Force", ref customJumpForce, 1f, 1f, 30f);
        if (oldJump != customJumpForce && playerController) playerController.jumpForce = customJumpForce;
        DrawGridButton("Fly Up", ref flyUpMode);
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        bool oldInvis = invisibleMode; DrawGridButton("Invisibility", ref invisibleMode);
        if (oldInvis != invisibleMode) UpdateEnemyVision();
        DrawStepper("Player Size", ref customPlayerScale, 0.5f, 0.1f, 5.0f);
        DrawGridButton("Moon Gravity", ref lowGravity);
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        bool oldFreeze = freezePlayer; DrawGridButton("Freeze Player", ref freezePlayer);
        if (oldFreeze != freezePlayer && playerController != null) playerController.canControl = !freezePlayer;
        DrawStepper("TP Step", ref playerTpStep, 1.0f, 0.5f, 50f);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("TP Fwd", btnStyle, GUILayout.Width(50))) TeleportPlayer(playerController.transform.forward);
        if (GUILayout.Button("TP Bck", btnStyle, GUILayout.Width(50))) TeleportPlayer(-playerController.transform.forward);
        if (GUILayout.Button("TP Lft", btnStyle, GUILayout.Width(50))) TeleportPlayer(-playerController.transform.right);
        if (GUILayout.Button("TP Rgt", btnStyle, GUILayout.Width(50))) TeleportPlayer(playerController.transform.right);
        if (GUILayout.Button("TP Up", btnStyle, GUILayout.Width(50))) TeleportPlayer(Vector3.up);
        if (GUILayout.Button("TP Dwn", btnStyle, GUILayout.Width(50))) TeleportPlayer(Vector3.down);
        GUILayout.EndHorizontal();
    }

    void TeleportPlayer(Vector3 dir) {
        if (playerController != null && playerController.controller != null) {
            playerController.controller.enabled = false;
            playerController.transform.position += dir * playerTpStep;
            playerController.controller.enabled = true;
        }
    }

    void DrawEnemySettings()
    {
        GUILayout.BeginHorizontal();
        bool oldFreeze = freezeEnemy; DrawGridButton("Freeze AI", ref freezeEnemy);
        if (oldFreeze != freezeEnemy) UpdateEnemyFreeze();
        float oldSpeed = customEnemySpeed; DrawStepper("Enemy Speed", ref customEnemySpeed, 1f, 0f, 25f);
        if (oldSpeed != customEnemySpeed) 
        {
            foreach(var e in FindObjectsByType<node_AIMovement>(FindObjectsSortMode.None)) 
                if (e != null) e.speedSettings.chaseSpeed = customEnemySpeed;
        }
        if (GUILayout.Button("Teleport Enemy Here", btnStyle, GUILayout.Width(160)))
        {
            if (playerController && mainEnemy) mainEnemy.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(playerController.transform.position + playerController.transform.forward * 3f);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        DrawGridButton("ESP Box", ref espEnabled);
        DrawGridButton("ESP Line", ref espLineEnabled);
        float oldW = customEnemyWidth; float oldH = customEnemyHeight;
        DrawStepper("Width", ref customEnemyWidth, 0.1f, 0.1f, 5.0f);
        DrawStepper("Height", ref customEnemyHeight, 0.1f, 0.1f, 5.0f);
        if (oldW != customEnemyWidth || oldH != customEnemyHeight)
        {
            foreach(var e in FindObjectsByType<node_AIMovement>(FindObjectsSortMode.None))
            {
                if (e != null && !pancakeMode && !wideMode && !slenderMode)
                    e.transform.localScale = new Vector3(customEnemyWidth, customEnemyHeight, customEnemyWidth);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        DrawGridButton("SpinBot", ref spinBotMode);
        DrawGridButton("Wide Mode", ref wideMode);
        DrawGridButton("Slender Mode", ref slenderMode);
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        DrawGridButton("Pancake Mode", ref pancakeMode);
        DrawGridButton("Mute Audio", ref muteEnemyAudio);
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("LAUNCH TO SPACE!", btnStyle, GUILayout.Width(160))) LaunchEnemiesToSpace();
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.Label("--- Spawner ---", textStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Clone", btnStyle, GUILayout.Width(160))) SpawnClone();
        if (GUILayout.Button("Delete All Clones", btnStyle, GUILayout.Width(160))) DeleteAllClones();
        bool isMainActive = mainEnemy != null && mainEnemy.gameObject.activeSelf;
        if (isMainActive) {
            if (GUILayout.Button("Delete Original", btnStyle, GUILayout.Width(160))) if (mainEnemy) mainEnemy.gameObject.SetActive(false);
        } else {
            if (GUILayout.Button("Restore Original", btnStyle, GUILayout.Width(160))) if (mainEnemy) { mainEnemy.gameObject.SetActive(true); if(playerController) mainEnemy.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(playerController.transform.position + playerController.transform.forward * 5f); }
        }
        GUILayout.EndHorizontal();
    }

    void DrawFunSettings()
    {
        GUILayout.BeginHorizontal();
        DrawStepper("Time Scale", ref customTimeScale, 0.1f, 0.1f, 3.0f, "F1");
        DrawStepper("FOV", ref customFOV, 5f, 30f, 170f, "F0");
        if (GUILayout.Button("Gotta Go Fast!", btnStyle, GUILayout.Width(160))) EnableSonicMode();
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.Label("--- Camera Memes ---", textStyle);
        GUILayout.BeginHorizontal();
        DrawGridButton("Upside Down Cam", ref upsideDownCamera);
        DrawGridButton("Drunk Camera", ref drunkMode);
        DrawGridButton("Acid Mode (FOV)", ref acidMode);
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        DrawGridButton("Earthquake", ref earthquakeMode);
        DrawGridButton("Disco Lighting", ref discoMode);
        if (GUILayout.Button("Reset Camera", btnStyle, GUILayout.Width(150))) ResetCamera();
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.Label("--- World Physics Memes ---", textStyle);
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("Rain Items!", btnStyle, GUILayout.Width(160))) RainItems();
        GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
        if (GUILayout.Button("Yeet All Physics!", btnStyle, GUILayout.Width(160))) YeetPhysics();
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        DrawGridButton("Show Screen Text", ref showScreenText);
        GUILayout.Label("Text:", textStyle, GUILayout.Width(40));
        customScreenText = GUILayout.TextField(customScreenText, 30, GUILayout.Width(150));
        GUILayout.EndHorizontal();
    }

    void DrawItemSpawner()
    {
        if (playerController == null) { GUILayout.Label("Player not found", textStyle); return; }
        DrawItemCategory("Items", itemPrefabs);
        DrawItemCategory("Keys", keyPrefabs);
        DrawItemCategory("Melee", meleePrefabs);
        DrawItemCategory("Guns", gunPrefabs);
    }

    void DrawItemCategory(string title, List<GameObject> list)
    {
        if (list.Count == 0) return;
        GUILayout.Label($"--- {title} ---", textStyle);
        GUILayout.BeginHorizontal();
        int count = 0;
        foreach (var prefab in list)
        {
            if (GUILayout.Button(prefab.name, btnStyle, GUILayout.Width(150), GUILayout.Height(30))) SpawnItem(prefab);
            count++;
            if (count % 4 == 0) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
        }
        GUILayout.EndHorizontal(); GUILayout.Space(10);
    }

    void DrawObjectEditor()
    {
        GUILayout.Label("Spawn Shapes:", textStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cube", btnStyle, GUILayout.Width(80))) SpawnPrimitive(PrimitiveType.Cube, "Cube");
        if (GUILayout.Button("Sphere", btnStyle, GUILayout.Width(80))) SpawnPrimitive(PrimitiveType.Sphere, "Sphere");
        if (GUILayout.Button("Capsule", btnStyle, GUILayout.Width(80))) SpawnPrimitive(PrimitiveType.Capsule, "Capsule");
        if (GUILayout.Button("Cylinder", btnStyle, GUILayout.Width(80))) SpawnPrimitive(PrimitiveType.Cylinder, "Cylinder");
        if (GUILayout.Button("Plane", btnStyle, GUILayout.Width(80))) SpawnPrimitive(PrimitiveType.Plane, "Plane");
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();

        // --- Левая колонка (Рейкаст и свойства) ---
        GUILayout.BeginVertical(GUILayout.Width(350));
        GameObject target = isObjectLocked ? lockedObject : currentHitObject;
        string status = target == null ? "Look at an object to select..." : (isObjectLocked ? $"LOCKED: {target.name}" : $"LOOKING AT: {target.name}");
        
        if (GUILayout.Button(status, btnStyle, GUILayout.Width(340)))
            if (target != null) { isObjectLocked = !isObjectLocked; lockedObject = isObjectLocked ? currentHitObject : null; }

        if (isObjectLocked && lockedObject != null)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Colors:", textStyle, GUILayout.Width(50));
            if(GUILayout.Button("Wht", btnStyle, GUILayout.Width(35))) ChangeObjectColor(Color.white);
            if(GUILayout.Button("Red", btnStyle, GUILayout.Width(35))) ChangeObjectColor(Color.red);
            if(GUILayout.Button("Grn", btnStyle, GUILayout.Width(35))) ChangeObjectColor(Color.green);
            if(GUILayout.Button("Blu", btnStyle, GUILayout.Width(35))) ChangeObjectColor(Color.blue);
            if(GUILayout.Button("Blk", btnStyle, GUILayout.Width(35))) ChangeObjectColor(Color.black);
            if (GUILayout.Button("TP", btnStyle, GUILayout.Width(35))) TeleportObjectToPlayer();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            DrawGridButton("ESP Box", ref objectEspEnabled, 100);

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            DrawStepper("Move", ref editorMoveStep, 0.5f, 0.1f, 5.0f);
            DrawStepper("Rot", ref editorRotStep, 15f, 1f, 90f, "F0");
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            DrawStepper("Scale", ref editorScaleStep, 0.5f, 0.1f, 5.0f);

            GUILayout.Space(10);
            // POSITION
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pos X-", btnStyle, GUILayout.Width(50))) lockedObject.transform.position += Vector3.left * editorMoveStep;
            if (GUILayout.Button("Pos X+", btnStyle, GUILayout.Width(50))) lockedObject.transform.position += Vector3.right * editorMoveStep;
            if (GUILayout.Button("Pos Y-", btnStyle, GUILayout.Width(50))) lockedObject.transform.position += Vector3.down * editorMoveStep;
            if (GUILayout.Button("Pos Y+", btnStyle, GUILayout.Width(50))) lockedObject.transform.position += Vector3.up * editorMoveStep;
            if (GUILayout.Button("Pos Z-", btnStyle, GUILayout.Width(50))) lockedObject.transform.position += Vector3.back * editorMoveStep;
            if (GUILayout.Button("Pos Z+", btnStyle, GUILayout.Width(50))) lockedObject.transform.position += Vector3.forward * editorMoveStep;
            GUILayout.EndHorizontal();

            // ROTATION 
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rot X-", btnStyle, GUILayout.Width(50))) lockedObject.transform.Rotate(-editorRotStep, 0, 0);
            if (GUILayout.Button("Rot X+", btnStyle, GUILayout.Width(50))) lockedObject.transform.Rotate(editorRotStep, 0, 0);
            if (GUILayout.Button("Rot Y-", btnStyle, GUILayout.Width(50))) lockedObject.transform.Rotate(0, -editorRotStep, 0);
            if (GUILayout.Button("Rot Y+", btnStyle, GUILayout.Width(50))) lockedObject.transform.Rotate(0, editorRotStep, 0);
            if (GUILayout.Button("Rot Z-", btnStyle, GUILayout.Width(50))) lockedObject.transform.Rotate(0, 0, -editorRotStep);
            if (GUILayout.Button("Rot Z+", btnStyle, GUILayout.Width(50))) lockedObject.transform.Rotate(0, 0, editorRotStep);
            GUILayout.EndHorizontal();
            
            // SCALE
            GUILayout.BeginHorizontal();
            Vector3 s = lockedObject.transform.localScale;
            if (GUILayout.Button("Scl X-", btnStyle, GUILayout.Width(50))) lockedObject.transform.localScale = new Vector3(s.x - editorScaleStep, s.y, s.z);
            if (GUILayout.Button("Scl X+", btnStyle, GUILayout.Width(50))) lockedObject.transform.localScale = new Vector3(s.x + editorScaleStep, s.y, s.z);
            if (GUILayout.Button("Scl Y-", btnStyle, GUILayout.Width(50))) lockedObject.transform.localScale = new Vector3(s.x, s.y - editorScaleStep, s.z);
            if (GUILayout.Button("Scl Y+", btnStyle, GUILayout.Width(50))) lockedObject.transform.localScale = new Vector3(s.x, s.y + editorScaleStep, s.z);
            if (GUILayout.Button("Scl Z-", btnStyle, GUILayout.Width(50))) lockedObject.transform.localScale = new Vector3(s.x, s.y, s.z - editorScaleStep);
            if (GUILayout.Button("Scl Z+", btnStyle, GUILayout.Width(50))) lockedObject.transform.localScale = new Vector3(s.x, s.y, s.z + editorScaleStep);
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            Rigidbody rb = lockedObject.GetComponent<Rigidbody>();
            if (rb != null) if (GUILayout.Button(rb.isKinematic ? "Physics: OFF" : "Physics: ON", btnStyle, GUILayout.Width(100))) rb.isKinematic = !rb.isKinematic;
            if (GUILayout.Button(lockedObject.activeSelf ? "Active: ON" : "Active: OFF", btnStyle, GUILayout.Width(100))) lockedObject.SetActive(!lockedObject.activeSelf);
            if (GUILayout.Button("DELETE OBJECT", btnStyle, GUILayout.Width(120))) { Destroy(lockedObject); isObjectLocked = false; lockedObject = null; objectEspEnabled = false; }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        // --- Правая колонка (Иерархия) ---
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Hierarchy:", textStyle, GUILayout.Width(80));
        if (GUILayout.Button("Refresh", btnStyle, GUILayout.Width(80))) RefreshHierarchy();
        GUILayout.EndHorizontal();
        
        hierarchyScrollPosition = GUILayout.BeginScrollView(hierarchyScrollPosition, GUILayout.Height(300));
        foreach (var obj in sceneObjects)
        {
            if (obj == null) continue;
            DrawHierarchyNode(obj, 0);
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    void DrawHierarchyNode(GameObject obj, int indentLevel)
    {
        if (obj == null) return;
        
        GUILayout.BeginHorizontal();
        if (indentLevel > 0) GUILayout.Space(indentLevel * 15);
        
        bool hasChildren = obj.transform.childCount > 0;
        if (hasChildren)
        {
            bool isExpanded = expandedObjects.Contains(obj);
            if (GUILayout.Button(isExpanded ? "-" : "+", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
            {
                if (isExpanded) expandedObjects.Remove(obj);
                else expandedObjects.Add(obj);
            }
        }
        else
        {
            GUILayout.Space(29); // Свободное место, если нет детей (чтобы выровнять)
        }
        
        if (!obj.activeInHierarchy) GUI.color = Color.gray;
        if (lockedObject == obj && isObjectLocked) GUI.color = Color.green;

        if (GUILayout.Button(obj.name, tabStyle, GUILayout.Height(25)))
        {
            isObjectLocked = true;
            lockedObject = obj;
        }
        GUI.color = Color.white;
        GUILayout.EndHorizontal();

        if (hasChildren && expandedObjects.Contains(obj))
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                DrawHierarchyNode(obj.transform.GetChild(i).gameObject, indentLevel + 1);
            }
        }
    }

    void DrawMenuManager()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Menu Title Text:", textStyle, GUILayout.Width(120));
        menuTitle = GUILayout.TextField(menuTitle, GUILayout.Width(300));
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        DrawStepper("UI Opacity", ref uiOpacity, 0.1f, 0f, 1f);
        GUILayout.Space(20);
        DrawStepper("Menu Scale", ref menuScale, 0.1f, 0.5f, 3.0f);
        GUILayout.Space(20);
        DrawGridButton("Invisible MOD Button", ref hideModButtonVisually, 200);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        GUILayout.Label("Background Color Presets:", textStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Dark Blue", btnStyle, GUILayout.Width(100))) ChangeBgColor(new Color(0.07f, 0.05f, 0.15f, 0.95f));
        if (GUILayout.Button("Black", btnStyle, GUILayout.Width(100))) ChangeBgColor(new Color(0f, 0f, 0f, 0.95f));
        if (GUILayout.Button("Dark Red", btnStyle, GUILayout.Width(100))) ChangeBgColor(new Color(0.15f, 0.02f, 0.02f, 0.95f));
        if (GUILayout.Button("Dark Green", btnStyle, GUILayout.Width(100))) ChangeBgColor(new Color(0.02f, 0.15f, 0.02f, 0.95f));
        if (GUILayout.Button("Pink", btnStyle, GUILayout.Width(100))) ChangeBgColor(new Color(0.2f, 0.05f, 0.15f, 0.95f));
        GUILayout.EndHorizontal();
    }

    void RefreshHierarchy()
    {
        sceneObjects.Clear();
        // Находим все объекты в сцене
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            // Убираем скрытые объекты или объекты, которые нам не нужны в иерархии (по желанию)
            if (obj.transform.parent == null)
            {
                sceneObjects.Add(obj);
            }
        }
    }

    // --- EDITOR LOGIC & HELPERS ---
    void SpawnPrimitive(PrimitiveType type, string typeName)
    {
        if (playerController == null) return;
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = $"Editor_{typeName}_{UnityEngine.Random.Range(100,999)}";
        obj.transform.position = playerController.transform.position + playerController.transform.forward * 3f + Vector3.up * 1f;
        Renderer r = obj.GetComponent<Renderer>();
        if (buildingMaterial != null && r != null) r.material = new Material(buildingMaterial);
        Rigidbody rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true; 
    }
    void ChangeObjectColor(Color c) { if (lockedObject != null) { Renderer r = lockedObject.GetComponent<Renderer>(); if (r != null) r.material.color = c; } }
    void TeleportObjectToPlayer() { if (lockedObject != null && playerController != null) lockedObject.transform.position = playerController.transform.position + playerController.transform.forward * 2f; }
    void SpawnItem(GameObject prefab) { if (playerController == null || prefab == null) return; Vector3 pos = playerController.transform.position + playerController.transform.forward * 1.5f + Vector3.up * 1.0f; GameObject obj = Instantiate(prefab, pos, Quaternion.identity); Rigidbody rb = obj.GetComponent<Rigidbody>(); if (rb != null) { rb.isKinematic = false; rb.AddForce(playerController.transform.forward * 2f, ForceMode.Impulse); } }
    
    void SpawnClone() 
    { 
        if (enemyPrefabTemplate) 
        { 
            Vector3 spawnPos = Vector3.zero;
            if (mainEnemy != null && mainEnemy.gameObject.activeInHierarchy) spawnPos = mainEnemy.transform.position + mainEnemy.transform.forward * 3f;
            else if (playerController != null) spawnPos = playerController.transform.position + playerController.transform.forward * 3f;
            else return;

            GameObject clone;
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                clone = Instantiate(enemyPrefabTemplate, hit.position, Quaternion.identity); clone.SetActive(true);
                var agent = clone.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null) agent.Warp(hit.position); 
            }
            else 
            {
                clone = Instantiate(enemyPrefabTemplate, spawnPos, Quaternion.identity); clone.SetActive(true);
            }

            var ai = clone.GetComponent<node_AIMovement>();
            if (ai != null)
            {
                ai.speedSettings.chaseSpeed = customEnemySpeed;
                if (!pancakeMode && !wideMode && !slenderMode) ai.transform.localScale = new Vector3(customEnemyWidth, customEnemyHeight, customEnemyWidth);
            }
        } 
    }

    void DeleteAllClones() { var all = FindObjectsByType<node_AIMovement>(FindObjectsSortMode.None); foreach (var e in all) if (e != mainEnemy) Destroy(e.gameObject); }
    void UpdateEnemyVision() { foreach (var e in FindObjectsByType<node_AIMovement>(FindObjectsSortMode.None)) { e.detectionSettings.normalVisionDistance = invisibleMode ? 0f : 40f; e.detectionSettings.pursueVisionDistance = invisibleMode ? 0f : 100f; e.detectionSettings.visionDistance = invisibleMode ? 0f : 40f; } }
    void UpdateEnemyFreeze() { foreach (var e in FindObjectsByType<node_AIMovement>(FindObjectsSortMode.None)) { var agent = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); if(agent) agent.isStopped = freezeEnemy; e.enabled = !freezeEnemy; } }
    
    // --- MEME FUNCTIONS ---
    void EnableSonicMode()
    {
        customWalkSpeed = 50f;
        customJumpForce = 25f;
        customFOV = 130f;
        if (playerController) { playerController.walkSpeed = 50f; playerController.runSpeed = 80f; playerController.jumpForce = 25f; }
    }

    void RainItems()
    {
        if (playerController == null) return;
        List<GameObject> allItems = new List<GameObject>();
        allItems.AddRange(itemPrefabs); allItems.AddRange(keyPrefabs); allItems.AddRange(meleePrefabs); allItems.AddRange(gunPrefabs);
        if (allItems.Count == 0) return;

        for (int i = 0; i < 30; i++)
        {
            GameObject randomPrefab = allItems[UnityEngine.Random.Range(0, allItems.Count)];
            Vector3 spawnPos = playerController.transform.position + Vector3.up * 15f + (Vector3)UnityEngine.Random.insideUnitCircle * 10f;
            GameObject obj = Instantiate(randomPrefab, spawnPos, Quaternion.identity);
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.velocity = UnityEngine.Random.insideUnitSphere * 2f; }
        }
    }

    void YeetPhysics()
    {
        Rigidbody[] rbs = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        Vector3 origin = playerController != null ? playerController.transform.position : Vector3.zero;
        foreach (var rb in rbs)
        {
            if (rb.gameObject == playerController?.gameObject) continue; // Игрока не трогаем
            if (rb.isKinematic) rb.isKinematic = false;
            rb.AddExplosionForce(5000f, origin, 100f, 10f); // Сильный отлет + вверх
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 100f, ForceMode.VelocityChange); // Раскрутка
        }
    }

    void LaunchEnemiesToSpace()
    {
        foreach (var enemy in FindObjectsByType<node_AIMovement>(FindObjectsSortMode.None))
        {
            if (enemy == null || !enemy.gameObject.activeSelf) continue;
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null) agent.enabled = false; 
            var rb = enemy.GetComponent<Rigidbody>(); if (rb == null) rb = enemy.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.AddForce(Vector3.up * 45f, ForceMode.VelocityChange);
            rb.AddTorque(new Vector3(10f, 30f, 20f), ForceMode.VelocityChange);
        }
    }

    void DrawAllEnemiesESP(float sw, float sh) { foreach (var enemy in FindObjectsByType<node_AIMovement>(FindObjectsSortMode.None)) { if (enemy && enemy.gameObject.activeSelf) DrawBoxESP(enemy.transform, sh, false); } }
    void DrawBoxESP(Transform t, float sh, bool isObject) {
        if(bgTex == null) return;
        float hScale = isObject ? (t.GetComponent<Collider>() != null ? t.GetComponent<Collider>().bounds.size.y : t.localScale.y) : t.localScale.y; 
        Vector3 foot = t.position; 
        
        if (isObject) {
            Collider c = t.GetComponent<Collider>();
            if (c != null) foot = c.bounds.min;
        }

        Vector3 head = isObject ? (t.GetComponent<Collider>() != null ? new Vector3(foot.x, t.GetComponent<Collider>().bounds.max.y, foot.z) : foot + Vector3.up * 1.9f * hScale) : foot + Vector3.up * 1.9f * hScale;
        Vector3 w2s_f = mainCam.WorldToScreenPoint(foot); Vector3 w2s_h = mainCam.WorldToScreenPoint(head);
        if (w2s_f.z <= 0) return;
        
        // Переводим координаты для scaled matrix
        if (Application.isMobilePlatform) 
        {
            w2s_f.x *= 1280f / Screen.width; w2s_f.y *= 720f / Screen.height;
            w2s_h.x *= 1280f / Screen.width; w2s_h.y *= 720f / Screen.height;
        }

        w2s_f.x /= menuScale; w2s_f.y /= menuScale;
        w2s_h.x /= menuScale; w2s_h.y /= menuScale;
        
        float hY = sh - w2s_h.y; float fY = sh - w2s_f.y; float h = fY - hY; 
        float w = isObject ? (t.GetComponent<Collider>() != null ? t.GetComponent<Collider>().bounds.size.x / menuScale * (100f / w2s_f.z) : h / 2f * t.localScale.x) : h / 2f * t.localScale.x; 
        
        DrawRectOutline(new Rect(w2s_f.x - w/2f, hY, w, h), 2f, whiteTex);

        if (!isObject && espLineEnabled)
        {
            DrawLine(new Vector2(Screen.width / 2f / menuScale, sh), new Vector2(w2s_f.x, fY), whiteTex, 2f);
        }
    }
    void DrawRectOutline(Rect r, float t, Texture2D tex) {
        if(tex == null) return;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), tex); GUI.DrawTexture(new Rect(r.x, r.y + r.height - t, r.width, t), tex); 
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), tex); GUI.DrawTexture(new Rect(r.x + r.width - t, r.y, t, r.height), tex);
    }
    void DrawLine(Vector2 start, Vector2 end, Texture2D tex, float width)
    {
        Vector2 d = end - start;
        float a = Mathf.Rad2Deg * Mathf.Atan2(d.y, d.x);
        float l = d.magnitude;

        Matrix4x4 backupMatrix = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(l, width), new Vector2(start.x, start.y + 0.5f));
        GUIUtility.RotateAroundPivot(a, start);
        GUI.DrawTexture(new Rect(start.x, start.y, 1, 1), tex);
        GUI.matrix = backupMatrix;
    }
}