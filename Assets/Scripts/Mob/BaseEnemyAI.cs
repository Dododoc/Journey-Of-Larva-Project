using UnityEngine;
using System.Collections;

public class BaseEnemyAI : MonoBehaviour
{
    [Header("Base Settings")]
    public float detectRange = 8f;
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float attackRange = 1.5f;

    [Header("Patrol Settings")]
    public float patrolDistance = 5f; 
    protected Vector2 startPos;
    protected int patrolDir = 1;

    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Animator anim;
    protected EnemyStats stats; 
    protected bool isFacingRight = true;
    protected bool isAttacking = false;
    
    protected bool isKnockedBack = false;
    protected bool isGrabbed = false; 

    [Header("Ledge Detection")]
    public LayerMask groundLayer;         // 바닥으로 인식할 레이어
    public float ledgeCheckLength = 1.0f; // 낭떠러지 감지 레이저의 길이
    protected Collider2D myCollider;      // 몹의 콜라이더
    // 던져졌을 때 호출될 함수
    public virtual void OnThrown(Vector2 force)
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            // rb.gravityScale = 1f; // (이전 단계에서 지운 부분 유지)
            rb.AddForce(force, ForceMode2D.Impulse);
            
            // ★ 공중에 있는 동안 AI를 멈추게 하는 코루틴 실행
            StartCoroutine(AirborneStunRoutine());
        }
    }

    // ★ 새로 추가된 코루틴: 공중 체공 및 착지 감지
    protected IEnumerator AirborneStunRoutine()
    {
        // 기존 넉백 타이머를 취소하고, 던져짐 전용 기절 상태로 돌입
        StopCoroutine("KnockbackRoutine"); 
        isKnockedBack = true; // AI 이동 정지
        isAttacking = false;

        // 물리적인 힘이 적용될 때까지 딱 한 프레임 대기
        yield return new WaitForFixedUpdate();

        // 1. 위로 솟구치는 중이라면, 최고점을 지날 때까지 기다림
        while (rb != null && rb.linearVelocity.y > 0.1f)
        {
            yield return null;
        }

        // 2. 아래로 떨어지는 중이라면, 바닥에 닿아 멈출 때까지 기다림
        while (rb != null && rb.linearVelocity.y < -0.1f)
        {
            yield return null;
        }

        // 3. 땅에 쾅! 착지한 후 비틀거리는(스턴) 시간 0.3초 부여
        yield return new WaitForSeconds(0.3f);

        // 다시 AI 정상화 (플레이어 추적 재시작)
        isKnockedBack = false; 
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();

        // ★ 추가: 내 콜라이더 가져오기
        myCollider = GetComponent<Collider2D>();
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        startPos = transform.position; 
    }

    // ★ [에러 해결] virtual 키워드를 추가하여 자식에서 override 가능하게 함
    protected virtual void Update() { }

    public void SetGrabbed(bool grabbed)
    {
        isGrabbed = grabbed;
        if (grabbed)
        {
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
            if (anim != null) anim.SetBool("IsWalking", false);
        }
        else
        {
            if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    // ★ [버그 해결] 정지 시간이 짧아지는 문제: 기존 넉백 루틴을 중단하고 새로 시작
    public virtual void ApplyKnockback(Vector2 force, float duration = 0.4f)
    {
        StopCoroutine("KnockbackRoutine"); 
        StartCoroutine(KnockbackRoutine(force, duration));
    }

    IEnumerator KnockbackRoutine(Vector2 force, float duration)
    {
        isKnockedBack = true;
        isAttacking = false; 
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
            rb.AddForce(force, ForceMode2D.Impulse); 
        }
        yield return new WaitForSeconds(duration);
        isKnockedBack = false;
    }

    protected void LookAt(float targetX)
    {
        if (isKnockedBack || isGrabbed) return;
        float xDir = targetX - transform.position.x;
        if (xDir > 0 && !isFacingRight) Flip();
        else if (xDir < 0 && isFacingRight) Flip();
    }

    protected virtual void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.eulerAngles = isFacingRight ? new Vector3(0, 0, 0) : new Vector3(0, 180, 0);
        if (stats != null && stats.hpCanvas != null) stats.hpCanvas.transform.rotation = Quaternion.identity;
    }
    // ★ 낭떠러지 감지 센서 (이동하려는 방향의 X값을 넣으면 검사해 줍니다)
    // ★ 낭떠러지 감지 센서 (오작동 완벽 해결 버전)
    protected bool IsLedgeAhead(float moveDirX)
    {
        // 콜라이더가 없거나 넉백 상태면 낭떠러지 무시
        if (myCollider == null || isKnockedBack) return false;

        float dirSign = Mathf.Sign(moveDirX);
        if (dirSign == 0) return false;

        // ★ [핵심 1] 몸통 맨 앞쪽 끝이 아니라, '안쪽으로 아주 살짝(0.1f)' 들어온 위치에서 쏩니다.
        // (경사면이나 타일 가장자리에 살짝만 걸쳐도 낭떠러지로 오해하는 현상 방지)
        float checkX = myCollider.bounds.center.x + (dirSign * (myCollider.bounds.extents.x - 0.1f));
        
        // 발바닥(min.y)에서 위로 0.1f 올린 지점
        float checkY = myCollider.bounds.min.y + 0.1f; 
        Vector2 checkPos = new Vector2(checkX, checkY);

        // ★ [핵심 2] 레이저 길이를 '0.8f'로 넉넉하게 늘려서 땅에 확실히 닿게 만듭니다.
        float rayLength = 0.8f;
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, rayLength, groundLayer);

        // ★ 씬(Scene) 뷰에서 눈으로 확인하기 위한 레이저 그리기
        // 초록색 선 = "땅 감지 완료!" / 빨간색 선 = "땅 없음! 낭떠러지다!"
        Debug.DrawRay(checkPos, Vector2.down * rayLength, hit.collider == null ? Color.red : Color.green);

        // 부딪힌 땅이 없으면(null) 낭떠러지이므로 true 반환
        return hit.collider == null; 
    }
    // ★ 벽 감지 센서 (진행 방향 앞쪽에 벽이 있는지 확인)
    // ★ 벽 감지 센서 (수정됨: 바닥 긁힘 완벽 방지)
    protected bool IsWallAhead(float moveDirX)
    {
        if (myCollider == null || isKnockedBack) return false;

        float dirSign = Mathf.Sign(moveDirX);
        if (dirSign == 0) return false; // 방향이 없으면 검사 안 함

        // ★ [핵심 수정 1] 발밑이 아니라 몹의 '정확한 허리(중간) 높이'에서 레이저를 쏩니다.
        float centerY = myCollider.bounds.min.y + (myCollider.bounds.size.y / 2f);
        Vector2 checkPos = new Vector2(myCollider.bounds.center.x, centerY);
        
        // ★ [핵심 수정 2] 몸통 바깥으로 아주 살짝(0.1f)만 튀어나가게 거리를 확 줄입니다.
        float checkDist = myCollider.bounds.extents.x + 0.1f;

        // 수평으로만 정확하게 레이저 발사
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.right * dirSign, checkDist, groundLayer);

        return hit.collider != null; 
    }
}