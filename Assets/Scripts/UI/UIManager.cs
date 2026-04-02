using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; // ★ 코루틴을 쓰기 위해 필요함!
using Unity.Cinemachine;
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
    // ★ [새로 추가] 현재 상호작용 중인 진화 아이템을 기억해 둘 공간
    private GameObject pendingEvolutionItem;
    [Header("진화 후 캐릭터 교체")]
    public GameObject antPlayerPrefab;    // 개미 플레이어 프리팹
    public GameObject beetlePlayerPrefab; // 풍뎅이 플레이어 프리팹
    public float evolutionAnimDuration = 1.5f; // 폭발 애니메이션 시간
    private CinemachineCamera virtualCamera;

    [Header("Skill UI Containers")]
    public GameObject larvaSkillUI;
    public GameObject antSkillUI;
    public GameObject beetleSkillUI;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    [Header("Ending UI")]
    public GameObject endingPanel;      
    public TextMeshProUGUI playTimeText; 
    public TextMeshProUGUI totalXPText;  

    [Header("Pause (일시정지) UI")]
    public GameObject pauseMenuPanel; 
    private bool isPaused = false; 

    [Header("이펙트 UI")]
    public Image slamFlashPanel;

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
        // ★ 시작할 때 씬에 있는 시네마신 카메라를 찾아옵니다.
        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        // ★ [추가 1] 게임을 처음 시작할 때 0번(애벌레) 스킬 UI를 켜줍니다!
        UpdateSkillUI(0);
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
    // ★ [수정됨] 이제 게임을 종료하지 않고 타이틀 화면으로 보냅니다.
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

    // ★ 괄호 안에 GameObject itemObj 를 추가합니다.
    public void ShowEvolutionChoice(int itemTypeIndex, GameObject itemObj)
    {
        pendingEvolutionIndex = itemTypeIndex;
        pendingEvolutionItem = itemObj; // ★ 수락할 때 부수기 위해 아이템을 기억해 둡니다!
        
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

    public void AcceptEvolution()
    {
        evolutionPanel.SetActive(false);
        Time.timeScale = 1f; // 여기서 시간 다시 흐르게 함
        if (blurVolume != null) blurVolume.SetActive(false);

        // ★ [새로 추가] 수락을 눌렀으니 바닥에 있던 아이템을 진짜로 파괴합니다!
        if (pendingEvolutionItem != null)
        {
            Destroy(pendingEvolutionItem);
        }

        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null) currentPlayer.SetActive(false); // 폭발 안에 가려짐

        GameObject activeAnim = null;

        if (pendingEvolutionIndex == 0) 
        {
            if (antEvolutionAnim != null) 
            {
                if (currentPlayer != null) antEvolutionAnim.transform.position = currentPlayer.transform.position;
                antEvolutionAnim.SetActive(true);
                activeAnim = antEvolutionAnim;
            }
            UpdateEvolutionUI(1); 
        }
        else if (pendingEvolutionIndex == 1) 
        {
            if (beetleEvolutionAnim != null) 
            {
                if (currentPlayer != null) beetleEvolutionAnim.transform.position = currentPlayer.transform.position;
                beetleEvolutionAnim.SetActive(true);
                activeAnim = beetleEvolutionAnim;
            }
            UpdateEvolutionUI(2); 
        }

        // ★ 캐릭터 교체 코루틴 시작!
        StartCoroutine(EndEvolutionRoutine(activeAnim, currentPlayer));
    }
    // ==========================================
    // ★ [새로 추가] 진화 애니메이션이 끝난 후 실행될 로직
    // ==========================================
    IEnumerator EndEvolutionRoutine(GameObject animObject, GameObject oldPlayer)
    {
        // 1. 애니메이션 시간만큼 넉넉히 대기
        yield return new WaitForSeconds(evolutionAnimDuration);

        // 2. 폭발 연출 오브젝트 끄기
        if (animObject != null) animObject.SetActive(false);
        // =========================================================
        // ★ [추가 2] 프리팹을 소환하기 직전에, 알맞은 스킬 UI 패널을 먼저 켜줍니다!
        // pendingEvolutionIndex가 0(망토)이면 1(개미UI), 아니면 2(풍뎅이UI)를 켭니다.
        // =========================================================
        UpdateSkillUI((pendingEvolutionIndex == 0) ? 1 : 2);
    

        // 3. 소환할 프리팹 결정 (망토=0=개미 / 투구=1=풍뎅이)
        GameObject prefabToSpawn = (pendingEvolutionIndex == 0) ? antPlayerPrefab : beetlePlayerPrefab;

        if (prefabToSpawn != null && oldPlayer != null)
        {
            // 새 캐릭터를 이전 애벌레 위치에 소환
            GameObject newPlayer = Instantiate(prefabToSpawn, oldPlayer.transform.position, oldPlayer.transform.rotation);
            
            // 시네마신 카메라 타겟을 새 캐릭터로 교체
            if (virtualCamera != null)
            {
                virtualCamera.Follow = newPlayer.transform;
                virtualCamera.LookAt = newPlayer.transform;
            }

            // GameManager에 진화 상태 기록
            if (GameManager.instance != null)
            {
                GameManager.CharacterType newType = (pendingEvolutionIndex == 0) ? GameManager.CharacterType.Ant : GameManager.CharacterType.Beetle;
                GameManager.instance.ChangeCharacter(newType);
            }

            // 기존 애벌레 영구 삭제
            Destroy(oldPlayer);
        }
    }

    // 진화 거절 버튼
    public void CloseEvolutionPopup()
    {
        if (evolutionPanel != null) evolutionPanel.SetActive(false);
        Time.timeScale = 1f; 
        
        if (blurVolume != null) blurVolume.SetActive(false);

        // ==========================================
        // ★ [새로 추가] 거절했을 때 아이템의 테두리를 다시 켜줍니다!
        // ==========================================
        if (pendingEvolutionItem != null)
        {
            // 기억해둔 아이템에서 EvolutionItem 스크립트를 찾아옵니다.
            EvolutionItem evoItem = pendingEvolutionItem.GetComponent<EvolutionItem>();
            
            if (evoItem != null) 
            {
                // 테두리를 다시 켜라고 명령합니다.
                evoItem.SetOutline(true); 
            }
        }
    }
    // ==========================================
    // ★ [새로 추가] 풍뎅이가 궁극기를 쓸 때 호출할 함수
    // ==========================================
    public void ShowSlamFlash()
    {
        if (slamFlashPanel != null)
        {
            StartCoroutine(SlamFlashEffectRoutine());
        }
    }

    IEnumerator SlamFlashEffectRoutine()
    {
        // 1. 순간적으로 하얗게(Alpha 1.0) 꽉 채웁니다.
        slamFlashPanel.color = new Color(1f, 1f, 1f, 1f); 

        // 2. 0.1초 동안 눈부신 상태를 유지 (타격감 극대화)
        yield return new WaitForSecondsRealtime(0.1f);

        // 3. 아주 빠르게 서서히 투명하게 만듭니다.
        float flashFadeSpeed = 5f; 
        while (slamFlashPanel.color.a > 0)
        {
            Color color = slamFlashPanel.color;
            color.a -= Time.unscaledDeltaTime * flashFadeSpeed;
            slamFlashPanel.color = color;
            yield return null;
        }

        // 4. 확실하게 투명하게 고정합니다.
        slamFlashPanel.color = new Color(1f, 1f, 1f, 0f);
    }
    // ==========================================
    // ★ [새로 추가] 현재 캐릭터에 맞게 스킬 UI를 교체합니다.
    // ==========================================
    public void UpdateSkillUI(int characterTypeIndex) // 0:애벌레, 1:개미, 2:풍뎅이
    {
        if (larvaSkillUI != null) larvaSkillUI.SetActive(characterTypeIndex == 0);
        if (antSkillUI != null) antSkillUI.SetActive(characterTypeIndex == 1);
        if (beetleSkillUI != null) beetleSkillUI.SetActive(characterTypeIndex == 2);
    }
}