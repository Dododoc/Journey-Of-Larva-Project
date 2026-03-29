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

    // ★ [추가됨] 억울하게 다른 코루틴이 꺼지지 않도록, 대시와 상태이상 전용 보관함을 만듭니다.
    private Coroutine dashCoroutine;
    private Coroutine stateCoroutine;

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
            if (isDashing || !isInvincible) HandleEnemyCollision(other.gameObject);
        }
    }

    void HandleEnemyCollision(GameObject enemyObj)
    {
        EnemyStats es = enemyObj.GetComponentInParent<EnemyStats>();
        Rigidbody2D erb = enemyObj.GetComponentInParent<Rigidbody2D>();
        Vector2 dirToEnemy = (Vector2)(enemyObj.transform.position - transform.position).normalized;

        if (isDashing)
        {
            if (es != null) es.TakeDamage(myStats.TotalAttack);
            
            if (erb != null) {
                BaseEnemyAI enemyAI = enemyObj.GetComponentInParent<BaseEnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.ApplyKnockback(dirToEnemy * recoilPower, 0.4f);
                }
            }

            float recoilPushX = (transform.position.x < enemyObj.transform.position.x) ? -1f : 1f;
            Vector2 recoilDir = new Vector2(recoilPushX, 0f).normalized; 
            ApplyRecoil(recoilDir * recoilPower);

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

    IEnumerator IgnoreCollisionRoutine(Collider2D c, float d = 0.5f) { if (c != null) { Collider2D myCol = GetComponent<Collider2D>(); Physics2D.IgnoreCollision(myCol, c, true); yield return new WaitForSeconds(d); if (c != null) Physics2D.IgnoreCollision(myCol, c, false); } }
    void CheckGround() { if (jumpCooldown > 0) { isGrounded = false; return; } Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f; RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, castDistance + 0.3f, groundLayer); isGrounded = hit.collider != null; if (isGrounded) surfaceNormal = hit.normal; else surfaceNormal = Vector2.up; transform.rotation = Quaternion.identity; }
    
    void ProcessInput() 
    { 
        if (Input.GetKeyDown(KeyCode.Z) && canDash) 
        { 
            // ★ [수정됨] 대시를 시작할 때 보관함에 담아서 실행합니다.
            if (dashCoroutine != null) StopCoroutine(dashCoroutine);
            dashCoroutine = StartCoroutine(DashRoutine()); 
            return; 
        } 
        float moveInput = Input.GetAxisRaw("Horizontal"); 
        if (Input.GetButtonDown("Jump") && isGrounded) { jumpCooldown = 0.2f; isGrounded = false; rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); anim.SetTrigger("DoJump"); return; } 
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y); 
        if (moveInput > 0) sr.flipX = false; else if (moveInput < 0) sr.flipX = true; 
    }

    void UpdateAnimation() { anim.SetFloat("Speed", rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.magnitude : 0f); anim.SetBool("IsGrounded", isGrounded); anim.SetFloat("VerticalSpeed", rb.linearVelocity.y); }
    IEnumerator DashRoutine() { canDash = false; isDashing = true; float origGrav = rb.gravityScale; rb.gravityScale = 0f; float dashDir = sr.flipX ? -1f : 1f; rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f); anim.SetBool("IsDashing", true); anim.SetTrigger("DoAttack"); yield return new WaitForSeconds(dashDuration); isDashing = false; rb.gravityScale = origGrav; anim.SetBool("IsDashing", false); yield return new WaitForSeconds(dashCooldown); canDash = true; }
    
    public void ApplyKnockback(Vector2 f) 
    { 
        // ★ [핵심 수정] 무식한 StopAllCoroutines() 삭제! 대시와 상태이상만 콕 집어서 끕니다.
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        if (stateCoroutine != null) StopCoroutine(stateCoroutine);
        
        isInvincible = false;     
        if (sr != null) sr.color = Color.white; 

        isDashing = false; 
        anim.SetBool("IsDashing", false); 
        rb.gravityScale = defaultGravity;

        isKnockedBack = true; 
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(f, ForceMode2D.Impulse); 
        
        // ★ 넉백 코루틴을 보관함에 담아서 실행
        stateCoroutine = StartCoroutine(KnockbackRoutine(hitInvincibilityDuration)); 
        StartCoroutine(DashCooldownTimer()); 
    }

    public void ApplyRecoil(Vector2 f) 
    { 
        // ★ [핵심 수정] 여기도 똑같이 선택적 종료 적용!
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        if (stateCoroutine != null) StopCoroutine(stateCoroutine);
        
        isInvincible = false;     
        if (sr != null) sr.color = Color.white; 

        isDashing = false; 
        anim.SetBool("IsDashing", false); 
        rb.gravityScale = defaultGravity;

        isKnockedBack = true; 
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(f, ForceMode2D.Impulse); 
        
        // ★ 반동 코루틴을 보관함에 담아서 실행
        stateCoroutine = StartCoroutine(RecoilRoutine(attackInvincibilityDuration)); 
        StartCoroutine(DashCooldownTimer()); 
    }

    IEnumerator DashCooldownTimer() { yield return new WaitForSeconds(dashCooldown); canDash = true; }
    IEnumerator KnockbackRoutine(float d) { isInvincible = true; yield return new WaitForSeconds(0.3f); isKnockedBack = false; float blink = Time.time + (d - 0.3f); while (Time.time < blink) { sr.color = new Color(1, 1, 1, 0.4f); yield return new WaitForSeconds(0.1f); sr.color = Color.white; yield return new WaitForSeconds(0.1f); } isInvincible = false; }
    IEnumerator RecoilRoutine(float d) { isInvincible = true; yield return new WaitForSeconds(0.1f); isKnockedBack = false; yield return new WaitForSeconds(d); isInvincible = false; sr.color = Color.white; }
    void TryCollectLeaf() { float r = 2.5f; Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, r); foreach (var hit in hits) { LeafItem leaf = hit.GetComponent<LeafItem>(); if (leaf != null) leaf.Collect(myStats); } }
}