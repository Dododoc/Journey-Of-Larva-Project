using UnityEngine;
using System.Collections;

public class BeeAI : BaseEnemyAI
{
    [Header("Bee Movement")]
    public float flyWidth = 3f;
    public float flyHeight = 1.5f;
    public float loopSpeed = 2f;
    // stopDistance는 무조건 돌진하므로 삭제했습니다.

    [Header("Poison Attack")]
    public float poisonTotalDamage = 15f;
    public float poisonDuration = 3.0f;
    
    // ★ [추가] 독 주입 쿨타임 (닿아있을 때 프레임마다 무한정 독이 중첩되는 것을 방지)
    public float poisonCooldown = 1.5f;
    private float lastPoisonTime = 0f;

    private float patrolTimer;
    private bool isRecovering = false; 

    protected override void Start()
    {
        base.Start();
        if (rb != null) rb.gravityScale = 0f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (isFacingRight)
        {
            if(sr != null) sr.flipX = true; 
        }
        else
        {
            if(sr != null) sr.flipX = false;
        }
    }

    protected override void Update()
    {
        // 넉백, 잡힘, 복귀 중일 땐 일반 행동 중단
        if (player == null || isKnockedBack || isGrabbed || isRecovering) 
        {
            if (isGrabbed && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        
        if (dist <= detectRange)
        {
            LookAt(player.position.x);
            if (anim != null) anim.SetBool("IsFlying", true);
            
            // ★ 거리나 사거리 상관없이 무조건 플레이어에게 돌진!
            MoveToTarget(player.position, chaseSpeed); 
        }
        else
        {
            PatrolInfinity();
        }
    }

    void MoveToTarget(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * speed;
    }

    void PatrolInfinity()
    {
        if (anim != null) anim.SetBool("IsFlying", true);
        patrolTimer += Time.deltaTime * loopSpeed;
        float x = startPos.x + Mathf.Cos(patrolTimer) * flyWidth;
        float y = startPos.y + Mathf.Sin(2 * patrolTimer) * (flyHeight / 2);
        
        Vector2 targetPos = new Vector2(x, y);
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, targetPos);
        rb.linearVelocity = dir * moveSpeed * Mathf.Clamp(dist, 0.5f, 1f);
        LookAt(targetPos.x);
    }

    // =========================================================
    // ★ [핵심] 몸통 박치기 시 독 주입 로직
    // =========================================================
    
    void OnCollisionStay2D(Collision2D collision)
    {
        ApplyPoisonToTarget(collision.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        ApplyPoisonToTarget(other.gameObject);
    }

    void ApplyPoisonToTarget(GameObject target)
    {
        // 벌이 기절 중이거나, 던져지는 중일 땐 독을 묻히지 않음
        if (isKnockedBack || isGrabbed || isRecovering) return;

        // 타겟이 플레이어이고, 마지막으로 독을 묻힌 지 쿨타임이 지났다면
        if (target.CompareTag("Player") && Time.time >= lastPoisonTime + poisonCooldown)
        {
            PlayerStats pStats = target.GetComponent<PlayerStats>();
            if (pStats != null)
            {
                pStats.ApplyPoison(poisonTotalDamage, poisonDuration);
                lastPoisonTime = Time.time; // 쿨타임 리셋
            }
        }
    }

    // =========================================================
    // 아래부터는 기존 넉백 / 복귀 로직 (수정 없이 그대로 유지)
    // =========================================================

    public override void ApplyKnockback(Vector2 force, float duration = 0.4f)
    {
        StopCoroutine("RecoverFlightRoutine"); 
        StartCoroutine(RecoverFlightRoutine(force, duration, false));
    }

    public override void OnThrown(Vector2 force)
    {
        StopCoroutine("RecoverFlightRoutine");
        StartCoroutine(RecoverFlightRoutine(force, 2.0f, true)); 
    }

    IEnumerator RecoverFlightRoutine(Vector2 force, float duration, bool isThrown)
    {
        isKnockedBack = true;
        isAttacking = false;
        
        if (rb != null)
        {
            rb.gravityScale = 1.5f; 
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (IsTouchingGround()) break; 
            yield return null;
        }

        isKnockedBack = false;
        isRecovering = true; 
        if (rb != null)
        {
            rb.gravityScale = 0f;      
            rb.linearVelocity = Vector2.zero; 
        }

        float recoverTime = 0f;
        Vector2 recoverStartPos = transform.position;
        float targetY = (startPos.y > transform.position.y) ? startPos.y : transform.position.y + 1f;

        while (recoverTime < 1.0f)
        {
            recoverTime += Time.deltaTime;
            transform.position = Vector2.Lerp(recoverStartPos, new Vector2(transform.position.x, targetY), recoverTime);
            yield return null;
        }

        isRecovering = false; 
    }

    bool IsTouchingGround()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 0.5f, LayerMask.GetMask("Ground"));
    }
}