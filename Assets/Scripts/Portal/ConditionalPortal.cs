using UnityEngine;
using UnityEngine.SceneManagement;

public class ConditionalPortal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 플레이어가 포탈에 닿았는지 확인
        if (collision.CompareTag("Player"))
        {
            // 2. 현재 정보 가져오기 (내 캐릭터 종류, 지금 씬 이름)
            GameManager.CharacterType currentType = GameManager.instance.currentCharacter;
            string currentSceneName = SceneManager.GetActiveScene().name;

            // --- [개미(Ant)일 때 이동 로직] ---
            if (currentType == GameManager.CharacterType.Ant)
            {
                // 만약 지금 'Ant Stage'라면 -> 'ant ghost map'으로 이동
                if (currentSceneName == "Ant Stage")
                {
                    Debug.Log("개미 유령 맵으로 이동!");
                    SceneManager.LoadScene("ant ghost map");
                }
                // 만약 다른 곳(예: 첫 시작 마을)이라면 -> 'Ant Stage'로 이동 (기존 로직 유지)
                else
                {
                    Debug.Log("개미 스테이지로 이동!");
                    SceneManager.LoadScene("Ant Stage");
                }
            }
            // --- [풍뎅이(Beetle)일 때 이동 로직] ---
            else if (currentType == GameManager.CharacterType.Beetle)
            {
                // 만약 지금 'Beetle Map'이라면 -> 'Ant boss map'으로 이동
                if (currentSceneName == "Beetle Map")
                {
                    Debug.Log("보스 맵으로 이동!");
                    SceneManager.LoadScene("boss mantis map");
                }
                // 다른 곳이라면 -> 'Beetle Map'으로 이동
                else
                {
                    Debug.Log("풍뎅이 맵으로 이동!");
                    SceneManager.LoadScene("Beetle Map");
                }
            }
            // --- [그 외(라바 등)] ---
            else
            {
                Debug.Log("아직 이동할 수 없는 상태입니다.");
            }
        }
    }
}