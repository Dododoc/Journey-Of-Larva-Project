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
    public bool IsDashing => isDashing; // 외부에서 대시 여부를 읽을 수 있게 함

    [Header("Knockback & Invincibility")]
    public float knockbackPower = 10f;      
    public float recoilPower = 5f;          
    public float hitInvincibilityDuration = 1.5f;   
    public float attackInvincibilityDuration = 0.2f; 
    private bool isInvincible = false;      

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
        ProcessInput();
        UpdateAnimation();

        // ★ [추가] X 키를 눌러 아이템 수집 시도
        if (Input.GetKeyDown(KeyCode.X))
        {
            TryCollectLeaf();
        }
    }
    void TryCollectLeaf()
    {
        float collectRange = 2.5f; // 아이템을 인지하는 범위 (기호에 따라 조정 가능)
        
        // 플레이어 위치 중심으로 범위 내의 모든 콜라이더 검색
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, collectRange);
        
        foreach (var hitCollider in hitColliders)
        {
            // 충돌체에서 LeafItem 컴포넌트를 찾음
            LeafItem leaf = hitCollider.GetComponent<LeafItem>();
            
            if (leaf != null)
            {
                // 나뭇잎의 Collect 함수 호출 (PlayerStats를 인자로 전달)
                leaf.Collect(myStats); 
                
                // 한 번의 X 입력에 여러 개를 동시에 먹고 싶지 않다면 여기서 break;를 사용하세요.
                // 전체를 한 번에 먹고 싶다면 break 없이 진행합니다.
            }
        }
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

    IEnumerator DashRoutine()
    {
        // 레이어 번호를 미리 가져옵니다. (인스펙터에 설정한 이름과 똑같아야 합니다.)
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        canDash = false;
        isDashing = true;

        // ★ 1. 대시 시작: 몹과의 충돌을 허용함 (Ignore를 false로 설정)
        if (playerLayer != -1 && enemyLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }

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

        // ★ 2. 대시 종료: 다시 몹을 통과하도록 설정 (Ignore를 true로 설정)
        if (playerLayer != -1 && enemyLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }

        anim.SetBool("IsDashing", false);
        anim.ResetTrigger("DoAttack");

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    // --- 충돌 감지 로직 (수정됨) ---

    // 1. 처음 부딪혔을 때
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
            HandleEnemyCollision(collision.gameObject);
    }

    // ★ 2. [추가됨] 계속 붙어있을 때 (비비고 있을 때)
    // 이 코드가 있어야 이미 붙어있는 상태에서 Z키를 눌러도 공격이 들어갑니다.
    void OnCollisionStay2D(Collision2D collision)
    {
        // 나는 돌진 중인데 적이랑 붙어있다? -> 공격 처리!
        if (isDashing && collision.gameObject.CompareTag("Enemy"))
        {
            HandleEnemyCollision(collision.gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            HandleEnemyCollision(other.gameObject);
    }

    // ★ 3. [추가됨] 트리거 상태에서 붙어있을 때
    void OnTriggerStay2D(Collider2D other)
    {
        if (isDashing && other.CompareTag("Enemy"))
        {
            HandleEnemyCollision(other.gameObject);
        }
    }

    void HandleEnemyCollision(GameObject enemyObj)
{
    EnemyStats enemyStats = enemyObj.GetComponent<EnemyStats>(); //
    // ★ Vector3끼리의 뺄셈 결과를 (Vector2)로 형변환하여 모호성 제거
    Vector2 directionToEnemy = (Vector2)(enemyObj.transform.position - transform.position).normalized;

    if (isDashing)
    {
        if (enemyStats != null) 
        {
            float realDamage = (myStats != null) ? myStats.TotalAttack : 10f;
            enemyStats.TakeDamage(realDamage); //
        }

        // ★ 거미 체크 및 넉백 처리
        SpiderAI spider = enemyObj.GetComponent<SpiderAI>();
        if (spider != null)
        {
            spider.ApplyKnockback(new Vector2(recoilPower, 0)); //
        }
        else
        {
            Rigidbody2D erb = enemyObj.GetComponent<Rigidbody2D>();
            if (erb != null)
            {
                erb.linearVelocity = Vector2.zero;
                erb.AddForce(directionToEnemy * recoilPower, ForceMode2D.Impulse);
            }
        }

        // ★ 에러가 났던 지점: (Vector2)를 추가하여 Vector2 + Vector2 연산으로 만듦
        Vector2 recoilDir = -(Vector2)(enemyObj.transform.position - transform.position).normalized + Vector2.up * 0.5f;
        ApplyRecoil(recoilDir.normalized * recoilPower);
    }
    else
    {
        if (isInvincible) return; //

        float damage = (enemyStats != null) ? enemyStats.attackDamage : 10f; //
        if (myStats != null) myStats.TakeDamage(damage); //

        float pushDirX = (transform.position.x > enemyObj.transform.position.x) ? 1f : -1f;
        Vector2 knockbackDir = new Vector2(pushDirX, 1.5f).normalized;
        ApplyKnockback(knockbackDir * knockbackPower); //
    }
}

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
        // 반동 시 대시 코루틴 강제 종료 및 상태 초기화
        StopAllCoroutines(); 
        
        isDashing = false;
        anim.SetBool("IsDashing", false); 

        // 반동은 잠깐 멈칫하는 것
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
        isInvincible = true; // 반동 중 잠깐 무적
        yield return new WaitForSeconds(0.1f);
        isKnockedBack = false; 

        yield return new WaitForSeconds(duration);

        isInvincible = false;
        sr.color = Color.white;
        canDash = true; // 다시 대시 가능
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 2.5f);
    }
}