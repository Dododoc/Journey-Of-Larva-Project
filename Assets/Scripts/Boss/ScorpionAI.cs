using UnityEngine;
using System.Collections;

public class ScorpionAI : BaseEnemyAI
{
    [Header("Patrol Settings")]
    public float walkTime = 2.0f;       // 딱 2초 동안 움직임
    public float pauseTime = 1.5f;      // 멈춰서 대기하는 시간

    [Header("Combat Settings")]
    public float attackAnimationDuration = 1.0f; // 공격 애니메이션이 끝날 때까지 기다리는 시간

    private float patrolTimer = 0f;
    private bool isPausing = false;

    protected override void Update()
    {
        // 공격 중이거나 행동 불능일 땐 아무것도 안 하고 가만히 있음
        if (player == null || isKnockedBack || isGrabbed || isAttacking) 
        {
            if (isGrabbed && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // ==========================================
        // 1. 추격 및 공격 모드 (감지 사거리 내)
        // ==========================================
        if (dist <= detectRange)
        {
            isPausing = false; // 플레이어를 보면 쉬던 것도 취소!

            // [공격] 공격 사거리 안이라면 멈춰서 애니메이션 재생
            if (dist <= attackRange)
            {
                StartCoroutine(AttackRoutine());
            }
            // [추격] 사거리는 아니지만 감지 범위 안이라면 멈추지 않고 쫓아감
            else
            {
                MoveTo(player.position.x, chaseSpeed);
            }
        }
        // ==========================================
        // 2. 순찰 모드 (사거리 밖으로 도망갔을 때)
        // ==========================================
        else
        {
            PatrolWithPause();
        }
    }

    void PatrolWithPause()
    {
        if (isKnockedBack || isGrabbed) return;

        patrolTimer += Time.deltaTime;

        // [멈춰있는 상태]
        if (isPausing)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool("IsWalking", false);

            // 쉬는 시간이 다 지나면 다시 걷기 시작
            if (patrolTimer >= pauseTime)
            {
                isPausing = false;
                patrolTimer = 0f;
            }
        }
        // [2초간 걷는 상태]
        else
        {
            float currentDist = transform.position.x - startPos.x;

            // 활동 반경 끝에 다다르면 방향 뒤집기
            if (currentDist >= patrolDistance && patrolDir > 0)
            {
                patrolDir = -1;
            }
            else if (currentDist <= -patrolDistance && patrolDir < 0)
            {
                patrolDir = 1;
            }

            // ★ 뒤로 걷는 버그 해결: 이동하기 전 무조건 이동할 방향을 바라보게 강제
            LookAt(transform.position.x + patrolDir);
            
            if (anim != null) anim.SetBool("IsWalking", true);
            rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);

            // 정확히 2초 걸었으면 멈춤
            if (patrolTimer >= walkTime)
            {
                isPausing = true;
                patrolTimer = 0f;
            }
        }
    }

    void MoveTo(float targetX, float speed)
    {
        if (isKnockedBack || isGrabbed) return;
        float xDiff = targetX - transform.position.x;
        
        // 미세 떨림 방지용 정지
        if (Mathf.Abs(xDiff) <= 0.1f) 
        { 
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            if (anim != null) anim.SetBool("IsWalking", false);
            return; 
        }
        
        // ★ 뒤로 걷는 버그 해결: 추격할 때도 항상 타겟을 먼저 쳐다보게 강제
        LookAt(targetX);

        if (anim != null) anim.SetBool("IsWalking", true);
        rb.linearVelocity = new Vector2(Mathf.Sign(xDiff) * speed, rb.linearVelocity.y);
    }

    // ==========================================
    // 공격 코루틴 (데미지 기능 삭제, 애니메이션만 재생)
    // ==========================================
    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 제자리에 멈춰서 플레이어를 쳐다봄
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        LookAt(player.position.x);

        // 공격 애니메이션 켜기
        if (anim != null) 
        {
            anim.SetBool("IsWalking", false);
            anim.SetTrigger("Attack"); 
        }

        // 데미지는 몸통 박치기로 들어가므로, 여기서는 애니메이션이 다 끝날 때까지만 순수하게 기다림
        yield return new WaitForSeconds(attackAnimationDuration);

        // 공격이 끝났음을 알림 -> 바로 Update로 돌아가서 아직 범위 내면 다시 공격, 아니면 쫓아감
        isAttacking = false;
    }
}