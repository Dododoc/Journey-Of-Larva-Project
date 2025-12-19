using UnityEngine;
using System.Collections;

public class MantisAI : BaseEnemyAI
{
    [Header("Mantis Combat Settings")]
    public float mantisAttackDamage = 25f;  // 공격력
    public float attackHitDelay = 0.5f;     // 공격 모션 시작 후 판정까지 걸리는 시간 (선딜레이)
    public float attackFullDuration = 1.0f; // 공격 전체 지속 시간 (후딜레이 포함)
    // ★ [추가] 공격 쿨타임 (공격이 끝나고 다음 공격까지 대기하는 시간)
    public float attackCooldown = 2.0f; 
    private float lastAttackTime; // 마지막으로 공격한 시간 기록용
    [Header("Range Adjustments")]
    public float attackHitRange = 1.8f;     // ★ 실제 데미지가 들어가는 범위 (Trigger 범위보다 살짝 크게 설정 추천)
    public float stopThreshold = 0.5f;      // 목표 지점 도달 판정 거리

    protected override void Update()
    {
        if (player == null || isAttacking || isKnockedBack || isGrabbed) 
        {
            if (isGrabbed && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        float xDiff = player.position.x - transform.position.x;

        // 1. 감지 범위 안
        if (dist <= detectRange) //
        {
            // 플레이어 바라보기
            if (Mathf.Abs(xDiff) > stopThreshold) LookAt(player.position.x);

            if (dist <= attackRange) 
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    StartCoroutine(AttackRoutine());
                }
                else
                {
                    // 쿨타임 중일 때는 가만히 서있거나(공격 대기), 
                    // 원한다면 여기서 천천히 배회하게 할 수도 있음.
                    // 지금은 그냥 노려보며 대기하도록 설정:
                    rb.linearVelocity = Vector2.zero;
                    if(anim != null) anim.SetBool("IsWalking", false);
                }
            }
            else 
            {
                MoveTo(player.position.x, chaseSpeed);
            }
        }
        else 
        {
            float distFromStart = Vector2.Distance(transform.position, startPos);
            if (distFromStart > patrolDistance + 1f) 
            {
                LookAt(startPos.x);
                MoveTo(startPos.x, moveSpeed);
            }
            else 
            {
                Patrol();
            }
        }
    }

    void MoveTo(float targetX, float speed)
    {
        if (isKnockedBack || isGrabbed) return;
        float xDiff = targetX - transform.position.x;
        
        if (Mathf.Abs(xDiff) <= stopThreshold)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if(anim != null) anim.SetBool("IsWalking", false);
            return;
        }
        
        if(anim != null) anim.SetBool("IsWalking", true);
        rb.linearVelocity = new Vector2(Mathf.Sign(xDiff) * speed, rb.linearVelocity.y);
    }

    void Patrol()
    {
        if (isKnockedBack || isGrabbed) return;
        if (anim != null) anim.SetBool("IsWalking", true);

        float currentDist = transform.position.x - startPos.x;

        if (currentDist >= patrolDistance && patrolDir > 0)
        {
            patrolDir = -1;
            LookAt(transform.position.x + patrolDir);
        }
        else if (currentDist <= -patrolDistance && patrolDir < 0)
        {
            patrolDir = 1;
            LookAt(transform.position.x + patrolDir);
        }

        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true; //
        // ★ 공격 시작 시간을 기록 (쿨타임 계산용)
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero; // 공격 중 정지

        if(anim != null) 
        { 
            anim.SetBool("IsWalking", false); 
            anim.SetTrigger("Attack"); 
        }

        // 선 딜레이 (칼을 들어 올리는 시간)
        yield return new WaitForSeconds(attackHitDelay);

        if (!isKnockedBack && !isGrabbed)
        {
            // ★ 핵심 수정: attackRange 대신 attackHitRange 사용
            // 공격을 시작한 거리보다 타격 범위가 약간 더 넓어야 억울하게 안 맞는 상황이 줄어듭니다.
            Collider2D hit = Physics2D.OverlapCircle(transform.position, attackHitRange, LayerMask.GetMask("Player"));
            
            if (hit != null) 
            {
                hit.GetComponent<PlayerStats>()?.TakeDamage(mantisAttackDamage);
            }
        }

        // 전체 동작 시간만큼 대기 (후 딜레이)
        yield return new WaitForSeconds(Mathf.Max(0, attackFullDuration - attackHitDelay));
        
        isAttacking = false;
    }

    // 범위 확인용 기즈모 그리기
    void OnDrawGizmosSelected()
    {
        // 공격 발동 범위 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange); // attackRange는 부모 변수

        // 실제 타격 범위 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackHitRange);
    }
}