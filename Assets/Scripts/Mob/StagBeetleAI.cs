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
        
        if (Mathf.Abs(xDiff) <= 0.1f) { rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); return; }

        if (anim != null) anim.SetBool("IsWalking", true);
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