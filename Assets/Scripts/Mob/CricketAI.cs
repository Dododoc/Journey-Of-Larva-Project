using UnityEngine;
using System.Collections;

public class CricketAI : BaseEnemyAI
{
    [Header("1. 점프 힘 설정")]
    public float hopForce = 12f;       // 위로 뛰는 높이
    public float moveForce = 6f;       // 옆으로 뛰는 거리

    [Header("2. 점프 타이밍 설정 (깡충깡충 느낌 조절)")]
    public float jumpIntervalMin = 0.2f; // 착지 후 최소 대기 시간
    public float jumpIntervalMax = 0.5f; // 착지 후 최대 대기 시간
    // Tip: Min과 Max를 둘 다 0.1로 하면 미친듯이 연속 점프를 합니다.
    
    [Header("3. 애니메이션 싱크")]
    public float jumpPreDelay = 0.4f; // 점프 모션(움찔) 후 실제 튀어오르는 시간

    [Header("4. 활동 반경")]
    public float patrolRadius = 6f;

    private float waitTimer;
    private bool isGrounded;
    private bool isJumpingSequence = false; 

    protected override void Start()
    {
        base.Start();
        // 시작할 때 랜덤 대기 시간 설정
        waitTimer = Random.Range(jumpIntervalMin, jumpIntervalMax);
    }

    protected override void Update()
    {
        // 넉백이나 잡힘 상태면 로직 중단
        if (isKnockedBack || isGrabbed) 
        {
            isJumpingSequence = false;
            return;
        }

        // 바닥 체크 (Y축 속도가 거의 0일 때)
        isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.1f;

        // 애니메이션: 바닥에 닿아있고 점프 준비 중이 아닐 때만 땅에 있는 것으로 처리
        if (anim != null && !isJumpingSequence) 
        {
            anim.SetBool("IsGrounded", isGrounded); 
        }

        // 땅에 있고, 점프 시퀀스가 진행 중이 아닐 때만 타이머가 돌아감
        if (isGrounded && !isJumpingSequence)
        {
            // 착지 후 미끄러짐 방지 (마찰력 효과)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 10f);

            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0)
            {
                StartCoroutine(JumpRoutine());
            }
        }
    }

    IEnumerator JumpRoutine()
    {
        isJumpingSequence = true;

        // 1. 점프 방향 결정 (시작 지점 기준 범위 체크)
        float dirX = 0;
        float distFromStart = transform.position.x - startPos.x;
        
        // 범위를 벗어났으면 복귀 방향, 아니면 랜덤 방향
        if (Mathf.Abs(distFromStart) > patrolRadius)
            dirX = (distFromStart > 0) ? -1f : 1f;
        else
            dirX = Random.Range(0, 2) == 0 ? -1f : 1f;
        
        LookAt(transform.position.x + dirX);

        // 2. 점프 준비 애니메이션 (움찔!)
        if (anim != null) 
        {
            anim.SetTrigger("DoJump"); 
            anim.SetBool("IsGrounded", false);
        }

        // 3. 점프 선 딜레이 (애니메이션이 힘을 모으는 시간)
        yield return new WaitForSeconds(jumpPreDelay);

        // 딜레이 중에 넉백 당했으면 취소
        if (isKnockedBack || isGrabbed) 
        {
            isJumpingSequence = false;
            yield break;
        }

        // 4. 실제 도약 (물리 힘 적용)
        rb.linearVelocity = Vector2.zero; // 기존 속도 리셋
        Vector2 jumpVec = new Vector2(dirX * moveForce, hopForce);
        rb.AddForce(jumpVec, ForceMode2D.Impulse);

        // 5. 공중에 뜨자마자 isGrounded가 true 되는 것 방지
        yield return new WaitForSeconds(0.1f);

        // 6. 다음 점프를 위한 타이머 재설정 (여기서 간격을 조절!)
        waitTimer = Random.Range(jumpIntervalMin, jumpIntervalMax);
        isJumpingSequence = false;
    }
}