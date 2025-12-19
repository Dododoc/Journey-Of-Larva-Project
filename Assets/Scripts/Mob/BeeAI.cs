using UnityEngine;
using System.Collections;

public class BeeAI : BaseEnemyAI
{
    [Header("Bee Movement")]
    public float flyWidth = 3f;
    public float flyHeight = 1.5f;
    public float loopSpeed = 2f;
    public float stopDistance = 3.0f;

    [Header("Poison Attack")]
    public float poisonTotalDamage = 15f;
    public float poisonDuration = 3.0f;
    public float stingHitDelay = 0.3f;

    private float patrolTimer;
    private bool isRecovering = false; // 복귀 중인지 체크

protected override void Start()
{
    base.Start();
    if (rb != null) rb.gravityScale = 0f;

    // ★ [1. 초기 보정 수정]
    // transform(몸체 전체)을 돌리는 대신, 그림(Sprite)만 뒤집습니다.
    
    SpriteRenderer sr = GetComponent<SpriteRenderer>();

    if (isFacingRight)
    {
        // 원래 이미지가 왼쪽을 보고 있으므로, 
        // 오른쪽을 보게 하려면 X축으로 뒤집어야(Flip) 합니다.
        if(sr != null) sr.flipX = true; 
    }
    else
    {
        // 왼쪽을 봐야 한다면 원본 그대로(Flip 끔) 둡니다.
        if(sr != null) sr.flipX = false;
    }
}
// [중요] 자식(벌)은 'override'를 쓰고 내용을 반대로 뒤집습니다!

    protected override void Update()
    {
        // 넉백, 잡힘, 복귀 중(isRecovering)이면 일반 행동 중단
        if (player == null || isKnockedBack || isGrabbed || isAttacking || isRecovering) 
        {
            if (isGrabbed && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // ... (기존 이동 및 공격 로직 동일) ...
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= detectRange)
        {
            LookAt(player.position.x);
            if (dist <= attackRange) { rb.linearVelocity = Vector2.zero; StartCoroutine(AttackRoutine()); }
            else if (dist > stopDistance) { MoveToTarget(player.position, chaseSpeed); } // MoveToTarget 함수 분리함
            else { rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 5f); }
        }
        else
        {
            PatrolInfinity();
        }
    }

    // 이동 로직 분리 (재사용 위해)
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

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        if (anim != null) { anim.SetBool("IsFlying", true); anim.SetTrigger("Attack"); }
        yield return new WaitForSeconds(stingHitDelay);
        if (!isKnockedBack && !isGrabbed)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRange, LayerMask.GetMask("Player"));
            if (hit != null) hit.GetComponent<PlayerStats>()?.ApplyPoison(poisonTotalDamage, poisonDuration);
        }
        yield return new WaitForSeconds(1.0f);
        isAttacking = false;
    }

    // ★ [핵심 1] 넉백 당했을 때 중력 켜기
    public override void ApplyKnockback(Vector2 force, float duration = 0.4f)
    {
        StopCoroutine("RecoverFlightRoutine"); // 기존 복귀 취소
        StartCoroutine(RecoverFlightRoutine(force, duration, false));
    }

    // ★ [핵심 2] 던져졌을 때 중력 켜기 (풍뎅이 스킬용)
    public override void OnThrown(Vector2 force)
    {
        StopCoroutine("RecoverFlightRoutine");
        StartCoroutine(RecoverFlightRoutine(force, 2.0f, true)); // 2초 뒤 복귀
    }

    // ★ [핵심 3] 중력 켰다가 끄고 복귀하는 코루틴
    IEnumerator RecoverFlightRoutine(Vector2 force, float duration, bool isThrown)
    {
        isKnockedBack = true;
        isAttacking = false;
        
        // 1. 중력 켜고 힘 주기
        if (rb != null)
        {
            rb.gravityScale = 1.5f; // 떨어지는 맛이 있게 중력 부여
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        // 2. 지정된 시간(또는 바닥에 닿을 때까지) 대기
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 만약 바닥에 닿았다면 즉시 복귀 시작 (질질 끌지 않게)
            if (IsTouchingGround()) break; 
            yield return null;
        }

        // 3. 비행 모드 복구
        isKnockedBack = false;
        isRecovering = true; // 복귀 모드 진입
        if (rb != null)
        {
            rb.gravityScale = 0f;      // 중력 끄기
            rb.linearVelocity = Vector2.zero; // 속도 초기화
        }

        // 4. 원래 높이(startPos.y)로 부드럽게 상승
        float recoverTime = 0f;
        Vector2 recoverStartPos = transform.position;
        // 복귀 목표: 원래 높이보다 약간 위 (안전하게)
        float targetY = (startPos.y > transform.position.y) ? startPos.y : transform.position.y + 1f;

        while (recoverTime < 1.0f)
        {
            recoverTime += Time.deltaTime;
            // 위로 둥실 떠오르기
            transform.position = Vector2.Lerp(recoverStartPos, new Vector2(transform.position.x, targetY), recoverTime);
            yield return null;
        }

        isRecovering = false; // 정상 AI 복귀
    }

    bool IsTouchingGround()
    {
        // 발 밑에 땅이 있는지 체크
        return Physics2D.Raycast(transform.position, Vector2.down, 0.5f, LayerMask.GetMask("Ground"));
    }
}