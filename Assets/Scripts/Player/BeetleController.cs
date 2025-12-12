using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeetleController : MonoBehaviour
{
    [Header("1. 움직임 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("2. 일반 공격 (Z - 뿔치기)")]
    public float attackDamage = 20f;
    public float attackRange = 1.8f;
    public float attackDelay = 0.2f;
    public float attackCooldown = 0.5f;
    public float basicKnockback = 8f; 
    
    [Header("3. 스킬 1 (X - 들어 넘기기)")]
    public float liftDamage = 30f;
    public float liftRange = 2.5f;     
    public float liftCatchDelay = 0.3f; 
    public float liftThrowDelay = 0.6f; 
    public float liftThrowForce = 15f;  
    public float liftCooldown = 3.0f;
    private bool canLift = true;

    [Header("4. 스킬 2 (C - 공중 다이브 2연타)")]
    public float diveSpeed = 25f; 
    public float diveCooldown = 4.0f;
    public float emergeAnimDuration = 0.8f; // 애니메이션 전체 길이 (넉넉하게)
    
    [Header("4-1. 1타: 착지 충격 (Crash)")]
    public float impactDamage1 = 20f;       // 1타 데미지
    public float impactDelay1 = 0.05f;      // 땅에 닿고 1타 터지는 시간
    public float impactRadius = 3.5f;       // 1타 범위 (광역)
    public float impactKnockback1 = 5f;     // 1타는 살짝 밀침

    [Header("4-2. 2타: 뿔 찌르기 (Thrust)")]
    public float impactDamage2 = 40f;       // 2타 데미지 (강력)
    public float impactDelay2 = 0.3f;       // 1타 후 2타까지 걸리는 시간
    public float impactKnockback2 = 15f;    // 2타는 멀리 날림
    
    private bool canDive = true;

    [Header("5. 피격 및 넉백")]
    public float hitKnockbackPower = 3f; 
    public float hitInvincibilityDuration = 1.0f; 
    private bool isInvincible = false;      
    private bool isKnockedBack = false;     

    [Header("6. 체크 및 레이어")]
    public Transform attackPoint;       
    public Transform holdPoint;         
    public LayerMask enemyLayers;       
    public Vector2 boxSize = new Vector2(1.0f, 0.3f); 
    public float castDistance = 0.3f; 
    public LayerMask groundLayer;      

    // 상태 변수
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

        if (isKnockedBack || isDiving || isLifting) 
        {
            UpdateAnimation();
            return;
        }

        CheckGround();
        ProcessInput();
        UpdateAnimation();
    }

    void CheckGround()
    {
        if (jumpCooldown > 0) { isGrounded = false; return; }
        if (isDiving) return; 

        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.4f;
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

        if (Input.GetKeyDown(KeyCode.X) && canLift && isGrounded) 
        { 
            StartCoroutine(LiftSkillRoutine()); 
            return; 
        }

        if (Input.GetKeyDown(KeyCode.C) && canDive && !isGrounded) 
        { 
            StartCoroutine(DiveSkillRoutine()); 
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

        if (isGrounded && moveInput != 0)
        {
            rb.gravityScale = defaultGravity;
            Vector2 slopeDir = Vector2.Perpendicular(surfaceNormal).normalized;
            Vector2 moveDir = slopeDir * -moveInput;
            rb.linearVelocity = moveDir * moveSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - 5f);
        }
        else
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
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
        if (isDiving || isLifting || isKnockedBack)
        {
            anim.SetBool("IsGrounded", true);
            anim.SetFloat("Speed", 0f);
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
        ApplyDamage(attackPoint.position, attackRange, 1f, basicKnockback); 
        yield return new WaitForSeconds(attackCooldown);
        isBasicAttacking = false;
    }

    IEnumerator LiftSkillRoutine()
    {
        canLift = false;
        isLifting = true;
        
        rb.gravityScale = 0f; 
        rb.linearVelocity = Vector2.zero; 

        anim.SetTrigger("DoLift"); 

        yield return new WaitForSeconds(liftCatchDelay);

        Collider2D hitEnemy = Physics2D.OverlapCircle(attackPoint.position, liftRange, enemyLayers);
        Rigidbody2D targetRb = null;
        Collider2D targetCol = null;

        if (hitEnemy != null)
        {
            targetRb = hitEnemy.GetComponent<Rigidbody2D>();
            targetCol = hitEnemy.GetComponent<Collider2D>();

            if (targetRb != null)
            {
                targetRb.linearVelocity = Vector2.zero;
                targetRb.bodyType = RigidbodyType2D.Kinematic; 
                
                hitEnemy.transform.position = holdPoint.position;
                hitEnemy.transform.parent = holdPoint; 

                if (targetCol != null)
                    Physics2D.IgnoreCollision(myCollider, targetCol, true);
            }
        }

        yield return new WaitForSeconds(liftThrowDelay - liftCatchDelay);

        if (hitEnemy != null && targetRb != null)
        {
            hitEnemy.transform.parent = null; 
            targetRb.bodyType = RigidbodyType2D.Dynamic;     
            
            float throwDirX = isFacingRight ? -1f : 1f; 
            Vector2 throwDir = new Vector2(throwDirX, 1.0f).normalized;
            
            targetRb.AddForce(throwDir * liftThrowForce, ForceMode2D.Impulse);

            EnemyStats es = hitEnemy.GetComponent<EnemyStats>();
            float totalDmg = (myStats != null) ? myStats.TotalAttack + liftDamage : 30f;
            if (es != null) es.TakeDamage(totalDmg);
            
            StartCoroutine(IgnoreCollisionRoutine(targetCol, 1.0f));
        }

        yield return new WaitForSeconds(0.3f); 
        isLifting = false;
        rb.gravityScale = defaultGravity;
        yield return new WaitForSeconds(liftCooldown);
        canLift = true;
    }

    IEnumerator DiveSkillRoutine()
    {
        canDive = false;
        isDiving = true; 

        anim.SetTrigger("DoDive"); 
        rb.gravityScale = 0; 
        
        float xDir = isFacingRight ? 1f : -1f;
        Vector2 diveDirection = new Vector2(xDir, -0.55f).normalized; // 30도
        rb.linearVelocity = diveDirection * diveSpeed;

        float timer = 0f;
        while(isDiving && timer < 2.5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        if (isDiving) StartCoroutine(DiveImpactRoutine()); 
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDiving)
        {
            if (collision.gameObject.CompareTag("Enemy") || ((1 << collision.gameObject.layer) & groundLayer) != 0)
            {
                StartCoroutine(DiveImpactRoutine()); 
                return; 
            }
        }
        if (isLifting) return;

        if (collision.gameObject.CompareTag("Enemy")) HandleCollisionDamage(collision.gameObject);
    }

    // --- ★ [핵심] 2연타 충돌 코루틴 ---
    IEnumerator DiveImpactRoutine()
    {
        if (!isDiving) yield break;

        // 1. 착지 및 정지
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravity;
        anim.SetTrigger("DoImpact"); 

        // --- [1타] 쾅! (착지 충격) ---
        yield return new WaitForSeconds(impactDelay1); 
        StartCoroutine(FlashGoldEffect()); // 번쩍!
        PerformAreaDamage(impactDamage1, impactKnockback1); // 데미지 1

        // --- [2타] 푹! (뿔 찌르기) ---
        yield return new WaitForSeconds(impactDelay2);
        PerformAreaDamage(impactDamage2, impactKnockback2); // 데미지 2

        // --- 후딜레이 계산 ---
        // 전체 애니메이션 시간에서 (1타 딜레이 + 2타 딜레이)를 뺀 만큼만 더 기다림
        float usedTime = impactDelay1 + impactDelay2;
        float remainingTime = emergeAnimDuration - usedTime;
        
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        isDiving = false; 
        yield return new WaitForSeconds(diveCooldown);
        canDive = true;
    }

    // ★ 범위 공격 함수 (데미지와 넉백만 다르게 해서 재사용)
    void PerformAreaDamage(float addDamage, float knockback)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, impactRadius, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyStats es = enemy.GetComponent<EnemyStats>();
            float finalDmg = (myStats != null) ? myStats.TotalAttack + addDamage : 30f;
            if (es != null) es.TakeDamage(finalDmg);

            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 dir = (enemy.transform.position - transform.position).normalized;
                // 2타(찌르기)일 때는 살짝 위쪽으로 띄워주는 센스
                if(knockback > 10f) dir += Vector2.up * 0.5f; 
                
                enemyRb.linearVelocity = Vector2.zero; // 기존 힘 초기화 후 날리기
                enemyRb.AddForce(dir.normalized * knockback, ForceMode2D.Impulse);
                StartCoroutine(IgnoreCollisionRoutine(enemy.GetComponent<Collider2D>()));
            }
        }
    }

    IEnumerator FlashGoldEffect()
    {
        sr.color = new Color(1f, 0.9f, 0.4f); 
        yield return new WaitForSeconds(0.15f);
        sr.color = Color.white;
    }

    IEnumerator IgnoreCollisionRoutine(Collider2D enemyCol, float duration = 0.5f)
    {
        if (enemyCol == null || myCollider == null) yield break;
        Physics2D.IgnoreCollision(myCollider, enemyCol, true);
        yield return new WaitForSeconds(duration);
        if (enemyCol != null && myCollider != null) Physics2D.IgnoreCollision(myCollider, enemyCol, false);
    }

    void ApplyDamage(Vector2 point, float range, float multiplier, float knockbackForce)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point, range, enemyLayers);
        float baseDmg = (myStats != null) ? myStats.TotalAttack : attackDamage;
        float finalDmg = baseDmg * multiplier;

        foreach (Collider2D enemy in hitEnemies) {
            EnemyStats es = enemy.GetComponent<EnemyStats>();
            if (es != null) es.TakeDamage(finalDmg);

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

    void HandleCollisionDamage(GameObject target)
    {
        EnemyStats enemyStats = target.GetComponent<EnemyStats>();
        float damageToTake = (enemyStats != null) ? enemyStats.attackDamage : 10f; 
        if (myStats != null) myStats.TakeDamage(damageToTake);

        float pushDirX = (transform.position.x < target.transform.position.x) ? -1f : 1f;
        Vector2 knockbackDir = new Vector2(pushDirX, 1.0f).normalized; 
        ApplyKnockback(knockbackDir * hitKnockbackPower);
        StartCoroutine(IgnoreCollisionRoutine(target.GetComponent<Collider2D>()));
    }

    public void ApplyKnockback(Vector2 force)
    {
        isBasicAttacking = false;
        isLifting = false; 
        if(isDiving) { isDiving = false; rb.gravityScale = defaultGravity; }
        
        StopAllCoroutines(); 

        anim.ResetTrigger("DoAttack");
        anim.ResetTrigger("DoLift");
        anim.ResetTrigger("DoDive");
        anim.ResetTrigger("DoImpact");

        anim.Play("Beetle_JumpUp", 0, 0f); 

        sr.color = Color.white;
        float direction = isFacingRight ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(defaultScale.x) * direction, Mathf.Abs(defaultScale.y), defaultScale.z);

        isKnockedBack = true;
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(force, ForceMode2D.Impulse);

        StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        isInvincible = true; 
        yield return new WaitForSeconds(0.2f); 
        isKnockedBack = false; 

        float blinkEndTime = Time.time + (hitInvincibilityDuration - 0.2f);
        while (Time.time < blinkEndTime)
        {
            sr.color = new Color(1, 1, 1, 0.4f); 
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;              
            yield return new WaitForSeconds(0.1f);
        }
        isInvincible = false;
        canLift = true; canDive = true;
    }

    void OnDrawGizmos()
    {
        if (isGrounded) Gizmos.color = Color.green;
        else Gizmos.color = Color.red;
        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.4f;
        Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize);
        if (attackPoint != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (holdPoint != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(holdPoint.position, 0.3f);
        }
    }
}