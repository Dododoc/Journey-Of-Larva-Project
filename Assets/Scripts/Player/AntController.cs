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
    
    // 바닥 체크 변수 (애벌레 방식)
    public Vector2 boxSize = new Vector2(0.8f, 0.2f); 
    public float castDistance = 0.3f; // ★ 0.3 ~ 0.4 정도로 넉넉하게 주셔도 됩니다 (이제 공중부양 안 함)
    public LayerMask groundLayer;      

    // 상태 변수
    private bool isStrongAttacking = false;
    private bool isUnderground = false;
    private bool isBasicAttacking = false;

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

        if (isUnderground)
        {
            HandleUndergroundMove();
            return; 
        }

        CheckGround();

        if (isStrongAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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
        
        transform.rotation = Quaternion.identity;
    }

    void ProcessInput()
    {
        if (Input.GetKeyDown(KeyCode.Z) && !isBasicAttacking) StartCoroutine(BasicAttackRoutine());
        if (Input.GetKeyDown(KeyCode.X) && canStrongAttack && isGrounded) { StartCoroutine(StrongAttackRoutine()); return; }
        if (Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(KeyCode.C) && canDig && isGrounded) { StartCoroutine(DigRoutine()); return; }

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

        // ★ [여기가 문제 해결의 핵심입니다] ★
        // "땅 감지됨(isGrounded)" 상태라도, "떨어지는 속도가 빠르면(isFalling)" 아직 공중으로 취급합니다.
        // -3f보다 더 빠르게 떨어지고 있으면 낙하 중이라고 판단
        bool isFalling = rb.linearVelocity.y < -3f; 

        if (isGrounded && moveInput != 0 && !isFalling)
        {
            // [진짜 땅에 착지해서 걷는 중]
            // 떨어지는 속도가 줄어들었을 때만 이 로직이 실행됩니다.
            rb.gravityScale = defaultGravity;
            
            Vector2 slopeDir = Vector2.Perpendicular(surfaceNormal).normalized;
            Vector2 moveDir = slopeDir * -moveInput;
            rb.linearVelocity = moveDir * moveSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - 5f);
        }
        else if (!isGrounded || isFalling) // ★ 공중이거나, 센서는 닿았지만 아직 떨어지는 중일 때
        {
            // [공중 물리 적용]
            // 경사면 계산이나 강제 멈춤 없이, 물리 엔진(중력)에 몸을 맡깁니다.
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            // [땅에서 멈춤 - Idle]
            rb.gravityScale = defaultGravity;
            // 여기도 혹시 모르니 Y축 속도는 건드리지 않고 X축만 멈춥니다.
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 
        }

        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;
    }

    void UpdateAnimation()
    {
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        // 애니메이션은 센서값(isGrounded)을 그대로 써서, 땅에 닿기 직전에 미리 모션을 취하게 합니다.
        anim.SetBool("IsGrounded", isGrounded); 
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }

    // ... (아래 공격, 스킬 코루틴들은 기존과 동일) ...
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
        sr.color = Color.red; 
        transform.localScale = originalScale * 1.3f; 
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
        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;
    }

    IEnumerator DigRoutine()
    {
        canDig = false;
        anim.SetTrigger("DoDig");
        rb.gravityScale = 0f;        
        rb.linearVelocity = Vector2.zero;
        myCollider.enabled = false; 
        yield return new WaitForSeconds(0.5f); 
        isUnderground = true;
        Color oldColor = sr.color;
        sr.color = new Color(oldColor.r, oldColor.g, oldColor.b, 0.5f); 
        transform.position += Vector3.down * 0.5f;
        float timer = 0f;
        while (timer < digDuration) {
            timer += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.C)) break; 
            yield return null; 
        }
        isUnderground = false;
        transform.position += Vector3.up * 0.5f;
        sr.color = oldColor;
        sr.enabled = true;
        anim.SetTrigger("DoEmerge"); 
        EmergeAttack();
        yield return new WaitForSeconds(0.5f); 
        rb.gravityScale = defaultGravity;
        myCollider.enabled = true;
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