using UnityEngine;

public class ChasingEnemyAI : BaseEnemyAI
{
    [Header("Behavior Settings")]
    public bool hasAttackAnimation = false; 
    public float attackCooldown = 2f;
    private float lastAttackTime;

    protected override void Update()
    {
        if (player == null || isAttacking) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectRange)
        {
            LookAt(player.position.x); 

            if (dist <= attackRange && hasAttackAnimation)
            {
                if (Time.time >= lastAttackTime + attackCooldown) StartAttack();
            }
            else
            {
                MoveTo(player.position.x, chaseSpeed);
            }
        }
        else
        {
            Patrol(); // 플레이어가 없으면 정찰
        }
    }

    void Patrol()
    {
        if(anim != null) anim.SetBool("IsWalking", true);
        
        // 현재 위치와 시작점 사이의 거리 체크
        float currentDist = transform.position.x - startPos.x;

        if (Mathf.Abs(currentDist) >= patrolDistance)
        {
            patrolDir *= -1; // 방향 전환
            LookAt(transform.position.x + patrolDir);
        }

        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);
    }

    void MoveTo(float targetX, float speed)
    {
        if(anim != null) anim.SetBool("IsWalking", true);
        float xDir = Mathf.Sign(targetX - transform.position.x);
        rb.linearVelocity = new Vector2(xDir * speed, rb.linearVelocity.y);
    }

    void StartAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        if(anim != null) anim.SetTrigger("Attack");
        lastAttackTime = Time.time;
        Invoke("EndAttack", 1.0f);
    }

    void EndAttack() { isAttacking = false; }
}