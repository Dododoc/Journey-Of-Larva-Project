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

    [Header("Ultimate Skill Settings (V키)")]
    public float ultDamage = 100f;         // 궁극기 어마어마한 데미지
    public float ultDashSpeed = 20f;       // 1. 돌진 속도
    public float ultLiftHeight = 6f;       // 2. 하늘로 쳐올리는 높이
    public float ultPlungeSpeed = 40f;     // 4. 내리꽂는 엄청난 속도
    public float ultAoeRadius = 5f;        // 5. 바닥 강타 시 주변 광역 데미지 범위
    public float ultCooldown = 15f;        // 궁극기 쿨타임
    private bool canUltimate = true;
    [Header("Ultimate Skill Timings (궁극기 타이밍)")]
    public float ultLiftAnimTime = 0.4f;   // 쳐올리기 모션을 끝까지 보여줄 시간
    public float ultJumpAnimTime = 0.3f;   // 숙이고 점프하는 모션을 보여줄 시간
    public float ultRecoveryTime = 0.8f;   // 바닥에 꽂고 튕겨 오른 뒤, 무적을 유지하며 일어나는 시간

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
    private bool isAttacking = false;
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
         // 4. 궁극기 (V키)
        if (Input.GetKeyDown(KeyCode.V) && canUltimate && isGrounded && !isAttacking) 
        {
            StartCoroutine(UltimateSkillRoutine());
        }
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

    // ★ [수정됨] 일반 공격 루틴: basicKnockback 변수 대신 0f를 강제로 전달하여 넉백 무시
    IEnumerator BasicAttackRoutine() 
    { 
        isBasicAttacking = true; 
        anim.SetTrigger("DoAttack"); 
        yield return new WaitForSeconds(attackDelay); 
        
        // 여기에 0f를 넣어서 일반 공격은 넉백 힘이 0이 되도록 만듭니다.
        ApplyDamage(attackPoint.position, attackRange, 1f, 0f); 
        
        yield return new WaitForSeconds(attackCooldown); 
        isBasicAttacking = false; 
    }
    // ★ [수정] 들어 넘기기 스킬 (시작 즉시 무적 + 종료 후 3초 무적)
    IEnumerator LiftSkillRoutine() 
    { 
        canLift = false; 
        isLifting = true; 
        isInvincible = true;
        rb.gravityScale = 0f; 
        rb.linearVelocity = Vector2.zero; 
        anim.SetTrigger("DoLift"); 
        
        yield return new WaitForSeconds(liftCatchDelay); 
        
        Collider2D hitEnemy = Physics2D.OverlapCircle(attackPoint.position, liftRange, enemyLayers); 
        
        // =========================================================
        // [추가된 부분] 거미인지 먼저 확인하고, 거미라면 여기서 처리 후 스킬 종료
        // =========================================================
        if (hitEnemy != null)
        {
            SpiderAI spider = hitEnemy.GetComponentInParent<SpiderAI>();
            if (spider != null)
            {
                // 1. 데미지 주기
                EnemyStats es = hitEnemy.GetComponentInParent<EnemyStats>();
                if (es != null) es.TakeDamage(liftDamage);
                
                // 2. 거미 전용 흔들림(넉백 대체) 효과 실행
                spider.ApplyKnockback(Vector2.right * 5f);

                // 3. 풍뎅이 상태 원상복구 (스킬 종료 처리)
                isLifting = false; 
                rb.gravityScale = defaultGravity; 
                StartCoroutine(InvincibilityRoutine(2.0f));
                
                yield return new WaitForSeconds(liftCooldown); 
                canLift = true;
                
                // ★ 여기서 코루틴을 끝내서 아래의 "들기(Lift)" 로직이 실행되지 않게 함
                yield break; 
            }
        }
        // =========================================================

        // [기존 코드 유지] 보스 및 일반 몹 처리를 위한 변수 선언 (절대 지우지 마세요!)
        Rigidbody2D targetRb = null; 
        Collider2D targetCol = null; 
        BossMantis bossScript = null; 
        
        if (hitEnemy != null) { 
            targetRb = hitEnemy.GetComponentInParent<Rigidbody2D>(); 
            targetCol = hitEnemy.GetComponent<Collider2D>(); // 충돌 무시는 해당 콜라이더와 직접
            bossScript = hitEnemy.GetComponentInParent<BossMantis>();
            
            if (targetRb != null) { 
                if (bossScript != null) bossScript.SetGrabbedState(true); 
                targetRb.linearVelocity = Vector2.zero; 
                targetRb.bodyType = RigidbodyType2D.Kinematic; 
                
                // 부모(본체)를 이동시킴
                targetRb.transform.position = holdPoint.position; 
                targetRb.transform.parent = holdPoint; 
                if (targetCol != null) Physics2D.IgnoreCollision(myCollider, targetCol, true); 
            }
        } 
        
        yield return new WaitForSeconds(liftThrowDelay - liftCatchDelay); 
        
        if (hitEnemy != null && targetRb != null) 
{ 
    targetRb.transform.parent = null; 
    
    // [보스 처리]
    if (bossScript != null) bossScript.SetThrownState(); 
    
    // 던지는 방향 계산
    float throwDirX = isFacingRight ? -1f : 1f; 
    Vector2 throwDir = new Vector2(throwDirX, 1.0f).normalized; 
    Vector2 finalForce = throwDir * liftThrowForce;

    // ★ [수정] AI 여부와 상관없이 무조건 물리 엔진(Dynamic)을 다시 켜줍니다.
    targetRb.bodyType = RigidbodyType2D.Dynamic;

    BaseEnemyAI enemyAI = targetRb.GetComponent<BaseEnemyAI>();
    if (enemyAI != null)
    {
        enemyAI.OnThrown(finalForce); 
    }
    else
    {
        targetRb.AddForce(finalForce, ForceMode2D.Impulse); 
    }
    
    // (데미지 주는 코드 등 나머지는 그대로 유지...)
    EnemyStats es = targetRb.GetComponent<EnemyStats>(); 
    float totalDmg = (myStats != null) ? myStats.TotalAttack + liftDamage : 30f; 
    if (es != null) es.TakeDamage(totalDmg); 
    StartCoroutine(IgnoreCollisionRoutine(targetCol, 1.0f)); 
}
        
        yield return new WaitForSeconds(0.3f); 
        isLifting = false; 
        rb.gravityScale = defaultGravity; 
        StartCoroutine(InvincibilityRoutine(2.0f));
        yield return new WaitForSeconds(liftCooldown); 
        canLift = true; 
    }

    IEnumerator InvincibilityRoutine(float duration) { isInvincible = true; yield return new WaitForSeconds(duration); isInvincible = false; }
    IEnumerator DiveSkillRoutine() { canDive = false; isDiving = true; isInvincible = true; anim.SetTrigger("DoDive"); rb.gravityScale = 0; float xDir = isFacingRight ? 1f : -1f; Vector2 diveDirection = new Vector2(xDir, -0.55f).normalized; rb.linearVelocity = diveDirection * diveSpeed; float timer = 0f; while(isDiving && timer < 2.5f) { timer += Time.deltaTime; yield return null; } if (isDiving) { isDiving = false; isInvincible = false; rb.gravityScale = defaultGravity; canDive = true; } }
    IEnumerator DiveImpactRoutine() { if (!isDiving) yield break; rb.linearVelocity = Vector2.zero; rb.gravityScale = defaultGravity; anim.SetTrigger("DoImpact"); yield return new WaitForSeconds(impactDelay1); StartCoroutine(FlashGoldEffect()); PerformAreaDamage(impactDamage1, impactKnockback1); yield return new WaitForSeconds(impactDelay2); PerformAreaDamage(impactDamage2, impactKnockback2); float usedTime = impactDelay1 + impactDelay2; float remainingTime = emergeAnimDuration - usedTime; if (remainingTime > 0) yield return new WaitForSeconds(remainingTime); isDiving = false; StartCoroutine(PostDiveInvincibility(1.5f)); yield return new WaitForSeconds(diveCooldown); canDive = true; }
    IEnumerator PostDiveInvincibility(float duration) { yield return new WaitForSeconds(duration); isInvincible = false; }
    IEnumerator UltimateSkillRoutine()
    {
        canUltimate = false;
        isAttacking = true;
        isInvincible = true; // 시전 중 무적
        
        // 중력을 끄고 이동을 멈춤
        rb.linearVelocity = Vector2.zero;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; 

        // ==================================================
        // [1단계] 돌진 (Ult_Charge)
        // ==================================================
        anim.SetTrigger("Ult_Charge");
        
        float dashTimer = 0f;
        Collider2D caughtEnemy = null;
        BaseEnemyAI caughtAI = null;
        float faceDir = isFacingRight ? 1f : -1f;

        // 최대 0.6초 동안 앞으로 빠르게 돌진하며 적을 찾음
        while(dashTimer < 0.6f)
        {
            rb.linearVelocity = new Vector2(faceDir * ultDashSpeed, 0);
            
            // 내 앞의 적 감지
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
            foreach(var hit in hits) 
            {
                BaseEnemyAI ai = hit.GetComponentInParent<BaseEnemyAI>();
                if(ai != null) 
                {
                    caughtEnemy = hit;
                    caughtAI = ai;
                    break;
                }
            }

            if(caughtAI != null) break; // 적을 잡았으면 돌진 종료

            dashTimer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        // 만약 아무도 못 맞췄다면? 뻘쭘하게 스킬 취소
        if(caughtAI == null) 
        {
            rb.gravityScale = originalGravity;
            isAttacking = false;
            isInvincible = false;
            yield return new WaitForSeconds(ultCooldown);
            canUltimate = true;
            yield break;
        }

        // ==================================================
        // [2단계] 쳐올리기 (Ult_Lift) 및 점프 대기
        // ==================================================
        caughtAI.SetGrabbed(true); 
        anim.SetTrigger("Ult_Lift");
        
        // ★ [핵심 1] 쳐올리는(Lift) 애니메이션이 멋지게 재생될 시간을 벌어줍니다.
        yield return new WaitForSeconds(ultLiftAnimTime);

        // (애니메이터에서 Has Exit Time으로 인해 자동으로 Jump 모션으로 넘어갑니다)
        
        // ★ [핵심 2] 숙이고 점프(Jump)하며 땅을 박차는 모션이 나올 때까지 대기합니다.
        yield return new WaitForSeconds(ultJumpAnimTime);

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(0, ultLiftHeight, 0);
        
        // 대기가 끝났으니 풍뎅이와 적이 동시에 하늘로 솟아오름!
        float liftTimer = 0f;
        while(liftTimer < 0.4f) 
        {
            liftTimer += Time.deltaTime;
            float t = liftTimer / 0.4f;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            
            if(caughtEnemy != null) 
                caughtEnemy.transform.parent.position = transform.position + new Vector3(faceDir * 1.5f, 1f, 0); 
            
            yield return null;
        }

        // ==================================================
        // [3단계] 공중 솟구침 - 앞면 (Ult_Up)
        // ==================================================
        anim.SetTrigger("Ult_Up");
        
        // 적의 뒤통수, 살짝 위쪽으로 순식간에 휙! 순간이동
        if (caughtEnemy != null) 
        {
            transform.position = caughtEnemy.transform.parent.position + new Vector3(-faceDir * 1f, 2.5f, 0);
            // 풍뎅이가 적을 바라보도록 뱡향 전환
            Flip(); 
        }
        
        // 공중에서 날개를 파닥거리며 위엄있게 잠시 체공
        yield return new WaitForSeconds(0.5f); 

        // ==================================================
        // [4단계] 하강 뿔 꽂기 - 뒷모습 (Ult_Down)
        // ==================================================
        anim.SetTrigger("Ult_Down");
        yield return new WaitForSeconds(0.2f); // 내리꽂기 직전 딜레이

        while(true) 
        {
            // 한 프레임당 떨어질 거리 계산
            float dropStep = ultPlungeSpeed * Time.deltaTime;
            
            // ★ [수정 1] 낙하 속도가 아무리 빨라도 땅을 뚫지 못하도록 레이저 길이를 '이동할 거리'만큼 길게 늘림
            RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, dropStep + 1.0f, groundLayer);
            
            if(groundHit.collider != null) 
            {
                // 땅을 발견하면 뚫고 들어가지 않고, 바닥 높이에 정확히 주차함!
                transform.position = new Vector3(transform.position.x, groundHit.point.y + 0.5f, transform.position.z);
                break;
            }

            transform.Translate(Vector3.down * dropStep);
            
            if(caughtEnemy != null) 
                caughtEnemy.transform.parent.position = transform.position + new Vector3(0, -1.5f, 0);

            yield return null;
        }

        // ==================================================
        // [5단계] 바닥 강타 및 광역 데미지
        // ==================================================

        Collider2D[] aoeHits = Physics2D.OverlapCircleAll(transform.position, ultAoeRadius, enemyLayers);
        foreach(var hit in aoeHits) 
        {
            EnemyStats es = hit.GetComponentInParent<EnemyStats>();
            if(es != null) es.TakeDamage(ultDamage);
            
            BaseEnemyAI ai = hit.GetComponentInParent<BaseEnemyAI>();
            if(ai != null && ai != caughtAI) 
            {
                 float dirX = (hit.transform.position.x - transform.position.x) > 0 ? 1f : -1f;
                 ai.ApplyKnockback(new Vector2(dirX, 1f).normalized * 15f, 2.0f); // 주변 몹 넉백 및 2초 기절
            }
        }

        // 메인 타겟(잡힌 몹) 처리
        if(caughtAI != null) 
        {
            caughtAI.SetGrabbed(false);
            
            // ★ [수정 2] 잡혀있던 몹에게도 확실하게 데미지를 입힘
            EnemyStats caughtStats = caughtEnemy.GetComponentInParent<EnemyStats>();
            if(caughtStats != null) caughtStats.TakeDamage(ultDamage);
            
            // 몹은 2초 동안 기절하며 바닥에 튕김
            caughtAI.ApplyKnockback(new Vector2(-faceDir, 0.5f).normalized * 8f, 2.0f);
        }

        // ==================================================
        // [마무리] 스킬 종료 및 뒷수습 (무적 시간 연장)
        // ==================================================
        rb.gravityScale = originalGravity; 
        rb.linearVelocity = Vector2.zero;
        
        float recoilForceX = 8f;  
        float recoilForceY = 15f; 
        rb.AddForce(new Vector2(faceDir * recoilForceX, recoilForceY), ForceMode2D.Impulse); 

        // ★ [핵심 3] 튕겨 올라갔다가 착지하고 정신 차릴 때까지 대기 (이때까지 계속 무적!)
        yield return new WaitForSeconds(ultRecoveryTime); 
        
        // 대기 시간이 모두 끝나면 스킬 상태와 무적을 해제
        isAttacking = false;
        isInvincible = false;

        yield return new WaitForSeconds(ultCooldown);
        canUltimate = true;
    }
    // ★ [수정] 중복 데미지 방지 (HashSet 사용)
    void PerformAreaDamage(float addDamage, float knockback) 
    { 
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, impactRadius, enemyLayers);
        HashSet<GameObject> hitSet = new HashSet<GameObject>();

        foreach (Collider2D enemy in hitEnemies) 
        { 
            // 부모 오브젝트를 기준으로 중복 체크
            GameObject parentObj = enemy.transform.parent != null ? enemy.transform.parent.gameObject : enemy.gameObject;
            if(hitSet.Contains(parentObj)) continue; 
            hitSet.Add(parentObj);

            // 1. 데미지 전달 (부모 참조)
            EnemyStats es = enemy.GetComponentInParent<EnemyStats>(); 
            float finalDmg = (myStats != null) ? myStats.TotalAttack + addDamage : 30f; 
            if (es != null) es.TakeDamage(finalDmg);
            
            // 2. 넉백 처리 (부모 Rigidbody 참조)
            SpiderAI spider = enemy.GetComponentInParent<SpiderAI>();
            if (spider != null) spider.ApplyKnockback(new Vector2(knockback, 0)); 
            BaseEnemyAI enemyAI = enemy.GetComponentInParent<BaseEnemyAI>();
            if (enemyAI != null) {
                Vector2 dir = (enemy.transform.position - transform.position).normalized;
                if(knockback > 10f) dir += Vector2.up * 0.1f; // 강한 공격일 때 위로 더 띄움
                enemyAI.ApplyKnockback(dir.normalized * knockback, 1f); // 0.5초간 AI 정지
            }
        } 
    }
    // ★ [수정됨] 중복 데미지 방지 및 넉백 조건 추가
    void ApplyDamage(Vector2 point, float range, float multiplier, float knockbackForce) 
    { 
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point, range, enemyLayers);
        float baseDmg = (myStats != null) ? myStats.TotalAttack : attackDamage;
        float finalDmg = baseDmg * multiplier; 
        
        HashSet<GameObject> damagedEnemies = new HashSet<GameObject>();

        foreach (Collider2D enemy in hitEnemies) 
        { 
            GameObject parentObj = enemy.transform.parent != null ? enemy.transform.parent.gameObject : enemy.gameObject;
            if (damagedEnemies.Contains(parentObj)) continue;
            damagedEnemies.Add(parentObj);

            // 1. 데미지는 무조건 적용
            EnemyStats es = enemy.GetComponentInParent<EnemyStats>();
            if (es != null) es.TakeDamage(finalDmg);
            
            // 2. ★ 넉백 힘(knockbackForce)이 0보다 클 때만 밀어내고 경직(Stun) 상태를 줍니다!
            if (knockbackForce > 0f)
            {
                SpiderAI spider = enemy.GetComponentInParent<SpiderAI>();
                if (spider != null) spider.ApplyKnockback(new Vector2(knockbackForce, 0)); 
                
                BaseEnemyAI enemyAI = enemy.GetComponentInParent<BaseEnemyAI>();
                if (enemyAI != null) {
                    float dirX = (enemy.transform.position.x - transform.position.x) > 0 ? 1f : -1f;
                    Vector2 kDir = new Vector2(dirX, 0.1f).normalized; 
                    enemyAI.ApplyKnockback(kDir * knockbackForce, 1f); 
                }
            }
        } 
    }
    void OnDrawGizmos() { if (isGrounded) Gizmos.color = Color.green; else Gizmos.color = Color.red; Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.4f; Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize); if (attackPoint != null) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(attackPoint.position, attackRange); } if (holdPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(holdPoint.position, 0.3f); } }
}