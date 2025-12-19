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
    // 던져졌을 때 호출될 함수 (자식에서 오버라이드 가능)
    public virtual void OnThrown(Vector2 force)
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f; // 기본적으로 중력 켜기
            rb.AddForce(force, ForceMode2D.Impulse);
        }
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