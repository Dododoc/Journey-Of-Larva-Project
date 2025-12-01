using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject statusQuestPanel;     // 'Q' 키로 여는 창
    public GameObject evolutionPopupPanel;  // 진화 가능 알림 'R' 팝업
    public GameObject evolutionChoicePanel; // 3가지 루트 선택 창

    private bool isEvolutionReady = false; // 진화 가능 상태인지? (퀘스트 완료 시 true됨)

    void Update()
    {
        // 'Q' 키: 스테이터스/퀘스트 창 토글
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleUI(statusQuestPanel);
        }

        // 'R' 키: 진화 팝업이 떠있을 때 누르면 선택창 열기
        if (Input.GetKeyDown(KeyCode.R) && isEvolutionReady && evolutionPopupPanel.activeSelf)
        {
            OpenEvolutionChoiceWindow();
        }
    }

    // 창 열고 닫기 공용 함수
    void ToggleUI(GameObject panel)
    {
        bool isActive = panel.activeSelf;
        panel.SetActive(!isActive);

        // 창이 열리면 게임 일시정지, 닫히면 재개 (선택 사항)
        // Time.timeScale = !isActive ? 0f : 1f; 
    }

    // --- 외부(퀘스트 매니저)에서 호출할 함수들 ---

    // 모든 퀘스트 완료 시 호출
    public void OnAllQuestsCompleted()
    {
        isEvolutionReady = true;
        evolutionPopupPanel.SetActive(true); // 'R'키를 누르세요! 팝업 띄우기
        Debug.Log("진화 준비 완료! R키를 눌러 진화하세요.");
    }

    // 'R'을 눌렀을 때 호출
    void OpenEvolutionChoiceWindow()
    {
        evolutionPopupPanel.SetActive(false); // 팝업 닫고
        evolutionChoicePanel.SetActive(true); // 선택창 열기
        // 이때부터 방향키 입력으로 선택하는 로직이 필요합니다. (다음 단계)
    }

    // 진화 완료 후 호출 (PlayerStats에서 호출)
    public void CloseEvolutionPopup()
    {
        isEvolutionReady = false;
        evolutionChoicePanel.SetActive(false);
        // 스테이터스 창 UI 갱신 요청 등
    }
}