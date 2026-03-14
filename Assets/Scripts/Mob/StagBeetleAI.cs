using UnityEngine;

public class StagBeetleAI : BaseEnemyAI
{
    protected override void Update()
    {
        if (player == null || isKnockedBack || isGrabbed) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectRange)
        {
            LookAt(player.position.x);
            // ★ 공격 코루틴 삭제! 그냥 냅다 들이받도록 수정
            MoveTo(player.position.x, chaseSpeed);
        }
        else
        {
            float distFromStart = Vector2.Distance(transform.position, startPos);
            if (distFromStart > patrolDistance + 1f) { LookAt(startPos.x); MoveTo(startPos.x, moveSpeed); }
            else { Patrol(); }
        }
    }

    void MoveTo(float targetX, float speed)
    {
        if (isKnockedBack || isGrabbed) return;
        float xDiff = targetX - transform.position.x;
        
        // ★ [떨림 해결 핵심] 정지 거리를 0.1f에서 '0.4f' 정도로 늘립니다!
        // (충돌하기 직전에 AI가 먼저 멈추기 때문에 더 이상 바들바들 떨지 않습니다)
        if (Mathf.Abs(xDiff) <= 0.4f) 
        { 
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            if (anim != null) anim.SetBool("IsWalking", false);
            return; 
        }

        // ★ [벽/낭떠러지 감지] 낭떠러지거나 벽이 있으면 쫓아가던 걸 멈춤!
        if (IsLedgeAhead(xDiff) || IsWallAhead(xDiff))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            if (anim != null) anim.SetBool("IsWalking", false);
            return; 
        }
        
        LookAt(targetX);
        if (anim != null) anim.SetBool("IsWalking", true);
        rb.linearVelocity = new Vector2(Mathf.Sign(xDiff) * speed, rb.linearVelocity.y);
    }
    void Patrol()
    {
        if (isKnockedBack || isGrabbed) return;
        if (anim != null) anim.SetBool("IsWalking", true);
        
        float currentDist = transform.position.x - startPos.x;
        
        // ★ 낭떠러지 센서와 벽 센서 둘 다 작동!
        bool isLedge = IsLedgeAhead(patrolDir); 
        bool isWall = IsWallAhead(patrolDir);

        // 순찰 거리 끝에 도달했거나, '낭떠러지'나 '벽'을 만나면 뒤로 돌기!
        if ((currentDist >= patrolDistance && patrolDir > 0) || (patrolDir > 0 && (isLedge || isWall))) 
        { 
            patrolDir = -1; 
            LookAt(transform.position.x + patrolDir); 
        }
        else if ((currentDist <= -patrolDistance && patrolDir < 0) || (patrolDir < 0 && (isLedge || isWall))) 
        { 
            patrolDir = 1; 
            LookAt(transform.position.x + patrolDir); 
        }
        
        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);
    }
}