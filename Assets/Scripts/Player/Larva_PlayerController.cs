using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Attack Settings (Dash)")]
    public float dashSpeed = 15f;     // 돌격 속도
    public float dashDuration = 0.4f; // 돌격 지속 시간
    public float dashCooldown = 1f;   // 쿨타임
    // public float playerAttackDamage = 10f; // <-- 삭제함! (이제 고정 데미지 안 씀)
    private bool isDashing = false;   // 현재 돌격 중인가?
    private bool canDash = true;      // 쿨타임이 끝났는가?

    [Header("Knockback & Invincibility")]
    public float knockbackPower = 10f;      // 피격 넉백
    public float recoilPower = 5f;          // 공격 반동
    public float hitInvincibilityDuration = 1.5f;   // 피격 무적 시간
    public float attackInvincibilityDuration = 0.2f; // 공격 무적 시간
    private bool isInvincible = false;      

    [Header("Ground Check (BoxCast)")]
    public Vector2 boxSize = new Vector2(0.8f, 0.2f);
    public float castDistance = 0.2f;
    public LayerMask groundLayer;

    // 내부 변수들
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private PlayerStats myStats; // ★ 내 스탯 (여기서 공격력을 가져올 것임)

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
        if (isKnockedBack || isDashing) return;

        if (Input.GetKeyDown(KeyCode.Z) && canDash)
        {
            StartCoroutine(DashRoutine());
            return; 
        }

        float moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpCooldown = 0.2f;
            isGrounded = false;
            rb.gravityScale = defaultGravity;
            
            // Unity 6: linearVelocity, 구버전: velocity
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
        else if (!isGrounded)
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;
    }

    void UpdateAnimation()
    {
        anim.SetFloat("Speed", rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.magnitude : 0f);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }

    // --- [기능 1] 돌격 공격 코루틴 ---
    IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        float dashDir = sr.flipX ? -1f : 1f;
        float originalGravity = rb.gravityScale;
        
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        anim.SetBool("IsDashing", true); 
        anim.ResetTrigger("DoAttack");
        anim.SetTrigger("DoAttack");

        yield return new WaitForSeconds(dashDuration);

        if (isDashing) 
        {
            rb.gravityScale = originalGravity;
            rb.linearVelocity = Vector2.zero;
            isDashing = false;
        }

        anim.SetBool("IsDashing", false); 
        anim.ResetTrigger("DoAttack");

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // --- [기능 2] 충돌 처리 로직 ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
            HandleEnemyCollision(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            HandleEnemyCollision(other.gameObject);
    }

    // Larva_PlayerController.cs 내부의 함수 수정

    void HandleEnemyCollision(GameObject enemyObj)
    {
        if (isInvincible) return;

        EnemyStats enemyStats = enemyObj.GetComponent<EnemyStats>();
        
        // 상황 A: 돌격 공격 성공 (변화 없음)
        if (isDashing)
        {
            if (enemyStats != null) 
            {
                float realDamage = (myStats != null) ? myStats.TotalAttack : 10f;
                enemyStats.TakeDamage(realDamage);
            }
            // 반동도 살짝 위로 튀게 수정
            float recoilX = (transform.position.x > enemyObj.transform.position.x) ? 1f : -1f;
            Vector2 recoilDir = new Vector2(recoilX, 1.0f).normalized;
            ApplyRecoil(recoilDir * recoilPower);
        }
        // ★ 상황 B: 피격 (여기를 수정!)
        else
        {
            float damage = (enemyStats != null) ? enemyStats.attackDamage : 10f;
            if (myStats != null) 
                myStats.TakeDamage(damage);

            // 적 기준 반대 방향 X값 추출
            float pushDirX = (transform.position.x > enemyObj.transform.position.x) ? 1f : -1f;

            // ★ 공중으로 붕 뜨게 Y값을 1.5f로 설정
            Vector2 knockbackDir = new Vector2(pushDirX, 1.5f).normalized;
            
            ApplyKnockback(knockbackDir * knockbackPower);
        }
    }

    // --- [기능 3] 넉백 및 무적 적용 함수들 ---

    public void ApplyKnockback(Vector2 force)
    {
        StopAllCoroutines();
        
        isDashing = false;
        anim.SetBool("IsDashing", false); 

        isKnockedBack = true;
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(force, ForceMode2D.Impulse);

        StartCoroutine(KnockbackRoutine(hitInvincibilityDuration));
    }

    public void ApplyRecoil(Vector2 force)
    {
        StopAllCoroutines(); 
        
        isDashing = false;
        anim.SetBool("IsDashing", false); 

        isKnockedBack = true; 
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);

        StartCoroutine(RecoilRoutine(attackInvincibilityDuration));
    }

    IEnumerator KnockbackRoutine(float duration)
    {
        isInvincible = true; 
        yield return new WaitForSeconds(0.3f);
        isKnockedBack = false;

        float blinkEndTime = Time.time + (duration - 0.3f);
        while (Time.time < blinkEndTime)
        {
            sr.color = new Color(1, 1, 1, 0.4f); 
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;              
            yield return new WaitForSeconds(0.1f);
        }

        isInvincible = false;
        canDash = true; 
    }

    IEnumerator RecoilRoutine(float duration)
    {
        isInvincible = true; 
        yield return new WaitForSeconds(0.1f);
        isKnockedBack = false; 

        yield return new WaitForSeconds(duration);

        isInvincible = false;
        sr.color = Color.white;
        canDash = true; 
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize);
    }
}