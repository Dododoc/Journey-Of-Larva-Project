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

    // ===================================
    // ★ [새로 추가] 스킬 UI 및 궁극기 쿨타임 변수
    // ===================================
    [Header("Skill UI")]
    public SkillSlotUI xSkillUI; // 깨불어부수기
    public SkillSlotUI cSkillUI; // 땅파기
    public SkillSlotUI vSkillUI; // 궁극기

    public float ultCooldown = 15f;      // 궁극기 쿨타임
    private bool canUltimate = true;     // 궁극기 사용 가능 여부
    public bool isUltUnlocked = false; // ★ 궁극기 해금 여부 (기본 false)
    // ===================================
    [Header("Ultimate Skill (모래폭풍 절단)")]
    public float ultRadius = 8f;         // 폭풍에 빨려 들어갈 범위
    public float ultTeleportOffset = 2f; // 적과 떨어질 거리
    public float ultDamage = 50f;        // 마지막 일격 데미지
    public GameObject sandstormVFX;      // 모래폭풍 파티클 프리팹
    public GameObject slashEffectVFX;    // 마지막 거대 검기 이펙트 프리팹
    
    // ★ [추가됨] 인스펙터에서 이펙트 위치를 마음대로 조절하세요!
    [Tooltip("모래폭풍 생성 위치 (X: 앞으로 거리, Y: 위아래 높이)")]
    public Vector2 sandstormSpawnOffset = new Vector2(5.0f, -0.5f); // 기본값: 더 멀리, 더 아래로
    [Tooltip("마지막 검기 이펙트 생성 위치 (X: 앞으로 거리, Y: 위아래 높이)")]
    public Vector2 slashEffectOffset = new Vector2(1.0f, 0f); // 기본값: 개미 앞쪽으로 살짝
    
    private bool isAntUlt = false;

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
        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;
        if (isKnockedBack) { UpdateAnimation(); return; }
        if (isDiggingAnim) { if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero; UpdateAnimation(); return; }
        if (isUnderground) { HandleUndergroundMove(); UpdateAnimation(); return; }
        if (isStrongAttacking) { rb.linearVelocity = Vector2.zero; return; }
        // ★ [임시 해금 키] L키를 누르면 궁극기가 해금됩니다.
        if (Input.GetKeyDown(KeyCode.L) && !isUltUnlocked)
        {
            UnlockUltimate();
        }
        // ==========================================================
        // ★ [여기에 딱 한 줄 추가!] 궁극기 시전 중에는 멈춰있게 만듭니다.
        // ==========================================================
        if (isAntUlt) { rb.linearVelocity = Vector2.zero; return; }

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
    IEnumerator StrongAttackRoutine() 
    { 
        canStrongAttack = false; 
        isStrongAttacking = true; 
        isInvincible = true; 
        yield return new WaitForSeconds(0.1f); 

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, teleportRange, enemyLayers); 
        Transform target = null; 
        float closestDist = Mathf.Infinity; 
        
        foreach (var hit in hits) 
        { 
            float d = Vector2.Distance(transform.position, hit.transform.position); 
            if (d < closestDist) 
            { 
                closestDist = d; 
                target = hit.transform; 
            } 
        } 

        if (target != null) 
        { 
            float dirToEnemy = target.position.x - transform.position.x; 
            if (dirToEnemy > 0 && !isFacingRight) Flip(); 
            else if (dirToEnemy < 0 && isFacingRight) Flip(); 
            
            if (Vector2.Distance(transform.position, target.position) > attackRange * 1.2f) 
            { 
                sr.color = new Color(1f, 1f, 1f, 0.5f); 
                float dirSign = Mathf.Sign(target.position.x - transform.position.x);
                
                // ★ 수정: Y축 위치도 몹의 Y 위치(target.position.y)로 이동하도록 변경
                transform.position = new Vector3(target.position.x - (dirSign * teleportOffset), target.position.y, transform.position.z); 
            } 
        } 

        anim.SetTrigger("DoStrongAttack"); 
        yield return new WaitForSeconds(strongAttackDelay); 
        
        ApplyDamage(attackPoint.position, attackRange * 1.5f, strongDamageMultiplier, strongEnemyKnockback); 
        sr.color = Color.white; 
        isStrongAttacking = false; 
        
        StartCoroutine(SkillInvincibilityRoutine(1.5f)); 
        yield return new WaitForSeconds(strongCooldown); 
        canStrongAttack = true; 
    }
    IEnumerator DigRoutine() { canDig = false; isDiggingAnim = true; anim.SetTrigger("DoDig"); rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero; myCollider.enabled = false; yield return new WaitForSeconds(0.5f); isDiggingAnim = false; isUnderground = true; float timer = 0f; while (timer < digDuration) { timer += Time.deltaTime; if (Input.GetKeyDown(KeyCode.C)) break; yield return null; } isUnderground = false; isDiggingAnim = true; anim.SetTrigger("DoEmerge"); rb.gravityScale = defaultGravity; myCollider.enabled = true; yield return new WaitForSeconds(emergeDamageDelay); EmergeAttack(); yield return new WaitForSeconds(emergeAnimDuration - emergeDamageDelay); isDiggingAnim = false; StartCoroutine(SkillInvincibilityRoutine(2.0f)); yield return new WaitForSeconds(digCooldown); canDig = true; }
    void CheckGround() { if (jumpCooldown > 0) { isGrounded = false; return; } Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f; RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, castDistance + 0.3f, groundLayer); isGrounded = hit.collider != null; if (isGrounded) surfaceNormal = hit.normal; else surfaceNormal = Vector2.up; }
    void ProcessInput() 
    { 
        // 1. 평타 (Z키)
        if (Input.GetKeyDown(KeyCode.Z) && !isBasicAttacking) 
        {
            StartCoroutine(BasicAttackRoutine());
        }

        // 2. 강공격 (X키)
        if (Input.GetKeyDown(KeyCode.X) && canStrongAttack) 
        {
            if (xSkillUI != null) xSkillUI.StartCooldown(strongCooldown); // ★ UI 연동
            StartCoroutine(StrongAttackRoutine());
        }

        // 3. 땅파기 (아래 방향키 누른 상태에서 C키)
        if (Input.GetKeyDown(KeyCode.C) && Input.GetAxisRaw("Vertical") < 0f && canDig && isGrounded) 
        {
            if (cSkillUI != null) cSkillUI.StartCooldown(digCooldown); // ★ UI 연동
            StartCoroutine(DigRoutine());
        }

        // 4. 궁극기 (V키) - ★ isUltUnlocked 조건 추가
        if (Input.GetKeyDown(KeyCode.V) && !isAntUlt && isGrounded && canUltimate && isUltUnlocked) 
        {
            if (vSkillUI != null) vSkillUI.StartCooldown(ultCooldown);
            StartCoroutine(AntUltimateRoutine());
        }

        // 이동 및 점프 로직
        float m = Input.GetAxisRaw("Horizontal"); 
        rb.linearVelocity = new Vector2(m * moveSpeed, rb.linearVelocity.y); 
        
        if (Input.GetButtonDown("Jump") && isGrounded) 
        { 
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); 
            anim.SetTrigger("DoJump"); 
        } 
        
        if (m > 0 && !isFacingRight) Flip(); 
        else if (m < 0 && isFacingRight) Flip(); 
    }
    void Flip() { isFacingRight = !isFacingRight; Vector3 s = transform.localScale; s.x *= -1; transform.localScale = s; }
    void UpdateAnimation() 
    { 
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x)); 
        anim.SetBool("IsGrounded", isGrounded); 
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y); 
        
        // ★ [수정됨] 맨 끝에 `|| isAntUlt` 를 추가해서 궁극기 중에도 애니메이션이 끊기지 않게 보호합니다!
        anim.SetBool("IsAttacking", isBasicAttacking || isStrongAttacking || isDiggingAnim || isAntUlt);
    }
    public void ApplyKnockback(Vector2 f) { isKnockedBack = true; rb.linearVelocity = Vector2.zero; rb.AddForce(f, ForceMode2D.Impulse); StartCoroutine(KnockbackRoutine()); }
    IEnumerator KnockbackRoutine() { isInvincible = true; yield return new WaitForSeconds(0.3f); isKnockedBack = false; float blink = Time.time + (hitInvincibilityDuration - 0.3f); while (Time.time < blink) { sr.color = new Color(1,1,1,0.4f); yield return new WaitForSeconds(0.1f); sr.color = Color.white; yield return new WaitForSeconds(0.1f); } isInvincible = false; }
    IEnumerator SkillInvincibilityRoutine(float d) { isInvincible = true; yield return new WaitForSeconds(d); isInvincible = false; }
    IEnumerator IgnoreCollisionRoutine(Collider2D c, float d = 0.5f) { if (c != null && myCollider != null) Physics2D.IgnoreCollision(myCollider, c, true); yield return new WaitForSeconds(d); if (c != null && myCollider != null) Physics2D.IgnoreCollision(myCollider, c, false); }
    void HandleUndergroundMove() 
    { 
        float m = Input.GetAxisRaw("Horizontal"); 
        rb.linearVelocity = new Vector2(m * digSpeed, 0f); 
        
        // ★ [수정됨] 엉뚱한 곳으로 강제 텔레포트 시키던 Clamp 코드를 삭제했습니다!
        // float clampedX = Mathf.Clamp(transform.position.x, mapMinX, mapMaxX); 
        // transform.position = new Vector3(clampedX, transform.position.y, transform.position.z); 

        if (m > 0 && !isFacingRight) Flip(); 
        else if (m < 0 && isFacingRight) Flip(); 
    }

    // ==========================================================
    // 개미 궁극기 메인 코루틴
    // ==========================================================
    // ==========================================================
    // 개미 궁극기 메인 코루틴 (마지막 일격 뒤집힘 수정본)
    // ==========================================================
    // ==========================================================
    // 개미 궁극기 메인 코루틴 (마지막 일격 잔상 & 추진력 수정본)
    // ==========================================================
    // ==========================================================
    // 개미 궁극기 메인 코루틴 (마지막 일격 속도 미친듯 상향본)
    // ==========================================================
    // ==========================================================
    // 개미 궁극기 메인 코루틴 
    // ==========================================================
    IEnumerator AntUltimateRoutine()
    {
        isAntUlt = true; isInvincible = true;
        canStrongAttack = false; canDig = false;
        canUltimate = false; // ★ [추가] 시전 시 궁극기 잠금
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero;
        Vector3 castPos = transform.position;
        float originalFaceDir = isFacingRight ? 1f : -1f;

        // ==========================================================
        // ★ [수정됨] 1. 시전 (Ant_Ult_Start) : 인스펙터의 오프셋 변수 사용
        // ==========================================================
        anim.Play("Ant_Ult_Start");
        // 기존의 고정값 (3f, 2f) 대신 sandstormSpawnOffset 값을 사용합니다.
        Vector3 stormCenter = castPos + new Vector3(originalFaceDir * sandstormSpawnOffset.x, sandstormSpawnOffset.y, 0f);
        
        GameObject stormInstance = null;
        if (sandstormVFX != null) stormInstance = Instantiate(sandstormVFX, stormCenter, Quaternion.identity);

        // 적 구속 및 모래폭풍 띄우기 시작 (기존과 동일)
        Collider2D[] hits = Physics2D.OverlapCircleAll(stormCenter, ultRadius, enemyLayers);
        List<BaseEnemyAI> caughtEnemies = new List<BaseEnemyAI>();
        foreach (var hit in hits) {
            BaseEnemyAI enemy = hit.GetComponentInParent<BaseEnemyAI>();
            if (enemy != null && !caughtEnemies.Contains(enemy)) {
                enemy.SetGrabbed(true);
                caughtEnemies.Add(enemy);
            }
        }
        
        yield return new WaitForSeconds(1.1f);

        // 모래폭풍 적 띄우기 코루틴 시작
        Coroutine liftCoroutine = StartCoroutine(SandstormLiftRoutine(caughtEnemies, stormCenter, 5.2f));
        // ==========================================================
        // 2. 지정된 위치 & 지정된 애니메이션으로 7연속 검무 (각 0.4초)
        // ==========================================================
        int[] animSeq = { 2, 7, 5, 4, 3, 6, 2 };
        Vector3[] posSeq = {
            new Vector3(ultTeleportOffset, ultTeleportOffset, 0),
            new Vector3(-ultTeleportOffset, -ultTeleportOffset, 0),
            new Vector3(ultTeleportOffset, -ultTeleportOffset, 0),
            new Vector3(-ultTeleportOffset, ultTeleportOffset, 0),
            new Vector3(0, ultTeleportOffset + 1f, 0),
            new Vector3(-ultTeleportOffset, 0, 0),
            new Vector3(ultTeleportOffset, 0, 0)
        };

        for (int i = 0; i < animSeq.Length; i++)
        {
            yield return StartCoroutine(TeleportStrikeSingle(stormCenter, animSeq[i], posSeq[i], 0.4f));
        }

        // ==========================================================
        // 3. 원대복귀 (시전 위치) 및 방향성 강제 리셋 : 1.5초
        // ==========================================================
        sr.color = new Color(1, 1, 1, 1);
        transform.position = castPos;
        
        Vector3 resetScale = transform.localScale;
        resetScale.x = 1f; // Assumes Base-Right.
        transform.localScale = resetScale;
        isFacingRight = true;
        
        if (originalFaceDir < 0f) Flip(); // 원래 왼쪽이었다면 뒤집음

        anim.Play("Ant_Ult_3");
        yield return new WaitForSeconds(1.5f); // 비장한 기 모으기 시간

        // ==========================================================
        // ★ [수정됨 - 속도 미친듯 상향] 
        // 4. 절명 일섬 (Ant_Ult_4) & 1차 대각선 이동 (잔상 추가)
        // ==========================================================
        anim.Play("Ant_Ult_4");
        float finalFaceDir = isFacingRight ? 1f : -1f;
        Vector3 dashDir = new Vector3(finalFaceDir, 1f, 0f).normalized;
        
        float t = 0f;
        float ghostTimer = 0f;
        // ★ [속도 대폭 상향] 잔상 간격을 더 촘촘하게! (0.05 -> 0.03)
        float ghostInterval = 0.03f; 

        // ★ [속도 대폭 상향] 대각선 이동 시간 단축! (0.2 -> 0.1)
        while (t < 0.1f) {
            // ★ [속도 대폭 상향] 이동 속도 대폭 증가! (30 -> 80) 눈 깜짝할 새!
            transform.position += dashDir * (80f * Time.deltaTime);
            sr.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t / 0.1f)); // 더 빨리 투명해짐

            // [잔상 생성]
            ghostTimer += Time.deltaTime;
            if (ghostTimer >= ghostInterval) {
                SpawnGhost();
                ghostTimer = 0f;
            }

            t += Time.deltaTime;
            yield return null;
        }
        sr.color = new Color(1, 1, 1, 0f); // 완전히 사라짐

        // ==========================================================
        // ★ [수정됨 - 연출 변경] 
        // 5. 공중 출현 & 2차 대각선 이동 (초고속 관성 추진)
        // ==========================================================
        // ★ [연출 변경] 폭풍 너머 훨씬 멀리서 나타남 (Overshoot 효과)
        transform.position = stormCenter + new Vector3(finalFaceDir * 4f, 6f, 0f); 
        sr.color = new Color(1, 1, 1, 1f); // 다시 짠! 하고 나타남

        // ★ [연출 변경] 감속(Glide) 대신 초고속 추진(Thrust) 후 딱! 멈춤
        // 기존 0.4초 감속 glide에서 0.1초 고속 thrust로 변경
        float thrustSpeed = 100f; // 미친듯한 속도
        float thrustDuration = 0.1f; 
        t = 0f;
        ghostTimer = 0f;

        while (t < thrustDuration) {
            transform.position += dashDir * (thrustSpeed * Time.deltaTime); // 감속 없이 초고속 질주

            // [잔상 생성]
            ghostTimer += Time.deltaTime;
            if (ghostTimer >= ghostInterval) { 
                 SpawnGhost();
                 ghostTimer = 0f;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // ==========================================================
        // ★ [수정됨] 6. 화면 정지 (Hit-Stop) : 1초 (추진력이 끝나고 쾅! 멈춤)
        // ==========================================================
        rb.linearVelocity = Vector2.zero; // 물리적인 관성 완전 정지
        yield return new WaitForSeconds(1.0f); // ★ [중요] 이 1초 동안 PART1의 검기 이펙트 프리팹이 번쩍! 해야 합니다.

        // ==========================================================
        // ★ [수정됨] 7. 폭발, 지진, 적 추락 : 검기 이펙트 위치 오프셋 적용
        // ==========================================================
        StartCoroutine(CameraShakeRoutine(0.3f, 0.8f));
        
        // 인스펙터에서 설정한 slashEffectOffset을 적용하여 이펙트 위치를 잡습니다.
        Vector3 slashPos = transform.position + new Vector3(finalFaceDir * slashEffectOffset.x, slashEffectOffset.y, 0f);
        
        if (slashEffectVFX != null) 
        {
            GameObject slashObj = Instantiate(slashEffectVFX, slashPos, Quaternion.identity);
            
            // 검기 이펙트가 개미가 바라보는 방향에 맞춰 좌우로 뒤집히도록 처리 (필수!)
            Vector3 slashScale = slashObj.transform.localScale;
            slashScale.x = finalFaceDir > 0 ? Mathf.Abs(slashScale.x) : -Mathf.Abs(slashScale.x);
            slashObj.transform.localScale = slashScale;
            // ★ [여기에 딱 한 줄 추가!] 1.3초 뒤에 검기 오브젝트를 완전히 파괴합니다.
            Destroy(slashObj, 1.3f);
        }
        
        if (stormInstance != null) Destroy(stormInstance);

        foreach (var enemy in caughtEnemies) {
            if (enemy == null) continue;
            enemy.SetGrabbed(false);
            enemy.ApplyKnockback(new Vector2(finalFaceDir, -1.5f).normalized * 30f, 1.2f); 
            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            if (stats != null) stats.TakeDamage(ultDamage);
        }

        // 스킬 완전 종료
        rb.gravityScale = originalGravity;
        isAntUlt = false; isInvincible = false;
        canStrongAttack = true; canDig = true;

        // ★ [추가] 코루틴이 끝날 때 쿨타임 회복 예약
        Invoke("ResetAntUltCooldown", ultCooldown);
    }
    // ==========================================================
    // ★ [수정됨 - User 11 피드백 반영] 단일 타격 페이드 인/아웃 (포즈별 기본 방향성 추가)
    // ==========================================================
    IEnumerator TeleportStrikeSingle(Vector3 center, int poseNum, Vector3 offset, float duration)
    {
        float fadeTime = 0.1f; // 0.1초 페이드 인/아웃 (스피디하게!)
        float holdTime = duration - (fadeTime * 2f); // 0.2초 홀딩

        // 1. 지정된 오프셋으로 위치 이동
        transform.position = center + offset;
        
        // ==========================================================
        // ★ [핵심 - User 11 피드백 반영] 방향성 수정 로직
        // ==========================================================
        float faceDir = (center.x - transform.position.x) > 0 ? 1f : -1f; // 폭풍을 향하는 방향
        
        // 1. 이 포즈의 스프라이트가 기본적으로 어디를 보고 있는지 판단합니다.
        // 사용자 피드백: 2번과 5번은 이미 왼쪽을 보고 있음 (base_right = false)
        // 나머지는 오른쪽을 보고 있음 (base_right = true)
        bool isBaseRight = (poseNum != 2 && poseNum != 5);

        // 2. 폭풍을 바라보기 위해 필요한 *최종 스케일(scaleX)*을 계산합니다.
        // 이 스킬에서는 simplified된 global `Flip()` 함수 대신, ScaleX를 직접 세팅하여 mixed-base sprites를 제어합니다.
        float desiredScaleX = 1f;

        if (isBaseRight)
        {
            // 기본 오른쪽 포즈: 
            // 폭풍이 오른쪽(faceDir > 0) -> ScaleX=1f (안 뒤집음)
            // 폭풍이 왼쪽(faceDir < 0) -> ScaleX=-1f (왼쪽으로 뒤집음)
            desiredScaleX = (faceDir > 0) ? 1f : -1f;
        }
        else
        {
            // 기본 왼쪽 포즈: 
            // 폭풍이 오른쪽(faceDir > 0) -> ScaleX=-1f (오른쪽으로 뒤집음)
            // 폭풍이 왼쪽(faceDir < 0) -> ScaleX=1f (안 뒤집음)
            desiredScaleX = (faceDir > 0) ? -1f : 1f;
        }

        // 3. 계산된 스케일을 직접 적용합니다.
        Vector3 s = transform.localScale;
        s.x = desiredScaleX;
        transform.localScale = s;

        // 4. 전역 isFacingRight 플래그를 실제 물리적 방향에 맞게 업데이트합니다.
        // scaleX가 양수(default)이면 본래 방향을 유지, 음수이면 반대 방향.
        if (s.x > 0) // 스케일이 양수
        {
            isFacingRight = isBaseRight; // 본래 방향 (BaseRight이면 오른쪽, 아니면 왼쪽)
        }
        else // 스케일이 음수
        {
            isFacingRight = !isBaseRight; // 본래 방향의 반대 (BaseRight이면 왼쪽, 아니면 오른쪽)
        }
        // ==========================================================

        anim.Play("Ant_Ult_2_" + poseNum);

        // 페이드 인
        float t = 0;
        while (t < fadeTime) {
            sr.color = new Color(1, 1, 1, Mathf.Lerp(0f, 1f, t / fadeTime));
            t += Time.deltaTime; yield return null;
        }
        sr.color = new Color(1, 1, 1, 1f);

        // 포즈 유지 (때리는 순간)
        yield return new WaitForSeconds(holdTime);

        // 페이드 아웃
        t = 0;
        while (t < fadeTime) {
            sr.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t / fadeTime));
            t += Time.deltaTime; yield return null;
        }
        sr.color = new Color(1, 1, 1, 0f);
    }
    
    // SandstormLiftRoutine 코루틴은 기존과 동일... (생략)

    // ==========================================================
    // 서브 2: 모래폭풍 적 띄우기 코루틴
    // ==========================================================
    IEnumerator SandstormLiftRoutine(List<BaseEnemyAI> enemies, Vector3 center, float duration)
    {
        float t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            foreach (var enemy in enemies) {
                if (enemy == null) continue;
                float newY = Mathf.Lerp(enemy.transform.position.y, center.y + 4f, t / duration);
                float shakeX = Mathf.Sin(t * 40f) * 0.15f; // 덜덜 떨림
                enemy.transform.position = new Vector3(center.x + shakeX, newY, 0f);
            }
            yield return null;
        }
    }
    // ==========================================================
    // ★ [추가] 화면을 미친 듯이 흔들어주는 지진(카메라 쉐이크) 코루틴
    // ==========================================================
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
    // ==========================================================
    // ★ [helper - 클래스 내부에 추가] 잔상(Ghost) 생성 및 제어 로직
    // ==========================================================
    private void SpawnGhost()
    {
        // 1. Create Empty GameObject
        GameObject ghostObj = new GameObject("UltimateGhost");
        ghostObj.transform.position = transform.position;
        ghostObj.transform.rotation = transform.rotation;
        ghostObj.transform.localScale = transform.localScale;

        // 2. Add SpriteRenderer and copy settings
        SpriteRenderer ghostSR = ghostObj.AddComponent<SpriteRenderer>();
        ghostSR.sprite = sr.sprite; // 현재 개미의 애니메이션 프레임 복사
        ghostSR.sortingLayerID = sr.sortingLayerID;
        ghostSR.sortingOrder = sr.sortingOrder - 1; // 실제 플레이어 뒤에 위치
        ghostSR.material = sr.material; // 동일한 머티리얼 사용 (알파 지원 필수)

        // 궁극기 테마 색상 (예: 불타는 주황색/빨간색 테두리 또는 색상)
        // Shader가 Sprite Color Alpha를 지원한다고 가정합니다.
        Color initialGhostColor = new Color(1f, 0.4f, 0f, 0.6f); // 궁극기 테마 주황색, 60% 투명도
        ghostSR.color = initialGhostColor;

        // 3. Start Fade Routine on the *AntController* since the ghost doesn't have a script
        StartCoroutine(GhostFadeRoutine(ghostSR, initialGhostColor, 0.4f)); // fades over 0.4s
    }

    private IEnumerator GhostFadeRoutine(SpriteRenderer ghostSR, Color initialColor, float duration)
    {
        float t = 0;
        while (t < duration) {
            if (ghostSR == null) yield break; // Player destroyed/scene changed
            t += Time.deltaTime;
            // Linearly fade alpha from initial alpha to 0
            float alpha = Mathf.Lerp(initialColor.a, 0f, t / duration);
            ghostSR.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            yield return null;
        }

        // Cleanup
        if (ghostSR != null && ghostSR.gameObject != null) {
            Destroy(ghostSR.gameObject);
        }
        }
        // ★ [새로 추가] 스크립트 맨 아래 (마지막 괄호 } 바로 위)에 이 함수를 통째로 추가하세요!
    private void ResetAntUltCooldown()
    {
        canUltimate = true;
    }
    // ★ [새로 추가] 해금 함수
    void UnlockUltimate()
    {
        isUltUnlocked = true;
        if (vSkillUI != null) vSkillUI.UnlockSkill(); // UI 자물쇠 제거
        Debug.Log("개미 궁극기 해금 완료!");
    }
    }