using UnityEngine;

public class ElevatorPlatform : MonoBehaviour
{
    [Header("아래쪽 도착 지점")]
    public Transform bottomPosition; // ★ 이제 아래쪽 위치(Bottom) 하나만 연결하면 됩니다!
    
    [Header("이동 속도")]
    public float speed = 2f;         

    private Vector3 topPos;          // 원래 시작 위치를 자동으로 기억할 변수
    private Vector3 targetPos;
    private bool isPlayerOn = false; 

    void Start()
    {
        // ★ [핵심] 따로 Top Position을 만들 필요 없이, 게임 시작 시 엘리베이터가 있는 위치를 옥상으로 고정합니다!
        topPos = transform.position; 
        targetPos = topPos;
    }

    void Update()
    {
        // 목표 위치를 향해 부드럽게 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // 플레이어가 타고 있고 F키를 누르면 위/아래 전환
        if (isPlayerOn && Input.GetKeyDown(KeyCode.F))
        {
            ToggleElevator();
        }
    }

    public void MoveDown() { targetPos = bottomPosition.position; }
    public void MoveUp() { targetPos = topPos; }

    public void ToggleElevator()
    {
        // 현재 타겟이 아래쪽이면 -> 위로 설정
        if (targetPos == bottomPosition.position) targetPos = topPos;
        // 아니면 -> 아래로 설정
        else targetPos = bottomPosition.position; 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOn = true;
            collision.transform.SetParent(transform); // 미끄럼 방지

            PlayerStats player = collision.gameObject.GetComponent<PlayerStats>();
            if (player != null) player.AddNearbyItem();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOn = false;
            collision.transform.SetParent(null); 

            PlayerStats player = collision.gameObject.GetComponent<PlayerStats>();
            if (player != null) player.RemoveNearbyItem();
        }
    }
}