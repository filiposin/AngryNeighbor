using System.Collections;
using UnityEngine;

public class FirstLaunch : MonoBehaviour
{
    public GameObject skipper;

    public GameObject slider1;

    public GameObject slider2;

    public GameObject slider3;

    private void Start()
    {
        skipper.SetActive(true);
        
        StartCoroutine("TimeClose");

        if (!PlayerPrefs.HasKey("firslaunch"))
        {
            PlayerPrefs.SetString("firslaunch", "first");
            Debug.Log("первый запуск");
        }
    }

    public void Skip()
    {
        slider1.SetActive(false);
        slider2.SetActive(false);
        slider3.SetActive(false);
        skipper.SetActive(false);
    }

    private IEnumerator TimeClose()
    {
        yield return new WaitForSeconds(23f);
        if (skipper != null)
        {
            skipper.SetActive(false);
        }
    }

    public void SaveDateDelete()
    {
        PlayerPrefs.DeleteAll();
    }
}