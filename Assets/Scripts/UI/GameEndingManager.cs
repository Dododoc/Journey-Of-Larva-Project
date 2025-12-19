using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 씬 이동용
using System.Collections;

public class GameEndingManager : MonoBehaviour
{
    public static GameEndingManager instance; // 어디서든 부를 수 있게 싱글톤 설정

    [Header("UI 설정")]
    public GameObject endingPanel;      // 아까 만든 엔딩 패널
    public CanvasGroup endingAlpha;     // 패널의 투명도 조절용 (없으면 Panel에 Add Component 하세요)
    
    [Header("연출 설정")]
    public float delayBeforeEnding = 2.0f; // 보스 죽고 몇 초 뒤에 창이 뜰지

    private bool isEnding = false;

    void Awake()
    {
        instance = this;
    }

    // ★ 이 함수를 보스가 죽을 때 호출하면 됩니다!
    public void TriggerEnding()
    {
        if (isEnding) return;
        isEnding = true;
        
        StartCoroutine(ShowEndingRoutine());
    }

    IEnumerator ShowEndingRoutine()
    {
        // 1. 보스가 죽는 모션을 감상할 시간을 줍니다.
        yield return new WaitForSeconds(delayBeforeEnding);

        // 2. 엔딩 패널을 켭니다.
        endingPanel.SetActive(true);

        // 3. 서서히 밝아지게 (Fade In) 연출
        float timer = 0f;
        while(timer < 1f)
        {
            timer += Time.unscaledDeltaTime; // 게임 시간이 멈춰도 UI는 움직이게 unscaled 사용
            if(endingAlpha != null) endingAlpha.alpha = timer;
            yield return null;
        }

        // 4. 게임 시간을 멈춥니다 (선택 사항)
        Time.timeScale = 0f; 
    }

    // 버튼에 연결할 함수들
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // 시간 다시 흐르게 하고 이동
        SceneManager.LoadScene("Forest"); // 메인메뉴 씬 이름 넣기
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료!");

        // ★ 아래 내용을 복사해서 덮어씌우세요!
#if UNITY_EDITOR
        // 에디터에서 실행 중이라면, 플레이 모드를 끕니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 빌드된 게임이라면, 응용 프로그램을 종료합니다.
        Application.Quit();
#endif
    }
}