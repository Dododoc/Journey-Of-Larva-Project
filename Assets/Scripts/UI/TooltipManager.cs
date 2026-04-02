using UnityEngine;
using TMPro;
using UnityEngine.UI; // 추가 필요
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    public GameObject tooltipPanel;
    public TextMeshProUGUI titleText; // 우리가 쓰던 Title_Text와 연결
    public TextMeshProUGUI descText;  // 우리가 쓰던 Desc_Text와 연결

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            Vector2 mousePos = Input.mousePosition;
            // 마우스 우측 하단으로 툴팁 위치 약간 이동
            tooltipPanel.transform.position = new Vector2(mousePos.x + 15f, mousePos.y - 15f);
        }
    }

    public void ShowTooltip(string title, string desc)
{
    titleText.text = title;
    descText.text = desc;
    tooltipPanel.SetActive(true);

    // ★ 레이아웃을 즉시 다시 계산하라고 명령 (0,0 문제 해결)
    LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel.GetComponent<RectTransform>());
}

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
}