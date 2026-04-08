using UnityEngine;
using UnityEngine.SceneManagement;

public class ConditionalPortal : MonoBehaviour
{
    private bool isPlayerNearby = false;

    void Update()
    {
        // ★ 플레이어가 근처에 있고 F키를 누르면 이동 로직 실행!
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            ActivatePortal();
        }
    }

    void ActivatePortal()
    {
        // ★ 포탈 타기 직전에 플레이어의 현재 체력과 스탯을 GameManager에 싹 저장합니다!
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null) player.SaveStatsToManager();

        GameManager.CharacterType currentType = GameManager.instance.currentCharacter;
        string currentSceneName = SceneManager.GetActiveScene().name;

        // --- [개미(Ant)일 때 이동 로직] ---
        if (currentType == GameManager.CharacterType.Ant)
        {
            if (currentSceneName == "Ant Stage")
            {
                Debug.Log("개미 유령 맵으로 이동!");
                SceneManager.LoadScene("ant ghost map");
            }
            else
            {
                Debug.Log("개미 스테이지로 이동!");
                SceneManager.LoadScene("Ant Stage");
            }
        }
        // --- [풍뎅이(Beetle)일 때 이동 로직] ---
        else if (currentType == GameManager.CharacterType.Beetle)
        {
            if (currentSceneName == "Beetle Map")
            {
                Debug.Log("보스 맵으로 이동!");
                SceneManager.LoadScene("boss mantis map");
            }
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

    // 포탈 근처에 다가갔을 때 (레이더망)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            
            // F키 팝업 띄우기
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.AddNearbyItem();
        }
    }

    // 포탈에서 멀어졌을 때
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            
            // F키 팝업 끄기
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.RemoveNearbyItem();
        }
    }
}