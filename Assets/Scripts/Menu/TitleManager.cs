using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    // 유니티 에디터에서 만들어둔 설정창 패널(GameObject)을 여기에 연결할 겁니다.
    public GameObject settingsPanel;

    void Start()
    {
        // 게임이 시작될 때 설정창이 열려있으면 안 되므로, 혹시 모르니 닫아둡니다.
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // ---------------------------------------------------
    // 버튼 클릭 시 실행될 함수들
    // ---------------------------------------------------

    // 1. [Start] 버튼용 함수
    public void ClickStartGame()
    {
        SceneManager.LoadScene("Forest");
    }

    // 2. [Settings] 버튼용 함수 (추가됨!)
    public void ClickSettings()
    {
        Debug.Log("설정 버튼 클릭: 설정창을 엽니다.");
        // 설정 패널을 활성화(켜기)합니다.
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    // 3. [Quit] 버튼용 함수
    public void ClickQuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    // 4. 설정창 안의 [닫기(X)] 버튼용 함수 (추가됨!)
    public void CloseSettings()
    {
        Debug.Log("설정창 닫기");
        // 설정 패널을 비활성화(끄기)합니다.
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
}