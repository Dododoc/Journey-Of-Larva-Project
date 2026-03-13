using UnityEngine;
using System.Collections;

public class Larva_PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Attack Settings (Dash)")]
    public float dashSpeed = 15f;     
    public float dashDuration = 0.4f; 
    public float dashCooldown = 1f;   
    private bool isDashing = false;   
    private bool canDash = true;      
    public bool IsDashing => isDashing;

    [Header("Knockback & Invincibility")]
    public float knockbackPower = 10f;      
    public float recoilPower = 5f;          
    public float hitInvincibilityDuration = 1.5f;   
    public float attackInvincibilityDuration = 0.2f; 
    public bool isInvincible = false;      

    [Header("Ground Check")]
    public Vector2 boxSize = new Vector2(0.8f, 0.2f);
    public float castDistance = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private PlayerStats myStats; 

    private bool isGrounded;
    private Vector2 surfaceNormal;
    private float jumpCooldown;
    private bool isKnockedBack; 
    private float defaultGravity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myStats = GetComponent<PlayerStats>();
        defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;
        CheckGround();
        if (!isKnockedBack && !isDashing) ProcessInput();
        UpdateAnimation();
        if (Input.GetKeyDown(KeyCode.X)) TryCollectLeaf();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // ★ 무적 상태가 아니거나 대시 중일 때만 상호작용
            if (isDashing || !isInvincible) HandleEnemyCollision(other.gameObject);
        }
    }


    // ★ 핵심 수정: 코루틴 순서 변경
    void HandleEnemyCollision(GameObject enemyObj)
    {
        EnemyStats es = enemyObj.GetComponentInParent<EnemyStats>();
        Rigidbody2D erb = enemyObj.GetComponentInParent<Rigidbody2D>();
        Vector2 dirToEnemy = (Vector2)(enemyObj.transform.position - transform.position).normalized;

        if (isDashing)
        {
            // 1. 데미지 적용
            if (es != null) es.TakeDamage(myStats.TotalAttack);
            
            // 2. 적에게 넉백 적용
            if (erb != null) {
                BaseEnemyAI enemyAI = enemyObj.GetComponentInParent<BaseEnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.ApplyKnockback(dirToEnemy * recoilPower, 0.4f);
                }
            }

            // 3. 나에게 반동 적용 (여기서 StopAllCoroutines가 발동되어 청소됨)
            float recoilPushX = (transform.position.x < enemyObj.transform.position.x) ? -1f : 1f;
            Vector2 recoilDir = new Vector2(recoilPushX, 0f).normalized; 
            ApplyRecoil(recoilDir * recoilPower);

            // 4. ★ 청소가 끝난 '후에' 충돌 무시 코루틴을 실행! (그래야 안 죽음)
            if (erb != null) {
                StartCoroutine(IgnoreCollisionRoutine(enemyObj.GetComponent<Collider2D>()));
            }
        }
        else if (!isInvincible)
        {
            float dmg = (es != null) ? es.attackDamage : 10f;
            if (myStats != null) myStats.TakeDamage(dmg);

            float pushX = (transform.position.x > enemyObj.transform.position.x) ? 1f : -1f;
            ApplyKnockback(new Vector2(pushX, 1.5f).normalized * knockbackPower);
        }
    }

    // (IgnoreCollisionRoutine 및 나머지 함수 생략 없이 유지)
    IEnumerator IgnoreCollisionRoutine(Collider2D c, float d = 0.5f) { if (c != null) { Collider2D myCol = GetComponent<Collider2D>(); Physics2D.IgnoreCollision(myCol, c, true); yield return new WaitForSeconds(d); if (c != null) Physics2D.IgnoreCollision(myCol, c, false); } }
    void CheckGround() { if (jumpCooldown > 0) { isGrounded = false; return; } Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f; RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, castDistance + 0.3f, groundLayer); isGrounded = hit.collider != null; if (isGrounded) surfaceNormal = hit.normal; else surfaceNormal = Vector2.up; transform.rotation = Quaternion.identity; }
    void ProcessInput() { if (Input.GetKeyDown(KeyCode.Z) && canDash) { StartCoroutine(DashRoutine()); return; } float moveInput = Input.GetAxisRaw("Horizontal"); if (Input.GetButtonDown("Jump") && isGrounded) { jumpCooldown = 0.2f; isGrounded = false; rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); anim.SetTrigger("DoJump"); return; } rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y); if (moveInput > 0) sr.flipX = false; else if (moveInput < 0) sr.flipX = true; }
    void UpdateAnimation() { anim.SetFloat("Speed", rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.magnitude : 0f); anim.SetBool("IsGrounded", isGrounded); anim.SetFloat("VerticalSpeed", rb.linearVelocity.y); }
    IEnumerator DashRoutine() { canDash = false; isDashing = true; float origGrav = rb.gravityScale; rb.gravityScale = 0f; float dashDir = sr.flipX ? -1f : 1f; rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f); anim.SetBool("IsDashing", true); anim.SetTrigger("DoAttack"); yield return new WaitForSeconds(dashDuration); isDashing = false; rb.gravityScale = origGrav; anim.SetBool("IsDashing", false); yield return new WaitForSeconds(dashCooldown); canDash = true; }
    public void ApplyKnockback(Vector2 f) 
    { 
        StopAllCoroutines(); 
        
        // ★ [핵심 추가] 코루틴이 강제 종료되면서 변수가 고장나는 현상 방지
        canDash = true;           // 대시 다시 가능하게 초기화
        isInvincible = false;     // 영구 무적 버그 방지
        if (sr != null) sr.color = Color.white; // 투명해진 채로 굳는 현상 방지

        isDashing = false; 
        anim.SetBool("IsDashing", false); 
        rb.gravityScale = defaultGravity;

        isKnockedBack = true; 
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(f, ForceMode2D.Impulse); 
        StartCoroutine(KnockbackRoutine(hitInvincibilityDuration)); 
    }

    public void ApplyRecoil(Vector2 f) 
    { 
        StopAllCoroutines(); 
        
        // ★ [핵심 추가] 코루틴이 강제 종료되면서 변수가 고장나는 현상 방지
        canDash = true;           
        isInvincible = false;     
        if (sr != null) sr.color = Color.white; 

        isDashing = false; 
        anim.SetBool("IsDashing", false); 
        rb.gravityScale = defaultGravity;

        isKnockedBack = true; 
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(f, ForceMode2D.Impulse); 
        StartCoroutine(RecoilRoutine(attackInvincibilityDuration)); 
    }
    IEnumerator KnockbackRoutine(float d) { isInvincible = true; yield return new WaitForSeconds(0.3f); isKnockedBack = false; float blink = Time.time + (d - 0.3f); while (Time.time < blink) { sr.color = new Color(1, 1, 1, 0.4f); yield return new WaitForSeconds(0.1f); sr.color = Color.white; yield return new WaitForSeconds(0.1f); } isInvincible = false; }
    IEnumerator RecoilRoutine(float d) { isInvincible = true; yield return new WaitForSeconds(0.1f); isKnockedBack = false; yield return new WaitForSeconds(d); isInvincible = false; sr.color = Color.white; }
    void TryCollectLeaf() { float r = 2.5f; Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, r); foreach (var hit in hits) { LeafItem leaf = hit.GetComponent<LeafItem>(); if (leaf != null) leaf.Collect(myStats); } }
}