using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class EasyLoading : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreenPrefab; // Choose a prefab of loading canvas. It must contain a Slider.
    [SerializeField] private float loadingSmoothingSpeed = 10f;
    private GameObject loadingScreenInstance;
    private Slider loadingBar;
    public static EasyLoading Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Another instance of {nameof(EasyLoading)} already exists. Let me destroy this, okay?");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Object instance always exists in the scene. Must be called from another script.
    }

    private void OnValidate()
    {
        if (loadingSmoothingSpeed < 1)
        {
            loadingSmoothingSpeed = 1;
        }
    }

    // Call this function from buttons and anything you want!
    public void ChangeSceneWithLoading(string sceneName)
    {
        if (!loadingScreenInstance && loadingScreenPrefab)
        {
            loadingScreenInstance = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(loadingScreenInstance);
            loadingBar = loadingScreenInstance.GetComponentInChildren<Slider>();
        }

        StartCoroutine(LoadSceneAsync(sceneName));
    }
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Cursor.lockState = CursorLockMode.Locked;

        foreach (var obj in GameObject.FindGameObjectsWithTag("Music")) Destroy(obj);
        foreach (var obj in GameObject.FindGameObjectsWithTag("SFX")) Destroy(obj);

        if (loadingScreenInstance) loadingScreenInstance.SetActive(true);

        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float displayedProgress = 0f;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.deltaTime * loadingSmoothingSpeed);

            if (loadingBar) loadingBar.value = displayedProgress;

            if (displayedProgress >= 1f && targetProgress >= 1f)
            {
                Cursor.lockState = CursorLockMode.None;
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        if (loadingScreenInstance) Destroy(loadingScreenInstance);
    }

    public void ChangeScene(string sceneName) => SceneManager.LoadScene(sceneName);
}
