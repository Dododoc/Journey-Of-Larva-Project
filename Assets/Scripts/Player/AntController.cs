using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AntController : MonoBehaviour
{
    [Header("1. 움직임 설정")]
    public float moveSpeed = 6f;
    public float jumpForce = 13f;

    // ★ [추가] 맵 이동 제한 (좌우 벽)
    public float mapMinX = -25f;
    public float mapMaxX = 25f;

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
    public float lifestealRatio = 0.2f; 
    public float strongEnemyKnockback = 7f; 
    private bool canStrongAttack = true;
    public float teleportRange = 6.0f; 
    public float teleportOffset = 1.0f; 

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
    public float hitInvincibilityDuration = 2.0f; 
    private bool isInvincible = false;      
    private bool isKnockedBack = false;     

    [Header("6. 체크 및 레이어")]
    public Transform attackPoint;       
    public LayerMask enemyLayers;       
    public Vector2 boxSize = new Vector2(0.8f, 0.2f); 
    public float castDistance = 0.3f; 
    public LayerMask groundLayer;      
    public bool IsUnderground => isUnderground;
    
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
    
    private Vector3 defaultScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        myStats = GetComponent<PlayerStats>();
        defaultGravity = rb.gravityScale;
        defaultScale = transform.localScale;
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
        if (Input.GetKeyDown(KeyCode.Z) && !isBasicAttacking && isGrounded) StartCoroutine(BasicAttackRoutine());
        if (Input.GetKeyDown(KeyCode.X) && canStrongAttack && isGrounded) { StartCoroutine(StrongAttackRoutine()); return; }
        if (Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(KeyCode.C) && canDig && isGrounded) { StartCoroutine(DigRoutine()); return; }

        float moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpCooldown = 0.2f; isGrounded = false;
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
            anim.SetBool("IsGrounded", true); anim.SetFloat("Speed", 0f); anim.SetFloat("VerticalSpeed", 0f); return;
        }
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }

    IEnumerator BasicAttackRoutine()
    {
        isBasicAttacking = true; anim.SetTrigger("DoAttack"); 
        yield return new WaitForSeconds(attackDelay);
        ApplyDamage(attackPoint.position, attackRange, 1f, false, basicEnemyKnockback); 
        yield return new WaitForSeconds(attackCooldown);
        isBasicAttacking = false;
    }

    // ★ [수정] 강공격 (X스킬)
    // ★ [수정] 강공격 (X스킬) - 제자리 공격 시 투명화 제거
    IEnumerator StrongAttackRoutine()
    {
        canStrongAttack = false;
        isStrongAttacking = true;

        // 1. 키 누르자마자 즉시 무적 (투명해지지는 않음)
        isInvincible = true;
        // sr.color = new Color(1f, 1f, 1f, 0.5f); <--- 이 줄을 삭제했습니다.

        // 2. 아주 짧은 대기 (0.1초)
        yield return new WaitForSeconds(0.1f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, teleportRange, enemyLayers);
        Transform target = null;
        float closestDist = Mathf.Infinity;

        // 가장 가까운 적 찾기
        foreach (var hit in hits)
        {
            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < closestDist) { closestDist = d; target = hit.transform; }
        }

        if (target != null)
        {
            // 거리 계산 (콜라이더 표면 기준)
            float distToTarget = Vector2.Distance(attackPoint.position, target.position);
            Collider2D targetCol = target.GetComponent<Collider2D>();
            if (targetCol != null)
            {
                Vector2 closestPoint = targetCol.ClosestPoint(attackPoint.position);
                distToTarget = Vector2.Distance(attackPoint.position, closestPoint);
            }

            // A. 공격 사거리 내 (제자리 공격)
            if (distToTarget <= attackRange * 1.2f) 
            {
                // 투명해지지 않음 (그대로 흰색)
                
                // 방향만 적을 향해 전환
                float dirToEnemy = target.position.x - transform.position.x;
                if (dirToEnemy > 0 && !isFacingRight) Flip();
                else if (dirToEnemy < 0 && isFacingRight) Flip();
            }
            // B. 사거리 밖 (순간이동 공격)
            else
            {
                // ★ 순간이동 할 때만 투명해짐
                sr.color = new Color(1f, 1f, 1f, 0.5f);

                // 적의 뒤로 이동
                float dirSign = Mathf.Sign(target.position.x - transform.position.x); 
                float enemyHalfWidth = 0.5f; 
                if (targetCol != null) enemyHalfWidth = targetCol.bounds.extents.x;

                Vector3 teleportPos = target.position + new Vector3(dirSign * (enemyHalfWidth + teleportOffset), 0, 0);
                transform.position = teleportPos;

                if (dirSign > 0 && isFacingRight) Flip(); 
                else if (dirSign < 0 && !isFacingRight) Flip();
            }
        }

        anim.SetTrigger("DoStrongAttack"); 
        
        yield return new WaitForSeconds(strongAttackDelay);
        
        ApplyDamage(attackPoint.position, attackRange * 1.5f, strongDamageMultiplier, true, strongEnemyKnockback);
        
        // 색상 복구 (혹시 투명해졌다면 다시 흰색으로)
        sr.color = Color.white;
        isStrongAttacking = false;

        // 공격 후 1.5초 추가 무적
        StartCoroutine(SkillInvincibilityRoutine(1.5f));

        yield return new WaitForSeconds(strongCooldown);
        canStrongAttack = true;
    }
    IEnumerator SkillInvincibilityRoutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }

    IEnumerator IgnoreCollisionRoutine(Collider2D enemyCol, float duration = 0.5f)
    {
        if (enemyCol == null || myCollider == null) yield break;
        Physics2D.IgnoreCollision(myCollider, enemyCol, true);
        yield return new WaitForSeconds(duration);
        if (enemyCol != null && myCollider != null) Physics2D.IgnoreCollision(myCollider, enemyCol, false);
    }

    void PreventStuckOnEmerge()
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, emergeRadius, enemyLayers);
        foreach (Collider2D enemy in nearbyEnemies) StartCoroutine(IgnoreCollisionRoutine(enemy, 2.0f));
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

            if (isLeech && myStats != null) totalHeal += finalDmg * lifestealRatio;

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
        if (isLeech && totalHeal > 0 && myStats != null) myStats.Heal(totalHeal);
    }

    void OnCollisionEnter2D(Collision2D collision) { if (isUnderground || isDiggingAnim || isInvincible) return; if (collision.gameObject.CompareTag("Enemy")) HandleCollisionDamage(collision.gameObject); }
    void OnCollisionStay2D(Collision2D collision) { if (isUnderground || isDiggingAnim || isInvincible) return; if (collision.gameObject.CompareTag("Enemy")) HandleCollisionDamage(collision.gameObject); }
    void OnTriggerEnter2D(Collider2D other) { if (isUnderground || isDiggingAnim || isInvincible) return; if (other.CompareTag("Enemy") || other.CompareTag("Trap")) HandleCollisionDamage(other.gameObject); }
    void OnTriggerStay2D(Collider2D other) { if (isUnderground || isDiggingAnim || isInvincible) return; if (other.CompareTag("Enemy") || other.CompareTag("Trap")) HandleCollisionDamage(other.gameObject); }

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
        isBasicAttacking = false; isStrongAttacking = false; isDiggingAnim = false; canDig = true; canStrongAttack = true;
        StopAllCoroutines(); 
        anim.ResetTrigger("DoAttack"); anim.ResetTrigger("DoStrongAttack"); anim.ResetTrigger("DoDig"); anim.ResetTrigger("DoEmerge");
        anim.Play("Ant_Fly", 0, 0f); sr.color = Color.white;
        float direction = isFacingRight ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(defaultScale.x) * direction, Mathf.Abs(defaultScale.y), defaultScale.z);
        isKnockedBack = true; rb.gravityScale = defaultGravity; rb.linearVelocity = Vector2.zero; 
        rb.AddForce(force, ForceMode2D.Impulse);
        StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        isInvincible = true; 
        yield return new WaitForSeconds(0.3f);
        isKnockedBack = false; 
        float blinkEndTime = Time.time + (hitInvincibilityDuration - 0.3f);
        while (Time.time < blinkEndTime) { sr.color = new Color(1, 1, 1, 0.4f); yield return new WaitForSeconds(0.1f); sr.color = Color.white; yield return new WaitForSeconds(0.1f); }
        isInvincible = false; canDig = true; canStrongAttack = true;
    }

    // ★ [수정] 땅파기 이동 시 맵 밖으로 못 나가게 제한
    void HandleUndergroundMove()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * digSpeed, 0f);
        
        // X축 좌표 제한 (Clamp)
        float clampedX = Mathf.Clamp(transform.position.x, mapMinX, mapMaxX);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);

        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    IEnumerator DigRoutine()
    {
        canDig = false; isDiggingAnim = true; 
        anim.SetTrigger("DoDig");
        rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero; myCollider.enabled = false; 
        yield return new WaitForSeconds(0.5f); 
        isDiggingAnim = false; isUnderground = true;
        transform.position += Vector3.down * 0.5f;

        float timer = 0f;
        while (timer < digDuration) {
            timer += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.C)) break; 
            yield return null; 
        }

        isUnderground = false; isDiggingAnim = true; 
        sr.enabled = true; anim.SetTrigger("DoEmerge"); 
        rb.gravityScale = defaultGravity; myCollider.enabled = true; rb.linearVelocity = Vector2.zero;
        PreventStuckOnEmerge();
        yield return new WaitForSeconds(emergeDamageDelay); 
        EmergeAttack(); 
        float remainingTime = emergeAnimDuration - emergeDamageDelay;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);
        isDiggingAnim = false; 

        // 2초 무적
        StartCoroutine(SkillInvincibilityRoutine(2.0f));

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
                float dirX = (Mathf.Abs(diffX) < 0.1f) ? (isFacingRight ? 1f : -1f) : (diffX > 0 ? 1f : -1f);
                Vector2 knockbackDir = new Vector2(dirX, 0.5f).normalized;
                enemyRb.linearVelocity = Vector2.zero; enemyRb.AddForce(knockbackDir * emergeKnockback, ForceMode2D.Impulse);
                StartCoroutine(IgnoreCollisionRoutine(enemy.GetComponent<Collider2D>()));
            }
        }
    }

    void OnDrawGizmos()
    {
        if (isGrounded) Gizmos.color = Color.green; else Gizmos.color = Color.red;
        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize);
        if (attackPoint != null) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(attackPoint.position, attackRange); }
    }
}