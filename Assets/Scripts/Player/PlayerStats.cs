using UnityEngine;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    private PlayerHUD playerHUD; 
    public GameObject hitEffectPrefab; 

    [Header("Level Info")]
    public int currentLevel = 1;
    public float currentExp = 0;
    public float expToNextLevel;

    [Header("Base Stats (1레벨 기준)")]
    public float baseAttack = 10f;
    public float baseDefense = 5f;
    public float maxHp = 100f;
    public float currentHp;
    [Header("Status Effects (디버프 및 상태이상)")]
    public float speedMultiplier = 1.0f; // 속도 배율
    public bool isJumpDisabled = false;  // 점프 불가 상태
    public bool isGrabbedByBoss = false; // 보스에게 잡힌 상태
    private float defaultGravity;

    [Header("Evolution Bonus Stats")]
    public float bonusAttack = 0f;
    public float bonusDefense = 0f;
    public float bonusMaxHp = 0f;

    private Vector3 startPosition;
    private SpriteRenderer sr;

    public float TotalAttack => (baseAttack * currentLevel) + bonusAttack;
    public float TotalDefense => (baseDefense * currentLevel) + bonusDefense;
    public float TotalMaxHp => (maxHp * currentLevel) + bonusMaxHp;
    
    [Header("Interaction UI")]
    public GameObject interactPrompt; 
    private int nearbyItemCount = 0;

    [Header("Death Setting")]
    public float fallDeathY = -40f;
    
    // ★ [추가] 죽었는지 체크하는 변수
    private bool isDead = false;

    void Start()
    {
        if (playerHUD == null) playerHUD = FindFirstObjectByType<PlayerHUD>();
        sr = GetComponent<SpriteRenderer>();

        startPosition = transform.position;

        if (GameManager.instance != null)
        {
            currentLevel = GameManager.instance.globalLevel;
            currentExp = GameManager.instance.globalXP;

            if (GameManager.instance.currentCharacter != GameManager.CharacterType.Larva)
            {
                if (currentLevel == 1 && currentExp > 0)
                {
                    Debug.LogWarning("⚠️ 진화 직후 잔여 경험치 감지! 강제로 0으로 초기화합니다.");
                    currentExp = 0;
                    GameManager.instance.globalXP = 0;
                }
            }
            
            CalculateNextLevelExp();
            currentHp = TotalMaxHp; 
        }
        else
        {
            currentHp = TotalMaxHp;
            CalculateNextLevelExp();
        }

        UpdateUI(); 

        if (UIManager.instance != null && GameManager.instance != null)
        {
            UIManager.instance.UpdateEvolutionUI((int)GameManager.instance.currentCharacter);
        }
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if(rb != null) defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) GainExp(50); 
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);

        if (transform.position.y <= fallDeathY && currentHp > 0 && !isDead)
        {
            Debug.Log("으아악! 떨어졌다!");
            currentHp = 0;
            UpdateUI();
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        currentHp += amount;
        if (currentHp > TotalMaxHp) currentHp = TotalMaxHp; 
        UpdateUI();
    }

    public void GainExp(float amount)
    {
        if (isDead) return;
        currentExp += amount;
        if (currentExp >= expToNextLevel) LevelUp();
        
        SaveStatsToManager();
        UpdateUI(); 
    }
    public void SetDebuff(bool active, float speedMult)
    {
        if (active) 
        { 
            isJumpDisabled = true; 
            speedMultiplier = speedMult; 
            if (sr != null) sr.color = new Color(0.6f, 1f, 0.6f); 
        }
        else 
        { 
            isJumpDisabled = false; 
            speedMultiplier = 1.0f; 
            if (sr != null) sr.color = Color.white; 
        }
    }

    public void SetGrabbed(bool grabbed)
    {
        isGrabbedByBoss = grabbed;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (grabbed) 
            { 
                rb.linearVelocity = Vector2.zero; 
                rb.gravityScale = 0f; 
                rb.bodyType = RigidbodyType2D.Kinematic; // ★ 추가: 충돌/밀려남 완벽 무시
            } 
            else 
            { 
                rb.gravityScale = defaultGravity; 
                rb.bodyType = RigidbodyType2D.Dynamic; // ★ 추가: 풀려나면 물리 엔진 다시 복구
            }
        }
    }

    void LevelUp()
    {
        currentLevel++;
        currentExp -= expToNextLevel; 
        CalculateNextLevelExp(); 
        currentHp = TotalMaxHp;  
        
        SaveStatsToManager();

        if (currentLevel >= 3 && QuestManager.instance != null)
        {
            QuestManager.instance.CompleteQuest(3);
        }

        if(currentExp >= expToNextLevel) LevelUp();
    }

    void SaveStatsToManager()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.globalLevel = currentLevel;
            GameManager.instance.globalXP = (int)currentExp; 
        }
    }

    void CalculateNextLevelExp()
    {
        expToNextLevel = currentLevel * 100f * (1f + currentLevel * 0.1f);
    }

    void UpdateUI()
    {
        if (playerHUD != null)
        {
            playerHUD.UpdateHP(currentHp, TotalMaxHp);
            playerHUD.UpdateXP(currentExp, expToNextLevel);
            playerHUD.UpdateLevel(currentLevel);
        }
    }

    public void Evolve(int selectedPathIndex)
    {
        float bonusMultiplier = currentLevel * 0.5f; 
        bonusAttack += 2f * bonusMultiplier;
        bonusDefense += 1f * bonusMultiplier;
        bonusMaxHp += 10f * bonusMultiplier;

        switch (selectedPathIndex)
        {
            case 0: bonusAttack += 10f; break;
            case 1: bonusDefense += 10f; break;
            case 2: bonusMaxHp += 50f; break;
        }

        currentLevel = 1;
        currentExp = 0;
        
        CalculateNextLevelExp();
        currentHp = TotalMaxHp;

        SaveStatsToManager();
        UpdateUI();

        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateEvolutionUI(selectedPathIndex + 1);
            uiManager.CloseEvolutionPopup();
        }
    }
    
    // ==========================================
    // ★ [수정됨] 무적 판정 및 물리 정지 로직 반영
    // ==========================================
    public void TakeDamage(float damage)
    {
        // ★ 이미 죽었다면 데미지 무시!
        if (isDead) return;

        float defenseFactor = 100f / (100f + TotalDefense);
        float finalDamage = damage * defenseFactor;
        finalDamage = Mathf.Max(1f, finalDamage); // 최소 데미지 1 보장
        
        currentHp -= finalDamage;

        StartCoroutine(HitFlashRoutine());
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        // =========================================================
        // ★ [수정됨] 부동소수점 오차 방지: 0.01 이하라면 무조건 0으로 만들고 즉시 사망!
        // =========================================================
        if (currentHp <= 0.01f) 
        {
            currentHp = 0;
            UpdateUI(); 
            Die();
        }
        else
        {
            UpdateUI(); 
        }
    }

    IEnumerator HitFlashRoutine()
    {
        if (sr != null)
        {
            sr.color = new Color(1f, 0.4f, 0.4f); 
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white; 
        }
    }

    void Die()
    {
        Debug.Log("플레이어 사망!");
        
        // ★ 1. 죽음 판정 (중복 데미지 방지)
        isDead = true; 

        // ★ 2. 충돌체 끄기 (적들이 통과하게 만듦)
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false; 
        }

        // ★ 3. 리지드바디 정지 (날아가던 도중이었다면 허공에 정지)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
            rb.gravityScale = 0f;       
            rb.bodyType = RigidbodyType2D.Kinematic; // 외부 넉백 무시
        }

        // ★ 4. 사망 모션 재생 (컨트롤러의 Animator 활용)
        Animator anim = GetComponent<Animator>();

        // ★ 5. 슬로우 모션 및 게임 오버 UI 연출 코루틴 시작
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        // [연출 1] 히트 스탑 (0.1초 화면 완전 정지)
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.1f); 

        // [연출 2] 슬로우 모션으로 쓰러짐 감상 (1.5초 대기)
        Time.timeScale = 0.3f; 
        yield return new WaitForSecondsRealtime(1.5f); 

        // [연출 3] 완전 정지 후 UI 띄우기
        Time.timeScale = 0f; 
        
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
    }

    public void Respawn()
    {
        isDead = false; // ★ 부활 시 죽음 판정 초기화
        currentLevel = 1;
        currentExp = 0;
        SaveStatsToManager();
        CalculateNextLevelExp();
        currentHp = TotalMaxHp;
        
        transform.position = startPosition;

        // ★ 부활 시 리지드바디 및 콜라이더 원상복구
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if(rb != null) 
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f; // 기본 중력값 (게임에 맞게 수정 필요 시 수정)
            rb.linearVelocity = Vector2.zero; 
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = true; 
        }

        if (sr != null) sr.color = Color.white;
        UpdateUI();
    }

    private Coroutine currentPoisonCoroutine;
    public void ApplyPoison(float totalDamage, float duration)
    {
        if (isDead) return; // ★ 죽었으면 독 안 걸림

        if (currentPoisonCoroutine != null)
        {
            StopCoroutine(currentPoisonCoroutine);
        }
        currentPoisonCoroutine = StartCoroutine(PoisonRoutine(totalDamage, duration));
    }

    IEnumerator PoisonRoutine(float totalDamage, float duration)
    {
        float tickInterval = 0.5f; 
        int ticks = Mathf.FloorToInt(duration / tickInterval);
        float damagePerTick = totalDamage / ticks;
        
        Color poisonColor = new Color(0.6f, 0f, 0.8f);

        for (int i = 0; i < ticks; i++)
        {
            if (isDead) yield break; // ★ 도중에 죽으면 독 데미지 중단

            if (sr != null) sr.color = poisonColor; 
            TakeDamage(damagePerTick); 
            
            yield return new WaitForSeconds(0.15f);
            
            if (sr != null) sr.color = Color.white; 
            
            yield return new WaitForSeconds(tickInterval - 0.15f);
        }

        currentPoisonCoroutine = null;
    }

    public void AddNearbyItem()
    {
        if (isDead) return;
        nearbyItemCount++;
        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    public void RemoveNearbyItem()
    {
        nearbyItemCount--;
        if (nearbyItemCount <= 0)
        {
            nearbyItemCount = 0;
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }
}