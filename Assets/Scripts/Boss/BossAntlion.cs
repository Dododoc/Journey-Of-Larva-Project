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

    [Header("스킬 프리팹 & 위치")]
    public GameObject spikePrefab;   
    public GameObject[] sandStones;  
    public Transform mouthPos;       

    [Header("이펙트 설정 (신규)")]
    public GameObject vortexEffectPrefab; // 소용돌이 지속 이펙트
    public GameObject stoneBreakEffect;   // 돌이 깨질 때 나오는 이펙트

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
    public float chargeStartDelay = 2.0f; // 돌진 전 대기 시간 (기본 1초)     

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
        
        StartCoroutine(ThinkRoutine());
    }

    // --- [AI 메인 루프] ---
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
        // 플레이어 재탐색 (안전장치)
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
                
                if (player.position.x > transform.position.x) 
                    transform.localScale = new Vector3(-1, 1, 1);
                else 
                    transform.localScale = new Vector3(1, 1, 1);
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
            // 가시는 사라지는 이펙트가 따로 없으므로 hitEffectPrefab 설정 안 함 (필요시 추가)
            
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
            float targetX = transform.position.x; 
            if (player != null) 
            {
                targetX = player.position.x + Random.Range(-2.5f, 2.5f);
            }

            Vector3 spawnPos = new Vector3(targetX, sandRainHeight, 0); 

            GameObject stone = Instantiate(sandStones[Random.Range(0, sandStones.Length)], spawnPos, Quaternion.identity);
            
            // 데미지 딜러 설정 및 이펙트 전달
            DamageDealer dealer = stone.AddComponent<DamageDealer>();
            dealer.damage = stoneDamage;
            dealer.hitEffectPrefab = stoneBreakEffect; // ★ 이펙트 전달

            yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
        }
    }

    // --- [패턴 3: 땅파기 & 소용돌이 (Dig Ambush)] ---
    IEnumerator Pattern_DigAmbush()
    {
        anim.SetTrigger("DoDig");
        GetComponent<Collider2D>().enabled = false; 
        yield return new WaitForSeconds(1.0f);
        
        if (Random.value > 0.5f) // 50% 확률로 소용돌이 패턴
        {
            // Digging 접근
            float approachTime = 0f;
            float approachDuration = 1.5f; 

            while (approachTime < approachDuration)
            {
                if (player != null)
                {
                    float targetX = Mathf.MoveTowards(transform.position.x, player.position.x, moveSpeed * 3.0f * Time.deltaTime);
                    transform.position = new Vector3(targetX, transform.position.y, 0);

                    if (Mathf.Abs(transform.position.x - player.position.x) < 1.0f) 
                        break;
                }
                approachTime += Time.deltaTime;
                yield return null;
            }
            
            // 소용돌이 시작
            anim.SetBool("DoVortex", true);
            
            // ★ 소용돌이 이펙트 생성
            GameObject currentVortexEffect = null;
            if (vortexEffectPrefab != null)
            {
                currentVortexEffect = Instantiate(vortexEffectPrefab, transform.position, Quaternion.identity);
            }

            yield return new WaitForSeconds(0.5f); 

            // Vortexing 로직
            float timer = vortexDuration;
            float damageTick = 0f; 

            while (timer > 0)
            {
                // 이펙트 위치 동기화
                if (currentVortexEffect != null) 
                    currentVortexEffect.transform.position = transform.position;

                if (player != null)
                {
                    float dist = Vector2.Distance(player.position, transform.position);
                    
                    // 1. 빨아들이기 (넓은 범위)
                    if (dist < vortexPullRange) 
                    {
                        Vector2 pullDir = (transform.position - player.position).normalized;
                        player.Translate(pullDir * vortexPullPower * Time.deltaTime);

                        // 2. 데미지 입히기 (좁은 중심부 범위)
                        if (dist < vortexDamageRange)
                        {
                            damageTick -= Time.deltaTime;
                            if (damageTick <= 0)
                            {
                                float t = Mathf.Clamp01(1f - (dist / vortexDamageRange)); 
                                float dmg = Mathf.Lerp(vortexMinDamage, vortexMaxDamage, t);

                                PlayerStats ps = player.GetComponent<PlayerStats>();
                                if (ps != null) ps.TakeDamage(dmg);

                                damageTick = 0.5f; 
                            }
                        }
                    }
                }
                timer -= Time.deltaTime;
                yield return null;
            }

            // ★ 패턴 종료 후 이펙트 삭제
            if (currentVortexEffect != null) Destroy(currentVortexEffect);

            anim.SetBool("DoVortex", false); 
            yield return new WaitForSeconds(1.0f); 
        }

        // 추적 후 튀어나오기
        float chaseTime = 0f;
        float chaseDuration = 2.0f; 

        while(chaseTime < chaseDuration)
        {
            if(player != null)
            {
                float targetX = Mathf.MoveTowards(transform.position.x, player.position.x, moveSpeed * 3.0f * Time.deltaTime);
                transform.position = new Vector3(targetX, transform.position.y, 0);
            }
            chaseTime += Time.deltaTime;
            yield return null;
        }

        GetComponent<Collider2D>().enabled = true; 
        anim.SetTrigger("DoDigOut"); 
        
        StartCoroutine(CheckDigOutDamage());
        yield return new WaitForSeconds(1.0f); 
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
        // ★ IsCharging 사용 (Animator 설정 필수)
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

    public void OnHit()
    {
        StartCoroutine(HitFlashRoutine());
    }

    IEnumerator HitFlashRoutine()
    {
        if (sr != null)
        {
            sr.color = Color.red; 
            yield return new WaitForSeconds(0.1f); 
            sr.color = Color.white; 
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        if (collision.CompareTag("Player"))
        {
            PlayerStats ps = collision.GetComponent<PlayerStats>();
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