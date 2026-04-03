using UnityEngine;

public class ElevatorPlatform : MonoBehaviour
{
    [Header("위치 설정")]
    public Transform topPosition;    // 원래 있는 위쪽 위치 (WayPoint)
    public Transform bottomPosition; // 스위치를 밟으면 내려올 아래쪽 위치 (WayPoint)
    
    [Header("이동 설정")]
    public float speed = 2f;         // 지형이 위아래로 움직이는 속도

    private Vector3 targetPos;
    private bool isWaitingAtBottom = false; // 바닥에 도착해서 대기 중인지 확인

    void Start()
    {
        targetPos = topPosition.position;
        transform.position = topPosition.position;
    }

    void Update()
    {
        // 목표 위치를 향해 부드럽게 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // 지형이 아래쪽 목표 위치에 거의 다 도달했는지 체크
        if (targetPos == bottomPosition.position && Vector3.Distance(transform.position, bottomPosition.position) < 0.01f)
        {
            isWaitingAtBottom = true; // 바닥에서 대기 모드 ON
        }
    }

    // 스위치를 밟았을 때 호출될 함수 (내려오기)
    public void MoveDown()
    {
        targetPos = bottomPosition.position;
    }

    // 개미(플레이어)가 지형에 올라탔을 때
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 바닥까지 완전히 내려와서 대기 중일 때, 플레이어가 닿으면 위로 상승
        if (isWaitingAtBottom && collision.gameObject.CompareTag("Player"))
        {
            targetPos = topPosition.position;
            isWaitingAtBottom = false; // 올라갈 거니까 대기 모드 OFF

            // 플레이어가 이동하는 지형에서 미끄러지지 않게 자식으로 임시 설정
            collision.transform.SetParent(transform);
        }
    }

    // 개미(플레이어)가 지형에서 벗어났을 때
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null); // 자식 설정 해제
        }
    }
}