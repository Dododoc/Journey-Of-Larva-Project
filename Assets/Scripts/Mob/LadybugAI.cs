using UnityEngine;

public class LadybugAI : BaseEnemyAI
{
    protected override void Update()
    {
        if (player == null || isKnockedBack || isGrabbed) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectRange)
        {
            if (anim != null) { anim.SetBool("IsWalking", false); anim.SetBool("IsFlying", true); }
            LookAt(player.position.x);
            
            // ★ 거리 상관없이 무조건 플레이어에게 다가가서 몸을 부딪힘
            MoveTo(player.position.x, chaseSpeed); 
        }
        else
        {
            // 순찰
            if (anim != null) { anim.SetBool("IsFlying", false); anim.SetBool("IsWalking", true); }
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
        
        bool isLedge = IsLedgeAhead(patrolDir); 
        bool isWall = IsWallAhead(patrolDir);

        if ((currentDist >= patrolDistance && patrolDir > 0) || (patrolDir > 0 && (isLedge || isWall))) 
        { 
            patrolDir = -1; 
        }
        else if ((currentDist <= -patrolDistance && patrolDir < 0) || (patrolDir < 0 && (isLedge || isWall))) 
        { 
            patrolDir = 1; 
        }
        
        // ==========================================
        // ★ [핵심 고침] 이동하기 직전에 무조건 내가 이동할 방향(patrolDir)으로 고개를 돌리게 강제 동기화합니다!
        // ==========================================
        LookAt(transform.position.x + patrolDir); 

        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);
    }
}