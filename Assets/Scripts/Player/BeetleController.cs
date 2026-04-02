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
    [Header("Skill UI")]
    public SkillSlotUI xSkillUI; // 들어 넘기기
    public SkillSlotUI cSkillUI; // 공중 다이브
    public SkillSlotUI vSkillUI; // 궁극기
    // [기존 코드 아래에 추가]
    public bool isUltUnlocked = false; // ★ 궁극기 해금 여부 (기본 false)
    
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
    // ★ [새로 추가] 상승 속도 조절 (숫자가 작을수록 엄청 빨리 올라감)
    public float ultMobLiftDuration = 0.5f;    // 몹이 하늘로 올라가는 데 걸리는 시간
    public float ultPlayerRiseDuration = 0.25f; // 풍뎅이가 하늘로 솟구치는 데 걸리는 시간

    [Header("Ultimate VFX (궁극기 이펙트)")]
    public GameObject goldAuraPrefab;    // 금빛 아우라
    public GameObject impactVFXPrefab;   // 땅 찍기 충격
    
    private GameObject currentAura;      // 생성된 아우라를 끄기 위해 기억해둘 변수 
    public TrailRenderer hornTrail;
    public float shakeDuration = 0.2f;  // 흔들리는 시간 (0.2초면 쾅! 하기에 충분합니다)
    public float shakeMagnitude = 0.5f; // 흔들리는 강도 (숫자가 클수록 격렬하게 흔들립니다)

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
        // 주의: "Skill_X_Button" 같은 큰따옴표 안의 이름은 실제 하이어라키에 있는 UI 오브젝트 이름과 똑같아야 합니다!
        GameObject xObj = GameObject.Find("Skill_X_Button"); 
        if (xObj != null) xSkillUI = xObj.GetComponent<SkillSlotUI>();

        GameObject cObj = GameObject.Find("Skill_C_Button");
        if (cObj != null) cSkillUI = cObj.GetComponent<SkillSlotUI>();

        GameObject vObj = GameObject.Find("Skill_V_Button");
        if (vObj != null) vSkillUI = vObj.GetComponent<SkillSlotUI>();
    }

    void Update()
    {
        // ★ [임시 해금 키] L키를 누르면 궁극기가 해금됩니다.
        if (Input.GetKeyDown(KeyCode.L) && !isUltUnlocked)
        {
            UnlockUltimate();
        }
        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;
        
        // ★ [핵심 1] isAttacking(궁극기 시전 중) 조건을 추가했습니다!
        // 이제 궁극기를 쓰는 동안에는 이동, 점프, 바닥 감지 등 모든 키보드 조작과 물리 연산이 완벽하게 '정지'됩니다.
        if (isKnockedBack || isDiving || isLifting || isGrabbedByBoss || isAttacking) 
        { 
            UpdateAnimation(); 
            return; 
        }

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
        // ★ ProcessInput() 안의 Z키 조건문 바로 아래에 있는 X, C, V 조건문들 덮어쓰기
        if (Input.GetKeyDown(KeyCode.Z) && !isBasicAttacking && isGrounded) StartCoroutine(BasicAttackRoutine());
        
        if (Input.GetKeyDown(KeyCode.X) && canLift && isGrounded) 
        { 
            if (xSkillUI != null) xSkillUI.StartCooldown(liftCooldown); // ★ UI 연동
            StartCoroutine(LiftSkillRoutine()); 
            return; 
        }
        
        if (Input.GetKeyDown(KeyCode.C) && canDive && !isGrounded) 
        { 
            if (cSkillUI != null) cSkillUI.StartCooldown(diveCooldown); // ★ UI 연동
            StartCoroutine(DiveSkillRoutine()); 
            return; 
        }
        
        // 5. 궁극기 (V키) - ★ isUltUnlocked 조건 추가
        if (Input.GetKeyDown(KeyCode.V) && canUltimate && isGrounded && !isAttacking && isUltUnlocked) 
        {
            if (vSkillUI != null) vSkillUI.StartCooldown(ultCooldown);
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
        // 공중 공격, 궁극기, 스킬 사용 중인지 통합 확인
        bool isCurrentlyAttacking = isAttacking || isBasicAttacking || isLifting || isDiving;
        
        // 애니메이터에 공격 상태를 넘겨줌 (Any State -> Fly 조건에 사용!)
        anim.SetBool("IsAttacking", isCurrentlyAttacking);

        // 공격/스킬 중이면 일반 걷기, 점프, 낙하 모션으로 절대 바뀌지 않음
        if (isCurrentlyAttacking) return; 

        if (isKnockedBack || isGrabbedByBoss)
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
        isBasicAttacking = false; 
        isLifting = false; 
        
        // ★ [핵심 1] 데미지를 받아 스킬이 강제 취소될 때, 다시 키보드 조작이 가능하도록 풀어줍니다!
        isAttacking = false; 

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
        
        float blinkEndTime = Time.time + (hitInvincibilityDuration - 0.2f);
        while (Time.time < blinkEndTime) { sr.color = new Color(1, 1, 1, 0.4f); yield return new WaitForSeconds(0.1f); sr.color = Color.white; yield return new WaitForSeconds(0.1f); }
        
        // ★ [핵심 2] 궁극기(isAttacking)나 스킬 사용 중이라면 무적을 강제로 끄지 않고 보호합니다!
        if (!isAttacking && !isLifting && !isDiving) 
        {
            isInvincible = false; 
        }
        
        canLift = true; canDive = true;
    }
    // ★ [수정됨] 일반 공격 루틴: basicKnockback 변수 대신 0f를 강제로 전달하여 넉백 무시
    IEnumerator BasicAttackRoutine() 
    { 
        isBasicAttacking = true; 
        anim.SetTrigger("DoAttack"); 
        yield return new WaitForSeconds(attackDelay); 
        
        // 여기에 0f를 넣어서 일반 공격은 넉백 힘이 0이 되도록 만듭니다.
        ApplyDamage(attackPoint.position, attackRange, 1f, basicKnockback); 
        
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

    IEnumerator InvincibilityRoutine(float duration) 
    { 
        isInvincible = true; 
        yield return new WaitForSeconds(duration); 
        
        // ★ 여기도 스킬 중첩 방어
        if (!isAttacking && !isLifting && !isDiving) isInvincible = false; 
    }
    IEnumerator DiveSkillRoutine() { canDive = false; isDiving = true; isInvincible = true; anim.SetTrigger("DoDive"); rb.gravityScale = 0; float xDir = isFacingRight ? 1f : -1f; Vector2 diveDirection = new Vector2(xDir, -0.55f).normalized; rb.linearVelocity = diveDirection * diveSpeed; float timer = 0f; while(isDiving && timer < 2.5f) { timer += Time.deltaTime; yield return null; } if (isDiving) { isDiving = false; isInvincible = false; rb.gravityScale = defaultGravity; canDive = true; } }
    IEnumerator DiveImpactRoutine() { if (!isDiving) yield break; rb.linearVelocity = Vector2.zero; rb.gravityScale = defaultGravity; anim.SetTrigger("DoImpact"); yield return new WaitForSeconds(impactDelay1); StartCoroutine(FlashGoldEffect()); PerformAreaDamage(impactDamage1, impactKnockback1); yield return new WaitForSeconds(impactDelay2); PerformAreaDamage(impactDamage2, impactKnockback2); float usedTime = impactDelay1 + impactDelay2; float remainingTime = emergeAnimDuration - usedTime; if (remainingTime > 0) yield return new WaitForSeconds(remainingTime); isDiving = false; StartCoroutine(PostDiveInvincibility(1.5f)); yield return new WaitForSeconds(diveCooldown); canDive = true; }
    IEnumerator PostDiveInvincibility(float duration) 
    { 
        yield return new WaitForSeconds(duration); 
        
        // ★ 여기도 스킬 중첩 방어
        if (!isAttacking && !isLifting && !isDiving) isInvincible = false; 
    }
    IEnumerator UltimateSkillRoutine()
    {
        canUltimate = false;
        isAttacking = true;
        isInvincible = true; 
        
        float originalGravity = rb.gravityScale;
        float baseY = transform.position.y; 
        // ★ [여기에 추가 1] 나중에 원상복구하기 위해 잔상의 원래 위치를 기억해둡니다!
        Vector3 originalTrailPos = Vector3.zero;
        if (hornTrail != null) originalTrailPos = hornTrail.transform.localPosition;
        // ==================================================
        // [1] 돌진 (땅을 밟고 달림)
        // ==================================================
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = originalGravity;
        if (myCollider != null) myCollider.enabled = true;

        anim.SetTrigger("Ult_Charge"); 
        // ★ [여기에 추가 1] V키를 누르고 돌진을 시작할 때 잔상을 켭니다!
        if (hornTrail != null) hornTrail.emitting = true;
        // ★ [여기로 이동!] 돌진을 시작하는 순간 금빛 아우라 폭발!
        if (goldAuraPrefab != null)
        {
            // ★ Quaternion.identity를 지우고, 프리팹의 원래 회전값(-90)을 그대로 가져오도록 수정합니다!
            currentAura = Instantiate(goldAuraPrefab, transform.position, goldAuraPrefab.transform.rotation);
            currentAura.transform.parent = transform;
            // ★ [안전 장치 추가] 엉뚱한 곳에서 켜지지 않게 풍뎅이 한가운데(0,0,0)로 좌표를 강제 고정합니다!
            currentAura.transform.localPosition = new Vector3(0f, 1f, 0f);

            // ★ [추가] 부모 크기에 짓눌린 아우라의 크기를 원래 크기(1,1,1)로 쫙 펴줍니다!
            currentAura.transform.localScale = Vector3.one;
        }

        float dashTimer = 0f;
        BaseEnemyAI caughtAI = null; 
        float faceDir = isFacingRight ? 1f : -1f;

        while(dashTimer < 0.6f)
        {
            rb.linearVelocity = new Vector2(faceDir * ultDashSpeed, rb.linearVelocity.y);
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
            foreach(var hit in hits) 
            {
                BaseEnemyAI ai = hit.GetComponentInParent<BaseEnemyAI>();
                if(ai != null) { caughtAI = ai; break; }
            }
            if(caughtAI != null) break; 
            dashTimer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        if(caughtAI == null) 
        {
            isAttacking = false;
            isInvincible = false;
            anim.Play("Beetle_Idle"); 

            if (currentAura != null) 
            {
                ParticleSystem auraPS = currentAura.GetComponent<ParticleSystem>();
                if (auraPS != null) 
                {
                    auraPS.Stop(); 
                    currentAura.transform.parent = null; 

                    // ★ [버그 수정] 왼쪽(-1)을 보다가 끊어져도 파티클이 찢어지지 않게 크기를 강제로 정상화(1) 시킵니다!
                    currentAura.transform.localScale = Vector3.one; 
                    currentAura.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // 하늘 방향 고정
                    
                    Destroy(currentAura, 1.5f); 
                }
                else 
                {
                    Destroy(currentAura);
                }
            }

            Invoke("ResetUltCooldown", ultCooldown);
            yield break;
        }

        // ==================================================
        // [2] 들어올리기 (★ 바닥 높이 유지 및 에러 방지 반영!)
        // ==================================================
        caughtAI.SetGrabbed(true); 
        rb.linearVelocity = Vector2.zero;
        
        if (caughtAI != null)
        {
            // X, Z는 뿔 위치로 오되, Y(높이)는 몹이 있던 바닥을 유지합니다.
            caughtAI.transform.position = new Vector3(holdPoint.position.x, caughtAI.transform.position.y, holdPoint.position.z);
            caughtAI.transform.parent = holdPoint; 
        }

        anim.SetTrigger("Ult_Lift"); 
        
        // ★ 대기 시간 0.8초 반영 완료
        yield return new WaitForSeconds(0.6f); 

        // ==================================================
        // [3] 몹 50 상승
        // ==================================================
        if (caughtAI != null) caughtAI.transform.parent = null; 

        Vector3 enemyStartPos = caughtAI != null ? caughtAI.transform.position : transform.position;
        Vector3 enemyTargetPos = new Vector3(enemyStartPos.x, baseY + 50f, enemyStartPos.z);

        float liftT = 0f;
        while(liftT < 0.5f) 
        {
            liftT += Time.deltaTime;
            if (caughtAI != null) 
            {
                caughtAI.transform.position = Vector3.Lerp(enemyStartPos, enemyTargetPos, liftT / 0.5f);
            }
            yield return null;
        }

        // ==================================================
        // [4] 점프 대기
        // ==================================================
        
        anim.SetTrigger("Ult_Jump"); 
        yield return new WaitForSeconds(1.5f);
        // ==================================================
        // ★ [새로운 연출] 기운을 사방으로 폭발시키며 점프!
        // ==================================================
        if (currentAura != null) 
        {
            ParticleSystem auraPS = currentAura.GetComponent<ParticleSystem>();
            if (auraPS != null) 
            {
                currentAura.transform.parent = null; 
                currentAura.transform.localScale = Vector3.one; // 찢어짐 방지

                // 1. 파티클이 뿜어지는 모양을 콘(위쪽)에서 구(사방)로 순식간에 변경!
                var shape = auraPS.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere; 
                shape.radius = 30f; // 퍼지는 범위

                // 2. 뿜어지는 속도를 엄청나게 빠르게(15) 올려서 폭발력을 줍니다!
                var main = auraPS.main;
                main.startSpeed = 30f; 

                // 3. 사방으로 50개의 파티클을 강제로 쾅! 쏘아냅니다! (숫자를 올리면 더 화려해집니다)
                auraPS.Emit(300); 
                
                // 4. 화려하게 터뜨린 후 스위치를 끕니다. (남은 잔해들은 스르륵 사라짐)
                auraPS.Stop(); 
                Destroy(currentAura, 1.5f); 
            }
        }

        // ==================================================
        // [5] 100으로 순간이동 후 2초 체공
        // ==================================================
        sr.enabled = false; 
        yield return new WaitForSeconds(0.1f); 

        rb.bodyType = RigidbodyType2D.Kinematic; 
        rb.gravityScale = 0f;
        if (myCollider != null) myCollider.enabled = false;

        anim.SetTrigger("Ult_Up"); 
        
        float targetX = caughtAI != null ? caughtAI.transform.position.x : transform.position.x;
        transform.position = new Vector3(targetX, baseY + 100f, 0);
        Flip(); 
        sr.enabled = true; 

        yield return new WaitForSeconds(2.0f); 

        // ==================================================
        // [6] 100에서 강하 (★ 수직 뿔 꽂기 오프셋 복구!)
        // ==================================================
        anim.SetTrigger("Ult_Down"); 
        // ★ [여기에 추가 2] 내리꽂는 모션에서는 뿔이 아래를 향하므로, 잔상의 위치도 아래(-1.5f)로 강제로 내립니다!
        if (hornTrail != null) 
        {
            hornTrail.transform.localPosition = new Vector3(0f, -1.5f, 0f); // 뿔이 있는 발밑 위치
        }
        bool hasCaughtEnemyInAir = false;
        float currentPlungeSpeed = ultPlungeSpeed; 
        
        // ★ 수직으로 내려찍을 때 몹이 꽂힐 정확한 뿔 위치 복구!
        Vector3 plungeHornOffset = new Vector3(0, -1.5f, 0);

        while(true) 
        {
            float dropStep = currentPlungeSpeed * Time.deltaTime;
            Vector3 currentPos = transform.position;
            Vector3 nextPos = currentPos + Vector3.down * dropStep;
            
            RaycastHit2D hit = Physics2D.Linecast(currentPos, nextPos + Vector3.down * 0.5f, groundLayer);
            
            if(hit.collider != null) 
            {
                transform.position = new Vector3(currentPos.x, hit.point.y + 0.5f, currentPos.z);
                break;
            }

            transform.position = nextPos;

            if(caughtAI != null) 
            {
                // 풍뎅이 본체가 아니라 뿔(plungeHornOffset)이 몹을 지나칠 때 낚아챔
                if (!hasCaughtEnemyInAir && (transform.position.y + plungeHornOffset.y) <= caughtAI.transform.position.y) 
                {
                    hasCaughtEnemyInAir = true; 
                }

                if (hasCaughtEnemyInAir)
                {
                    // 정확히 뿔 위치로 고정
                    caughtAI.transform.position = transform.position + plungeHornOffset;
                }
            }
            yield return null;
        }

        // ==================================================
        // [7] 바닥 강타 및 데미지
        // ==================================================
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = originalGravity;
        if (myCollider != null) myCollider.enabled = true;

        anim.Play("Beetle_Idle"); 
        // ★ [여기에 추가] 바닥에 쾅! 찍히는 순간 화면 번쩍임과 동시에 지진을 일으킵니다!
        if (UIManager.instance != null) UIManager.instance.ShowSlamFlash();

        StartCoroutine(CameraShakeRoutine(shakeDuration, shakeMagnitude));
        StartCoroutine(CameraShakeRoutine(shakeDuration, shakeMagnitude)); // <--- 이 줄을 추가!
        // ==================================================
        // ★ [추가할 부분] 실수로 지워졌던 돌덩이 폭발 이펙트 소환 코드를 여기에 다시 넣습니다!
        // ==================================================
        if (impactVFXPrefab != null)
        {
            Vector3 impactPos = transform.position + Vector3.down * 0.5f; // 풍뎅이 발밑 바닥 위치
            Instantiate(impactVFXPrefab, impactPos, Quaternion.identity);
        }

        Collider2D[] aoeHits = Physics2D.OverlapCircleAll(transform.position, ultAoeRadius, enemyLayers);
        HashSet<GameObject> hitSet = new HashSet<GameObject>(); 

        foreach(var hit in aoeHits) 
        {
            if (hit == null) continue;
            GameObject rootObj = hit.transform.parent != null ? hit.transform.parent.gameObject : hit.gameObject;
            if(hitSet.Contains(rootObj)) continue;
            hitSet.Add(rootObj);

            BaseEnemyAI ai = hit.GetComponentInParent<BaseEnemyAI>();
            EnemyStats es = hit.GetComponentInParent<EnemyStats>();

            if(ai != null && ai != caughtAI) 
            {
                 float dirX = (hit.transform.position.x - transform.position.x) > 0 ? 1f : -1f;
                 ai.ApplyKnockback(new Vector2(dirX, 1f).normalized * 15f, 2.0f);
            }
            if(es != null && (caughtAI == null || es.gameObject != caughtAI.gameObject)) 
            {
                es.TakeDamage(ultDamage);
            }
        }

        if(caughtAI != null) 
        {
            caughtAI.SetGrabbed(false);
            caughtAI.ApplyKnockback(new Vector2(-faceDir, 0.5f).normalized * 8f, 2.0f);
            
            EnemyStats caughtStats = caughtAI.GetComponent<EnemyStats>();
            if(caughtStats != null) caughtStats.TakeDamage(ultDamage);
        }

        // ==================================================
        // [8] 시전 후 반동 및 스킬 종료 
        // ==================================================
        // ★ [여기에 추가 3] 스킬이 끝났으니 잔상을 다시 꺼줍니다!
        if (hornTrail != null) hornTrail.emitting = false;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(faceDir * 8f, 15f), ForceMode2D.Impulse); 
        // ★ [여기에 추가 3] 스킬이 끝났으니 잔상을 끄고, 위치도 맨 처음 기억해둔 원래 위치로 되돌립니다!
        if (hornTrail != null) 
        {
            hornTrail.emitting = false; 
            hornTrail.transform.localPosition = originalTrailPos; 
        }
        // ★ [수정] 반동 대기 시간 중에 맞아도 쿨타임이 안전하게 돌아가도록 미리 예약!
        Invoke("ResetUltCooldown", ultRecoveryTime + ultCooldown);

        yield return new WaitForSeconds(ultRecoveryTime); 
        
        isAttacking = false; 
        isInvincible = false;

        // (기존에 있던 yield return new WaitForSeconds(ultCooldown); 과 canUltimate = true; 는 지워주세요!)
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

    // ★ [추가] 넉백에도 절대 끊기지 않는 궁극기 쿨타임 회복 함수
    private void ResetUltCooldown()
    {
        canUltimate = true;
    }
    
    // ==================================================
    // ★ [추가] 화면을 미친 듯이 흔들어주는 지진(카메라 쉐이크) 코루틴
    // ==================================================
    IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        // 메인 카메라를 자동으로 찾아서 가져옵니다.
        Transform camTransform = Camera.main.transform;
        
        // 흔들기 전의 원래 카메라 위치를 기억해둡니다.
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // X축, Y축으로 설정한 강도(magnitude)만큼 랜덤하게 덜덜덜 떱니다!
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            camTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 지진이 끝나면 카메라를 원래 위치로 깔끔하게 원상복구 시킵니다.
        camTransform.localPosition = originalPos;
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
    // ★ [새로 추가] 해금 함수
    void UnlockUltimate()
    {
        isUltUnlocked = true;
        if (vSkillUI != null) vSkillUI.UnlockSkill(); // UI 자물쇠 제거
        Debug.Log("풍뎅이 궁극기 해금 완료!");
    }
    void OnDrawGizmos() { if (isGrounded) Gizmos.color = Color.green; else Gizmos.color = Color.red; Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.4f; Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize); if (attackPoint != null) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(attackPoint.position, attackRange); } if (holdPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(holdPoint.position, 0.3f); } }
}