using UnityEngine;
using UnityEngine.SceneManagement;

public class ConditionalPortal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어가 포탈에 닿았을 때
        if (collision.CompareTag("Player"))
        {
            // 게임 매니저에게 "지금 내 캐릭터가 뭐니?" 라고 물어봄
            GameManager.CharacterType currentType = GameManager.instance.currentCharacter;

            if (currentType == GameManager.CharacterType.Ant)
            {
                Debug.Log("개미 맵으로 이동!");
                SceneManager.LoadScene("Ant Stage"); // 이동할 씬 이름
            }
            else if (currentType == GameManager.CharacterType.Beetle)
            {
                Debug.Log("풍뎅이 맵으로 이동!");
                SceneManager.LoadScene("Beetle Map"); // 이동할 씬 이름
            }
            else
            {
                Debug.Log("라바 상태에서는 아직 이동할 수 없습니다.");
            }
        }
    }
}