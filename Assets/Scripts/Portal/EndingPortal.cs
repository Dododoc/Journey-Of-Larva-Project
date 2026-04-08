using UnityEngine;

public class EndingPortal : MonoBehaviour
{
    private bool isTriggered = false;
    private bool isPlayerNearby = false;

    void Update()
    {
        // ★ 플레이어가 근처에 있고, F키를 눌렀으며, 아직 한 번도 작동 안 했으면
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F) && !isTriggered)
        {
            isTriggered = true; // 중복 실행 방지
            Debug.Log("엔딩 포탈 접촉! 결과창을 띄웁니다.");

            if (UIManager.instance != null)
            {
                UIManager.instance.ShowEndingPopup();
                
                // ★ 포탈이 없어지기 전에 머리 위 F키 UI를 꺼줍니다.
                PlayerStats player = FindFirstObjectByType<PlayerStats>();
                if (player != null) player.RemoveNearbyItem();
                
                Destroy(gameObject); 
            }
            else
            {
                Debug.LogError("UIManager 인스턴스를 찾을 수 없습니다!");
            }
        }
    }

    // 포탈 근처에 다가갔을 때 (레이더망)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTriggered)
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
        if (collision.CompareTag("Player") && !isTriggered)
        {
            isPlayerNearby = false;
            
            // F키 팝업 끄기
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.RemoveNearbyItem();
        }
    }
}