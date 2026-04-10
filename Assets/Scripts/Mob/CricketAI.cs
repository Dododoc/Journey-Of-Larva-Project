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
            
        // 장거리 센서 확인
        if (!IsSafeToJump(dirX))
        {
            dirX *= -1f; 
            if (!IsSafeToJump(dirX)) dirX = 0f; 
        }
        
        if (dirX != 0f) LookAt(transform.position.x + dirX);

        // ===================================================
        // ★ 1. 점프 준비 애니메이션 즉시 시작 (다리 펴기)
        // ===================================================
        if (anim != null) 
        {
            anim.SetTrigger("DoJump"); 
        }

        // ===================================================
        // ★ 2. 다리를 펴는 시간(0.25초) 동안 꾹 참고 대기!
        // ===================================================
        yield return new WaitForSeconds(0.25f);

        // 대기하는 동안 맞거나 잡혔으면 점프 취소
        if (isKnockedBack || isGrabbed) 
        {
            isJumpingSequence = false;
            yield break;
        }

        // ===================================================
        // ★ 3. 땅을 박차고 실제 점프! (1.125초 체공)
        // ===================================================
        if (anim != null) anim.SetBool("IsGrounded", false);

        float airTime = 1.5f * 0.75f;     // 1.125초 (공중에 떠 있는 시간)
        float landingTime = 1.5f * 0.25f; // 0.375초 (착지 후 웅크린 시간)

        // 현재 중력을 바탕으로 정확히 1.125초 동안 체공할 수 있는 Y축 속도 역산
        float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        float jumpVelocityY = (airTime * g) / 2f;
        float jumpVelocityX = dirX * moveForce;

        rb.linearVelocity = new Vector2(jumpVelocityX, jumpVelocityY);

        // 정확히 체공 시간(1.125초)만큼만 대기
        yield return new WaitForSeconds(airTime);

        if (isKnockedBack || isGrabbed) 
        {
            isJumpingSequence = false;
            yield break;
        }

        // ===================================================
        // ★ 4. 1.125초 후 정확히 땅에 닿음 -> 착지 연출(0.375초)
        // ===================================================
        rb.linearVelocity = Vector2.zero; 
        if (anim != null) anim.SetBool("IsGrounded", true);

        // 나머지 착지 애니메이션이 재생될 시간 대기
        yield return new WaitForSeconds(landingTime);

        waitTimer = Random.Range(jumpIntervalMin, jumpIntervalMax);
        isJumpingSequence = false;
    }
}