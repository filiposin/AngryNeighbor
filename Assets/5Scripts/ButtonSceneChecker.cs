using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonSceneChecker : MonoBehaviour
{
    [Header("Ссылка на объект кнопки (можно оставить пустым)")]
    [SerializeField] private GameObject buttonObject;

    [Header("Имена сцен (без учёта регистра)")]
    [SerializeField] private string sceneWhereButtonShouldBeActive = "world";
    [SerializeField] private string sceneWhereButtonShouldBeInactive = "new menu";

    void Awake()
    {
        // если не указали — попробуем найти кнопку в дочерних (true позволяет найти и выключенные объекты)
        if (buttonObject == null)
        {
            Button btn = GetComponentInChildren<Button>(true);
            if (btn != null) buttonObject = btn.gameObject;
        }
    }

    void Start()
    {
        // сразу выставим состояние для текущей сцены
        UpdateActiveState(SceneManager.GetActiveScene().name);
        // и подпишемся на смену сцен
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateActiveState(scene.name);
    }

    private void UpdateActiveState(string sceneName)
    {
        if (buttonObject == null) return; // ничего не делаем, если ссылки нет

        string s = sceneName.Trim().ToLower();
        string activ = sceneWhereButtonShouldBeActive.Trim().ToLower();
        string inactiv = sceneWhereButtonShouldBeInactive.Trim().ToLower();

        if (s == activ)
            SetButton(true);
        else if (s == inactiv)
            SetButton(false);
        else
            SetButton(false); // по умолчанию выключаем в других сценах, можешь изменить
    }

    private void SetButton(bool on)
    {
        // безопасно включаем/выключаем сам объект кнопки
        if (buttonObject.activeSelf != on)
            buttonObject.SetActive(on);
    }
}
