using UnityEngine;

public class LadybugAI : BaseEnemyAI
{
    protected override void Update()
    {
        if (player == null || isKnockedBack || isGrabbed) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectRange)
        {
            // ★ [수정됨] 플레이어를 발견해 쫓아갈 때는 걷기를 확실히 끄고 날기를 켭니다!
            if (anim != null) 
            { 
                anim.SetBool("IsWalking", false); 
                anim.SetBool("IsFlying", true); 
            }
            LookAt(player.position.x);
            
            MoveTo(player.position.x, chaseSpeed); 
        }
        else
        {
            // 순찰 혹은 제자리로 돌아갈 때
            if (anim != null) 
            { 
                anim.SetBool("IsFlying", false); 
                anim.SetBool("IsWalking", true); 
            }
            float distFromStart = Vector2.Distance(transform.position, startPos);
            if (distFromStart > patrolDistance + 1f) { LookAt(startPos.x); MoveTo(startPos.x, moveSpeed); }
            else { Patrol(); }
        }
    }

    void MoveTo(float targetX, float speed)
    {
        if (isKnockedBack || isGrabbed) return;
        float xDiff = targetX - transform.position.x;
        
        // 정지 거리에 도달했을 때
        if (Mathf.Abs(xDiff) <= 0.4f) 
        { 
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            
            // ★ [수정됨] 멈춰 섰을 때는 날기, 걷기를 모두 꺼서 가만히 있는(Idle) 상태로 만듭니다.
            if (anim != null) { anim.SetBool("IsWalking", false); anim.SetBool("IsFlying", false); }
            return; 
        }

        // 벽이나 낭떠러지를 만났을 때
        if (IsLedgeAhead(xDiff) || IsWallAhead(xDiff))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            if (anim != null) { anim.SetBool("IsWalking", false); anim.SetBool("IsFlying", false); }
            return; 
        }
        
        LookAt(targetX);
        
        // ★ [핵심 고침] 기존에 무조건 걷기를 강제하던 코드를 지웠습니다!
        // (이제 Update 함수에서 명령한 날기/걷기 상태를 그대로 유지하면서 이동합니다)
        rb.linearVelocity = new Vector2(Mathf.Sign(xDiff) * speed, rb.linearVelocity.y);
    }

    void Patrol()
    {
        if (isKnockedBack || isGrabbed) return;
        
        // 순찰 중일 때는 확실하게 걷기 모션
        if (anim != null) 
        { 
            anim.SetBool("IsWalking", true); 
            anim.SetBool("IsFlying", false); 
        }
        
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
        
        LookAt(transform.position.x + patrolDir); 

        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);
    }
}