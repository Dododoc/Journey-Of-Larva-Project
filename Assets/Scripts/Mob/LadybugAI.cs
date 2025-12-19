using UnityEngine;
using System.Collections;

public class LadybugAI : BaseEnemyAI
{
    [Header("Ladybug Settings")]
    public float stopDistance = 0.8f; 
    
    // ★ 공격 설정 추가
    public float damage = 10f;       // 무당벌레 공격력
    public float attackRate = 1.0f;  // 공격 속도 (1초에 한 번)
    public float attackRangeCheck = 1.0f; // 공격 판정 범위 (stopDistance보다 약간 커야 함)

    protected override void Update()
    {
        if (player == null || isKnockedBack || isGrabbed || isAttacking) // isAttacking 체크 추가
        {
            if (isGrabbed && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // 1. 플레이어 감지 (추격 모드)
        if (dist <= detectRange)
        {
            if (anim != null) { anim.SetBool("IsWalking", false); anim.SetBool("IsFlying", true); }
            LookAt(player.position.x);

            // ★ 거리 체크 수정: 공격 사거리 안이면 공격 시작
            // (stopDistance보다 아주 조금 더 가까우면 공격)
            if (dist <= stopDistance + 0.1f)
            {
                rb.linearVelocity = Vector2.zero; // 멈춤
                StartCoroutine(AttackRoutine());  // 공격 코루틴 실행
            }
            else
            {
                MoveTo(player.position.x, chaseSpeed); // 추격
            }
        }
        // 2. 플레이어 미감지 (순찰 모드)
        else
        {
            // ... (기존 순찰 로직 그대로 유지) ...
            if (anim != null) { anim.SetBool("IsFlying", false); anim.SetBool("IsWalking", true); }
            
            float distFromStart = Vector2.Distance(transform.position, startPos);
            if (distFromStart > patrolDistance + 1f) { LookAt(startPos.x); MoveTo(startPos.x, moveSpeed); }
            else { Patrol(); }
        }
    }

    // ★ 공격 코루틴 추가 (사마귀와 유사하지만 애니메이션 없이 데미지만 줌)
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        // 공격 애니메이션이 있다면 여기서 실행: anim.SetTrigger("Attack");
        // 무당벌레는 없으므로 생략하고 딜레이만 줍니다.
        
        yield return new WaitForSeconds(0.2f); // 선 딜레이 (살짝 멈칫)

        if (!isKnockedBack && !isGrabbed)
        {
            // 범위 안에 플레이어가 있는지 확인하고 데미지
            Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRangeCheck, LayerMask.GetMask("Player"));
            if (hit != null)
            {
                // 플레이어 스크립트의 TakeDamage 호출
                PlayerStats pStats = hit.GetComponent<PlayerStats>();
                if (pStats != null) pStats.TakeDamage(damage);
            }
        }

        yield return new WaitForSeconds(attackRate); // 후 딜레이 (다음 공격까지 대기)
        isAttacking = false;
    }

    // 사마귀와 동일한 이동 로직 (물리 기반)
    void MoveTo(float targetX, float speed)
    {
        if (isKnockedBack || isGrabbed) return;

        float xDiff = targetX - transform.position.x;
        
        // 목표에 거의 도달했으면 속도 0 (미세 떨림 방지)
        if (Mathf.Abs(xDiff) <= 0.1f)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(Mathf.Sign(xDiff) * speed, rb.linearVelocity.y);
    }

    // 사마귀와 동일한 순찰 로직 (버그 수정된 버전)
    void Patrol()
    {
        if (isKnockedBack || isGrabbed) return;
        
        // 순찰 중엔 걷기 애니메이션 확실히 켜기
        if (anim != null) anim.SetBool("IsWalking", true);

        float currentDist = transform.position.x - startPos.x;

        // 오른쪽 한계 도달 시 왼쪽으로
        if (currentDist >= patrolDistance && patrolDir > 0)
        {
            patrolDir = -1;
            LookAt(transform.position.x + patrolDir);
        }
        // 왼쪽 한계 도달 시 오른쪽으로
        else if (currentDist <= -patrolDistance && patrolDir < 0)
        {
            patrolDir = 1;
            LookAt(transform.position.x + patrolDir);
        }

        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);
    }
}