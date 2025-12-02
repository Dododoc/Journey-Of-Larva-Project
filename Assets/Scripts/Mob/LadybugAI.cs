using UnityEngine;

public class LadybugAI : MonoBehaviour
{
    [Header("비행 설정")]
    public float speed = 2f;
    public Transform pointA; // 이동할 왼쪽 끝 지점
    public Transform pointB; // 이동할 오른쪽 끝 지점

    private Transform targetPoint; // 현재 가고 있는 목표
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        // 처음엔 B지점으로 가도록 설정
        targetPoint = pointB;
    }

    void Update()
    {
        // 1. 목표 지점이 없으면 아무것도 안 함 (오류 방지)
        if (pointA == null || pointB == null) return;

        // 2. 목표 지점 쪽으로 이동
        // MoveTowards: 현재위치에서 목표위치까지 speed 속도로 이동
        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

        // 3. 목표에 거의 도착했는지 확인 (거리 0.1 이하)
        if (Vector2.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            // 목표 변경 (A였으면 B로, B였으면 A로)
            if (targetPoint == pointB)
            {
                targetPoint = pointA;
                sr.flipX = true; // 방향 전환 (이미지에 따라 true/false 조절 필요)
            }
            else
            {
                targetPoint = pointB;
                sr.flipX = false;
            }
        }
    }

    // 에디터에서 이동 경로를 선으로 보여줌
    void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA.position, pointB.position); // 경로 선 그리기
            Gizmos.DrawWireSphere(pointA.position, 0.3f);
            Gizmos.DrawWireSphere(pointB.position, 0.3f);
        }
    }
}