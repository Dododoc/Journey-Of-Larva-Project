using UnityEngine;

public class ScorpionAI : MonoBehaviour
{
    [Header("1. 순찰 설정")]
    public float moveSpeed = 2f;        // 이동 속도
    public float patrolDistance = 3f;   // 좌우로 왔다 갔다 할 거리

    [Header("2. 공격 설정")]
    public float attackRange = 1.5f;    // 공격 감지 거리
    public float attackCooldown = 2f;   // 공격 후 대기 시간 (연타 방지)
    public Transform playerCheck;       // 거리 잴 기준점 (전갈의 중심)

    private Vector3 startPos;           // 처음 위치 저장
    private bool movingRight = true;    // 현재 이동 방향
    private bool isAttacking = false;   // 공격 중인가?
    private float lastAttackTime;       // 마지막 공격 시간

    private Animator anim;
    private Transform player;           // 플레이어 위치

    void Start()
    {
        anim = GetComponent<Animator>();
        startPos = transform.position;
        
        // 게임 시작 시 'Player' 태그를 가진 녀석(개미)을 찾아서 기억해둠
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        // 플레이어를 못 찾았거나 공격 중이면 움직이지 않음
        if (player == null || isAttacking) return;

        // 1. 거리 계산 (플레이어와 나 사이의 거리)
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // 2. 공격 범위 안에 들어왔니?
        if (distToPlayer < attackRange)
        {
            // 쿨타임이 지났다면 공격!
            if (Time.time > lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
        else
        {
            // 3. 범위 밖이라면 평소처럼 순찰(Patrol)
            Patrol();
        }
    }

    void Patrol()
    {
        // 걷는 애니메이션 켜기
        anim.SetBool("isMoving", true);

        if (movingRight)
        {
            transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
            transform.localScale = new Vector3(-1, 1, 1); // 오른쪽 보기 (이미지 반전 필요시 조절)

            // 설정한 거리만큼 갔으면 방향 전환
            if (transform.position.x >= startPos.x + patrolDistance)
                movingRight = false;
        }
        else
        {
            transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
            transform.localScale = new Vector3(1, 1, 1); // 왼쪽 보기

            // 설정한 거리만큼 갔으면 방향 전환
            if (transform.position.x <= startPos.x - patrolDistance)
                movingRight = true;
        }
    }

    void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 걷기 멈춤
        anim.SetBool("isMoving", false);
        
        // 공격 애니메이션 실행!
        anim.SetTrigger("DoAttack");

        // (중요) 플레이어 쪽을 바라보게 하기
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(-1, 1, 1); // 오른쪽
        else
            transform.localScale = new Vector3(1, 1, 1);  // 왼쪽

        // 1초 뒤에 다시 움직이게 풀어줌 (공격 모션 시간만큼 대기)
        Invoke("ResetAttack", 1.0f);
    }

    void ResetAttack()
    {
        isAttacking = false;
    }

    // 에디터에서 범위 눈으로 보기
    void OnDrawGizmos()
    {
        // 순찰 범위 (초록선)
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? startPos : transform.position;
        Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);

        // 공격 범위 (빨간원)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}