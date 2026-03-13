using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★ 1. 이 줄을 맨 위에 추가하세요! (TextMeshPro를 쓰겠다는 뜻)
public class QuestManager : MonoBehaviour
{
    // 어디서든 쉽게 접근할 수 있도록 싱글톤 패턴 사용
    public static QuestManager instance;

    [Header("UI 슬라이드 설정")]
    public RectTransform questPanel;
    public float slideSpeed = 8f;       // 스르륵 나타나는 속도
    public float visiblePosX = -20f;    // 화면에 보일 때 X 위치 (우측 여백 20)
    public float hiddenPosX = 300f;     // 화면 밖으로 숨었을 때 X 위치

    [System.Serializable]
    public class QuestLine
    {
        public GameObject questObject;  // 퀘스트 한 줄(체크박스+텍스트) 전체
        public Image checkbox;          // 체크박스 이미지

        public TextMeshProUGUI questText;
    }

    [Header("퀘스트 데이터")]
    public QuestLine[] quests;          // 여기에 5개의 퀘스트를 연결합니다.
    public Sprite emptyBoxSprite;       // 빈 체크박스 이미지
    public Sprite checkedBoxSprite;     // 체크된 박스 이미지

    private bool isVisible = false;     // Tab키 창 활성화 여부
    private int currentQuestIndex = 0;  // 현재 진행 중인 퀘스트 번호 (0부터 시작)

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // 시작할 때는 창을 화면 밖으로 숨김
        questPanel.anchoredPosition = new Vector2(hiddenPosX, questPanel.anchoredPosition.y);
        UpdateQuestUI();
    }

    void Update()
    {
        // Tab 키를 누르면 껐다 켜기
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isVisible = !isVisible;
        }

        // 목표 위치를 정해두고 부드럽게 이동 (스윽~ 슬라이딩 효과)
        float targetX = isVisible ? visiblePosX : hiddenPosX;
        Vector2 targetPos = new Vector2(targetX, questPanel.anchoredPosition.y);
        questPanel.anchoredPosition = Vector2.Lerp(questPanel.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
    }

    // 다른 스크립트에서 퀘스트를 완료시킬 때 부르는 함수
    // 예: QuestManager.instance.CompleteQuest(0); // 0번(무당벌레) 완료!
    public void CompleteQuest(int questID)
    {
        // 완료하려는 퀘스트가 현재 목표가 맞다면 진행
        if (questID == currentQuestIndex)
        {
            currentQuestIndex++;   // 다음 퀘스트로 넘어감
            UpdateQuestUI();       // 화면 갱신
            
            // 퀘스트를 깨면 창이 닫혀있어도 스윽 열려서 보여주기 (옵션)
            isVisible = true; 
        }
    }

    // 현재 퀘스트 진행도에 따라 UI 색상과 투명도를 갱신
    void UpdateQuestUI()
    {
        for (int i = 0; i < quests.Length; i++)
        {
            if (i == currentQuestIndex)
            {
                // [현재 집중할 퀘스트] 완전히 보이고 글씨는 하얀색
                quests[i].questObject.SetActive(true);
                quests[i].checkbox.sprite = emptyBoxSprite;
                quests[i].questText.color = Color.white;
            }
            else if (i < currentQuestIndex)
            {
                // [이미 완료한 퀘스트] 체크박스가 채워지고 글씨는 회색
                quests[i].questObject.SetActive(true);
                quests[i].checkbox.sprite = checkedBoxSprite;
                quests[i].questText.color = Color.gray;
            }
            else
            {
                // [아직 도달 못한 퀘스트] 아예 화면에서 숨겨버림 (집중력 상승!)
                quests[i].questObject.SetActive(false);
            }
        }
    }
}