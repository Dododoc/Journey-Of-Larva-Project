using UnityEngine;

public class CricketAI : BaseEnemyAI
{
    [Header("Patrol Range")]
    public float patrolRadius = 5f;
    private Vector2 targetPos;

    protected override void Start()
    {
        base.Start(); // startPos = transform.position 실행됨
        GetNewTarget();
    }

    protected override void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectRange)
        {
            // ★ 추격 로직: 물리 기반(linearVelocity)으로 X축만 이동
            float xDir = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(xDir * chaseSpeed, rb.linearVelocity.y);
            
            LookAt(player.position.x); //
            if(anim != null) anim.SetBool("IsWalking", true);
        }
        else
        {
            // 정찰 로직
            float xDirToTarget = Mathf.Sign(targetPos.x - transform.position.x);
            rb.linearVelocity = new Vector2(xDirToTarget * moveSpeed, rb.linearVelocity.y);

            if (Vector2.Distance(new Vector2(transform.position.x, 0), new Vector2(targetPos.x, 0)) < 0.5f) 
                GetNewTarget(); //
            
            LookAt(targetPos.x); //
            if(anim != null) anim.SetBool("IsWalking", true);
        }
    }

    void GetNewTarget()
    {
        float randomX = Random.Range(-patrolRadius, patrolRadius);
        targetPos = startPos + new Vector2(randomX, 0); //
    }
}