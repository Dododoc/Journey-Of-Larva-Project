using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; // ★ 코루틴을 쓰기 위해 필요함!

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("HUD Elements (Face Only)")]
    public Image charFaceImage;

    [Header("Evolution UI (진화 UI)")]
    public GameObject evolutionPanel;
    public Image evoPanelBackground;   // 배경(프레임) 이미지를 바꿀 타겟
    public Sprite redCloakBG;          // 붉은 망토 프레임 이미지
    public Sprite goldenCasqueBG;      // 황금 투구 프레임 이미지
    public Sprite[] evolutionFaces;    // 얼굴(개미/풍뎅이) 이미지 배열
    
    public TextMeshProUGUI evoTitleText; 
    public TextMeshProUGUI evoDescText;  
    
    [Header("진화 연출 설정")]
    public float typingSpeed = 0.05f;  // 타자기 글자 나오는 속도
    public GameObject blurVolume;      // 화면 흐리기(Blur) 효과를 켤 오브젝트

    [Header("Evolution Animations")]
    public GameObject antEvolutionAnim;    
    public GameObject beetleEvolutionAnim; 

    private int pendingEvolutionIndex = -1; 
    private Coroutine typingCoroutine; // 실행 중인 타자기 효과를 담을 보관함

    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    [Header("Ending UI")]
    public GameObject endingPanel;      
    public TextMeshProUGUI playTimeText; 
    public TextMeshProUGUI totalXPText;  

    [Header("Pause (일시정지) UI")]
    public GameObject pauseMenuPanel; 
    private bool isPaused = false; 

    // ★ Awake는 무조건 스크립트당 1개만 있어야 합니다!
    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        // ESC 키를 눌렀을 때 일시정지 처리
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 게임오버, 엔딩, 혹은 진화 창이 떠있을 때는 ESC(일시정지)가 안 먹히도록 막음
            if ((gameOverPanel != null && gameOverPanel.activeSelf) || 
                (endingPanel != null && endingPanel.activeSelf) ||
                (evolutionPanel != null && evolutionPanel.activeSelf))
            {
                return; 
            }

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // ==========================================
    // 1. 일시정지 (Pause) & 씬 이동 관련 기능
    // ==========================================
    public void PauseGame()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; 
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; 
        isPaused = false;
    }

    public void GoToTitleScene()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("TitleScene");
    }

    public void OnRespawnClick()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        Time.timeScale = 1f; 

        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null) player.Respawn();
    }

    // ==========================================
    // 2. 게임오버, 엔딩 관련 기능
    // ==========================================
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f; 
        }
    }

    public void ShowEndingPopup()
    {
        if (endingPanel != null && GameManager.instance != null)
        {
            GameManager.instance.StopGameTimer();
            Time.timeScale = 0f; 

            float time = GameManager.instance.playTime;
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time % 60F);
            string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (playTimeText != null) playTimeText.text = $"플레이 타임 : {formattedTime}";
            if (totalXPText != null) totalXPText.text = $"누적 경험치 : {GameManager.instance.globalXP} XP";

            endingPanel.SetActive(true);
        }
    }

    // ==========================================
    // 3. 진화 시스템 관련 기능 (UI 갱신, 팝업, 수락/거절)
    // ==========================================
    public void UpdateEvolutionUI(int evolutionIndex)
    {
        if (evolutionFaces != null && evolutionIndex >= 0 && evolutionIndex < evolutionFaces.Length)
        {
            charFaceImage.sprite = evolutionFaces[evolutionIndex];
        }
    }

    // 아이템을 주웠을 때 팝업창 띄우기 (타자기 효과 + 블러 적용)
    public void ShowEvolutionChoice(int itemTypeIndex)
    {
        pendingEvolutionIndex = itemTypeIndex;
        
        if (evolutionPanel != null)
        {
            evolutionPanel.SetActive(true);
            Time.timeScale = 0f; // 진화 고민하는 동안 시간 멈춤

            // 블러 켜기
            if (blurVolume != null) blurVolume.SetActive(true);

            // 텍스트 초기화
            if (evoTitleText != null) evoTitleText.text = "";
            if (evoDescText != null) evoDescText.text = "";

            string titleMessage = "";
            string descMessage = "";

            // 아이템에 맞게 배경 프레임 교체 & 문구 세팅
            if (itemTypeIndex == 0) // 망토
            {
                if (evoPanelBackground != null && redCloakBG != null) evoPanelBackground.sprite = redCloakBG;
                titleMessage = "바람을 가르는 붉은 망토에서 서늘한 기운이 느껴집니다...";
                descMessage = "빠르고 치명적인 [붉은 모래의 암살자]로 진화하시겠습니까?";
            }
            else if (itemTypeIndex == 1) // 투구
            {
                if (evoPanelBackground != null && goldenCasqueBG != null) evoPanelBackground.sprite = goldenCasqueBG;
                titleMessage = "묵직한 황금 투구에서 압도적인 파괴력이 요동칩니다...";
                descMessage = "단단하고 무자비한 [황금 뿔의 폭군]으로 진화하시겠습니까?";
            }

            // 타자기 효과 시작
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypewriterEffect(titleMessage, descMessage));
        }
    }

    // 타자기 효과 코루틴
    IEnumerator TypewriterEffect(string title, string desc)
    {
        for (int i = 0; i < title.Length; i++)
        {
            evoTitleText.text += title[i];
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        yield return new WaitForSecondsRealtime(0.3f); // 살짝 쉬고

        for (int i = 0; i < desc.Length; i++)
        {
            evoDescText.text += desc[i];
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }

    // 진화 수락 버튼
    public void AcceptEvolution()
    {
        evolutionPanel.SetActive(false);
        Time.timeScale = 1f;
        
        // 블러 효과 다시 끄기
        if (blurVolume != null) blurVolume.SetActive(false);

        // 애벌레 숨기기
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(false);
        }

        // 선택한 진화 애니메이션 재생
        if (pendingEvolutionIndex == 0) 
        {
            if (antEvolutionAnim != null) 
            {
                if (currentPlayer != null) antEvolutionAnim.transform.position = currentPlayer.transform.position;
                antEvolutionAnim.SetActive(true);
            }
            UpdateEvolutionUI(1); 
        }
        else if (pendingEvolutionIndex == 1) 
        {
            if (beetleEvolutionAnim != null) 
            {
                if (currentPlayer != null) beetleEvolutionAnim.transform.position = currentPlayer.transform.position;
                beetleEvolutionAnim.SetActive(true);
            }
            UpdateEvolutionUI(2); 
        }
    }

    // 진화 거절 버튼
    public void CloseEvolutionPopup()
    {
        if (evolutionPanel != null) evolutionPanel.SetActive(false);
        Time.timeScale = 1f; 
        
        // 블러 효과 다시 끄기
        if (blurVolume != null) blurVolume.SetActive(false);
    }
}