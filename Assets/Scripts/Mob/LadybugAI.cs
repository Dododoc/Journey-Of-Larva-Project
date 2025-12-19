using UnityEngine; // ★ 이 줄이 없으면 Vector2 에러가 납니다.

public class LadybugAI : BaseEnemyAI
{
    [Header("Ladybug Specific")]
    public Transform pointA;
    public Transform pointB;
    private Transform targetPoint;
    private bool isFlying = false;

    protected override void Start()
    {
        base.Start(); // 부모의 플레이어 찾기 로직 실행
        targetPoint = pointB;
    }

    protected override void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectRange)
        {
            // 플레이어 발견 시: 비행 모드 전환 및 추격
            if (!isFlying) 
            { 
                isFlying = true; 
                if(anim != null) anim.SetBool("IsFlying", true); 
            }
            
            // 부모에 정의된 방향 전환 로직 대신 추격용 시선 처리
            float xDir = player.position.x - transform.position.x;
            if (xDir > 0 && !isFacingRight) Flip();
            else if (xDir < 0 && isFacingRight) Flip();

            transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);
        }
        else
        {
            // 플레이어가 멀어지면: 정찰 모드
            if (isFlying) 
            { 
                isFlying = false; 
                if(anim != null) anim.SetBool("IsFlying", false); 
            }

            if (pointA == null || pointB == null) return;
            
            transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);
            
            if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
            {
                targetPoint = (targetPoint == pointA) ? pointB : pointA;
            }

            float xDir = targetPoint.position.x - transform.position.x;
            if (xDir > 0 && !isFacingRight) Flip();
            else if (xDir < 0 && isFacingRight) Flip();
        }
    }
}