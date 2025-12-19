using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    // [삭제됨] HP, XP, Level 관련 변수는 이제 PlayerHUD로 이사 갔습니다.
    
    [Header("HUD Elements (Face Only)")]
    public Image charFaceImage; // ★ 얼굴은 UIManager가 관리 (진화랑 연결돼서)

    [Header("Evolution UI")]
    public GameObject evolutionPanel;
    public Sprite[] evolutionFaces; 

    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    [Header("Ending UI")]
    public GameObject endingPanel;      // 엔딩 팝업창 패널
    public TextMeshProUGUI playTimeText; // 플레이 타임 표시할 텍스트
    public TextMeshProUGUI totalXPText;  // 누적 경험치 표시할 텍스트

    public static UIManager instance;

    void Awake()
    {
        instance = this;
    }

    // 얼굴 바꾸기 (이건 UIManager가 계속 함)
    public void UpdateEvolutionUI(int evolutionIndex)
    {
        if (evolutionFaces != null && evolutionIndex >= 0 && evolutionIndex < evolutionFaces.Length)
        {
            charFaceImage.sprite = evolutionFaces[evolutionIndex];
        }
    }

    // 진화 창 닫기
    public void CloseEvolutionPopup()
    {
        if (evolutionPanel != null) evolutionPanel.SetActive(false);
        Time.timeScale = 1f; 
    }

    // 게임 오버 창 띄우기
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f; 
        }
    }

    // 부활 버튼
    public void OnRespawnClick()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Time.timeScale = 1f; 
        }

        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null) player.Respawn();
    }

    // 타이틀 버튼
    public void OnTitleClick()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ShowEndingPopup()
    {
        if (endingPanel != null && GameManager.instance != null)
        {
            // 1. 게임 시간 정지 (GameManager 타이머 정지 + 물리 정지)
            GameManager.instance.StopGameTimer();
            Time.timeScale = 0f; 

            // 2. 플레이 타임 계산 (초 -> 분:초 포맷으로 변환)
            float time = GameManager.instance.playTime;
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time % 60F);
            string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            // 3. 텍스트 업데이트
            if (playTimeText != null) playTimeText.text = $"플레이 타임 : {formattedTime}";
            
            // GameManager의 globalXP를 가져와 표시
            if (totalXPText != null) totalXPText.text = $"누적 경험치 : {GameManager.instance.globalXP} XP";

            // 4. 패널 활성화
            endingPanel.SetActive(true);
            Debug.Log("엔딩 팝업 표시 완료");
        }
    }
    public void QuitGameClick()
    {
        // 1. 멈췄던 시간을 다시 흐르게 해줍니다. (중요!)
        Time.timeScale = 1f; 
        
        // 2. 타이틀 씬을 불러옵니다.
        // 따옴표("") 안에 본인의 타이틀 씬 이름을 정확히 적어주세요.
        // (예: "TitleScene", "MainTitle", "StartScene" 등)
        SceneManager.LoadScene("TitleScene"); 
        
        Debug.Log("타이틀 화면으로 이동합니다.");
    }
}

