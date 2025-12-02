using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 3f;        // 속도
    public float moveDistance = 3f; // 이동 거리

    [Header("타이밍 설정")]
    public bool startMovingRight = true; // 체크하면 오른쪽부터, 끄면 왼쪽부터 이동! ✅

    private float startX;
    private bool movingRight;

    void Start()
    {
        startX = transform.position.x;
        // 에디터에서 설정한 방향으로 시작하게 함
        movingRight = startMovingRight;
    }

    void Update()
    {
        if (movingRight)
        {
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            if (transform.position.x >= startX + moveDistance)
                movingRight = false;
        }
        else
        {
            transform.Translate(Vector2.left * speed * Time.deltaTime);
            if (transform.position.x <= startX - moveDistance)
                movingRight = true;
        }
    }

    // 캐릭터 태우기 (기존과 동일)
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

    // ⭐ 에디터에서 이동 경로 미리보기 (초록색 선)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // 현재 위치를 기준으로 좌우 이동 범위를 선으로 그려줍니다.
        // (게임 실행 전에는 현재 위치가 startX가 된다고 가정하고 그림)
        Vector3 center = Application.isPlaying ? new Vector3(startX, transform.position.y, 0) : transform.position;
        Gizmos.DrawLine(center - Vector3.right * moveDistance, center + Vector3.right * moveDistance);
    }
}