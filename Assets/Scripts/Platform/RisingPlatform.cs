using UnityEngine;

public class RisingPlatform : MonoBehaviour
{
    [Header("상승 설정")]
    public float riseHeight = 3f;   // 위로 올라갈 높이
    public float riseSpeed = 5f;    // 올라가는 속도 (빠르게 올라와야 멋있음!)

    private Vector3 targetPos;      // 목표 위치
    private bool isActivated = false; // 이미 작동했는지 확인

    void Start()
    {
        targetPos = transform.position + Vector3.up * riseHeight;
    }

    void Update()
    {
        // 작동 스위치가 켜지면 위로 이동
        if (isActivated)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, riseSpeed * Time.deltaTime);
        }
    }

    // 1. 밟았을 때: 아무 일도 안 함 (그냥 태워주기만 함)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 발판 위에 얌전히 있을 때는 같이 움직이도록 부모 설정 (미끄러짐 방지)
            collision.transform.SetParent(this.transform);
        }
    }

    // 2. 발판에서 벗어났을 때(지나갔을 때): 상승 시작! 🔥
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어 놓아주기
            collision.transform.SetParent(null);

            // 아직 작동 안 했다면, 이제 상승 시작!
            if (!isActivated)
            {
                isActivated = true;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 endPos = Application.isPlaying ? targetPos : transform.position + Vector3.up * riseHeight;
        Gizmos.DrawLine(transform.position, endPos);
        Gizmos.DrawWireSphere(endPos, 0.2f);
    }
}