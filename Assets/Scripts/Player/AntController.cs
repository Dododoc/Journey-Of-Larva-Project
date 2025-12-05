using UnityEngine;
using System.Collections;

public class AntController : MonoBehaviour
{
    [Header("1. 움직임 설정")]
    public float moveSpeed = 6f; 
    public float jumpForce = 13f;

    [Header("2. 공격 설정 (찝기)")]
    public float attackDamage = 15f;    // 기본 공격력
    public float attackRange = 1.5f;    // 공격 사거리
    public float attackDelay = 0.1f;    // 데미지 들어가는 타이밍
    public float attackCooldown = 0.5f; // 공격 속도
    public Transform attackPoint;       // ★ 공격 위치 (집게 앞)
    public LayerMask enemyLayers;       // 적 레이어

    [Header("3. 스킬 설정 (땅파기)")]
    public float digDuration = 2f;      // 땅속 지속 시간
    public float digCooldown = 5f;      // 스킬 쿨타임
    
    // 상태 변수들
    private bool isAttacking = false;
    private bool isDigging = false; // 땅속인가?
    private bool canDig = true;     // 쿨타임 찼나?

    [Header("4. 바닥 체크")]
    public Vector2 boxSize = new Vector2(0.8f, 0.2f);
    public float castDistance = 0.2f;
    public LayerMask groundLayer;

    // 컴포넌트들
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D myCollider; // 충돌체 (숨을 때 끄려고)
    private PlayerStats myStats;   // 스탯 가져오기용

    private bool isGrounded;
    private float defaultGravity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        myStats = GetComponent<PlayerStats>();
        defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        // 땅속에 있으면 움직임/공격 불가
        if (isDigging) return;

        CheckGround();
        
        // 공격 중엔 이동 멈춤
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        ProcessInput();
        UpdateAnimation();
    }

    void CheckGround()
    {
        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, castDistance + 0.3f, groundLayer);
        isGrounded = hit.collider != null;
    }

    void ProcessInput()
    {
        // 1. 공격 (Z키)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            StartCoroutine(AttackRoutine());
            return;
        }

        // 2. 구멍 파기 (아래화살표 + C키)
        if (Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(KeyCode.C) && canDig && isGrounded)
        {
            StartCoroutine(DigRoutine());
            return;
        }

        // 3. 이동 및 점프
        float moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("DoJump");
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        // 방향 뒤집기
        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;
    }

    void UpdateAnimation()
    {
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("IsGrounded", isGrounded);
    }

    // --- 공격 코루틴 ---
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        anim.SetTrigger("DoAttack"); // 애니메이션 실행

        yield return new WaitForSeconds(attackDelay); // 공격 모션 타이밍 맞추기

        // 범위 내 적 감지
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
        // 데미지 계산 (Stats 스크립트가 있으면 거기서 가져오고, 없으면 기본값)
        float finalDamage = (myStats != null) ? myStats.TotalAttack : attackDamage;

        foreach(Collider2D enemy in hitEnemies)
        {
            Debug.Log("개미가 적을 물었습니다!");
            EnemyStats es = enemy.GetComponent<EnemyStats>();
            if(es != null) es.TakeDamage(finalDamage);
        }

        yield return new WaitForSeconds(attackCooldown); // 후딜레이
        isAttacking = false;
    }

    // --- 구멍 파기 코루틴 ---
    IEnumerator DigRoutine()
    {
        canDig = false;
        isDigging = true;

        // 숨기 시작
        anim.SetTrigger("DoDig");
        rb.gravityScale = 0f;       // 중력 끄기
        rb.linearVelocity = Vector2.zero; // 멈춤
        myCollider.enabled = false; // 무적 (충돌 끔)

        yield return new WaitForSeconds(0.5f); // 땅 파는 모션 시간
        sr.enabled = false; // 모습 숨기기 (땅속 들어감)

        yield return new WaitForSeconds(digDuration); // 땅속 대기 시간

        // 나오기 시작
        sr.enabled = true; // 모습 등장
        anim.SetTrigger("DoEmerge");
        
        yield return new WaitForSeconds(0.5f); // 나오는 모션 시간

        // 복귀 완료
        rb.gravityScale = defaultGravity;
        myCollider.enabled = true;
        isDigging = false;

        // 쿨타임 대기
        yield return new WaitForSeconds(digCooldown);
        canDig = true;
    }

    // 공격 범위 그리기 (에디터용)
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}