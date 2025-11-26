using UnityEngine;

public class MantisAI : MonoBehaviour
{
    [Header("이동 설정")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float patrolDistance = 3f; // 정찰 거리 (3발자국 정도)

    [Header("감지 설정")]
    public float detectRange = 5f;    // 플레이어 감지 범위
    public Transform groundCheck;     // 낭떠러지 체크 위치
    public float checkDistance = 1f;  // 낭떠러지 체크 깊이
    public LayerMask groundLayer;     // 땅 레이어

    private Rigidbody2D rb;
    private Transform player;
    private bool isFacingRight = true; // 현재 오른쪽을 보는 중인가?
    
    // 정찰 시작 위치를 기억하기 위한 변수
    private Vector2 startPos; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 게임 시작 시 현재 위치를 '시작점'으로 기억
        startPos = transform.position;
    }

    void Update()
    {
        if (player == null) return;

        // 공중에 떠 있을 땐 로직 정지 (떨림 방지)
        if (Mathf.Abs(rb.linearVelocity.y) > 0.5f) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 플레이어가 감지 범위 안에 있으면 -> 추적
        if (distanceToPlayer < detectRange)
        {
            ChasePlayer();
        }
        // 없으면 -> 정찰 (거리 제한 기능 추가됨)
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        // 1. 앞으로 이동
        rb.linearVelocity = new Vector2(transform.right.x * patrolSpeed, rb.linearVelocity.y);

        // 2. 이동한 거리 계산 (현재 위치 - 아까 방향 바꾼 위치)
        float distMoved = Vector2.Distance(transform.position, startPos);

        // 3. 조건 확인: (설정한 거리만큼 갔거나) OR (앞이 낭떠러지거나)
        bool isCliff = false;
        RaycastHit2D groundInfo = Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, groundLayer);
        if (groundInfo.collider == null) isCliff = true;

        if (distMoved >= patrolDistance || isCliff)
        {
            TurnAround();
        }
    }

    void ChasePlayer()
    {
        float xDir = player.position.x - transform.position.x;

        if (xDir > 0 && !isFacingRight) TurnAround();
        else if (xDir < 0 && isFacingRight) TurnAround();

        rb.linearVelocity = new Vector2(Mathf.Sign(xDir) * chaseSpeed, rb.linearVelocity.y);
    }

    void TurnAround()
    {
        isFacingRight = !isFacingRight;

        // 몸통 회전
        if (isFacingRight) transform.eulerAngles = new Vector3(0, 0, 0);
        else transform.eulerAngles = new Vector3(0, 180, 0);

        // [중요] 방향을 바꿨으니, 지금 위치를 새로운 '시작점'으로 갱신!
        // 그래야 여기서부터 다시 3발자국을 셉니다.
        startPos = transform.position;
    }

    void OnDrawGizmos()
    {
        // 정찰 범위를 초록색 선으로 표시 (에디터 확인용)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPos, 0.2f); // 기준점
        Gizmos.DrawLine(transform.position, startPos); // 현재 이동한 거리

        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(groundCheck.position, Vector2.down * checkDistance);
        }
    }
}