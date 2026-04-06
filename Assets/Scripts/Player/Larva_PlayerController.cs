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
    
    // ★ [핵심 1] 꼬이기 쉬운 canDash 변수를 아예 지워버리고, 
    // 오직 이 변수 하나로만 쿨타임을 완벽하게 통제합니다!
    private float currentDashTimer = 0f; 
    
    public bool IsDashing => isDashing;
    [Header("Skill UI")]
    public SkillSlotUI zSkillUI; // 유니티 에디터에서 애벌레 Z 슬롯을 드래그해서 넣습니다.

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

    private Coroutine dashCoroutine;
    private Coroutine stateCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myStats = GetComponent<PlayerStats>();
        defaultGravity = rb.gravityScale;
        
        // 주의: "Skill_X_Button" 같은 큰따옴표 안의 이름은 실제 하이어라키에 있는 UI 오브젝트 이름과 똑같아야 합니다!
        GameObject xObj = GameObject.Find("Skill_Z_Button"); 
        if (xObj != null) zSkillUI = xObj.GetComponent<SkillSlotUI>();
    }

    void Update()
    {
        // ★ [추가] 보스에게 잡혔다면 다른 모든 입력을 무시하고 가만히 있는다!
        if (myStats != null && myStats.isGrabbedByBoss) { UpdateAnimation(); return; }
        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;
        
        // ★ [핵심 2] 매 프레임마다 절대적으로 쿨타임을 줄여나갑니다. 유령 타이머가 낄 틈이 없습니다!
        if (currentDashTimer > 0) currentDashTimer -= Time.deltaTime;

        CheckGround();
        if (!isKnockedBack && !isDashing) ProcessInput();
        UpdateAnimation();
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
        if (Input.GetKeyDown(KeyCode.Z) && currentDashTimer <= 0f) 
        { 
            currentDashTimer = dashDuration + dashCooldown; 
            if (zSkillUI != null) zSkillUI.StartCooldown(currentDashTimer); 

            if (dashCoroutine != null) StopCoroutine(dashCoroutine);
            dashCoroutine = StartCoroutine(DashRoutine()); 
            return; 
        }
        
        float moveInput = Input.GetAxisRaw("Horizontal"); 

        // ★ [수정 1] 점프 조건에 !myStats.isJumpDisabled 추가
        if (Input.GetButtonDown("Jump") && isGrounded && !myStats.isJumpDisabled) 
        { 
            jumpCooldown = 0.2f; 
            isGrounded = false; 
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); 
            anim.SetTrigger("DoJump"); 
            return; 
        } 
        
        // ★ [수정 2] 속도 계산 시 myStats.speedMultiplier를 곱해줌
        float currentSpeed = moveSpeed * myStats.speedMultiplier;
        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y); 

        if (moveInput > 0) sr.flipX = false; 
        else if (moveInput < 0) sr.flipX = true; 
    }

    void UpdateAnimation() { anim.SetFloat("Speed", rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.magnitude : 0f); anim.SetBool("IsGrounded", isGrounded); anim.SetFloat("VerticalSpeed", rb.linearVelocity.y); }
    
    // ★ [핵심 4] 코루틴은 오직 '대시 이동' 자체만 담당하게 만듭니다. (쿨타임 관련 내용 싹 제거)
    IEnumerator DashRoutine() 
    { 
        isDashing = true; 
        isInvincible = true; // ★ [추가] 대시하는 동안 확실하게 무적 켜기!
        float origGrav = rb.gravityScale; 
        rb.gravityScale = 0f; 
        float dashDir = sr.flipX ? -1f : 1f; 
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f); 
        anim.SetBool("IsDashing", true); 
        anim.SetTrigger("DoAttack"); 
        
        yield return new WaitForSeconds(dashDuration); 
        
        isDashing = false; 
        isInvincible = false;

        rb.gravityScale = origGrav; 
        anim.SetBool("IsDashing", false); 
    }
    
    public void ApplyKnockback(Vector2 f) 
    { 
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
        
        stateCoroutine = StartCoroutine(KnockbackRoutine(hitInvincibilityDuration)); 
        // ★ 유령 타이머를 소환하던 StartCoroutine(DashCooldownTimer()); 삭제 완료!
    }

    public void ApplyRecoil(Vector2 f) 
    { 
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        if (stateCoroutine != null) StopCoroutine(stateCoroutine);
        isInvincible = true;
      
        if (sr != null) sr.color = Color.white; 

        isDashing = false; 
        anim.SetBool("IsDashing", false); 
        rb.gravityScale = defaultGravity;

        isKnockedBack = true; 
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(f, ForceMode2D.Impulse); 
        
        stateCoroutine = StartCoroutine(RecoilRoutine(attackInvincibilityDuration)); 
        // ★ 유령 타이머를 소환하던 StartCoroutine(DashCooldownTimer()); 삭제 완료!
    }

    // ★ DashCooldownTimer 코루틴 자체를 완전히 삭제했습니다.
    IEnumerator KnockbackRoutine(float d) { isInvincible = true; yield return new WaitForSeconds(0.3f); isKnockedBack = false; float blink = Time.time + (d - 0.3f); while (Time.time < blink) { sr.color = new Color(1, 1, 1, 0.4f); yield return new WaitForSeconds(0.1f); sr.color = Color.white; yield return new WaitForSeconds(0.1f); } isInvincible = false; }
    IEnumerator RecoilRoutine(float d) { isInvincible = true; yield return new WaitForSeconds(0.1f); isKnockedBack = false; yield return new WaitForSeconds(d); isInvincible = false; sr.color = Color.white; }
}