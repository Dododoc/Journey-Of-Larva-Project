using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // ★ 마우스 감지를 위해 필수!

public class SkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Skill Info")]
    public string skillName;
    [TextArea] public string skillDescription;

    [Header("UI Elements")]
    public Image cooldownOverlay; // 아까 만든 Filled 타입 검은 덮개 연결
    public GameObject lockIcon;   // 자물쇠 아이콘 (궁극기 전용)

    private float maxCooldown;
    private float currentCooldown;

    void Start()
    {
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f; 
    }

    void Update()
    {
        // 쿨타임이 남아있다면 서서히 줄여서 덮개를 벗깁니다.
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
            cooldownOverlay.fillAmount = currentCooldown / maxCooldown;
        }
    }

    // 플레이어 스크립트(개미/풍뎅이)에서 스킬을 쓸 때 이 함수를 쏠 겁니다!
    public void StartCooldown(float cooldownTime)
    {
        maxCooldown = cooldownTime;
        currentCooldown = cooldownTime;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 1f; // 100% 까맣게 덮어서 시작
    }

    // 기믹을 풀어 잠금을 해제할 때 부를 함수
    public void UnlockSkill()
    {
        if (lockIcon != null) lockIcon.SetActive(false);
    }

    // ==========================================
    // 마우스 호버 기능 (툴팁 띄우기)
    // ==========================================

    // ==========================================
    // 마우스 호버 기능 (툴팁 띄우기)
    // ==========================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 자물쇠가 연결되어 있고, 그 자물쇠가 켜져(잠겨) 있다면
        if (lockIcon != null && lockIcon.activeSelf)
        {
            // 만약 잠겼을 때 "미해금"이라는 툴팁을 띄우고 싶다면 아래 코드를 쓸 수 있습니다.
            // TooltipManager.Instance.ShowTooltip("잠긴 스킬", "아직 해금되지 않은 스킬입니다.");
            return; // 아무것도 안 띄우고 싶다면 원래대로 return만 둡니다.
        }

        // ★ 소문자 instance를 대문자 Instance로 수정!
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowTooltip(skillName, skillDescription);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ★ 소문자 instance를 대문자 Instance로 수정!
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
}