using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필요

public class PauseManager : MonoBehaviour
{
    [Header("일시 정지 메뉴 패널")]
    public GameObject pauseMenuPanel; // Inspector에서 PauseMenu를 연결할 변수

    private bool isPaused = false; // 현재 일시 정지 상태인지 확인

    void Start()
    {
        // 게임 시작 시 메뉴를 확실하게 닫아줍니다.
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false); 
        }
        
        // 혹시라도 시간이 멈춰있을 경우를 대비해 시간도 정상으로 돌려둡니다.
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        // ESC 키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame(); // 이미 멈춰있으면 게임 재개
            }
            else
            {
                PauseGame(); // 게임 중이면 일시 정지
            }
        }
    }

    // 게임 일시 정지 기능
    public void PauseGame()
    {
        pauseMenuPanel.SetActive(true); // 메뉴 창 켜기
        Time.timeScale = 0f; // ★ 게임 시간 멈추기 (가장 중요!)
        isPaused = true;
    }

    // 계속하기(Continue) 버튼 기능
    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false); // 메뉴 창 끄기
        Time.timeScale = 1f; // ★ 게임 시간 다시 흐르게 하기
        isPaused = false;
    }

    // 나가기(Quit) 버튼 기능 - 메인 메뉴로 이동하거나 게임 종료
    public void QuitGame()
    {
        // 1. 시간이 멈춘 채로 씬을 이동하면 다음 씬도 멈춰있으므로 시간을 돌려놔야 함
        Time.timeScale = 1f; 

        // 2. 메인 메뉴 씬으로 이동 (따옴표 안에 메인 메뉴 씬 이름을 정확히 적으세요!)
        SceneManager.LoadScene("TitleScene"); 

        // 3. 아예 게임을 끄고 싶다면 아래 코드 사용
       // Debug.Log("게임 종료!"); // 에디터에서는 안 꺼지므로 로그로 확인
       // Application.Quit(); 
    }
}