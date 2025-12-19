using UnityEngine;
using System.Collections;

public class StagBeetleAI : BaseEnemyAI
{
    [Header("Stag Beetle Settings")]
    public float stopDistance = 1.0f;     // 플레이어 앞에서 멈추는 거리
    public float damage = 15f;            // 공격력
    public float attackRate = 1.0f;       // 공격 주기 (초)
    public float attackRangeCheck = 1.2f; // 실제 데미지 판정 거리 (stopDistance보다 약간 커야 함)

    protected override void Update()
    {
        // 넉백, 잡힘, 공격 중(쿨타임) 등 행동 불능 체크
        if (player == null || isKnockedBack || isGrabbed) 
        {
            if (isGrabbed && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // 1. 플레이어 감지 (추격 및 공격)
        if (dist <= detectRange)
        {
            LookAt(player.position.x);

            // 공격 사거리 안에 들어왔다면
            if (dist <= stopDistance)
            {
                // 이동 멈춤
                rb.linearVelocity = Vector2.zero;
                
                // ★ 핵심: 멈췄지만 공격 중인 느낌을 내기 위해 Walk 애니메이션 계속 재생
                if (anim != null) anim.SetBool("IsWalking", true);

                // 공격 실행 (쿨타임 체크는 코루틴 내부에서 처리)
                if (!isAttacking)
                {
                    StartCoroutine(AttackRoutine());
                }
            }
            // 사거리 밖이라면 추격
            else
            {
                MoveTo(player.position.x, chaseSpeed);
            }
        }
        // 2. 플레이어 미감지 (순찰)
        else
        {
            float distFromStart = Vector2.Distance(transform.position, startPos);
            
            // 시작 지점에서 너무 멀어지면 복귀
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

    // 공격 코루틴 (애니메이션 없이 데미지만 주기)
    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 1. 데미지 판정
        // (애니메이션이 없으므로 딜레이 없이 바로 때리거나, 약간의 선딜레이를 줘도 됨)
        yield return new WaitForSeconds(0.1f); 

        if (!isKnockedBack && !isGrabbed)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRangeCheck, LayerMask.GetMask("Player"));
            if (hit != null)
            {
                PlayerStats pStats = hit.GetComponent<PlayerStats>();
                if (pStats != null) pStats.TakeDamage(damage);
            }
        }

        // 2. 쿨타임 대기 (이 동안 다시 공격하지 않음)
        yield return new WaitForSeconds(attackRate);
        isAttacking = false;
    }

    // 이동 로직 (Walk 애니메이션 켜기 포함)
    void MoveTo(float targetX, float speed)
    {
        if (isKnockedBack || isGrabbed) return;

        float xDiff = targetX - transform.position.x;
        
        // 목표에 거의 도달했으면 정지
        if (Mathf.Abs(xDiff) <= 0.1f)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            // 여기서 애니메이션을 끌 수도 있지만, 사슴벌레는 계속 움직이는 느낌이 좋을 수 있음
            // 필요하다면: if(anim != null) anim.SetBool("IsWalking", false);
            return;
        }

        // 이동 중엔 무조건 Walk 켜기
        if (anim != null) anim.SetBool("IsWalking", true);
        rb.linearVelocity = new Vector2(Mathf.Sign(xDiff) * speed, rb.linearVelocity.y);
    }

    // 순찰 로직 (사마귀와 동일)
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
}