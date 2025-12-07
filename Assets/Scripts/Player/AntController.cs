using UnityEngine;
using System.Collections;

public class AntController : MonoBehaviour
{
    [Header("1. 움직임 설정")]
    public float moveSpeed = 6f;
    public float jumpForce = 13f;

    [Header("2. 평타 설정 (Z - 찝기)")]
    public float attackDamage = 15f;
    public float attackRange = 1.5f;
    public float attackDelay = 0.1f;
    public float attackCooldown = 0.3f;
    
    [Header("3. 강공격 설정 (X - 깨불어부수기)")]
    public float strongDamageMultiplier = 2.5f;
    public float strongAttackDelay = 0.4f;
    public float strongCooldown = 3.0f;
    private bool canStrongAttack = true;

    [Header("4. 스킬 설정 (땅파기 - Down + C)")]
    public float digDuration = 3f;
    public float digCooldown = 8f;
    public float digSpeed = 4f;
    public float emergeDamage = 20f;
    public float emergeKnockback = 10f;
    public float emergeRadius = 2.5f;
    private bool canDig = true;

    [Header("5. 체크 및 레이어")]
    public Transform attackPoint;       
    public LayerMask enemyLayers;       
    
    // 바닥 체크
    public Vector2 boxSize = new Vector2(0.8f, 0.2f); 
    public float castDistance = 0.3f; 
    public LayerMask groundLayer;      

    // 상태 변수
    private bool isStrongAttacking = false;
    private bool isUnderground = false; // 땅속 상태
    private bool isBasicAttacking = false;
    private bool isDiggingAnim = false; // 파고 들거나 나오는 애니메이션 중

    // 컴포넌트
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D myCollider;
    private PlayerStats myStats;

    private bool isGrounded;
    private float defaultGravity;
    private float jumpCooldown; 
    private Vector2 surfaceNormal;

    private bool isFacingRight = true;

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
        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;

        // --- [우선순위 로직 정리] ---
        
        // 1. 구멍 파는 애니메이션 중 (진입/탈출) -> 꼼짝 마!
        if (isDiggingAnim)
        {
            rb.linearVelocity = Vector2.zero; // 물리력 완전 차단
            UpdateAnimation(); // 땅속 상태 갱신을 위해 호출
            return; // 다른 입력 무시
        }
        
        // 2. 땅속 이동 중 -> 좌우 이동만 가능
        if (isUnderground)
        {
            HandleUndergroundMove();
            UpdateAnimation();
            return;
        }
        
        // 3. 강공격 중 -> 멈춤
        if (isStrongAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 4. 평상시 -> 바닥 체크 및 이동
        CheckGround();
        ProcessInput();
        UpdateAnimation();
    }

    void CheckGround()
    {
        if (jumpCooldown > 0)
        {
            isGrounded = false;
            surfaceNormal = Vector2.up;
            return;
        }

        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, castDistance + 0.3f, groundLayer);

        isGrounded = hit.collider != null;

        if (isGrounded) surfaceNormal = hit.normal;
        else surfaceNormal = Vector2.up;
    }

    void ProcessInput()
    {
        // [Z] 평타
        if (Input.GetKeyDown(KeyCode.Z) && !isBasicAttacking && isGrounded) 
        {
            StartCoroutine(BasicAttackRoutine());
        }

        // [X] 강공격
        if (Input.GetKeyDown(KeyCode.X) && canStrongAttack && isGrounded) 
        { 
            StartCoroutine(StrongAttackRoutine()); 
            return; 
        }

        // [Down+C] 땅파기
        if (Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(KeyCode.C) && canDig && isGrounded) 
        { 
            StartCoroutine(DigRoutine()); 
            return; 
        }

        float moveInput = Input.GetAxisRaw("Horizontal");

        // [점프]
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpCooldown = 0.2f;
            isGrounded = false;
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("DoJump");
            return;
        }

        // [이동 및 낙하 처리]
        bool isFalling = rb.linearVelocity.y < -3f; 

        if (isGrounded && moveInput != 0 && !isFalling)
        {
            // 땅에서 걷기
            rb.gravityScale = defaultGravity;
            Vector2 slopeDir = Vector2.Perpendicular(surfaceNormal).normalized;
            Vector2 moveDir = slopeDir * -moveInput;
            rb.linearVelocity = moveDir * moveSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - 5f);
        }
        else if (!isGrounded || isFalling)
        {
            // 공중
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            // 아이들 (Idle)
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 
        }

        // 방향 전환
        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1; 
        transform.localScale = scaler;
    }

    void UpdateAnimation()
    {
        // 땅속이거나 파는 중이면, 무조건 땅에 있는 것으로 처리 (Fly 방지)
        if (isUnderground || isDiggingAnim)
        {
            anim.SetBool("IsGrounded", true);
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("VerticalSpeed", 0f);
            return;
        }

        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }

    // --- 공격 코루틴 ---
    IEnumerator BasicAttackRoutine()
    {
        isBasicAttacking = true;
        anim.SetTrigger("DoAttack"); 
        yield return new WaitForSeconds(attackDelay);
        ApplyDamage(attackPoint.position, attackRange, 1f); 
        yield return new WaitForSeconds(attackCooldown);
        isBasicAttacking = false;
    }

    IEnumerator StrongAttackRoutine()
    {
        canStrongAttack = false;
        isStrongAttacking = true;
        
        Color originalColor = sr.color;
        Vector3 originalScale = transform.localScale;

        transform.localScale = new Vector3(originalScale.x * 1.1f, originalScale.y * 1.1f, originalScale.z);

        anim.SetTrigger("DoStrongAttack"); 
        yield return new WaitForSeconds(strongAttackDelay);
        
        ApplyDamage(attackPoint.position, attackRange * 1.5f, strongDamageMultiplier);
        
        sr.color = originalColor;
        transform.localScale = originalScale; 

        isStrongAttacking = false;
        yield return new WaitForSeconds(strongCooldown);
        canStrongAttack = true;
    }

    void ApplyDamage(Vector2 point, float range, float multiplier)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point, range, enemyLayers);
        float baseDmg = (myStats != null) ? myStats.TotalAttack : attackDamage;
        float finalDmg = baseDmg * multiplier;
        foreach (Collider2D enemy in hitEnemies) {
            EnemyStats es = enemy.GetComponent<EnemyStats>();
            if (es != null) es.TakeDamage(finalDmg);
        }
    }

    void HandleUndergroundMove()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * digSpeed, 0f);
        
        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    // --- 구멍 파기 코루틴 (수정 완료) ---
    IEnumerator DigRoutine()
    {
        canDig = false;
        
        // 1. 파고 들기 시작
        isDiggingAnim = true; 
        anim.SetTrigger("DoDig");
        rb.gravityScale = 0f;        
        rb.linearVelocity = Vector2.zero;
        myCollider.enabled = false; 
        
        yield return new WaitForSeconds(0.5f); 

        // 2. 땅속 진입 완료
        isDiggingAnim = false;
        isUnderground = true;
        
        // ★ [수정됨] 투명도 변경 코드 삭제 (원래 색 유지)
        // Color oldColor = sr.color; 
        // sr.color = new Color(oldColor.r, oldColor.g, oldColor.b, 0.5f); <-- 삭제함
        
        transform.position += Vector3.down * 0.5f;

        // 3. 땅속 대기
        float timer = 0f;
        while (timer < digDuration) {
            timer += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.C)) break; 
            yield return null; 
        }

        // 4. 나오기 시작
        isUnderground = false; 
        isDiggingAnim = true; 
        
        // ★ [수정됨] 0.8f -> 0.6f로 변경 (나오는 높이 조절)
     
        
        // sr.color = oldColor; <-- 투명도 안 바꿨으니 복구도 필요 없음
        sr.enabled = true;
        anim.SetTrigger("DoEmerge"); 
        EmergeAttack();
        
        yield return new WaitForSeconds(0.5f); 

        // 5. 복귀 완료
        rb.gravityScale = defaultGravity; 
        myCollider.enabled = true;        
        isDiggingAnim = false;            

        yield return new WaitForSeconds(digCooldown);
        canDig = true;
    }

    void EmergeAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, emergeRadius, enemyLayers);
        foreach (Collider2D enemy in hitEnemies) {
            EnemyStats es = enemy.GetComponent<EnemyStats>();
            if (es != null) es.TakeDamage(emergeDamage);
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null) {
                Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                knockbackDir += Vector2.up * 0.5f; 
                enemyRb.AddForce(knockbackDir.normalized * emergeKnockback, ForceMode2D.Impulse);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (isGrounded) Gizmos.color = Color.green;
        else Gizmos.color = Color.red;
        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize);
        if (attackPoint != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}