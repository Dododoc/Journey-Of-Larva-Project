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
        
        // 미세 떨림 방지용으로 아주아주 가까울 때만 멈춤
        if (Mathf.Abs(xDiff) <= 0.1f) { rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); return; }
        rb.linearVelocity = new Vector2(Mathf.Sign(xDiff) * speed, rb.linearVelocity.y);
    }

    void Patrol()
    {
        if (isKnockedBack || isGrabbed) return;
        if (anim != null) anim.SetBool("IsWalking", true);
        float currentDist = transform.position.x - startPos.x;
        if (currentDist >= patrolDistance && patrolDir > 0) { patrolDir = -1; LookAt(transform.position.x + patrolDir); }
        else if (currentDist <= -patrolDistance && patrolDir < 0) { patrolDir = 1; LookAt(transform.position.x + patrolDir); }
        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);
    }
}