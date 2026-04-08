using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // ★ 현재 무슨 맵인지 확인하기 위해 추가!
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("UI 슬라이드 설정")]
    public RectTransform questPanel;
    public float slideSpeed = 8f;
    public float visiblePosX = -20f;
    public float hiddenPosX = 300f;

    [Header("Tap 튜토리얼 설정")]
    public TextMeshProUGUI tapPromptText; 
    public float blinkSpeed = 4f;         

    // ★ [추가됨] 맵 설정
    [Header("맵 설정")]
    public string larvaSceneName = "LarvaMap"; // 인스펙터에서 애벌레 맵의 정확한 이름을 입력하세요!

    [System.Serializable]
    public class QuestLine
    {
        public GameObject questObject;
        public Image checkbox;
        public TextMeshProUGUI questText;
    }

    [Header("퀘스트 데이터")]
    public QuestLine[] quests;
    public Sprite emptyBoxSprite;
    public Sprite checkedBoxSprite;

    private bool isVisible = false;
    private int currentQuestIndex = 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // ★ [추가] 컴퓨터가 기억하는 튜토리얼 완료 기록을 강제로 삭제합니다 (테스트용)
        PlayerPrefs.DeleteKey("HasTapped");
        // 1. 현재 맵이 '애벌레 맵'인지 확인합니다.
        if (SceneManager.GetActiveScene().name != larvaSceneName)
        {
            // 애벌레 맵이 아니라면 퀘스트 창과 Tap 텍스트를 아예 꺼버립니다.
            questPanel.gameObject.SetActive(false);
            if (tapPromptText != null) tapPromptText.gameObject.SetActive(false);
            return; // 아래 코드는 무시하고 종료
        }

        // --- 여기서부터는 '애벌레 맵'일 때만 실행됩니다 ---
        questPanel.gameObject.SetActive(true);
        questPanel.anchoredPosition = new Vector2(hiddenPosX, questPanel.anchoredPosition.y);
        UpdateQuestUI();

        // 2. Tap 텍스트 최초 1회만 표시 (기록 확인)
        // 컴퓨터에 "HasTapped"라는 기록이 1로 저장되어 있다면 이미 누른 것입니다.
        if (PlayerPrefs.GetInt("HasTapped", 0) == 1)
        {
            if (tapPromptText != null) tapPromptText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 애벌레 맵이 아니면 Update 기능도 작동하지 않게 막습니다.
        if (SceneManager.GetActiveScene().name != larvaSceneName) return;

        // 1. "Press Tab!" 튜토리얼 글자 깜빡임 처리 (켜져 있을 때만)
        if (tapPromptText != null && tapPromptText.gameObject.activeSelf)
        {
            Color textColor = tapPromptText.color;
            textColor.a = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            tapPromptText.color = textColor;
        }

        // ==========================================================
        // ★ [수정됨] 2. 키보드 'Tab' 키를 눌렀을 때의 동작
        // ==========================================================
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // 퀘스트 창 열기/닫기 상태를 뒤집습니다.
            isVisible = !isVisible;

            // 만약 튜토리얼 안내 문구가 아직 화면에 떠 있다면?
            if (tapPromptText != null && tapPromptText.gameObject.activeSelf)
            {
                // 글자를 영원히 지워버립니다.
                tapPromptText.gameObject.SetActive(false);

                // 컴퓨터에 "이 유저는 튜토리얼을 깼음"이라고 영구 저장합니다.
                PlayerPrefs.SetInt("HasTapped", 1);
                PlayerPrefs.Save();

                // 처음 누른 순간에는 무조건 창이 '열리도록' 고정해줍니다.
                isVisible = true; 
            }
        }

        // 3. 목표 위치를 정해두고 부드럽게 이동 (스윽~ 슬라이딩 효과)
        float targetX = isVisible ? visiblePosX : hiddenPosX;
        Vector2 targetPos = new Vector2(targetX, questPanel.anchoredPosition.y);
        questPanel.anchoredPosition = Vector2.Lerp(questPanel.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
    }

    // (이하 CompleteQuest와 UpdateQuestUI 함수는 기존과 완전히 동일합니다)
    public void CompleteQuest(int questID)
    {
        if (questID == currentQuestIndex)
        {
            currentQuestIndex++;
            UpdateQuestUI();
            isVisible = true; 
        }
    }

    void UpdateQuestUI()
    {
        for (int i = 0; i < quests.Length; i++)
        {
            if (i == currentQuestIndex)
            {
                quests[i].questObject.SetActive(true);
                quests[i].checkbox.sprite = emptyBoxSprite;
                quests[i].questText.color = Color.white;
            }
            else if (i < currentQuestIndex)
            {
                quests[i].questObject.SetActive(true);
                quests[i].checkbox.sprite = checkedBoxSprite;
                quests[i].questText.color = Color.gray;
            }
            else
            {
                quests[i].questObject.SetActive(false);
            }
        }
        // ==========================================================
        // ★ [추가된 코드] UI가 갱신될 때마다, 혹시 이미 달성한 퀘스트가 있는지 검사합니다!
        // ==========================================================
        CheckAutoCompletes();
        
    }
    void CheckAutoCompletes()
    {
        // 애벌레 맵이 아니면 검사하지 않음
        if (SceneManager.GetActiveScene().name != larvaSceneName) return;

        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player == null) return;

        // ==========================================================
        // ★ [핵심 수정] 4번째 퀘스트 (인덱스 3)가 '레벨 3 달성' 퀘스트입니다!
        // 이 번호가 4(5번째 퀘스트)로 되어 있어서 전갈 퀘스트가 멋대로 깨지는 버그가 있었습니다.
        // ==========================================================
        if (currentQuestIndex == 3) 
        {
            // 이미 레벨이 3 이상이라면?
            if (player.currentLevel >= 3) 
            {
                Debug.Log("레벨 3 달성 퀘스트 자동 완료!");
                CompleteQuest(3); // 즉시 4번째 퀘스트 완료 처리!
            }
        }
    }
    
}