using UnityEngine;
using UnityEngine.UI;
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
        PlayerPrefs.DeleteKey("HasTapped");
        
        // ==========================================================
        // ★ [핵심 수정] 맵 이름이 아니라 '현재 플레이어가 애벌레인지' 확인합니다.
        // ==========================================================
        if (GameManager.instance == null || GameManager.instance.currentCharacter != GameManager.CharacterType.Larva)
        {
            // 애벌레가 아니라면 퀘스트 창과 Tap 텍스트를 아예 꺼버립니다.
            questPanel.gameObject.SetActive(false);
            if (tapPromptText != null) tapPromptText.gameObject.SetActive(false);
            return; 
        }

        // --- 여기서부터는 '애벌레'일 때만 실행됩니다 ---
        questPanel.gameObject.SetActive(true);
        questPanel.anchoredPosition = new Vector2(hiddenPosX, questPanel.anchoredPosition.y);
        UpdateQuestUI();

        if (PlayerPrefs.GetInt("HasTapped", 0) == 1)
        {
            if (tapPromptText != null) tapPromptText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // ★ 애벌레가 아니면 Update 기능도 작동하지 않게 막습니다.
        if (GameManager.instance == null || GameManager.instance.currentCharacter != GameManager.CharacterType.Larva) return;

        if (tapPromptText != null && tapPromptText.gameObject.activeSelf)
        {
            Color textColor = tapPromptText.color;
            textColor.a = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            tapPromptText.color = textColor;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isVisible = !isVisible;

            if (tapPromptText != null && tapPromptText.gameObject.activeSelf)
            {
                tapPromptText.gameObject.SetActive(false);
                PlayerPrefs.SetInt("HasTapped", 1);
                PlayerPrefs.Save();
                isVisible = true; 
            }
        }

        float targetX = isVisible ? visiblePosX : hiddenPosX;
        Vector2 targetPos = new Vector2(targetX, questPanel.anchoredPosition.y);
        questPanel.anchoredPosition = Vector2.Lerp(questPanel.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
    }

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
        CheckAutoCompletes();
    }

    void CheckAutoCompletes()
    {
        // ★ 애벌레가 아니면 검사하지 않음
        if (GameManager.instance == null || GameManager.instance.currentCharacter != GameManager.CharacterType.Larva) return;

        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player == null) return;

        if (currentQuestIndex == 3) 
        {
            if (player.currentLevel >= 3) 
            {
                Debug.Log("레벨 3 달성 퀘스트 자동 완료!");
                CompleteQuest(3); 
            }
        }
    }
}