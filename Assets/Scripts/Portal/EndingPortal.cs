using UnityEngine;
// using UnityEngine.SceneManagement; // 이제 씬 이동이 아니므로 필요 없습니다.

public class EndingPortal : MonoBehaviour
{
    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어가 닿았고, 아직 한 번도 작동 안 했으면
        if (collision.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true; // 중복 실행 방지
            Debug.Log("엔딩 포탈 접촉! 결과창을 띄웁니다.");

            if (UIManager.instance != null)
            {
                // ★ 씬 이동 대신 UIManager에게 팝업을 띄우라고 요청
                UIManager.instance.ShowEndingPopup();
                
                // 포탈은 할 일을 다 했으니 제거 (선택사항)
                Destroy(gameObject); 
            }
        }
    }
}