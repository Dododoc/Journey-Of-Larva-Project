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
}