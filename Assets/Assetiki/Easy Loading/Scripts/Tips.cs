using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class Tips : MonoBehaviour
{
    [SerializeField] private string[] TipsTexts;
    private Text TipsText;

    private void Start()
    {
        TipsText = GetComponent<Text>();
        GenerateTip();
    }
    public void GenerateTip()
    {
        int RadnomSplash = Random.Range(0, TipsTexts.Length);
        TipsText.text = TipsTexts[RadnomSplash];
    }
}
