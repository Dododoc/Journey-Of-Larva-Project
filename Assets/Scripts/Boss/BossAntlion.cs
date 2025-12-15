using UnityEngine;
using System.Collections;

public class BossAntlion : MonoBehaviour
{
    [Header("기본 설정")]
    public Animator anim;
    public Transform player;
    public float moveSpeed = 2f;
    private bool isDead = false;
    private Rigidbody2D rb;
    private SpriteRenderer sr; 

    [Header("감지 설정")]
    public float detectionRange = 15.0f; // 플레이어 감지 거리

    [Header("스킬 프리팹 & 위치")]
    public GameObject spikePrefab;   
    public GameObject[] sandStones;  
    public Transform mouthPos;       

    [Header("이펙트 설정")]
    public GameObject vortexEffectPrefab; // 소용돌이 지속 이펙트
    public GameObject stoneBreakEffect;   // 돌이 깨질 때 나오는 이펙트
    
    // [삭제됨] hitImpactPrefab (타격 이펙트는 안 쓰기로 함)

    [Header("패턴 변수")]
    public float chargeSpeed = 10f;
    public float vortexPullPower = 8f;   
    public float knockbackForce = 10f;   

    [Header("패턴 세부 설정")]
    public float spikeYPosition = -3.5f; 
    public float spikeDelayTime = 1.0f;  
    public float vortexDuration = 4.0f;  
    public float sandRainHeight = 8.0f;  
    public float chargeDuration = 2.5f;  
    public int sandRainCount = 3;   
    public float chargeStartDelay = 2.0f; // 돌진 전 뜸 들이는 시간
    public float diggingChaseTime = 2.5f; // 땅속 추적 시간

    [Header("데미지 설정")]
    public float touchDamage = 10f;     
    public float spikeDamage = 15f;     
    public float stoneDamage = 10f;     
    public float digOutDamage = 20f;    
    
    [Header("소용돌이 데미지 설정")]
    public float vortexPullRange = 12f;    // 빨아들이는 범위
    public float vortexDamageRange = 4.0f; // 데미지를 입는 중심부 범위
    public float vortexMinDamage = 2f;  
    public float vortexMaxDamage = 10f; 

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        if(player == null && GameObject.FindWithTag("Player") != null) 
            player = GameObject.FindWithTag("Player").transform;
        
        StartCoroutine(WaitPlayerRoutine());
    }

    // --- [1단계: 대기 모드] ---
    IEnumerator WaitPlayerRoutine()
    {
        // 보스 숨기기 (무적 + 투명)
        GetComponent<Collider2D>().enabled = false;
        if(sr != null) sr.enabled = false;

        while (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            
            // 감지 거리 안으로 들어오면 등장!
            if (dist <= detectionRange)
            {
                StartCoroutine(IntroSequence());
                yield break; 
            }
            yield return null;
        }
    }

    // --- [2단계: 등장 연출 (Intro)] ---
    IEnumerator IntroSequence()
    {
        // 1. 보스 모습 켜기
        if(sr != null) sr.enabled = true;

        // 2. 등장 애니메이션 '즉시' 재생
        anim.Play("DigOut"); 
        yield return new WaitForSeconds(1.0f); 

        // 3. 피격 판정 켜기
        GetComponent<Collider2D>().enabled = true;

        // 4. 체력바 UI 켜기
        EnemyStats stats = GetComponent<EnemyStats>();
        if (stats != null) stats.ShowBossUI();

        // 5. 포효!
        anim.SetTrigger("DoRoar");
        yield return new WaitForSeconds(1.5f);

        // 6. 애니메이션 상태 초기화
        anim.SetBool("DoWalk", false);
        anim.SetBool("DoVortex", false);
        anim.SetBool("IsCharging", false);
        anim.ResetTrigger("DoDigOut");
        anim.ResetTrigger("DoRoar");

        // 7. 전투 시작
        StartCoroutine(ThinkRoutine());
    }

    // --- [3단계: 전투 AI 루프] ---
    IEnumerator ThinkRoutine()
    {
        while (!isDead)
        {
            yield return StartCoroutine(IdleAndMove());

            if (isDead) break;

            int pattern = Random.Range(0, 4); 
            
            switch (pattern)
            {
                case 0: yield return StartCoroutine(Pattern_GroundSlam()); break;
                case 1: yield return StartCoroutine(Pattern_SandRain()); break;
                case 2: yield return StartCoroutine(Pattern_Charge()); break;
                case 3: yield return StartCoroutine(Pattern_DigAmbush()); break;
            }

            yield return new WaitForSeconds(1.5f);
        }
    }

    // --- [패턴 0: 걷기 & 추적] ---
    IEnumerator IdleAndMove()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        float time = Random.Range(1.5f, 2.5f);
        float timer = 0;

        while (timer < time)
        {
            if (player == null) break;

            float dist = Vector2.Distance(transform.position, player.position);
            
            if (dist > 3.0f)
            {
                anim.SetBool("DoWalk", true);
                transform.position = Vector2.MoveTowards(transform.position, new Vector2(player.position.x, transform.position.y), moveSpeed * Time.deltaTime);
                
                // 좌우 반전 (스케일 유지하며 X축만 변경)
                float scaleX = (player.position.x > transform.position.x) ? -1 : 1;
                transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            }
            else
            {
                anim.SetBool("DoWalk", false);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        anim.SetBool("DoWalk", false);
    }

    // --- [패턴 1: 가시 (Ground Slam)] ---
    IEnumerator Pattern_GroundSlam()
    {
        anim.SetTrigger("DoSlam");
        yield return new WaitForSeconds(2.0f);
    }

    public void SpawnSpike() 
    {
        if (player == null) return;
        StartCoroutine(SpawnSpikeDelayed());
    }

    IEnumerator SpawnSpikeDelayed()
    {
        Vector3 targetPos = new Vector3(player.position.x, spikeYPosition, 0);
        yield return new WaitForSeconds(spikeDelayTime);

        if (spikePrefab != null)
        {
            GameObject spike = Instantiate(spikePrefab, targetPos, Quaternion.identity);
            DamageDealer dealer = spike.AddComponent<DamageDealer>();
            dealer.damage = spikeDamage;
            Destroy(spike, 1.5f);
        }
    }

    // --- [패턴 2: 모래비 (Sand Rain)] ---
    IEnumerator Pattern_SandRain()
    {
        anim.SetTrigger("DoSpit");
        yield return new WaitForSeconds(2.0f);
    }

    public void FireProjectile()
    {
        StartCoroutine(FireSandRainRoutine());
    }

    IEnumerator FireSandRainRoutine()
    {
        if (sandStones.Length == 0) yield break;

        for (int i = 0; i < sandRainCount; i++)
        {
            float targetX = (player != null) ? player.position.x + Random.Range(-2.5f, 2.5f) : transform.position.x;
            Vector3 spawnPos = new Vector3(targetX, sandRainHeight, 0); 

            GameObject stone = Instantiate(sandStones[Random.Range(0, sandStones.Length)], spawnPos, Quaternion.identity);
            
            DamageDealer dealer = stone.AddComponent<DamageDealer>();
            dealer.damage = stoneDamage;
            dealer.hitEffectPrefab = stoneBreakEffect; 

            yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
        }
    }

    // --- [패턴 3: 땅파기 & 소용돌이 (Dig Ambush)] ---
    IEnumerator Pattern_DigAmbush()
    {
        anim.SetTrigger("DoDig");
        GetComponent<Collider2D>().enabled = false; 
        yield return new WaitForSeconds(1.0f);
        
        // 1. 소용돌이 위치로 이동 (접근)
        float approachTime = 0f;
        while (approachTime < 1.5f)
        {
            if (player != null)
            {
                float targetX = Mathf.MoveTowards(transform.position.x, player.position.x, moveSpeed * 3.0f * Time.deltaTime);
                transform.position = new Vector3(targetX, transform.position.y, 0);
                if (Mathf.Abs(transform.position.x - player.position.x) < 1.0f) break;
            }
            approachTime += Time.deltaTime;
            yield return null;
        }
        
        // 2. 소용돌이
        anim.SetBool("DoVortex", true);
        GameObject currentVortexEffect = null;
        if (vortexEffectPrefab != null)
        {
            currentVortexEffect = Instantiate(vortexEffectPrefab, transform.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.5f); 

        // 3. 빨아들이기 & 데미지
        float timer = vortexDuration;
        float damageTick = 0f; 

        while (timer > 0)
        {
            if (currentVortexEffect != null) 
                currentVortexEffect.transform.position = transform.position;

            if (player != null)
            {
                float dist = Vector2.Distance(player.position, transform.position);
                
                // 빨아들이기
                if (dist < vortexPullRange) 
                {
                    Vector2 pullDir = (transform.position - player.position).normalized;
                    player.Translate(pullDir * vortexPullPower * Time.deltaTime);

                    // 데미지
                    if (dist < vortexDamageRange)
                    {
                        damageTick -= Time.deltaTime;
                        if (damageTick <= 0)
                        {
                            float t = Mathf.Clamp01(1f - (dist / vortexDamageRange)); 
                            float dmg = Mathf.Lerp(vortexMinDamage, vortexMaxDamage, t);

                            PlayerStats ps = player.GetComponent<PlayerStats>();
                            // 플레이어 피격 이펙트는 PlayerStats에서 자동 처리
                            if (ps != null) ps.TakeDamage(dmg);

                            damageTick = 0.5f; 
                        }
                    }
                }
            }
            timer -= Time.deltaTime;
            yield return null;
        }

        if (currentVortexEffect != null) Destroy(currentVortexEffect);
        anim.SetBool("DoVortex", false); 
        yield return new WaitForSeconds(1.0f); 

        // 4. 반반 확률 분기 (바로 나오기 vs 추적 후 나오기)
        if (Random.value > 0.5f)
        {
            float chaseTime = 0f;
            while(chaseTime < diggingChaseTime)
            {
                if(player != null)
                {
                    float targetX = Mathf.MoveTowards(transform.position.x, player.position.x, moveSpeed * 3.0f * Time.deltaTime);
                    transform.position = new Vector3(targetX, transform.position.y, 0);
                }
                chaseTime += Time.deltaTime;
                yield return null;
            }
        }

        // 5. 튀어나오기
        GetComponent<Collider2D>().enabled = true; 
        anim.SetTrigger("DoDigOut"); 
        
        StartCoroutine(CheckDigOutDamage());
        yield return new WaitForSeconds(1.0f); 
        
        anim.ResetTrigger("DoDigOut");
    }

    IEnumerator CheckDigOutDamage()
    {
        float duration = 0.5f; 
        while(duration > 0)
        {
            if(player != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if(dist < 2.5f) 
                {
                    PlayerStats ps = player.GetComponent<PlayerStats>();
                    if (ps != null) ps.TakeDamage(digOutDamage);

                    Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                    if(playerRb != null)
                    {
                        Vector2 dir = (player.position - transform.position).normalized;
                        Vector2 knockbackDir = (dir + Vector2.up).normalized; 
                        playerRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
                    }
                    yield break; 
                }
            }
            duration -= Time.deltaTime;
            yield return null;
        }
    }

    // --- [패턴 4: 돌진 (Charge)] ---
    IEnumerator Pattern_Charge()
    {
        anim.SetTrigger("DoRoar");
        yield return new WaitForSeconds(chargeStartDelay);

        anim.SetBool("IsCharging", true);

        float currentChargeTime = chargeDuration;
        Vector2 dir = Vector2.left;
        
        if (player != null)
        {
            dir = (player.position.x > transform.position.x) ? Vector2.right : Vector2.left;
        }

        while (currentChargeTime > 0)
        {
            transform.Translate(dir * chargeSpeed * Time.deltaTime, Space.World);
            currentChargeTime -= Time.deltaTime;
            yield return null;
        }

        anim.SetBool("IsCharging", false);
        yield return new WaitForSeconds(0.5f);
    }

    // --- [보스 피격 처리] ---
    public void OnHit()
    {
        // ★ 이펙트 생성 코드 제거됨 (요청사항 반영)
        
        // 보스 피격 연출 (빨간맛 + 크기 반동)은 유지
        StopCoroutine("HitActionRoutine");
        StartCoroutine("HitActionRoutine");
    }

    IEnumerator HitActionRoutine()
    {
        // X축 방향(좌우)을 유지한 채 1.1배 커짐
        float currentScaleX = Mathf.Sign(transform.localScale.x);
        transform.localScale = new Vector3(currentScaleX * 1.1f, 1.1f, 1f); 
        
        if (sr != null) sr.color = new Color(1f, 0.4f, 0.4f); // 붉은색
        
        yield return new WaitForSeconds(0.05f); 
        
        if (sr != null) sr.color = Color.white;
        transform.localScale = new Vector3(currentScaleX * 1.0f, 1.0f, 1f); // 원래 크기
    }

    // --- [몸통 박치기 데미지] ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        if (collision.CompareTag("Player"))
        {
            PlayerStats ps = collision.GetComponent<PlayerStats>();
            // 플레이어 피격 연출은 PlayerStats 내부에서 처리
            if (ps != null) ps.TakeDamage(touchDamage);
        }
    }

    public void StartDeathSequence()
    {
        if (isDead) return;
        isDead = true;
        
        StopAllCoroutines(); 
        anim.SetTrigger("DoDie");
        GetComponent<Collider2D>().enabled = false; 
        
        if(sr != null) sr.color = Color.white;
        
        Destroy(gameObject, 3.0f);
    }
}