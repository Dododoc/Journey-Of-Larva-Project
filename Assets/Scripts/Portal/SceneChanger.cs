using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 반드시 필요합니다.

public class SceneChanger : MonoBehaviour
{
    // 버튼에 연결할 퍼블릭 함수
    public void LoadNextScene()
    {
        // 현재 활성화된 씬의 인덱스를 가져옵니다.
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        // 빌드 설정(Build Settings)에 등록된 다음 인덱스의 씬을 불러옵니다.
        // 마지막 씬에서 호출하면 에러가 날 수 있으니 주의하세요!
        SceneManager.LoadScene(currentSceneIndex + 1);
    }

    // 특정 씬 이름을 지정해서 이동하고 싶을 때
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}