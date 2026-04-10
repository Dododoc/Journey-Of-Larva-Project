using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using Unity.Cinemachine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("HUD Elements (Face Only)")]
    public Image charFaceImage;

    [Header("Evolution UI (진화 UI)")]
    public GameObject evolutionPanel;
    public Image evoPanelBackground;   
    public Sprite redCloakBG;          
    public Sprite goldenCasqueBG;      
    public Sprite[] evolutionFaces;    
    public TextMeshProUGUI evoTitleText; 
    public TextMeshProUGUI evoDescText;  
    
    [Header("진화 연출 설정")]
    public float typingSpeed = 0.05f;  
    public GameObject blurVolume;      

    [Header("Evolution Animations")]
    public GameObject antEvolutionAnim;    
    public GameObject beetleEvolutionAnim; 

    private int pendingEvolutionIndex = -1; 
    private Coroutine typingCoroutine; 
    private GameObject pendingEvolutionItem;

    [Header("진화 후 캐릭터 교체")]
    public GameObject antPlayerPrefab;    
    public GameObject beetlePlayerPrefab; 
    public float evolutionAnimDuration = 1.5f; 
    private CinemachineCamera virtualCamera;

    [Header("Skill UI Containers")]
    public GameObject larvaSkillUI;
    public GameObject antSkillUI;
    public GameObject beetleSkillUI;

    // ==========================================
    // ★ [수정됨] Game Over UI 관련 설정이 대폭 추가되었습니다.
    // ==========================================
    [Header("Game Over UI")]
    public GameObject gameOverPanel; // 패널 자체 (페이드인용)
    public Image frameImage;          // ★ [추가] 프레임 이미지 연결용
    public GameObject eggButton;     // 알 버튼 (나중에 등장용)
    public TextMeshProUGUI titleText; // ★ [추가] 하이어라키의 TitleText 연결용
    public TextMeshProUGUI mainText;  // "생태계의 밑거름..."
    public TextMeshProUGUI statsText; // "최종 진화..."

    [Header("Game Over Animation Settings")]
    public float panelFadeDuration = 1.0f;    // 패널이 스르륵 뜨는 시간
    public float gameOverTypeSpeed = 0.05f;   // 글자가 써지는 속도
    public float buttonsFadeDuration = 0.8f;  // 알/버튼이 스르륵 뜨는 시간

    private string originalMainStr;
    private string originalStatsStr;
    // ==========================================
    // Ending UI 설정
    // ==========================================
    [Header("Ending UI")]
    public GameObject endingPanel;      
    public GameObject endingTitleButton; 
    public TextMeshProUGUI endingMainText;
    public TextMeshProUGUI endingStatsText; // ★ [수정] 스탯을 한 번에 보여줄 변수

    private string originalEndingMainStr;

    [Header("Pause (일시정지) UI")]
    public GameObject pauseMenuPanel; 
    private bool isPaused = false; 

    [Header("이펙트 UI")]
    public Image slamFlashPanel;

    void Awake()
    {
        if (instance == null) instance = this;

        // 게임 시작 시 모든 패널 강제 초기화
        if (gameOverPanel != null)
        {
            if (mainText != null) originalMainStr = mainText.text;
            if (statsText != null) originalStatsStr = statsText.text;
            
            if (mainText != null) mainText.text = "";
            if (statsText != null) statsText.text = "";

            SetGameOverPanelAlpha(0f); 
            gameOverPanel.SetActive(false); 
        }

        // 버튼과 텍스트 미리 꺼두기 (연출을 위해)
        if (eggButton != null) eggButton.SetActive(false);
        if (titleText != null) titleText.gameObject.SetActive(false);

        if (evolutionPanel != null) evolutionPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        // 엔딩 패널 초기화 (Awake 내부에 작성)
        if (endingPanel != null)
        {
            if (endingMainText != null) originalEndingMainStr = endingMainText.text;
            
            if (endingMainText != null) endingMainText.text = "";
            if (endingStatsText != null) endingStatsText.text = ""; // ★ 수정

            if (endingTitleButton != null) endingTitleButton.SetActive(false);
            endingPanel.SetActive(false);
        }
        if (slamFlashPanel != null) slamFlashPanel.color = new Color(1f, 1f, 1f, 0f);
        if (blurVolume != null) blurVolume.SetActive(false);
    }

    void Start()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        UpdateSkillUI(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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

    // ==========================================
    // 2. 게임오버 관련 기능 (새로운 연출 반영)
    // ==========================================
    // ★ [수정됨] PlayerStats 등에서 체력이 0일 때 이 함수를 부르게 됩니다.
    // ★ [수정됨] PlayerStats 등에서 체력이 0일 때 이 함수를 부르게 됩니다.
    public void ShowGameOver()
    {
        // =========================================================
        // ★ [추가] 패널이 켜지기 전에 이전 텍스트의 흔적을 미리 싹 지워줍니다!
        // =========================================================
        if (mainText != null) mainText.text = "";
        if (statsText != null) statsText.text = "";

        // 만약 GameManager 등을 통해 킬수/시간을 받아온다면,
        // 이 시점에서 originalStatsStr의 내용을 완성된 문장으로 바꿔주면 됩니다.
        
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        // 1. 패널 켜기 (알과 타이틀은 끈 상태 유지)
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (eggButton != null) eggButton.SetActive(false);
        if (titleText != null) titleText.gameObject.SetActive(false);

        // 2. 패널(검은 배경)과 Frame 동시에 스르륵 등장
        Image panelImg = gameOverPanel != null ? gameOverPanel.GetComponent<Image>() : null;
        
        float timer = 0f;
        while (timer < panelFadeDuration)
        {
            timer += Time.unscaledDeltaTime; 
            float alpha = Mathf.Lerp(0f, 1f, timer / panelFadeDuration);
            
            if (panelImg != null) panelImg.color = new Color(panelImg.color.r, panelImg.color.g, panelImg.color.b, alpha);
            if (frameImage != null) frameImage.color = new Color(frameImage.color.r, frameImage.color.g, frameImage.color.b, alpha);
            
            yield return null;
        }
        
        if (panelImg != null) panelImg.color = new Color(panelImg.color.r, panelImg.color.g, panelImg.color.b, 1f);
        if (frameImage != null) frameImage.color = new Color(frameImage.color.r, frameImage.color.g, frameImage.color.b, 1f);

        // ==========================================
        // 3. 실제 스탯 정보 불러오기 (GameManager 연동)
        // ==========================================
        string finalEvoName = "알 수 없음";
        string timeString = "00:00";
        int kills = 0;

        if (GameManager.instance != null)
        {
            // 진화 형태 텍스트 변환
            switch(GameManager.instance.currentCharacter)
            {
                case GameManager.CharacterType.Larva: finalEvoName = "끈질긴 애벌레"; break;
                case GameManager.CharacterType.Ant: finalEvoName = "붉은 모래의 개미"; break;
                case GameManager.CharacterType.Beetle: finalEvoName = "황금 뿔의 풍뎅이"; break;
            }
            
            // 생존 시간 변환
            float time = GameManager.instance.playTime;
            int m = Mathf.FloorToInt(time / 60F);
            int s = Mathf.FloorToInt(time % 60F);
            timeString = string.Format("{0:00}:{1:00}", m, s);

            // ★ 주의: GameManager에 killCount 변수가 추가되어 있다면 주석을 해제하세요!
            kills = GameManager.instance.killCount; 
        }

        // 스탯 텍스트 최종 조립
        string finalStatsStr = $"최종 진화: {finalEvoName}\n처치한 적: {kills}마리\n생존 시간: {timeString}";

        // ==========================================
        // 4. 메인 텍스트 -> 스탯 텍스트 순으로 타이핑
        // ==========================================
        if (mainText != null)
        {
            yield return StartCoroutine(TypeGameOverText(mainText, originalMainStr));
            yield return new WaitForSecondsRealtime(0.3f); 
        }

        if (statsText != null)
        {
            yield return StartCoroutine(TypeGameOverText(statsText, finalStatsStr));
            yield return new WaitForSecondsRealtime(0.5f); 
        }

        // ==========================================
        // 5. 알 버튼과 타이틀 텍스트 서서히 등장
        // ==========================================
        if (eggButton != null) eggButton.SetActive(true);
        if (titleText != null) titleText.gameObject.SetActive(true);

        Image eggImg = eggButton != null ? eggButton.GetComponent<Image>() : null;

        float fadeTimer = 0f;
        while (fadeTimer < buttonsFadeDuration)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, fadeTimer / buttonsFadeDuration);
            
            if (eggImg != null) eggImg.color = new Color(eggImg.color.r, eggImg.color.g, eggImg.color.b, alpha);
            if (titleText != null) titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, alpha);
            
            yield return null;
        }

        if (eggImg != null) eggImg.color = Color.white;
        if (titleText != null) titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, 1f);

        // 연출 끝. 세계 완전 정지
        Time.timeScale = 0f; 
    }
    // 게임오버 전용 텍스트 타이핑 효과 코루틴
    IEnumerator TypeGameOverText(TextMeshProUGUI textTmp, string targetStr)
    {
        textTmp.text = "";
        foreach (char letter in targetStr.ToCharArray())
        {
            textTmp.text += letter;
            yield return new WaitForSecondsRealtime(gameOverTypeSpeed);
        }
    }

    // 게임오버 패널의 알파값 조절 헬퍼 함수
    void SetGameOverPanelAlpha(float alpha)
    {
        if (gameOverPanel != null)
        {
            Image panelImg = gameOverPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = new Color(panelImg.color.r, panelImg.color.g, panelImg.color.b, alpha);
            }
        }
    }

    // 알 버튼을 눌렀을 때 씬 재시작
    public void OnRespawnClick()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        Time.timeScale = 1f; 

        if (GameManager.instance != null)
        {
            // 1. 레벨, 경험치, 캐릭터(애벌레)를 모두 초기화합니다.
            GameManager.instance.ResetGameData(); 
            
            // ==========================================
            // ★ [추가] 마지막으로 탔던 포탈 티켓을 비워버립니다!
            // 이렇게 해야 포탈에서 태어나지 않고 맨 처음 지점(InitialSpawnPoint)에서 태어납니다.
            // ==========================================
            GameManager.instance.targetPortalID = ""; 
        }

        // ==========================================
        // ★ [수정] 무조건 맨 처음 시작 맵인 'Larva Stage'로 보냅니다!
        // ==========================================
        SceneManager.LoadScene("Larva Stage"); 
    }
    // ==========================================


    public void ShowEndingPopup()
    {
        if (endingPanel != null && GameManager.instance != null)
        {
            GameManager.instance.StopGameTimer();
            // 텍스트 비우기 (중복 방지)
            if (endingMainText != null) endingMainText.text = "";
            if (endingStatsText != null) endingStatsText.text = ""; // ★ 수정

            StartCoroutine(EndingRoutine());
        }
    }

    IEnumerator EndingRoutine()
    {
        // 1. 패널 켜기 (버튼은 끈 상태 유지)
        endingPanel.SetActive(true);
        if (endingTitleButton != null) endingTitleButton.SetActive(false);

        Image panelImg = endingPanel.GetComponent<Image>();
        float timer = 0f;
        while (timer < panelFadeDuration)
        {
            timer += Time.unscaledDeltaTime; 
            float alpha = Mathf.Lerp(0f, 1f, timer / panelFadeDuration);
            if (panelImg != null) panelImg.color = new Color(panelImg.color.r, panelImg.color.g, panelImg.color.b, alpha);
            yield return null;
        }
        if (panelImg != null) panelImg.color = new Color(panelImg.color.r, panelImg.color.g, panelImg.color.b, 1f);

        // ==========================================
        // ★ [수정됨] 2. 실제 스탯 정보 불러오기
        // ==========================================
        string finalEvoName = "알 수 없음";
        string timeString = "00:00";
        int kills = 0;

        if (GameManager.instance != null)
        {
            // 진화 형태 텍스트 변환
            switch(GameManager.instance.currentCharacter)
            {
                case GameManager.CharacterType.Larva: finalEvoName = "끈질긴 애벌레"; break;
                case GameManager.CharacterType.Ant: finalEvoName = "붉은 모래의 개미"; break;
                case GameManager.CharacterType.Beetle: finalEvoName = "황금 뿔의 풍뎅이"; break;
            }
            
            // 생존 시간 변환
            float time = GameManager.instance.playTime;
            int m = Mathf.FloorToInt(time / 60F);
            int s = Mathf.FloorToInt(time % 60F);
            timeString = string.Format("{0:00}:{1:00}", m, s);

            // ★ 주의: GameManager에 killCount 변수가 추가되어 있다면 주석을 해제하세요!
            kills = GameManager.instance.killCount; 
        }

        // 스탯 텍스트 최종 조립 (경험치도 포함!)
        string finalStatsStr = $"최종 진화: {finalEvoName}\n처치한 적: {kills}마리\n플레이 타임: {timeString}\n누적 경험치: {GameManager.instance.globalXP} XP";

        // ==========================================
        // 3. 메인 텍스트 -> 스탯 텍스트 순으로 타이핑
        // ==========================================
        if (endingMainText != null)
        {
            yield return StartCoroutine(TypeGameOverText(endingMainText, originalEndingMainStr));
            yield return new WaitForSecondsRealtime(0.3f);
        }

        if (endingStatsText != null)
        {
            yield return StartCoroutine(TypeGameOverText(endingStatsText, finalStatsStr));
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // 4. 타이틀 버튼 서서히 등장
        if (endingTitleButton != null) endingTitleButton.SetActive(true);
        Image btnImg = endingTitleButton != null ? endingTitleButton.GetComponent<Image>() : null;
        TextMeshProUGUI btnTextTmp = endingTitleButton != null ? endingTitleButton.GetComponentInChildren<TextMeshProUGUI>() : null;

        float fadeTimer = 0f;
        while (fadeTimer < buttonsFadeDuration)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, fadeTimer / buttonsFadeDuration);
            
            if (btnImg != null) btnImg.color = new Color(btnImg.color.r, btnImg.color.g, btnImg.color.b, alpha);
            if (btnTextTmp != null) btnTextTmp.color = new Color(btnTextTmp.color.r, btnTextTmp.color.g, btnTextTmp.color.b, alpha);
            
            yield return null;
        }

        if (btnImg != null) btnImg.color = Color.white;
        if (btnTextTmp != null) btnTextTmp.color = new Color(btnTextTmp.color.r, btnTextTmp.color.g, btnTextTmp.color.b, 1f);

        // 연출 끝. 세계 완전 정지
        Time.timeScale = 0f; 
    }

    // ★ [추가] Quit 버튼을 눌렀을 때 실행될 함수
    public void OnQuitGameClick()
    {
        Debug.Log("게임을 종료합니다.");
        
        #if UNITY_EDITOR
            // 유니티 에디터 환경일 때는 플레이 모드를 끕니다.
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 실제 빌드된 게임(exe)일 때는 프로그램을 종료합니다.
            Application.Quit();
        #endif
    }

    public void UpdateEvolutionUI(int evolutionIndex)
    {
        if (evolutionFaces != null && evolutionIndex >= 0 && evolutionIndex < evolutionFaces.Length)
        {
            charFaceImage.sprite = evolutionFaces[evolutionIndex];
        }
    }

    public void ShowEvolutionChoice(int itemTypeIndex, GameObject itemObj)
    {
        pendingEvolutionIndex = itemTypeIndex;
        pendingEvolutionItem = itemObj; 
        
        if (evolutionPanel != null)
        {
            evolutionPanel.SetActive(true);
            Time.timeScale = 0f; 

            if (blurVolume != null) blurVolume.SetActive(true);

            if (evoTitleText != null) evoTitleText.text = "";
            if (evoDescText != null) evoDescText.text = "";

            string titleMessage = "";
            string descMessage = "";

            if (itemTypeIndex == 0) 
            {
                if (evoPanelBackground != null && redCloakBG != null) evoPanelBackground.sprite = redCloakBG;
                titleMessage = "바람을 가르는 붉은 망토에서 서늘한 기운이 느껴집니다...";
                descMessage = "빠르고 치명적인 [붉은 모래의 암살자]로 진화하시겠습니까?";
            }
            else if (itemTypeIndex == 1) 
            {
                if (evoPanelBackground != null && goldenCasqueBG != null) evoPanelBackground.sprite = goldenCasqueBG;
                titleMessage = "묵직한 황금 투구에서 압도적인 파괴력이 요동칩니다...";
                descMessage = "단단하고 무자비한 [황금 뿔의 폭군]으로 진화하시겠습니까?";
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypewriterEffect(titleMessage, descMessage));
        }
    }

    IEnumerator TypewriterEffect(string title, string desc)
    {
        for (int i = 0; i < title.Length; i++)
        {
            evoTitleText.text += title[i];
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        yield return new WaitForSecondsRealtime(0.3f); 

        for (int i = 0; i < desc.Length; i++)
        {
            evoDescText.text += desc[i];
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }

    public void AcceptEvolution()
    {
        evolutionPanel.SetActive(false);
        Time.timeScale = 1f; 
        if (blurVolume != null) blurVolume.SetActive(false);

        if (pendingEvolutionItem != null)
        {
            Destroy(pendingEvolutionItem);
        }

        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null) currentPlayer.SetActive(false); 

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

        StartCoroutine(EndEvolutionRoutine(activeAnim, currentPlayer));
    }

    IEnumerator EndEvolutionRoutine(GameObject animObject, GameObject oldPlayer)
    {
        yield return new WaitForSeconds(evolutionAnimDuration);

        if (animObject != null) animObject.SetActive(false);
        
        // ==========================================
        // ★ [핵심 1] 애벌레를 파괴하기 전에, 현재 레벨과 경험치를 GameManager에 확실히 저장합니다!
        // ==========================================
        if (oldPlayer != null)
        {
            oldPlayer.SendMessage("SaveStatsToManager", SendMessageOptions.DontRequireReceiver);
        }

        UpdateSkillUI((pendingEvolutionIndex == 0) ? 1 : 2);
    
        GameObject prefabToSpawn = (pendingEvolutionIndex == 0) ? antPlayerPrefab : beetlePlayerPrefab;

        if (prefabToSpawn != null && oldPlayer != null)
        {
            // 새로운 캐릭터 소환
            GameObject newPlayer = Instantiate(prefabToSpawn, oldPlayer.transform.position, oldPlayer.transform.rotation);
            
            if (virtualCamera != null)
            {
                virtualCamera.Follow = newPlayer.transform;
                virtualCamera.LookAt = newPlayer.transform;
            }

            if (GameManager.instance != null)
            {
                GameManager.CharacterType newType = (pendingEvolutionIndex == 0) ? GameManager.CharacterType.Ant : GameManager.CharacterType.Beetle;
                GameManager.instance.ChangeCharacter(newType);
            }

            // 구형 캐릭터(애벌레) 삭제
            Destroy(oldPlayer);

            // ==========================================
            // ★ [핵심 2] 새로 태어난 플레이어가 HUD를 꽉 잡도록 강제로 갱신 명령을 내립니다!
            // Start 함수가 실행될 시간을 벌기 위해 1프레임(yield return null) 대기합니다.
            // ==========================================
            yield return null; 
            if (newPlayer != null)
            {
                newPlayer.SendMessage("UpdateUI", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void CloseEvolutionPopup()
    {
        if (evolutionPanel != null) evolutionPanel.SetActive(false);
        Time.timeScale = 1f; 
        
        if (blurVolume != null) blurVolume.SetActive(false);

        if (pendingEvolutionItem != null)
        {
            EvolutionItem evoItem = pendingEvolutionItem.GetComponent<EvolutionItem>();
            if (evoItem != null) 
            {
                evoItem.SetOutline(true); 
            }
        }
    }

    public void ShowSlamFlash()
    {
        if (slamFlashPanel != null)
        {
            StartCoroutine(SlamFlashEffectRoutine());
        }
    }

    IEnumerator SlamFlashEffectRoutine()
    {
        slamFlashPanel.color = new Color(1f, 1f, 1f, 1f); 
        yield return new WaitForSecondsRealtime(0.1f);

        float flashFadeSpeed = 5f; 
        while (slamFlashPanel.color.a > 0)
        {
            Color color = slamFlashPanel.color;
            color.a -= Time.unscaledDeltaTime * flashFadeSpeed;
            slamFlashPanel.color = color;
            yield return null;
        }

        slamFlashPanel.color = new Color(1f, 1f, 1f, 0f);
    }

    public void UpdateSkillUI(int characterTypeIndex) 
    {
        if (larvaSkillUI != null) larvaSkillUI.SetActive(characterTypeIndex == 0);
        if (antSkillUI != null) antSkillUI.SetActive(characterTypeIndex == 1);
        if (beetleSkillUI != null) beetleSkillUI.SetActive(characterTypeIndex == 2);
    }
}