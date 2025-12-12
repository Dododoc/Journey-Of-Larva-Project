using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
}