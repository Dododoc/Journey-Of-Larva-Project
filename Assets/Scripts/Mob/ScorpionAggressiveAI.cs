using UnityEngine;

public class ScorpionAggressiveAI : MonoBehaviour
{
    [Header("1. 속도 설정")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("2. 거리 설정")]
    public float patrolDistance = 3f;
    public float detectRange = 6f;
    public float attackRange = 1.0f;

    [Header("3. 공격 설정")]
    public float attackCooldown = 2f;

    private Vector3 startPos;
    private bool isFacingRight = false; // ★ 수정됨: 기본 이미지가 왼쪽을 보고 있으므로 false로 시작
    private bool movingRight = true;   
    
    private bool isAttacking = false;
    private float lastAttackTime;

    private Animator anim;
    private Transform player;

    void Start()
    {
        anim = GetComponent<Animator>();
        startPos = transform.position;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (player == null || isAttacking) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (distToPlayer <= attackRange)
        {
            if (Time.time > lastAttackTime + attackCooldown)
                Attack();
            else
                anim.SetBool("isMoving", false);
        }
        else if (distToPlayer <= detectRange)
        {
            Chase();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        anim.SetBool("isMoving", true);

        if (movingRight)
        {
            transform.position += Vector3.right * patrolSpeed * Time.deltaTime;
            LookRight(true); // 오른쪽으로 갈 때

            if (transform.position.x >= startPos.x + patrolDistance)
            {
                movingRight = false;
                startPos = transform.position; 
            }
        }
        else
        {
            transform.position += Vector3.left * patrolSpeed * Time.deltaTime;
            LookRight(false); // 왼쪽으로 갈 때

            if (transform.position.x <= startPos.x - patrolDistance)
            {
                movingRight = true;
                startPos = transform.position; 
            }
        }
    }

    void Chase()
    {
        anim.SetBool("isMoving", true);

        float xDir = player.position.x - transform.position.x;

        if (xDir > 0)
        {
            transform.position += Vector3.right * chaseSpeed * Time.deltaTime;
            LookRight(true);
        }
        else
        {
            transform.position += Vector3.left * chaseSpeed * Time.deltaTime;
            LookRight(false);
        }
    }

    void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        anim.SetBool("isMoving", false);
        anim.SetTrigger("DoAttack");

        float xDir = player.position.x - transform.position.x;
        LookRight(xDir > 0);

        Invoke("ResetAttack", 1.0f);
    }

    void ResetAttack()
    {
        isAttacking = false;
    }

    // ★ 수정된 부분: 전갈 원본이 '왼쪽'을 보고 있으므로 반대로 설정!
    void LookRight(bool lookRight)
    {
        if (lookRight)
        {
            // 오른쪽을 봐야 하는데, 원본이 왼쪽이므로 180도 돌려야 함
            isFacingRight = true;
            transform.eulerAngles = new Vector3(0, 180, 0); 
        }
        else
        {
            // 왼쪽을 봐야 하는데, 원본이 왼쪽이므로 회전 없음 (0도)
            isFacingRight = false;
            transform.eulerAngles = new Vector3(0, 0, 0); 
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? startPos : transform.position;
        Gizmos.DrawLine(center, center + (movingRight ? Vector3.right : Vector3.left) * patrolDistance);
    }
}