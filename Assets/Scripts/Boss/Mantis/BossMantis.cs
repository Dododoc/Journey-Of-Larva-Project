using UnityEngine;
using System.Collections;

public class BossMantis : MonoBehaviour
{
    [Header("Basic Components")]
    public Animator anim;
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public Transform player;
    public EnemyStats stats;

    [Header("Movement & Status")]
    public float moveSpeed = 3.0f;
    public float detectionRange = 15.0f;
    public float attackRange = 12.0f;    
    public bool isDead = false;
    private bool isActing = false; 
    private Vector3 defaultScale; 
    public bool isStunned = false; 
    private bool pendingDeath = false;

    // 스킬 반복 방지
    private int lastSkillIndex = -1;
    private int skillRepetitionCount = 0;

    [Header("Contact Damage")]
    public float bodyContactDamage = 5f; 

    [Header("Skill 1: X-Slash")]
    public float thrustSpeed = 15.0f;
    public GameObject xSlashEffectPrefab; 
    public Transform thrustFirePoint;     

    [Header("Skill 2: Blade Wave")]
    public GameObject[] bladePrefabs;
    public Transform bladePoint1; 
    public Transform bladePoint2; 
    public Transform mouthPoint;
    public int minCombo = 3;
    public int maxCombo = 5;

    [Header("Skill 3: Aerial Drop")]
    public float jumpPrepDelay = 2.0f; 
    public float jumpUpSpeed = 15.0f;  
    public float jumpHeight = 10.0f;
    public float dropSpeed = 25.0f;
    public float landDamage = 25f;
    public float landRadius = 4.0f;
    public GameObject landEffectPrefab; 

    [Header("Skill 4: Fluid Trap")]
    public GameObject spitProjectilePrefab; 
    public GameObject trapPrefab;           
    
    [Header("Skill 5: Grab Attack")]
    public float grabDashSpeed = 15.0f;
    public Transform holdPoint;   
    public float grabDotDamage = 2.0f;
    public int requiredMashCount = 10; 
    private bool isHoldingPlayer = false;

    private bool isCharging = false;       
    private bool shouldStunOnLand = false; 

    void Start()
    {
        // ★ [수정] 자식 오브젝트(Visual)에 있는 컴포넌트를 찾아옵니다.
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        
        // 리지드바디와 스탯은 부모(Root)에 있으므로 그대로 가져옵니다.
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (stats == null) stats = GetComponent<EnemyStats>();

        if (player == null && GameObject.FindWithTag("Player") != null) player = GameObject.FindWithTag("Player").transform;
        if (stats != null) stats.ShowBossUI();
        
        defaultScale = transform.localScale;
        rb.bodyType = RigidbodyType2D.Kinematic;

        StartCoroutine(ThinkRoutine());
    }

    void Update()
    {
        if (isDead) return;
        
        if (isStunned || isActing || isHoldingPlayer) 
        {
            anim.SetBool("IsWalking", false);
            return; 
        }

        if (isHoldingPlayer) CheckGrabEscape();
    }

    void LookAtPlayer()
    {
        if (player == null || isDead || isStunned) return;
        
        // Root(부모)의 스케일을 뒤집으면 자식들도 다 같이 깔끔하게 뒤집힙니다.
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(-Mathf.Abs(defaultScale.x), defaultScale.y, defaultScale.z); 
        else
            transform.localScale = new Vector3(Mathf.Abs(defaultScale.x), defaultScale.y, defaultScale.z);
    }

    IEnumerator ThinkRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        while (!isDead)
        {
            if (isStunned) { yield return null; continue; }
            if (player == null) yield break;

            if (!isActing && !isHoldingPlayer)
            {
                float dist = Vector2.Distance(transform.position, player.position);

                if (dist > attackRange)
                {
                    LookAtPlayer();
                    anim.SetBool("IsWalking", true);
                    transform.position = Vector2.MoveTowards(transform.position, new Vector2(player.position.x, transform.position.y), moveSpeed * Time.deltaTime);
                }
                else
                {
                    anim.SetBool("IsWalking", false);
                    isActing = true;
                    
                    int selectedSkill = -1; 
                    for(int i=0; i<10; i++)
                    {
                        int rand = Random.Range(0, 100);
                        int tempSkill = 0;

                        if (rand < 20) tempSkill = 1;
                        else if (rand < 45) tempSkill = 2;
                        else if (rand < 65) tempSkill = 3;
                        else if (rand < 80) tempSkill = 4;
                        else tempSkill = 5;

                        if (tempSkill == lastSkillIndex && skillRepetitionCount >= 2) continue;
                        selectedSkill = tempSkill;
                        break;
                    }

                    if (selectedSkill == -1) selectedSkill = (lastSkillIndex % 5) + 1;

                    if (selectedSkill == lastSkillIndex) skillRepetitionCount++;
                    else { lastSkillIndex = selectedSkill; skillRepetitionCount = 1; }

                    if (selectedSkill == 1) yield return StartCoroutine(Skill1_ChargingThrust());
                    else if (selectedSkill == 2) yield return StartCoroutine(Skill2_BladeCombo());
                    else if (selectedSkill == 3) yield return StartCoroutine(Skill3_AerialDrop());
                    else if (selectedSkill == 4) yield return StartCoroutine(Skill4_FluidTrap());
                    else if (selectedSkill == 5) yield return StartCoroutine(Skill5_GrabAttack());
                    
                    yield return new WaitForSeconds(2.5f);
                    isActing = false;
                }
            }
            yield return null;
        }
    }

    void ResetAnimTriggers()
    {
        anim.ResetTrigger("Skill1_Charge"); anim.ResetTrigger("Skill1_Fire");
        anim.ResetTrigger("Skill2_Slash"); anim.ResetTrigger("Skill3_Jump");
        anim.ResetTrigger("Skill3_Land"); anim.ResetTrigger("Skill4_Spit");
        anim.ResetTrigger("Skill5_Grab"); anim.ResetTrigger("Recover");
        anim.ResetTrigger("Stun"); anim.ResetTrigger("DoDie");
    }

    public void SetGrabbedState(bool grabbed)
    {
        if (isDead) return;
        if (grabbed)
        {
            shouldStunOnLand = isCharging;
            StopAllCoroutines(); ResetAnimTriggers();
            isCharging = false; isActing = false; isStunned = true;
            anim.SetBool("IsGrabbed", true); anim.SetBool("IsWalking", false);
        }
    }

    public void SetThrownState()
    {
        if (isDead) return;
        StopAllCoroutines(); ResetAnimTriggers();
        isStunned = true; 
        anim.SetBool("IsGrabbed", false); anim.SetTrigger("Thrown"); 
        Collider2D col = GetComponent<Collider2D>();
        if(col != null) { col.enabled = true; col.isTrigger = false; }
        StartCoroutine(RecoverFromThrow());
    }

    IEnumerator RecoverFromThrow()
    {
        isStunned = true; 
        rb.gravityScale = 5.0f; 
        yield return new WaitForSeconds(0.2f); 

        float flightTime = 0f;
        while(flightTime < 5.0f)
        {
            if (rb.linearVelocity.magnitude < 1.0f && CheckGround()) break;
            flightTime += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f; 

        // [추가] 착지했는데, 아까 공중에서 죽음 예약(pendingDeath)이 걸려있었다면?
        if (pendingDeath)
        {
            StartDeathSequence(); // 이제 진짜 죽음 함수 실행 (애니메이션 재생, 콜라이더 끔)
            yield break; // 코루틴 종료 (일어나지 않음)
        }

        if (shouldStunOnLand)
        {
            anim.SetTrigger("Stun"); 
            yield return new WaitForSeconds(3.0f); 
        }

        anim.SetTrigger("Recover");
        yield return new WaitForSeconds(0.5f);

        isStunned = false;
        isActing = false; 
        anim.SetBool("IsWalking", false); 
        rb.linearVelocity = Vector2.zero; 
        shouldStunOnLand = false;

        StartCoroutine(ThinkRoutine());
    }

    bool CheckGround() { return Physics2D.Raycast(transform.position, Vector2.down, 2.0f, LayerMask.GetMask("Ground", "Default")); }
    public void OnHit() { if (isDead) return; StartCoroutine(FlashRed()); }
    IEnumerator FlashRed() { if (sr != null) { sr.color = Color.red; yield return new WaitForSeconds(0.1f); sr.color = Color.white; } }

    IEnumerator Skill1_ChargingThrust()
    {
        LookAtPlayer(); isCharging = true; anim.SetTrigger("Skill1_Charge"); yield return new WaitForSeconds(1.5f);
        isCharging = false; anim.SetTrigger("Skill1_Fire");
        
        float dirX = (player.position.x - transform.position.x) > 0 ? 1f : -1f;
        Vector2 dir = new Vector2(dirX, 0f);
        
        float dashTime = 0.3f;
        while(dashTime > 0 && !isStunned) 
        { 
            // Space.World를 사용하여 절대 좌표로 이동 (스케일 반전 영향 안 받음)
            transform.Translate(dir * thrustSpeed * Time.deltaTime, Space.World); 
            dashTime -= Time.deltaTime; 
            yield return null; 
        }
        yield return new WaitForSeconds(1.0f);
    }

    IEnumerator Skill2_BladeCombo()
    {
        LookAtPlayer(); rb.linearVelocity = Vector2.zero; int attacks = Random.Range(minCombo, maxCombo + 1);
        for (int i = 0; i < attacks; i++) { if(isStunned) break; LookAtPlayer(); int type = i % 2; anim.SetInteger("Skill2_Type", type); anim.SetTrigger("Skill2_Slash"); yield return new WaitForSeconds(0.6f); }
    }

    IEnumerator Skill3_AerialDrop()
    {
        anim.SetTrigger("Skill3_Jump");
        yield return new WaitForSeconds(jumpPrepDelay);
        if(isStunned) yield break;

        float targetX = transform.position.x;
        if(player != null) targetX = player.position.x;

        GetComponent<Collider2D>().enabled = false;
        
        float originY = transform.position.y;
        float currentHeight = 0f;

        while(currentHeight < jumpHeight && !isStunned)
        {
            float moveUp = jumpUpSpeed * Time.deltaTime;
            transform.Translate(Vector2.up * moveUp);
            currentHeight += moveUp;
            yield return null;
        }

        if(isStunned) { GetComponent<Collider2D>().enabled = true; yield break; }

        yield return new WaitForSeconds(0.3f); 

        transform.position = new Vector3(targetX, transform.position.y, 0);

        while (transform.position.y > originY && !isStunned) 
        {
            transform.Translate(Vector2.down * dropSpeed * Time.deltaTime);
            yield return null;
        }
        
        transform.position = new Vector3(transform.position.x, originY, 0);

        GetComponent<Collider2D>().enabled = true;
        anim.SetTrigger("Skill3_Land");

        if (landEffectPrefab) Instantiate(landEffectPrefab, transform.position, Quaternion.identity);
        
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, landRadius);
        foreach (var p in hitPlayers) { if (p.CompareTag("Player")) p.GetComponent<PlayerStats>().TakeDamage(landDamage); }
        yield return new WaitForSeconds(1.0f);
    }

    IEnumerator Skill4_FluidTrap() { LookAtPlayer(); anim.SetTrigger("Skill4_Spit"); yield return new WaitForSeconds(1.0f); }
    
    IEnumerator Skill5_GrabAttack()
    {
        LookAtPlayer(); anim.SetTrigger("Skill5_Grab"); yield return new WaitForSeconds(0.5f);
        float dashDuration = 0.5f; Vector2 dir = (player.position - transform.position).normalized; dir.y = 0; 
        
        while(dashDuration > 0 && !isHoldingPlayer && !isStunned) 
        { 
            transform.Translate(dir * grabDashSpeed * Time.deltaTime, Space.World); 
            dashDuration -= Time.deltaTime; 
            yield return null; 
        }
        
        if (!isHoldingPlayer) yield return new WaitForSeconds(0.5f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isHoldingPlayer || isDead || isStunned) return;
        if (collision.CompareTag("Player") && anim.GetCurrentAnimatorStateInfo(0).IsName("Mantis_Skill5_Grab_Attempt"))
            StartCoroutine(GrabPlayerRoutine(collision.gameObject));
    }
    IEnumerator GrabPlayerRoutine(GameObject playerObj)
    {
        isHoldingPlayer = true; anim.SetBool("Grab_Success", true); 
        Rigidbody2D prb = playerObj.GetComponent<Rigidbody2D>(); BeetleController beetle = playerObj.GetComponent<BeetleController>();
        if(prb) prb.linearVelocity = Vector2.zero; if(beetle) beetle.SetGrabbed(true);
        playerObj.transform.SetParent(holdPoint); playerObj.transform.localPosition = Vector3.zero;
        int mash = 0; float timeLimit = 5.0f; float timer = 0f;
        while (mash < 10 && timer < timeLimit) { timer += Time.deltaTime; if(playerObj.GetComponent<PlayerStats>()) playerObj.GetComponent<PlayerStats>().TakeDamage(grabDotDamage); if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)) mash++; yield return new WaitForSeconds(0.1f); }
        anim.SetTrigger("Grab_Break"); anim.SetBool("Grab_Success", false);
        playerObj.transform.SetParent(null); if(beetle) beetle.SetGrabbed(false);
        if(prb) { prb.bodyType = RigidbodyType2D.Dynamic; prb.AddForce((playerObj.transform.position - transform.position).normalized * 5f, ForceMode2D.Impulse); }
        isHoldingPlayer = false; yield return new WaitForSeconds(1.0f);
    }
    void CheckGrabEscape() { }

    public void OnAnimEvent_SpawnXSlash() 
    { 
        if(xSlashEffectPrefab) 
        {
            Instantiate(xSlashEffectPrefab, new Vector3(thrustFirePoint.position.x, thrustFirePoint.position.y, -1f), Quaternion.identity); 
        }
    }

    public void OnAnimEvent_FireBladeA() { FireBlade(0, bladePoint1); }
    public void OnAnimEvent_FireBladeB() { FireBlade(1, bladePoint2); }
    
    private void FireBlade(int index, Transform spawnPoint)
    {
        if (spawnPoint == null) spawnPoint = transform; 

        if (bladePrefabs.Length > index && bladePrefabs[index] != null)
        {
            GameObject blade = Instantiate(bladePrefabs[index], spawnPoint.position, Quaternion.identity);
            BladeProjectile bp = blade.GetComponent<BladeProjectile>();
            if (bp != null) 
            {
                Vector2 dir = player.position - spawnPoint.position;
                dir.y = 0; 
                dir.Normalize();
                if (dir == Vector2.zero) dir = transform.localScale.x < 0 ? Vector2.left : Vector2.right;
                bp.Setup(dir); 
            }
        }
    }

    public void OnAnimEvent_Spit() 
    { 
        if(spitProjectilePrefab && player != null) 
        {
            // 만약 mouthPoint를 깜빡하고 연결 안 했으면, 임시로 내 위치(transform) 사용
            Transform firePos = (mouthPoint != null) ? mouthPoint : transform;

            // ★ 여기서 firePos.position (즉, 입 위치)에서 생성!
            GameObject spit = Instantiate(spitProjectilePrefab, firePos.position, Quaternion.identity);
            
            SpitProjectile sp = spit.GetComponent<SpitProjectile>();
            if(sp != null)
            {
                Vector3 target = new Vector3(player.position.x, -3.5f, 0);
                sp.Setup(target, trapPrefab);
            }
        } 
    }
    public void StartDeathSequence() 
    { 
        // [수정] 만약 던져져서 날아가고 있는 중(중력이 켜져있음)이라면?
        // 아직 땅에 안 닿았는데 죽으라고 하면 -> "착지하고 죽겠다"고 예약만 함
        if (rb.gravityScale > 0)
        {
            pendingDeath = true;
            return; // 여기서 함수 종료 (아직 애니메이션 재생 안 함, 콜라이더 안 끔)
        }

        // [기존 로직] 땅에 있거나 평상시라면 바로 죽음 처리
        isDead = true; 
        anim.SetBool("IsDead", true); 
        anim.SetTrigger("DoDie"); 
        
        GetComponent<Collider2D>().enabled = false; 
        StopAllCoroutines(); 
    }
    }