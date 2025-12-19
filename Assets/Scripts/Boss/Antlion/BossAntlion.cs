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

    // ★ [핵심] 맵 이동 제한 (이 값을 인스펙터에서 맵 크기에 맞게 조절하세요!)
    [Header("맵 이동 제한")]
    public float mapMinX = -25f;
    public float mapMaxX = 25f;

    [Header("감지 설정")]
    public float detectionRange = 15.0f; 

    [Header("스킬 프리팹 & 위치")]
    public GameObject spikePrefab;   
    public GameObject[] sandStones;  
    public Transform mouthPos;       

    [Header("이펙트 설정")]
    public GameObject vortexEffectPrefab; 
    public GameObject stoneBreakEffect;   

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
    public float chargeStartDelay = 2.0f; 
    public float diggingChaseTime = 2.5f; 

    [Header("데미지 설정")]
    public float touchDamage = 10f;     
    public float spikeDamage = 15f;     
    public float stoneDamage = 10f;     
    public float digOutDamage = 20f;    
    
    [Header("소용돌이 데미지 설정")]
    public float vortexPullRange = 12f;    
    public float vortexDamageRange = 4.0f; 
    public float vortexMinDamage = 2f;  
    public float vortexMaxDamage = 10f; 

    [Header("엔딩 설정")]
    public GameObject endingPortalPrefab; // ★ [추가] 엔딩 포탈 프리팹 연결하는 칸
    public Vector3 portalSpawnOffset = new Vector3(0, 2f, 0); // ★ [추가] 포탈이 나타날 위치 (보스 위치 기준)

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        if(player == null && GameObject.FindWithTag("Player") != null) player = GameObject.FindWithTag("Player").transform;
        
        StartCoroutine(WaitPlayerRoutine());
    }

    IEnumerator WaitPlayerRoutine()
    {
        GetComponent<Collider2D>().enabled = false;
        if(sr != null) sr.enabled = false;
        while (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= detectionRange) { StartCoroutine(IntroSequence()); yield break; }
            yield return null;
        }
    }

    IEnumerator IntroSequence()
    {
        if(sr != null) sr.enabled = true;
        anim.Play("Antlion_DigOut"); 
        yield return new WaitForSeconds(1.0f); 
        GetComponent<Collider2D>().enabled = true;
        EnemyStats stats = GetComponent<EnemyStats>();
        if (stats != null) stats.ShowBossUI();
        anim.SetTrigger("DoRoar");
        yield return new WaitForSeconds(1.5f);
        anim.SetBool("DoWalk", false); anim.SetBool("DoVortex", false); anim.SetBool("IsCharging", false);
        anim.ResetTrigger("DoDigOut"); anim.ResetTrigger("DoRoar");
        StartCoroutine(ThinkRoutine());
    }

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

    IEnumerator IdleAndMove()
    {
        if (player == null) { GameObject p = GameObject.FindWithTag("Player"); if (p != null) player = p.transform; }
        float time = Random.Range(1.5f, 2.5f);
        float timer = 0;

        while (timer < time)
        {
            if (player == null) break;
            float dist = Vector2.Distance(transform.position, player.position);
            
            if (dist > 3.0f)
            {
                anim.SetBool("DoWalk", true);
                
                // 이동
                Vector3 target = Vector2.MoveTowards(transform.position, new Vector2(player.position.x, transform.position.y), moveSpeed * Time.deltaTime);
                
                // ★ 맵 제한 (이 부분 때문에 맵 끝으로 튕길 수 있음 -> 인스펙터 값 확인!)
                target.x = Mathf.Clamp(target.x, mapMinX, mapMaxX);
                transform.position = target;
                
                float scaleX = (player.position.x > transform.position.x) ? -1 : 1;
                transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            }
            else anim.SetBool("DoWalk", false);
            timer += Time.deltaTime;
            yield return null;
        }
        anim.SetBool("DoWalk", false);
    }

    IEnumerator Pattern_GroundSlam() { anim.SetTrigger("DoSlam"); yield return new WaitForSeconds(2.0f); }
    public void SpawnSpike() { if (player == null) return; StartCoroutine(SpawnSpikeDelayed()); }
    IEnumerator SpawnSpikeDelayed() { Vector3 targetPos = new Vector3(player.position.x, spikeYPosition, 0); yield return new WaitForSeconds(spikeDelayTime); if (spikePrefab != null) { GameObject spike = Instantiate(spikePrefab, targetPos, Quaternion.identity); DamageDealer dealer = spike.AddComponent<DamageDealer>(); dealer.damage = spikeDamage; Destroy(spike, 1.5f); } }
    IEnumerator Pattern_SandRain() { anim.SetTrigger("DoSpit"); yield return new WaitForSeconds(2.0f); }
    public void FireProjectile() { StartCoroutine(FireSandRainRoutine()); }
    IEnumerator FireSandRainRoutine() { if (sandStones.Length == 0) yield break; for (int i = 0; i < sandRainCount; i++) { float targetX = (player != null) ? player.position.x + Random.Range(-2.5f, 2.5f) : transform.position.x; Vector3 spawnPos = new Vector3(targetX, sandRainHeight, 0); GameObject stone = Instantiate(sandStones[Random.Range(0, sandStones.Length)], spawnPos, Quaternion.identity); DamageDealer dealer = stone.AddComponent<DamageDealer>(); dealer.damage = stoneDamage; dealer.hitEffectPrefab = stoneBreakEffect; yield return new WaitForSeconds(Random.Range(0.2f, 0.5f)); } }

    IEnumerator Pattern_DigAmbush()
    {
        anim.SetTrigger("DoDig");
        GetComponent<Collider2D>().enabled = false; 
        yield return new WaitForSeconds(1.0f);
        
        // 1. 추적
        float approachTime = 0f;
        while (approachTime < 1.5f)
        {
            if (player != null) {
                float targetX = Mathf.MoveTowards(transform.position.x, player.position.x, moveSpeed * 3.0f * Time.deltaTime);
                // ★ 맵 제한
                targetX = Mathf.Clamp(targetX, mapMinX, mapMaxX);
                transform.position = new Vector3(targetX, transform.position.y, 0);
                if (Mathf.Abs(transform.position.x - player.position.x) < 1.0f) break;
            }
            approachTime += Time.deltaTime;
            yield return null;
        }
        
        anim.SetBool("DoVortex", true);
        GameObject currentVortexEffect = null;
        if (vortexEffectPrefab != null) currentVortexEffect = Instantiate(vortexEffectPrefab, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.5f); 

        float timer = vortexDuration; float damageTick = 0f; 
        while (timer > 0)
        {
            if (currentVortexEffect != null) currentVortexEffect.transform.position = transform.position;
            if (player != null) {
                float dist = Vector2.Distance(player.position, transform.position);
                if (dist < vortexPullRange) {
                    Vector2 pullDir = (transform.position - player.position).normalized;
                    player.Translate(pullDir * vortexPullPower * Time.deltaTime);
                    if (dist < vortexDamageRange) {
                        damageTick -= Time.deltaTime;
                        if (damageTick <= 0) {
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

        if (currentVortexEffect != null) Destroy(currentVortexEffect);
        anim.SetBool("DoVortex", false); 
        yield return new WaitForSeconds(1.0f); 

        // 2. 추가 추적
        if (Random.value > 0.5f) {
            float chaseTime = 0f;
            while(chaseTime < diggingChaseTime) {
                if(player != null) {
                    float targetX = Mathf.MoveTowards(transform.position.x, player.position.x, moveSpeed * 3.0f * Time.deltaTime);
                    // ★ 맵 제한
                    targetX = Mathf.Clamp(targetX, mapMinX, mapMaxX);
                    transform.position = new Vector3(targetX, transform.position.y, 0);
                }
                chaseTime += Time.deltaTime;
                yield return null;
            }
        }

        GetComponent<Collider2D>().enabled = true; 
        anim.SetTrigger("DoDigOut"); 
        StartCoroutine(CheckDigOutDamage());
        yield return new WaitForSeconds(1.0f); 
        anim.ResetTrigger("DoDigOut");
    }

    IEnumerator CheckDigOutDamage() { float duration = 0.5f; while(duration > 0) { if(player != null) { float dist = Vector2.Distance(transform.position, player.position); if(dist < 2.5f) { PlayerStats ps = player.GetComponent<PlayerStats>(); if (ps != null) ps.TakeDamage(digOutDamage); Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>(); if(playerRb != null) { Vector2 dir = (player.position - transform.position).normalized; Vector2 knockbackDir = (dir + Vector2.up).normalized; playerRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse); } yield break; } } duration -= Time.deltaTime; yield return null; } }

    // --- [패턴 4: 돌진 (Charge)] ---
    IEnumerator Pattern_Charge()
    {
        anim.SetTrigger("DoRoar");
        yield return new WaitForSeconds(chargeStartDelay);

        anim.SetBool("IsCharging", true);

        float currentChargeTime = chargeDuration;
        
        // ★ 방향 고정 (문워크 방지: 돌진 시작 시점에만 방향 결정)
        Vector2 dir = Vector2.left;
        if (player != null) dir = (player.position.x > transform.position.x) ? Vector2.right : Vector2.left;

        // 스프라이트 방향도 고정
        float fixedScaleX = (dir.x > 0) ? -1 : 1;
        transform.localScale = new Vector3(fixedScaleX, transform.localScale.y, transform.localScale.z);

        while (currentChargeTime > 0)
        {
            transform.Translate(dir * chargeSpeed * Time.deltaTime, Space.World);
            
            // ★ 맵 제한 (이것 때문에 맵 끝으로 감)
            float clampedX = Mathf.Clamp(transform.position.x, mapMinX, mapMaxX);
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);

            currentChargeTime -= Time.deltaTime;
            yield return null;
        }

        anim.SetBool("IsCharging", false);
        
        // ★ 요청사항: 돌진 후 2초 경직
        yield return new WaitForSeconds(2.0f);
    }

    public void OnHit() { StopCoroutine("HitActionRoutine"); StartCoroutine("HitActionRoutine"); }
    IEnumerator HitActionRoutine() { float currentScaleX = Mathf.Sign(transform.localScale.x); transform.localScale = new Vector3(currentScaleX * 1.1f, 1.1f, 1f); if (sr != null) sr.color = new Color(1f, 0.4f, 0.4f); yield return new WaitForSeconds(0.05f); if (sr != null) sr.color = Color.white; transform.localScale = new Vector3(currentScaleX * 1.0f, 1.0f, 1f); }
    private void OnTriggerEnter2D(Collider2D collision) { if (isDead) return; if (collision.CompareTag("Player")) { PlayerStats ps = collision.GetComponent<PlayerStats>(); if (ps != null) ps.TakeDamage(touchDamage); } }
<<<<<<< HEAD
    public void StartDeathSequence() { if (isDead) return; isDead = true; StopAllCoroutines(); anim.SetTrigger("DoDie"); GetComponent<Collider2D>().enabled = false; if(sr != null) sr.color = Color.white; // ★★★ [여기 추가!] 엔딩 매니저에게 "엔딩 시작해!" 라고 신호 보내기 ★★★
        if (GameEndingManager.instance != null)
        {
            GameEndingManager.instance.TriggerEnding();
        } Destroy(gameObject, 3.0f); }
=======
    public void StartDeathSequence()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        anim.SetTrigger("DoDie");
        GetComponent<Collider2D>().enabled = false;
        if(sr != null) sr.color = Color.white;
        
        // ★ [추가] 2초 뒤에 포탈 생성 코루틴 시작
        StartCoroutine(SpawnPortalRoutine());
        
        Destroy(gameObject, 3.0f);
    }

    // ★ [추가] 포탈 생성 코루틴
    IEnumerator SpawnPortalRoutine()
    {
        // 보스 사망 애니메이션이 어느 정도 재생된 후 포탈 생성
        yield return new WaitForSeconds(2.0f); 

        if (endingPortalPrefab != null)
        {
            // 보스 위치보다 약간 위에 포탈 생성
            Instantiate(endingPortalPrefab, transform.position + portalSpawnOffset, Quaternion.identity);
            Debug.Log("보스 사망! 엔딩 포탈이 생성되었습니다.");
        }
        else
        {
            Debug.LogError("엔딩 포탈 프리팹이 연결되지 않았습니다!");
        }
    }
        
    


>>>>>>> 3af38861f8e43197c32a6576f845799e2b4d9d92

    // ★ [추가] 맵 제한 구역을 눈으로 확인하는 기능
    // 인스펙터에서 Map Min X, Map Max X를 조절하면 초록색 선이 움직입니다.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 leftBound = new Vector3(mapMinX, transform.position.y, 0);
        Vector3 rightBound = new Vector3(mapMaxX, transform.position.y, 0);
        
        // 제한 구역 표시
        Gizmos.DrawLine(new Vector3(mapMinX, -100, 0), new Vector3(mapMinX, 100, 0));
        Gizmos.DrawLine(new Vector3(mapMaxX, -100, 0), new Vector3(mapMaxX, 100, 0));
    }
}