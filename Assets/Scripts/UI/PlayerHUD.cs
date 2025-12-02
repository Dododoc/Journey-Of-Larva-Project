using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★ TextMeshPro를 쓰려면 이게 꼭 있어야 합니다!

public class PlayerHUD : MonoBehaviour
{
    [Header("HP UI")]
    public Image hpFillImage;       // HP바 이미지
    public TextMeshProUGUI hpText;  // ★ HP 숫자 텍스트 (예: 100 / 100)

    [Header("XP UI")]
    public Image xpFillImage;       // XP바 이미지
    public TextMeshProUGUI xpText;  // ★ XP 숫자 텍스트 (예: 50 / 100)

    [Header("Level UI")]
    public TextMeshProUGUI levelText; // ★ 레벨 텍스트 (예: Lv. 1)

    // 1. HP 갱신 (숫자 표시 포함)
    public void UpdateHP(float currentHP, float maxHP)
    {
        // 바 채우기
        if (hpFillImage != null)
            hpFillImage.fillAmount = (maxHP > 0) ? (currentHP / maxHP) : 0;

        // ★ 텍스트 표시 방법 (핵심!)
        if (hpText != null)
        {
            // $"{변수:F0}" -> 소수점 없이 정수로 보여달라는 뜻입니다.
            hpText.text = $"{currentHP:F0} / {maxHP:F0}";
        }
    }

    // 2. XP 갱신 (숫자 표시 포함)
    public void UpdateXP(float currentXP, float maxXP)
    {
        if (xpFillImage != null)
            xpFillImage.fillAmount = (maxXP > 0) ? (currentXP / maxXP) : 0;

        if (xpText != null)
        {
            // "현재 / 목표" 형태로 표시
            xpText.text = $"{currentXP:F0} / {maxXP:F0}";
        }
    }

    // 3. 레벨 갱신
    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv. {level}";
        }
    }
}