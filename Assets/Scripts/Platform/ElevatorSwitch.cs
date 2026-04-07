using UnityEngine;

public class ElevatorSwitch : MonoBehaviour
{
    [Header("연결할 엘리베이터 플랫폼")]
    public ElevatorPlatform targetElevator; // 움직일 엘리베이터를 끌어다 넣습니다.

    [Header("상호작용 효과")]
    public GameObject outlineEffect; // 스위치 주변 테두리나 이펙트 (선택사항)

    private bool isPlayerNearby = false;
    private bool isActivated = false; // 스위치를 한 번 누르면 중복 작동하지 않게 막음

    void Start()
    {
        if (outlineEffect != null) outlineEffect.SetActive(false);
    }

    void Update()
    {
        // ★ [핵심] 플레이어가 근처에 있고, F키를 눌렀으며, 아직 작동 전일 때!
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F) && !isActivated)
        {
            ActivateSwitch();
        }
    }

    void ActivateSwitch()
    {
        isActivated = true; // 스위치 작동 완료
        Debug.Log("스위치 작동! 엘리베이터가 내려옵니다.");

        // 엘리베이터 스크립트의 MoveDown 함수 실행
        if (targetElevator != null)
        {
            targetElevator.MoveDown();
        }

        // 작동 후에는 팝업 UI와 외곽선을 꺼줍니다.
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null) player.RemoveNearbyItem();
        
        if (outlineEffect != null) outlineEffect.SetActive(false);
    }

    // 플레이어가 근처에 왔을 때 감지 (레이더망)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 아직 작동하지 않은 스위치일 때만 팝업 띄우기
        if (collision.CompareTag("Player") && !isActivated)
        {
            isPlayerNearby = true;
            if (outlineEffect != null) outlineEffect.SetActive(true);

            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.AddNearbyItem(); // F키 누르라는 팝업 표시
        }
    }

    // 플레이어가 멀어지면 팝업 끄기
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isActivated)
        {
            isPlayerNearby = false;
            if (outlineEffect != null) outlineEffect.SetActive(false);

            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.RemoveNearbyItem(); // 팝업 제거
        }
    }
}