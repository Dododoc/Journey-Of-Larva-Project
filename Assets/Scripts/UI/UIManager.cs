using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections; 

public class UIManager : MonoBehaviour
{
    [Header("UI Panels (연결 필수!)")]
    public GameObject questWindow;      // Q키로 여는 퀘스트 창
    public GameObject evolutionPopup;   // "진화 가능! R키를 누르세요" 알림 띠
    public GameObject evolutionChoice;  // 진화 루트 선택 창 (버튼 3개)
    
    [Header("Notice System")]
    public GameObject noticePanel;      // 중앙 하단 알림 메시지 패널
    public TextMeshProUGUI noticeText;  // 알림 메시지 텍스트

    // 내부 상태 변수
    private bool isQuestAllClear = false; // 퀘스트를 다 깼는지 확인하는 변수

    void Start()
    {
        // ★ 1. 게임 시작 시 모든 팝업창 강제로 끄기
        if (questWindow != null) questWindow.SetActive(false);
        if (evolutionPopup != null) evolutionPopup.SetActive(false);
        if (evolutionChoice != null) evolutionChoice.SetActive(false);
        if (noticePanel != null) noticePanel.SetActive(false);
    }

    void Update()
    {
        // ★ 2. Q키: 퀘스트 창 열고 닫기 (토글)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (questWindow != null)
            {
                bool isActive = questWindow.activeSelf; // 현재 켜져있는지 확인
                questWindow.SetActive(!isActive);       // 반대로 변경 (켜져있으면 끄고, 꺼져있으면 켬)
            }
        }

        // ★ 3. R키: 진화 창 열기 로직
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 조건 A: 퀘스트를 다 깼고, 화면에 "R키 누르세요" 알림이 떠 있을 때
            if (isQuestAllClear) 
            {
                OpenEvolutionChoiceWindow();
            }
            // 조건 B: 아직 퀘스트를 안 깼는데 눌렀을 때
            else
            {
                ShowNotice("아직 진화할 수 없습니다! 퀘스트를 완료하세요.");
            }
        }

        // (테스트용) T키를 누르면 강제로 퀘스트 완료 처리 (나중에 삭제)
        if (Input.GetKeyDown(KeyCode.T))
        {
            QuestAllCompleted(); 
        }
    }

    // --- 외부에서 호출하는 함수들 ---

    // 퀘스트가 모두 완료되었을 때 호출 (QuestManager 등에서 부름)
    public void QuestAllCompleted()
    {
        isQuestAllClear = true; // 상태 변경
        
        if (evolutionPopup != null)
            evolutionPopup.SetActive(true); // "진화 가능! R키를 누르세요" 팝업 띄움
        
        ShowNotice("모든 퀘스트 완료! R키를 눌러 진화하세요.");
    }

    // R키 눌렀을 때: 진화 선택창 열기
    void OpenEvolutionChoiceWindow()
    {
        if (evolutionPopup != null) evolutionPopup.SetActive(false); // R키 알림 끄고
        if (questWindow != null) questWindow.SetActive(false);       // 퀘스트 창도 끄고 (깔끔하게)
        if (evolutionChoice != null) evolutionChoice.SetActive(true); // 선택창 열기
    }

    // 진화 선택 완료 후 호출 (PlayerStats에서 진화 끝난 뒤 부름)
    public void CloseEvolutionPopup()
    {
        if (evolutionChoice != null) evolutionChoice.SetActive(false); // 선택창 닫기
        isQuestAllClear = false; // 진화했으니 상태 초기화 (중복 진화 방지)
        ShowNotice("진화 성공!");
    }

    // --- 알림 메시지 시스템 ---
    public void ShowNotice(string msg)
    {
        if (noticePanel == null || noticeText == null) return;
        noticeText.text = msg;
        noticePanel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideNoticeRoutine());
    }

    IEnumerator HideNoticeRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        noticePanel.SetActive(false);
    }
}