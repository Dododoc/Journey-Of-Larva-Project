using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;
    
    [Header("Tooltip UI")]
    public GameObject tooltipPanel;    // 만들어둔 툴팁 창 패널 연결
    public TextMeshProUGUI titleText;  // 툴팁 제목 텍스트 연결
    public TextMeshProUGUI descText;   // 툴팁 설명 텍스트 연결

    void Awake() 
    { 
        if (instance == null) instance = this; 
    }

    void Start() 
    { 
        tooltipPanel.SetActive(false); // 시작할 땐 숨김
    }

    public void ShowTooltip(string title, string desc)
    {
        titleText.text = title;
        descText.text = desc;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
}