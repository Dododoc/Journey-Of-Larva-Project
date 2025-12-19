using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeetleController : MonoBehaviour
{
    [Header("1. 움직임 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 6f; 
    private int jumpCount = 0; 
    private int maxJumps = 2; 

    [Header("1-1. 점프 상세 설정")]
    public float jumpGravity = 1.0f; 
    public float fallGravity = 0.8f; 
    public float speedMultiplier = 1.0f; 
    public bool isJumpDisabled = false;

    [Header("2. 일반 공격")]
    public float attackDamage = 20f;
    public float attackRange = 1.8f;
    public float attackDelay = 0.2f;
    public float attackCooldown = 0.5f;
    public float basicKnockback = 8f; 
    
    [Header("3. 스킬 1 (들어 넘기기)")]
    public float liftDamage = 30f;
    public float liftRange = 2.5f;     
    public float liftCatchDelay = 0.3f; 
    public float liftThrowDelay = 0.6f; 
    public float liftThrowForce = 15f;  
    public float liftCooldown = 3.0f;
    private bool canLift = true;

    [Header("4. 스킬 2 (공중 다이브)")]
    public float diveSpeed = 25f; 
    public float diveCooldown = 4.0f;
    public float emergeAnimDuration = 0.8f; 
    public float impactDamage1 = 20f;      
    public float impactDelay1 = 0.05f;      
    public float impactRadius = 3.5f;       
    public float impactKnockback1 = 5f;     
    public float impactDamage2 = 40f;       
    public float impactDelay2 = 0.3f;       
    public float impactKnockback2 = 15f;    
    private bool canDive = true;

    [Header("5. 피격 및 넉백")]
    public float hitKnockbackPower = 3f; 
    // ★ [수정] 무적 시간 2초로 증가 (인스펙터에서 확인 필요)
    public float hitInvincibilityDuration = 2.0f; 
    private bool isInvincible = false;      
    private bool isKnockedBack = false;     
    public bool isGrabbedByBoss = false;

    [Header("6. 체크 및 레이어")]
    public Transform attackPoint;       
    public Transform holdPoint;         
    public LayerMask enemyLayers;       
    public Vector2 boxSize = new Vector2(1.0f, 0.3f); 
    public float castDistance = 0.3f; 
    public LayerMask groundLayer;      

    private bool isDiving = false; 
    private bool isLifting = false; 
    private bool isBasicAttacking = false;
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

        if(holdPoint == null)
        {
            GameObject point = new GameObject("HoldPoint");
            point.transform.parent = transform;
            point.transform.localPosition = new Vector3(1.5f, 0.5f, 0); 
            holdPoint = point.transform;
        }
    }

    void Update()
    {
        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;
        if (isKnockedBack || isDiving || isLifting || isGrabbedByBoss) { UpdateAnimation(); return; }

        CheckGround();
        
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0) rb.gravityScale = jumpGravity; 
            else rb.gravityScale = fallGravity; 
        }
        else
        {
            rb.gravityScale = defaultGravity;
        }

        ProcessInput();
        UpdateAnimation();
    }

    void CheckGround()
    {
        if (jumpCooldown > 0) { isGrounded = false; return; }
        if (isDiving) return; 
        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.4f;
        RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, castDistance + 0.3f, groundLayer);
        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;
        if (isGrounded && !wasGrounded) jumpCount = 0;
        if (isGrounded) surfaceNormal = hit.normal; else surfaceNormal = Vector2.up;
    }

    void ProcessInput()
    {
        if (Input.GetKeyDown(KeyCode.Z) && !isBasicAttacking && isGrounded) StartCoroutine(BasicAttackRoutine());
        if (Input.GetKeyDown(KeyCode.X) && canLift && isGrounded) { StartCoroutine(LiftSkillRoutine()); return; }
        if (Input.GetKeyDown(KeyCode.C) && canDive && !isGrounded) { StartCoroutine(DiveSkillRoutine()); return; }

        float moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && !isJumpDisabled)
        {
            if (isGrounded || jumpCount < maxJumps)
            {
                jumpCooldown = 0.1f; isGrounded = false; jumpCount++;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
                rb.linearVelocity += Vector2.up * (jumpForce * 1.5f);
                if (jumpCount == 1) anim.SetTrigger("DoJump"); 
            }
        }

        float currentSpeed = moveSpeed * speedMultiplier;
        if (isGrounded && moveInput != 0)
        {
            rb.gravityScale = defaultGravity;
            Vector2 slopeDir = Vector2.Perpendicular(surfaceNormal).normalized;
            Vector2 moveDir = slopeDir * -moveInput;
            rb.linearVelocity = moveDir * currentSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - 5f);
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);
        }

        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1; transform.localScale = scaler;
    }

    void UpdateAnimation()
    {
        if (isDiving || isLifting || isKnockedBack || isGrabbedByBoss)
        {
            anim.SetBool("IsGrounded", true); anim.SetFloat("Speed", 0f); return;
        }
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }

    public void SetDebuff(bool active, float speedMult)
    {
        if (active) { isJumpDisabled = true; speedMultiplier = speedMult; sr.color = new Color(0.6f, 1f, 0.6f); }
        else { isJumpDisabled = false; speedMultiplier = 1.0f; sr.color = Color.white; }
    }

    public void SetGrabbed(bool grabbed)
    {
        isGrabbedByBoss = grabbed;
        if(grabbed) { rb.linearVelocity = Vector2.zero; rb.gravityScale = 0f; } 
        else { rb.gravityScale = defaultGravity; }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (isDiving || isLifting || isGrabbedByBoss || isInvincible) return;
        if (collision.gameObject.CompareTag("Enemy")) HandleCollisionDamage(collision.gameObject);
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (isDiving || isLifting || isGrabbedByBoss || isInvincible) return;
        if (other.CompareTag("Enemy") || other.CompareTag("Trap")) HandleCollisionDamage(other.gameObject);
    }
    // 1. 몹과 충돌 시 일시적으로 충돌을 무시하는 로직 (끼임 방지)
IEnumerator IgnoreCollisionRoutine(Collider2D enemyCol, float duration = 0.5f)
{
    if (enemyCol == null || myCollider == null) yield break;
    
    // 플레이어와 해당 적의 충돌을 끔
    Physics2D.IgnoreCollision(myCollider, enemyCol, true);
    
    yield return new WaitForSeconds(duration);
    
    // 다시 충돌을 켬
    if (enemyCol != null && myCollider != null)
        Physics2D.IgnoreCollision(myCollider, enemyCol, false);
}

// 2. 다이브 공격 성공 시 몸이 금색으로 반짝이는 효과
IEnumerator FlashGoldEffect()
{
    if (sr != null)
    {
        sr.color = new Color(1f, 0.85f, 0f); // 금색
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        sr.color = new Color(1f, 0.85f, 0f);
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }
}

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDiving) { 
            if (collision.gameObject.CompareTag("Enemy") || ((1 << collision.gameObject.layer) & groundLayer) != 0) 
            { StartCoroutine(DiveImpactRoutine()); return; } 
        }
        if (isLifting || isGrabbedByBoss || isInvincible) return;
        if (collision.gameObject.CompareTag("Enemy")) HandleCollisionDamage(collision.gameObject);
    }

    void HandleCollisionDamage(GameObject target)
    {
        if (isInvincible) return;
        EnemyStats enemyStats = target.GetComponent<EnemyStats>();
        float damageToTake = (enemyStats != null) ? enemyStats.attackDamage : 10f; 
        BossMantis boss = target.GetComponent<BossMantis>();
        if (boss != null) damageToTake = boss.bodyContactDamage;

        if (myStats != null) myStats.TakeDamage(damageToTake);

        float pushDirX = (transform.position.x < target.transform.position.x) ? -1f : 1f;
        Vector2 knockbackDir = new Vector2(pushDirX, 1.0f).normalized; 
        ApplyKnockback(knockbackDir * hitKnockbackPower);
        StartCoroutine(IgnoreCollisionRoutine(target.GetComponent<Collider2D>()));
    }

    public void ApplyKnockback(Vector2 force)
    {
        isBasicAttacking = false; isLifting = false; 
        if(isDiving) { isDiving = false; rb.gravityScale = defaultGravity; isInvincible = false; }
        StopAllCoroutines(); 
        anim.ResetTrigger("DoAttack"); anim.ResetTrigger("DoLift"); anim.ResetTrigger("DoDive"); anim.ResetTrigger("DoImpact");
        anim.Play("Beetle_JumpUp", 0, 0f); sr.color = Color.white;
        float direction = isFacingRight ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(defaultScale.x) * direction, Mathf.Abs(defaultScale.y), defaultScale.z);

        isKnockedBack = true; speedMultiplier = 1.0f; isJumpDisabled = false; 
        rb.gravityScale = defaultGravity; rb.linearVelocity = Vector2.zero; 
        rb.AddForce(force, ForceMode2D.Impulse);
        StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        isInvincible = true; 
        yield return new WaitForSeconds(0.2f); 
        isKnockedBack = false; 
        // 2초 무적
        float blinkEndTime = Time.time + (hitInvincibilityDuration - 0.2f);
        while (Time.time < blinkEndTime) { sr.color = new Color(1, 1, 1, 0.4f); yield return new WaitForSeconds(0.1f); sr.color = Color.white; yield return new WaitForSeconds(0.1f); }
        isInvincible = false; canLift = true; canDive = true;
    }

    IEnumerator BasicAttackRoutine() { isBasicAttacking = true; anim.SetTrigger("DoAttack"); yield return new WaitForSeconds(attackDelay); ApplyDamage(attackPoint.position, attackRange, 1f, basicKnockback); yield return new WaitForSeconds(attackCooldown); isBasicAttacking = false; }
    
    // ★ [수정] 들어 넘기기 스킬 (시작 즉시 무적 + 종료 후 3초 무적)
    IEnumerator LiftSkillRoutine() 
    { 
        canLift = false; isLifting = true; isInvincible = true;
        rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero; anim.SetTrigger("DoLift"); 
        yield return new WaitForSeconds(liftCatchDelay); 
        
        Collider2D hitEnemy = Physics2D.OverlapCircle(attackPoint.position, liftRange, enemyLayers); 
        Rigidbody2D targetRb = null; Collider2D targetCol = null; BossMantis bossScript = null; 
        if (hitEnemy != null) { 
            targetRb = hitEnemy.GetComponent<Rigidbody2D>(); targetCol = hitEnemy.GetComponent<Collider2D>(); bossScript = hitEnemy.GetComponent<BossMantis>(); 
            if (targetRb != null) { 
                if (bossScript != null) bossScript.SetGrabbedState(true); 
                targetRb.linearVelocity = Vector2.zero; targetRb.bodyType = RigidbodyType2D.Kinematic; 
                hitEnemy.transform.position = holdPoint.position; hitEnemy.transform.parent = holdPoint; 
                if (targetCol != null) Physics2D.IgnoreCollision(myCollider, targetCol, true); 
            } 
        } 
        yield return new WaitForSeconds(liftThrowDelay - liftCatchDelay); 
        
        if (hitEnemy != null && targetRb != null) { 
            hitEnemy.transform.parent = null; targetRb.bodyType = RigidbodyType2D.Dynamic; 
            if (bossScript != null) bossScript.SetThrownState(); 
            float throwDirX = isFacingRight ? -1f : 1f; Vector2 throwDir = new Vector2(throwDirX, 1.0f).normalized; 
            targetRb.AddForce(throwDir * liftThrowForce, ForceMode2D.Impulse); 
            EnemyStats es = hitEnemy.GetComponent<EnemyStats>(); float totalDmg = (myStats != null) ? myStats.TotalAttack + liftDamage : 30f; if (es != null) es.TakeDamage(totalDmg); 
            StartCoroutine(IgnoreCollisionRoutine(targetCol, 1.0f)); 
        } 
        yield return new WaitForSeconds(0.3f); 
        isLifting = false; rb.gravityScale = defaultGravity; 
        StartCoroutine(InvincibilityRoutine(2.0f));
        yield return new WaitForSeconds(liftCooldown); canLift = true; 
    }

    IEnumerator InvincibilityRoutine(float duration) { isInvincible = true; yield return new WaitForSeconds(duration); isInvincible = false; }
    IEnumerator DiveSkillRoutine() { canDive = false; isDiving = true; isInvincible = true; anim.SetTrigger("DoDive"); rb.gravityScale = 0; float xDir = isFacingRight ? 1f : -1f; Vector2 diveDirection = new Vector2(xDir, -0.55f).normalized; rb.linearVelocity = diveDirection * diveSpeed; float timer = 0f; while(isDiving && timer < 2.5f) { timer += Time.deltaTime; yield return null; } if (isDiving) { isDiving = false; isInvincible = false; rb.gravityScale = defaultGravity; canDive = true; } }
    IEnumerator DiveImpactRoutine() { if (!isDiving) yield break; rb.linearVelocity = Vector2.zero; rb.gravityScale = defaultGravity; anim.SetTrigger("DoImpact"); yield return new WaitForSeconds(impactDelay1); StartCoroutine(FlashGoldEffect()); PerformAreaDamage(impactDamage1, impactKnockback1); yield return new WaitForSeconds(impactDelay2); PerformAreaDamage(impactDamage2, impactKnockback2); float usedTime = impactDelay1 + impactDelay2; float remainingTime = emergeAnimDuration - usedTime; if (remainingTime > 0) yield return new WaitForSeconds(remainingTime); isDiving = false; StartCoroutine(PostDiveInvincibility(1.5f)); yield return new WaitForSeconds(diveCooldown); canDive = true; }
    IEnumerator PostDiveInvincibility(float duration) { yield return new WaitForSeconds(duration); isInvincible = false; }

    // ★ [수정] 중복 데미지 방지 (HashSet 사용)
    void PerformAreaDamage(float addDamage, float knockback) 
{ 
    Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, impactRadius, enemyLayers);
    HashSet<GameObject> hitSet = new HashSet<GameObject>();

    foreach (Collider2D enemy in hitEnemies) 
    { 
        if(hitSet.Contains(enemy.gameObject)) continue; 
        hitSet.Add(enemy.gameObject);

        // 1. 데미지 전달
        EnemyStats es = enemy.GetComponent<EnemyStats>(); 
        float finalDmg = (myStats != null) ? myStats.TotalAttack + addDamage : 30f; 
        if (es != null) es.TakeDamage(finalDmg);
        
        // 2. ★ 넉백 처리 (거미 vs 일반 몹 분기)
        SpiderAI spider = enemy.GetComponent<SpiderAI>();
        if (spider != null)
        {
            // 거미줄에 매달린 거미에게도 충격 전달
            spider.ApplyKnockback(new Vector2(knockback, 0)); 
        }
        else
        {
            // 일반 몹 넉백 처리
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>(); 
            if (enemyRb != null) 
            { 
                Vector2 dir = (enemy.transform.position - transform.position).normalized; 
                if(knockback > 10f) dir += Vector2.up * 0.5f; 
                enemyRb.linearVelocity = Vector2.zero; 
                enemyRb.AddForce(dir.normalized * knockback, ForceMode2D.Impulse); 
                StartCoroutine(IgnoreCollisionRoutine(enemy.GetComponent<Collider2D>()));
            } 
        }
    } 
}
    // ★ [수정] 중복 데미지 방지
    void ApplyDamage(Vector2 point, float range, float multiplier, float knockbackForce) 
{ 
    Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point, range, enemyLayers);
    float baseDmg = (myStats != null) ? myStats.TotalAttack : attackDamage;
    float finalDmg = baseDmg * multiplier; 
    
    HashSet<GameObject> damagedEnemies = new HashSet<GameObject>();

    foreach (Collider2D enemy in hitEnemies) 
    { 
        if (damagedEnemies.Contains(enemy.gameObject)) continue;
        damagedEnemies.Add(enemy.gameObject);

        // 1. 데미지 전달
        EnemyStats es = enemy.GetComponent<EnemyStats>(); 
        if (es != null) es.TakeDamage(finalDmg);
        
        // 2. ★ 넉백 처리 (거미 vs 일반 몹 분기)
        SpiderAI spider = enemy.GetComponent<SpiderAI>();
        if (spider != null)
        {
            // 거미라면 흔들림 효과 호출
            spider.ApplyKnockback(new Vector2(knockbackForce, 0)); 
        }
        else
        {
            // 일반 몹이라면 물리적인 힘을 가함
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>(); 
            if (enemyRb != null) 
            { 
                float dirX = (enemy.transform.position.x - transform.position.x) > 0 ? 1f : -1f; 
                Vector2 knockbackDir = new Vector2(dirX, 0.5f).normalized; 
                enemyRb.linearVelocity = Vector2.zero; 
                enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse); 
                StartCoroutine(IgnoreCollisionRoutine(enemy.GetComponent<Collider2D>()));
            } 
        }
    } 
}
    void OnDrawGizmos() { if (isGrounded) Gizmos.color = Color.green; else Gizmos.color = Color.red; Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.4f; Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize); if (attackPoint != null) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(attackPoint.position, attackRange); } if (holdPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(holdPoint.position, 0.3f); } }
}