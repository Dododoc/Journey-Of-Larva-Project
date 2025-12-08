using UnityEngine;
using System.Collections;
using System.Collections.Generic; // 리스트 사용을 위해 추가

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
    public float basicEnemyKnockback = 5f; 
    
    [Header("3. 강공격 설정 (X - 깨불어부수기)")]
    public float strongDamageMultiplier = 2.5f;
    public float strongAttackDelay = 0.25f; 
    public float strongCooldown = 3.0f;
    // ★ [수정] 흡혈 비율 0.2로 감소
    public float lifestealRatio = 0.2f; 
    // ★ [수정] 넉백 힘 7로 감소
    public float strongEnemyKnockback = 7f; 
    private bool canStrongAttack = true;

    [Header("4. 스킬 설정 (땅파기 - Down + C)")]
    public float digDuration = 3f;
    public float digCooldown = 8f;
    public float digSpeed = 4f;
    public float emergeDamage = 20f;
    public float emergeKnockback = 10f; 
    public float emergeRadius = 2.5f;
    
    [Header("4-1. 땅파기 탈출 설정")]
    public float emergeAnimDuration = 0.6f; 
    public float emergeDamageDelay = 0.3f;  
    private bool canDig = true;

    [Header("5. 피격 및 넉백 설정")]
    public float hitKnockbackPower = 5f; 
    public float hitInvincibilityDuration = 1.0f; 
    private bool isInvincible = false;      
    private bool isKnockedBack = false;     

    [Header("6. 체크 및 레이어")]
    public Transform attackPoint;       
    public LayerMask enemyLayers;       
    public Vector2 boxSize = new Vector2(0.8f, 0.2f); 
    public float castDistance = 0.3f; 
    public LayerMask groundLayer;      

    // 상태 변수
    private bool isStrongAttacking = false;
    private bool isUnderground = false; 
    private bool isBasicAttacking = false;
    private bool isDiggingAnim = false; 

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

        if (isKnockedBack) 
        {
            UpdateAnimation();
            return;
        }

        if (isDiggingAnim)
        {
            if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero; 
            UpdateAnimation(); 
            return; 
        }
        
        if (isUnderground)
        {
            HandleUndergroundMove();
            UpdateAnimation();
            return;
        }
        
        if (isStrongAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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
        if (Input.GetKeyDown(KeyCode.Z) && !isBasicAttacking && isGrounded) 
        {
            StartCoroutine(BasicAttackRoutine());
        }

        if (Input.GetKeyDown(KeyCode.X) && canStrongAttack && isGrounded) 
        { 
            StartCoroutine(StrongAttackRoutine()); 
            return; 
        }

        if (Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(KeyCode.C) && canDig && isGrounded) 
        { 
            StartCoroutine(DigRoutine()); 
            return; 
        }

        float moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpCooldown = 0.2f;
            isGrounded = false;
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("DoJump");
            return;
        }

        bool isFalling = rb.linearVelocity.y < -3f; 

        if (isGrounded && moveInput != 0 && !isFalling)
        {
            rb.gravityScale = defaultGravity;
            Vector2 slopeDir = Vector2.Perpendicular(surfaceNormal).normalized;
            Vector2 moveDir = slopeDir * -moveInput;
            rb.linearVelocity = moveDir * moveSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - 5f);
        }
        else if (!isGrounded || isFalling)
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 
        }

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
        if (isUnderground || isDiggingAnim || isKnockedBack)
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

    IEnumerator BasicAttackRoutine()
    {
        isBasicAttacking = true;
        anim.SetTrigger("DoAttack"); 
        yield return new WaitForSeconds(attackDelay);
        ApplyDamage(attackPoint.position, attackRange, 1f, false, basicEnemyKnockback); 
        yield return new WaitForSeconds(attackCooldown);
        isBasicAttacking = false;
    }

    IEnumerator StrongAttackRoutine()
    {
        canStrongAttack = false;
        isStrongAttacking = true;
        Color originalColor = sr.color;
        Vector3 originalScale = transform.localScale;
        sr.color = new Color(1f, 0.8f, 0.8f); 
        transform.localScale = new Vector3(originalScale.x * 1.1f, originalScale.y * 1.1f, originalScale.z);

        anim.SetTrigger("DoStrongAttack"); 
        
        yield return new WaitForSeconds(strongAttackDelay);
        
        ApplyDamage(attackPoint.position, attackRange * 1.5f, strongDamageMultiplier, true, strongEnemyKnockback);
        
        sr.color = originalColor;
        transform.localScale = originalScale; 
        isStrongAttacking = false;
        yield return new WaitForSeconds(strongCooldown);
        canStrongAttack = true;
    }

    // 넉백/공격 시 끼임 방지용
    IEnumerator IgnoreCollisionRoutine(Collider2D enemyCol, float duration = 0.5f)
    {
        if (enemyCol == null || myCollider == null) yield break;

        Physics2D.IgnoreCollision(myCollider, enemyCol, true);
        yield return new WaitForSeconds(duration);
        
        if (enemyCol != null && myCollider != null)
            Physics2D.IgnoreCollision(myCollider, enemyCol, false);
    }

    // ★ [추가] 땅에서 나올 때 주변 모든 적과 충돌 무시 (끼임 해결)
    void PreventStuckOnEmerge()
    {
        // 내 주변(반경 2.5)에 있는 모든 적을 찾음
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, emergeRadius, enemyLayers);
        
        foreach (Collider2D enemy in nearbyEnemies)
        {
            // 그 적들과 2초 동안 물리 충돌을 끈다 (서로 통과됨)
            StartCoroutine(IgnoreCollisionRoutine(enemy, 2.0f));
        }
    }

    void ApplyDamage(Vector2 point, float range, float multiplier, bool isLeech, float knockbackForce)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point, range, enemyLayers);
        float baseDmg = (myStats != null) ? myStats.TotalAttack : attackDamage;
        float finalDmg = baseDmg * multiplier;
        float totalHeal = 0f;

        foreach (Collider2D enemy in hitEnemies) {
            EnemyStats es = enemy.GetComponent<EnemyStats>();
            if (es != null) es.TakeDamage(finalDmg);

            if (isLeech && myStats != null)
                totalHeal += finalDmg * lifestealRatio;

            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                float dirX = (enemy.transform.position.x - transform.position.x) > 0 ? 1f : -1f;
                Vector2 knockbackDir = new Vector2(dirX, 1.5f).normalized;
                
                enemyRb.linearVelocity = Vector2.zero; 
                enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

                StartCoroutine(IgnoreCollisionRoutine(enemy.GetComponent<Collider2D>()));
            }
        }

        if (isLeech && totalHeal > 0 && myStats != null)
            myStats.Heal(totalHeal);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isUnderground || isDiggingAnim || isInvincible) return;

        if (collision.gameObject.CompareTag("Enemy"))
            HandleCollisionDamage(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isUnderground || isDiggingAnim || isInvincible) return;

        if (other.CompareTag("Enemy") || other.CompareTag("Trap")) 
            HandleCollisionDamage(other.gameObject);
    }

    void HandleCollisionDamage(GameObject target)
    {
        EnemyStats enemyStats = target.GetComponent<EnemyStats>();
        float damageToTake = (enemyStats != null) ? enemyStats.attackDamage : 10f; 

        if (myStats != null) myStats.TakeDamage(damageToTake);

        float pushDirX = (transform.position.x < target.transform.position.x) ? -1f : 1f;
        Vector2 knockbackDir = new Vector2(pushDirX, 1.5f).normalized;
        
        ApplyKnockback(knockbackDir * hitKnockbackPower);

        StartCoroutine(IgnoreCollisionRoutine(target.GetComponent<Collider2D>()));
    }

    public void ApplyKnockback(Vector2 force)
    {
        // 1. 진행 중인 모든 공격/스킬 로직 중단
        isBasicAttacking = false;
        isStrongAttacking = false;
        isDiggingAnim = false; // 땅파기 모션도 취소
        canDig = true;         // 스킬 쿨타임 등의 꼬임 방지
        canStrongAttack = true;

        StopAllCoroutines(); 

        // 2. ★ [핵심] 공격 애니메이션 예약된 것들 모두 취소
        anim.ResetTrigger("DoAttack");
        anim.ResetTrigger("DoStrongAttack");
        anim.ResetTrigger("DoDig");
        anim.ResetTrigger("DoEmerge");

        // 3. ★ [핵심] 강제로 피격 모션(또는 점프 모션)으로 전환
        // "Hit"라는 애니메이션이 있다면 anim.SetTrigger("DoHit"); 을 쓰겠지만,
        // 지금은 없으므로 강제로 '점프(공중)' 상태로 보내서 공격 모션을 끊어버립니다.
        anim.Play("Ant_Fly", 0, 0f); // "Ant_Jump"는 점프 애니메이션 이름입니다. (확인 필요)
        // 만약 점프 애니메이션 이름이 다르다면 그 이름을 넣거나, 
        // 그냥 아래처럼 IsGrounded를 끄는 것만으로도 Animator 설정에 따라 바뀔 수 있습니다.

        // 4. 시각적 효과 (깜빡임, 방향)
        sr.color = Color.white;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * (isFacingRight ? 1 : -1), Mathf.Abs(transform.localScale.y), transform.localScale.z);

        // 5. 물리적 넉백 적용
        isKnockedBack = true;
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(force, ForceMode2D.Impulse);

        StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        isInvincible = true; 
        yield return new WaitForSeconds(0.3f);
        isKnockedBack = false; 

        float blinkEndTime = Time.time + (hitInvincibilityDuration - 0.3f);
        while (Time.time < blinkEndTime)
        {
            sr.color = new Color(1, 1, 1, 0.4f); 
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;              
            yield return new WaitForSeconds(0.1f);
        }

        isInvincible = false;
        canDig = true; 
        canStrongAttack = true;
    }

    void HandleUndergroundMove()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * digSpeed, 0f);
        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    IEnumerator DigRoutine()
    {
        canDig = false;
        isDiggingAnim = true; 
        anim.SetTrigger("DoDig");
        rb.gravityScale = 0f;        
        rb.linearVelocity = Vector2.zero;
        myCollider.enabled = false; 
        
        yield return new WaitForSeconds(0.5f); 

        isDiggingAnim = false;
        isUnderground = true;
        transform.position += Vector3.down * 0.5f;

        float timer = 0f;
        while (timer < digDuration) {
            timer += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.C)) break; 
            yield return null; 
        }

        // --- 탈출 ---
        isUnderground = false; 
        isDiggingAnim = true; 
        
        sr.enabled = true;
        anim.SetTrigger("DoEmerge"); 

        rb.gravityScale = defaultGravity; 
        myCollider.enabled = true;
        rb.linearVelocity = Vector2.zero;

        // ★ [핵심] 콜라이더가 켜지자마자, 내 위치에 있는 적들과 충돌을 끈다!
        PreventStuckOnEmerge();

        yield return new WaitForSeconds(emergeDamageDelay); 
        
        EmergeAttack(); 

        float remainingTime = emergeAnimDuration - emergeDamageDelay;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);
        
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
                float diffX = enemy.transform.position.x - transform.position.x;
                float dirX = 0;
                if (Mathf.Abs(diffX) < 0.1f) dirX = isFacingRight ? 1f : -1f;
                else dirX = diffX > 0 ? 1f : -1f;

                Vector2 knockbackDir = new Vector2(dirX, 0.5f).normalized;
                
                enemyRb.linearVelocity = Vector2.zero;
                enemyRb.AddForce(knockbackDir * emergeKnockback, ForceMode2D.Impulse);
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