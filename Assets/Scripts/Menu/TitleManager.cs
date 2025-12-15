using UnityEngine;
using UnityEngine.SceneManagement; 

public class TitleManager : MonoBehaviour
{
    // "게임 시작" 버튼을 눌렀을 때 실행될 함수
    public void ClickStartGame()
    {
        // "GameScene" 부분에 실제 게임플레이가 진행되는 씬 이름을 정확히 적어주세요.
        // 예: "Stage1", "MainGame" 등
        SceneManager.LoadScene("Forest"); 
    }

    // "게임 종료" 버튼을 눌렀을 때 실행될 함수
    public void ClickQuitGame()
    {
        Debug.Log("게임이 종료되었습니다."); // 에디터에서는 안 꺼지므로 로그로 확인
        Application.Quit();
    }
}