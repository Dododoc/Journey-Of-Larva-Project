using UnityEngine;
using System.Collections;
using TMPro;

public class ScarecrowAI : BaseEnemyAI 
{
    [Header("허수아비 공격 설정")]
    public float attackHitDelay = 0.5f; 
    public float attackCooldown = 2.0f; 
    private float lastAttackTime;
    private bool isDead = false;

    [Header("궁극기 해금 연출")]
    public GameObject poofVFXPrefab; 
    public GameObject sandstormVFXPrefab; 
    
    // ==========================================
    // ★ [타이밍 수정] 총 5초 동안 유지되도록 두 단계로 나눴습니다.
    // ==========================================
    public float sandstormHoverDuration = 2.0f;  // 1. 허수아비 자리에서 기운을 모으며 머무르는 시간
    public float sandstormTravelDuration = 3.0f; // 2. 개미를 향해 날아가는 시간 (합쳐서 총 5초)

    public int vfxSortingOrder = 50; 
    public TextMeshProUGUI unlockUIText;  

    private EnemyStats myStats; 

    new void Start()
    {
        base.Start(); 
        
        myStats = GetComponent<EnemyStats>();
        if (unlockUIText != null) unlockUIText.gameObject.SetActive(false);
    }

    protected override void Update() 
    {
        if (isDead || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange && !isAttacking && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        LookAt(player.position.x);

        if (anim != null) anim.SetTrigger("Attack"); 
        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                float damage = (myStats != null) ? myStats.attackDamage : 10f;
                player.GetComponent<PlayerStats>()?.TakeDamage(damage);
            }
        }

        yield return new WaitForSeconds(attackCooldown - attackHitDelay);
        isAttacking = false;
    }

    public void StartDeathSequence()
    {
        if (isDead) return;
        isDead = true;
        
        StopAllCoroutines(); 
        StartCoroutine(DestroyAndUnlockRoutine());
    }

    IEnumerator DestroyAndUnlockRoutine()
    {
        if (myCollider != null) myCollider.enabled = false;
        if (sr != null) sr.enabled = false;

        // 1. 펑! 터지는 이펙트
        if (poofVFXPrefab != null)
        {
            // ★ [수정됨] 허수아비 위치에서 Y축으로 1.0f 만큼 위에서 터지도록 오프셋 추가
            Vector3 poofSpawnPos = transform.position + new Vector3(0, 5.0f, 0);
            GameObject poof = Instantiate(poofVFXPrefab, poofSpawnPos, Quaternion.identity);
            
            SetVFXSortingOrder(poof, vfxSortingOrder);
            Destroy(poof, 2f); 
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        // 2. 모래폭풍 생성 및 5초 연출 시작
        if (sandstormVFXPrefab != null && playerObj != null)
        {
            // ★ 모래폭풍도 같이 위에서 생성하고 싶다면 여기도 똑같이 적용 가능합니다!
            Vector3 stormSpawnPos = transform.position + new Vector3(0, 1.0f, 0);
            GameObject sandstorm = Instantiate(sandstormVFXPrefab, stormSpawnPos, Quaternion.identity);
            
            SetVFXSortingOrder(sandstorm, vfxSortingOrder);

            StartCoroutine(MoveAndDissipateSandstormRoutine(sandstorm, playerObj));
        }

        // 전체 연출(대기 2초 + 비행 3초 + 텍스트 유지 시간)이 끝날 때까지 넉넉히 기다렸다가 허수아비 삭제
        yield return new WaitForSeconds(sandstormHoverDuration + sandstormTravelDuration + 5f);
        Destroy(gameObject); 
    }

    // ==========================================
    // ★ [핵심 연출 로직] 5초 대기/이동 -> 도달 즉시 해금!
    // ==========================================
    IEnumerator MoveAndDissipateSandstormRoutine(GameObject sandstorm, GameObject targetPlayer)
    {
        if (sandstorm == null || targetPlayer == null) yield break;

        // 1. 생성 시점에 이미 플레이어보다 앞에 있도록 설정
        SetVFXLayerInFrontOfPlayer(sandstorm, targetPlayer);

        // 허수아비 자리에서 대기
        yield return new WaitForSeconds(sandstormHoverDuration);

        Vector3 startPos = sandstorm.transform.position;
        float timer = 0f;

        // 2. 개미에게 이동
        while (timer < sandstormTravelDuration)
        {
            if (sandstorm == null || targetPlayer == null) yield break;

            timer += Time.deltaTime;
            float progress = timer / sandstormTravelDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 targetPos = targetPlayer.transform.position + new Vector3(0, 1.0f, 0);
            sandstorm.transform.position = Vector3.Lerp(startPos, targetPos, smoothProgress);
            
            // 이동 중에도 플레이어의 레이어가 바뀔 수 있으므로 계속 앞에 있도록 갱신 (선택 사항)
            SetVFXLayerInFrontOfPlayer(sandstorm, targetPlayer);
            
            yield return null;
        }

        // 도달 및 해금 로직 (기존과 동일)
        if(sandstorm != null && targetPlayer != null)
            sandstorm.transform.position = targetPlayer.transform.position + new Vector3(0, 1.0f, 0);

        AntController ant = targetPlayer.GetComponent<AntController>();
        if (ant != null) ant.UnlockUltimate(); 

        if (unlockUIText != null) StartCoroutine(AnimateUnlockTextRoutine());

        // 흩어지기 연출 (기존과 동일)
        if (sandstorm != null)
        {
            ParticleSystem[] particleSystems = sandstorm.GetComponentsInChildren<ParticleSystem>();
            float maxLifetime = 0f;
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (ps.main.startLifetime.constant > maxLifetime)
                    maxLifetime = ps.main.startLifetime.constant;
            }
            Destroy(sandstorm, maxLifetime);
        }
    }

    // ★ [핵심 추가] 플레이어의 레이어 정보를 읽어서 그보다 무조건 앞으로 보내는 함수
    private void SetVFXLayerInFrontOfPlayer(GameObject vfxObj, GameObject playerObj)
    {
        SpriteRenderer playerSr = playerObj.GetComponent<SpriteRenderer>();
        if (playerSr == null) return;

        // 플레이어의 현재 레이어 이름과 순서를 가져옵니다.
        string playerLayer = playerSr.sortingLayerName;
        int playerOrder = playerSr.sortingOrder;

        // 모래폭풍의 모든 파티클 렌더러를 찾아서 설정 변경
        ParticleSystemRenderer[] renderers = vfxObj.GetComponentsInChildren<ParticleSystemRenderer>();
        foreach (ParticleSystemRenderer renderer in renderers)
        {
            // 플레이어와 같은 레이어(예: "Player" 레이어)에 놓되,
            renderer.sortingLayerName = playerLayer;
            // 순서를 플레이어보다 큰 값(+10 등)으로 설정하여 항상 앞에 그리게 합니다.
            renderer.sortingOrder = playerOrder + 10;
        }
    }

    private void SetVFXSortingOrder(GameObject vfxObj, int order)
    {
        ParticleSystemRenderer[] renderers = vfxObj.GetComponentsInChildren<ParticleSystemRenderer>();
        foreach (ParticleSystemRenderer renderer in renderers)
        {
            renderer.sortingOrder = order;
        }
    }

    IEnumerator AnimateUnlockTextRoutine()
    {
        unlockUIText.gameObject.SetActive(true);
        unlockUIText.text = "모래폭풍 절단 해금!";
        
        unlockUIText.color = new Color(unlockUIText.color.r, unlockUIText.color.g, unlockUIText.color.b, 0);
        RectTransform textRect = unlockUIText.GetComponent<RectTransform>();
        textRect.localScale = Vector3.one * 0.5f;

        float timer = 0f;
        float appearDuration = 0.5f;
        while (timer < appearDuration)
        {
            timer += Time.unscaledDeltaTime; 
            float progress = timer / appearDuration;
            
            float scale = Mathf.Lerp(0.5f, 1.2f, progress); 
            float alpha = Mathf.Lerp(0f, 1f, progress);

            if(textRect != null) textRect.localScale = Vector3.one * scale;
            unlockUIText.color = new Color(unlockUIText.color.r, unlockUIText.color.g, unlockUIText.color.b, alpha);
            yield return null;
        }
        
        textRect.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(2.5f); 

        timer = 0f;
        float fadeDuration = 1.0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / fadeDuration;
            float alpha = Mathf.Lerp(1f, 0f, progress);
            unlockUIText.color = new Color(unlockUIText.color.r, unlockUIText.color.g, unlockUIText.color.b, alpha);
            yield return null;
        }

        unlockUIText.gameObject.SetActive(false);
    }
}