using UnityEngine;

public class VerticalPlatform : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 2f;        // 이동 속도
    public float moveDistance = 2f; // 위아래로 움직일 범위

    [Header("방향 설정 (이것만 바꾸면 됩니다!)")]
    public bool startMovingUp = true; // ✅ 체크하면 '위'로 먼저, 끄면 '아래'로 먼저 출발

    private float startY;           // 시작 높이(기준점)
    private bool movingUp;          // 현재 방향 상태

    void Start()
    {
        startY = transform.position.y;
        movingUp = startMovingUp; // 설정한 방향대로 시작!
    }

    void Update()
    {
        if (movingUp) // 위로 가는 중
        {
            transform.Translate(Vector2.up * speed * Time.deltaTime);
            // 기준점보다 위로 올라갔으면 -> 아래로 방향 전환
            if (transform.position.y >= startY + moveDistance)
                movingUp = false;
        }
        else // 아래로 가는 중
        {
            transform.Translate(Vector2.down * speed * Time.deltaTime);
            // 기준점보다 아래로 내려갔으면 -> 위로 방향 전환
            if (transform.position.y <= startY - moveDistance)
                movingUp = true;
        }
    }

    // 캐릭터 태우기 (같이 움직임)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            collision.transform.SetParent(this.transform);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            collision.transform.SetParent(null);
    }
}