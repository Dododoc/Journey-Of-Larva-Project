using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AntController : MonoBehaviour
{
    [Header("1. 움직임 설정")]
    public float moveSpeed = 6f;
    public float jumpForce = 13f;
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
    public float emergeAnimDuration = 0.6f; 
    public float emergeDamageDelay = 0.3f;  
    private bool canDig = true;

    [Header("5. 피격 및 넉백 설정")]
    public float hitKnockbackPower = 5f; 
    public float hitInvincibilityDuration = 2.0f; 
    public bool isInvincible = false;      
    private bool isKnockedBack = false;     

    [Header("6. 체크 및 레이어")]
    public Transform attackPoint;       
    public LayerMask enemyLayers;       
    public Vector2 boxSize = new Vector2(0.8f, 0.2f); 
    public float castDistance = 0.3f; 
    public LayerMask groundLayer;      
    
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
        if (isKnockedBack) { UpdateAnimation(); return; }
        if (isDiggingAnim) { if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero; UpdateAnimation(); return; }
        if (isUnderground) { HandleUndergroundMove(); UpdateAnimation(); return; }
        if (isStrongAttacking) { rb.linearVelocity = Vector2.zero; return; }

        CheckGround();
        ProcessInput();
        UpdateAnimation();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isUnderground || isDiggingAnim || isInvincible) return;
        if (other.CompareTag("Enemy") || other.CompareTag("Trap")) HandleCollisionDamage(other.gameObject);
    }

    void HandleCollisionDamage(GameObject target)
    {
        // ★ 부모의 스탯 참조
        EnemyStats es = target.GetComponentInParent<EnemyStats>();
        float damage = (es != null) ? es.attackDamage : 10f;
        if (myStats != null) myStats.TakeDamage(damage);

        float pushDirX = (transform.position.x < target.transform.position.x) ? -1f : 1f;
        ApplyKnockback(new Vector2(pushDirX, 1.5f).normalized * hitKnockbackPower);
        StartCoroutine(IgnoreCollisionRoutine(target.GetComponent<Collider2D>()));
    }

    // ★ 평타 및 강공격 데미지 로직 수정
    void ApplyDamage(Vector2 point, float range, float multiplier, float kForce)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point, range, enemyLayers);
        float finalDmg = ((myStats != null) ? myStats.TotalAttack : attackDamage) * multiplier;

        foreach (Collider2D enemy in hitEnemies) {
            // 부모의 스탯과 리지드바디 참조
            EnemyStats es = enemy.GetComponentInParent<EnemyStats>();
            if (es != null) es.TakeDamage(finalDmg);

            // AntController.cs의 ApplyDamage 함수 안에서 수정
            BaseEnemyAI enemyAI = enemy.GetComponentInParent<BaseEnemyAI>();
            if (enemyAI != null) {
                float dirX = (enemy.transform.position.x - transform.position.x) > 0 ? 1f : -1f;
                Vector2 kDir = new Vector2(dirX, 0.5f).normalized; 
                enemyAI.ApplyKnockback(kDir * kForce, 0.4f);
            }
        }
    }

    // ★ 땅파기 탈출 공격 수정
    void EmergeAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, emergeRadius, enemyLayers);
        foreach (Collider2D enemy in hitEnemies) {
            EnemyStats es = enemy.GetComponentInParent<EnemyStats>();
            if (es != null) es.TakeDamage(emergeDamage);

            // AntController.cs의 EmergeAttack 함수 안에서 수정
            BaseEnemyAI enemyAI = enemy.GetComponentInParent<BaseEnemyAI>();
            if (enemyAI != null) {
                float diffX = enemy.transform.position.x - transform.position.x;
                float dirX = (Mathf.Abs(diffX) < 0.1f) ? (isFacingRight ? 1f : -1f) : (diffX > 0 ? 1f : -1f);
                Vector2 kDir = new Vector2(dirX, 0.5f).normalized;
                enemyAI.ApplyKnockback(kDir * emergeKnockback, 0.5f);
            }
        }
    }

    // (나머지 이동, 애니메이션, 넉백 등 기존 헬퍼 함수들은 생략 없이 유지)
    IEnumerator BasicAttackRoutine() { isBasicAttacking = true; anim.SetTrigger("DoAttack"); yield return new WaitForSeconds(attackDelay); ApplyDamage(attackPoint.position, attackRange, 1f, basicEnemyKnockback); yield return new WaitForSeconds(attackCooldown); isBasicAttacking = false; }
    IEnumerator StrongAttackRoutine() { /* 기존 순간이동 및 공격 로직 유지하며 ApplyDamage 호출 */ canStrongAttack = false; isStrongAttacking = true; isInvincible = true; yield return new WaitForSeconds(0.1f); Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, teleportRange, enemyLayers); Transform target = null; float closestDist = Mathf.Infinity; foreach (var hit in hits) { float d = Vector2.Distance(transform.position, hit.transform.position); if (d < closestDist) { closestDist = d; target = hit.transform; } } if (target != null) { float dirToEnemy = target.position.x - transform.position.x; if (dirToEnemy > 0 && !isFacingRight) Flip(); else if (dirToEnemy < 0 && isFacingRight) Flip(); if (Vector2.Distance(transform.position, target.position) > attackRange * 1.2f) { sr.color = new Color(1f, 1f, 1f, 0.5f); float dirSign = Mathf.Sign(target.position.x - transform.position.x); transform.position = target.position + new Vector3(dirSign * teleportOffset, 0, 0); } } anim.SetTrigger("DoStrongAttack"); yield return new WaitForSeconds(strongAttackDelay); ApplyDamage(attackPoint.position, attackRange * 1.5f, strongDamageMultiplier, strongEnemyKnockback); sr.color = Color.white; isStrongAttacking = false; StartCoroutine(SkillInvincibilityRoutine(1.5f)); yield return new WaitForSeconds(strongCooldown); canStrongAttack = true; }
    IEnumerator DigRoutine() { canDig = false; isDiggingAnim = true; anim.SetTrigger("DoDig"); rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero; myCollider.enabled = false; yield return new WaitForSeconds(0.5f); isDiggingAnim = false; isUnderground = true; float timer = 0f; while (timer < digDuration) { timer += Time.deltaTime; if (Input.GetKeyDown(KeyCode.C)) break; yield return null; } isUnderground = false; isDiggingAnim = true; anim.SetTrigger("DoEmerge"); rb.gravityScale = defaultGravity; myCollider.enabled = true; yield return new WaitForSeconds(emergeDamageDelay); EmergeAttack(); yield return new WaitForSeconds(emergeAnimDuration - emergeDamageDelay); isDiggingAnim = false; StartCoroutine(SkillInvincibilityRoutine(2.0f)); yield return new WaitForSeconds(digCooldown); canDig = true; }
    void CheckGround() { if (jumpCooldown > 0) { isGrounded = false; return; } Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f; RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, castDistance + 0.3f, groundLayer); isGrounded = hit.collider != null; if (isGrounded) surfaceNormal = hit.normal; else surfaceNormal = Vector2.up; }
    void ProcessInput() { float m = Input.GetAxisRaw("Horizontal"); rb.linearVelocity = new Vector2(m * moveSpeed, rb.linearVelocity.y); if (Input.GetButtonDown("Jump") && isGrounded) { rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); anim.SetTrigger("DoJump"); } if (m > 0 && !isFacingRight) Flip(); else if (m < 0 && isFacingRight) Flip(); }
    void Flip() { isFacingRight = !isFacingRight; Vector3 s = transform.localScale; s.x *= -1; transform.localScale = s; }
    void UpdateAnimation() { anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x)); anim.SetBool("IsGrounded", isGrounded); anim.SetFloat("VerticalSpeed", rb.linearVelocity.y); }
    public void ApplyKnockback(Vector2 f) { isKnockedBack = true; rb.linearVelocity = Vector2.zero; rb.AddForce(f, ForceMode2D.Impulse); StartCoroutine(KnockbackRoutine()); }
    IEnumerator KnockbackRoutine() { isInvincible = true; yield return new WaitForSeconds(0.3f); isKnockedBack = false; float blink = Time.time + (hitInvincibilityDuration - 0.3f); while (Time.time < blink) { sr.color = new Color(1,1,1,0.4f); yield return new WaitForSeconds(0.1f); sr.color = Color.white; yield return new WaitForSeconds(0.1f); } isInvincible = false; }
    IEnumerator SkillInvincibilityRoutine(float d) { isInvincible = true; yield return new WaitForSeconds(d); isInvincible = false; }
    IEnumerator IgnoreCollisionRoutine(Collider2D c, float d = 0.5f) { if (c != null && myCollider != null) Physics2D.IgnoreCollision(myCollider, c, true); yield return new WaitForSeconds(d); if (c != null && myCollider != null) Physics2D.IgnoreCollision(myCollider, c, false); }
    void HandleUndergroundMove() { float m = Input.GetAxisRaw("Horizontal"); rb.linearVelocity = new Vector2(m * digSpeed, 0f); float clampedX = Mathf.Clamp(transform.position.x, mapMinX, mapMaxX); transform.position = new Vector3(clampedX, transform.position.y, transform.position.z); if (m > 0 && !isFacingRight) Flip(); else if (m < 0 && isFacingRight) Flip(); }
}