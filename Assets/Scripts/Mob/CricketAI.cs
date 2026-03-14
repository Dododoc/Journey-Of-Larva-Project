using UnityEngine;
using System.Collections;

public class CricketAI : BaseEnemyAI
{
    [Header("1. 점프 힘 설정")]
    public float hopForce = 12f;       
    public float moveForce = 6f;       

    [Header("2. 점프 타이밍 설정 (깡충깡충 느낌 조절)")]
    public float jumpIntervalMin = 0.2f; 
    public float jumpIntervalMax = 0.5f; 
    
    [Header("3. 애니메이션 싱크")]
    public float jumpPreDelay = 0.4f; 

    [Header("4. 활동 반경")]
    public float patrolRadius = 6f;

    private float waitTimer;
    private bool isGrounded;
    private bool isJumpingSequence = false; 

    protected override void Start()
    {
        base.Start();
        waitTimer = Random.Range(jumpIntervalMin, jumpIntervalMax);
    }

    protected override void Update()
    {
        if (isKnockedBack || isGrabbed) 
        {
            isJumpingSequence = false;
            return;
        }

        isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.1f;

        if (anim != null && !isJumpingSequence) 
        {
            anim.SetBool("IsGrounded", isGrounded); 
        }

        if (isGrounded && !isJumpingSequence)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 10f);
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0)
            {
                StartCoroutine(JumpRoutine());
            }
        }
    }

    // =========================================================
    // ★ [새로 추가] 귀뚜라미 전용 '장거리' 안전 검사 레이저
    // =========================================================
    bool IsSafeToJump(float dirX)
    {
        if (myCollider == null) return false;

        // 1. 벽 감지 (앞으로 약 2.5 거리까지 벽이 있는지 확인)
        float wallCheckDist = 2.5f; 
        float centerY = myCollider.bounds.min.y + (myCollider.bounds.size.y / 2f);
        Vector2 centerPos = new Vector2(myCollider.bounds.center.x, centerY);
        
        RaycastHit2D wallHit = Physics2D.Raycast(centerPos, Vector2.right * dirX, wallCheckDist, groundLayer);
        Debug.DrawRay(centerPos, Vector2.right * dirX * wallCheckDist, wallHit.collider ? Color.red : Color.magenta, 1f);
        
        if (wallHit.collider != null) return false; // 벽이 있으면 위험!

        // 2. 낭떠러지 감지 (착지 예상 지점 아래에 땅이 있는지 확인)
        float jumpDistance = 2.5f; // 귀뚜라미가 뛰는 X 거리 예측값
        float landingX = myCollider.bounds.center.x + (dirX * jumpDistance);
        Vector2 landingPos = new Vector2(landingX, myCollider.bounds.min.y + 0.1f);
        
        RaycastHit2D groundHit = Physics2D.Raycast(landingPos, Vector2.down, 2.0f, groundLayer);
        Debug.DrawRay(landingPos, Vector2.down * 2.0f, groundHit.collider ? Color.cyan : Color.red, 1f);

        if (groundHit.collider == null) return false; // 착지할 땅이 없으면 위험!

        return true; // 벽도 없고 낭떠러지도 없으면 점프 승인!
    }

    IEnumerator JumpRoutine()
    {
        isJumpingSequence = true;

        float dirX = 0;
        float distFromStart = transform.position.x - startPos.x;
        
        if (Mathf.Abs(distFromStart) > patrolRadius)
            dirX = (distFromStart > 0) ? -1f : 1f;
        else
            dirX = Random.Range(0, 2) == 0 ? -1f : 1f;
            
        // ===================================================
        // ★ [수정] 기존 코앞 센서 대신, 귀뚜라미 전용 장거리 센서 사용!
        if (!IsSafeToJump(dirX))
        {
            dirX *= -1f; // 위험하면 반대로 뜀

            // 만약 반대쪽도 벽이거나 낭떠러지라면? (양쪽 다 막힘)
            if (!IsSafeToJump(dirX))
            {
                dirX = 0f; // 제자리에서 위로만 뜀!
            }
        }
        // ===================================================
        
        // dirX가 0이면 원래 보던 방향을 유지함
        if (dirX != 0f) LookAt(transform.position.x + dirX);

        if (anim != null) 
        {
            anim.SetTrigger("DoJump"); 
            anim.SetBool("IsGrounded", false);
        }

        yield return new WaitForSeconds(jumpPreDelay);

        if (isKnockedBack || isGrabbed) 
        {
            isJumpingSequence = false;
            yield break;
        }

        rb.linearVelocity = Vector2.zero; 
        Vector2 jumpVec = new Vector2(dirX * moveForce, hopForce);
        rb.AddForce(jumpVec, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.1f);

        waitTimer = Random.Range(jumpIntervalMin, jumpIntervalMax);
        isJumpingSequence = false;
    }
}