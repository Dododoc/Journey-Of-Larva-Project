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

    [Header("Base Stats (기본 스탯)")]
    public float baseAttack = 10f;
    public float baseDefense = 5f;
    public float maxHp = 100f;
    public float currentHp;

    [Header("Growth Per Level (레벨당 성장치)")]
    public float hpPerLevel = 20f;      
    public float attackPerLevel = 2f;   
    public float defensePerLevel = 1f;  

    [Header("Status Effects (디버프 및 상태이상)")]
    public float speedMultiplier = 1.0f; 
    public bool isJumpDisabled = false;  
    public bool isGrabbedByBoss = false; 
    private float defaultGravity;

    [Header("Evolution Bonus Stats")]
    public float bonusAttack = 0f;
    public float bonusDefense = 0f;
    public float bonusMaxHp = 0f;

    private Vector3 startPosition;
    private SpriteRenderer sr;

    public float TotalAttack => baseAttack + ((currentLevel - 1) * attackPerLevel) + bonusAttack;
    public float TotalDefense => baseDefense + ((currentLevel - 1) * defensePerLevel) + bonusDefense;
    public float TotalMaxHp => maxHp + ((currentLevel - 1) * hpPerLevel) + bonusMaxHp;
    
    [Header("Interaction UI")]
    public GameObject interactPrompt; 
    private int nearbyItemCount = 0;

    [Header("Death Setting")]
    public float fallDeathY = -40f;
    private bool isDead = false;

    void Start()
    {
        if (playerHUD == null) playerHUD = FindFirstObjectByType<PlayerHUD>();
        sr = GetComponent<SpriteRenderer>();

        startPosition = transform.position;

        if (GameManager.instance != null)
        {
            // 진화 형태라면 기본 체력통이 200부터 시작합니다.
            if (GameManager.instance.currentCharacter != GameManager.CharacterType.Larva)
            {
                maxHp = 200f; 
            }
            else
            {
                maxHp = 100f; 
            }

            // GameManager에서 경험치와 레벨을 고스란히 가져옵니다 (초기화 없음!)
            currentLevel = GameManager.instance.globalLevel;
            currentExp = GameManager.instance.globalXP;

            bonusAttack = GameManager.instance.globalBonusAttack;
            bonusDefense = GameManager.instance.globalBonusDefense;
            bonusMaxHp = GameManager.instance.globalBonusMaxHp;

            CalculateNextLevelExp();

            if (GameManager.instance.globalCurrentHp > 0)
            {
                currentHp = GameManager.instance.globalCurrentHp;
                if (currentHp > TotalMaxHp) currentHp = TotalMaxHp; 
            }
            else
            {
                currentHp = TotalMaxHp; 
            }
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

    public void SaveStatsToManager()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.globalLevel = currentLevel;
            GameManager.instance.globalXP = (int)currentExp; 
            
            GameManager.instance.globalCurrentHp = currentHp;
            GameManager.instance.globalBonusAttack = bonusAttack;
            GameManager.instance.globalBonusDefense = bonusDefense;
            GameManager.instance.globalBonusMaxHp = bonusMaxHp;
        }
    }

    void Update()
    {
        // [기존 테스트 키]
        if (Input.GetKeyDown(KeyCode.J)) GainExp(50); 
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);

        // ==========================================
        // ★ [개발자 전용 치트키 모음] (출시할 때는 이 부분을 지워주세요!)
        // ==========================================
        
        // 1. [알파벳 O] 데스노트: 맵에 있는 모든 적(보스 포함) 즉사!
        if (Input.GetKeyDown(KeyCode.O))
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                // 적들에게 무려 9999의 데미지를 강제로 먹여서 즉사시킵니다.
                enemy.SendMessage("TakeDamage", 9999f, SendMessageOptions.DontRequireReceiver);
            }
            Debug.Log($"[Cheat] 맵에 있는 {enemies.Length}마리의 적(전갈 포함)을 모두 즉사시켰습니다!");
        }

        // 2. [알파벳 I] 신 모드: 체력을 9999로 만들고 꽉 채웁니다.
        if (Input.GetKeyDown(KeyCode.I))
        {
            maxHp = 9999f;
            currentHp = maxHp;
            UpdateUI();
            Debug.Log("[Cheat] 체력이 9999로 고정되었습니다! (신 모드)");
        }
        
        // 3. [알파벳 P] 축지법: 현재 맵의 '다음 맵으로 가는 포탈(End)' 앞으로 순간이동!
        if (Input.GetKeyDown(KeyCode.P))
        {
            ConditionalPortal[] portals = FindObjectsByType<ConditionalPortal>(FindObjectsSortMode.None);
            foreach(var portal in portals)
            {
                if (portal.myPortalID == "End") // 끝 포탈 찾기
                {
                    transform.position = portal.transform.position;
                    Debug.Log("[Cheat] 다음 맵으로 가는 포탈 앞으로 순간이동했습니다!");
                    break;
                }
            }
        }
        // ==========================================

        // 기존 낙사 로직 유지
        if (transform.position.y <= fallDeathY && currentHp > 0 && !isDead)
        {
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
                rb.bodyType = RigidbodyType2D.Kinematic; 
            } 
            else 
            { 
                rb.gravityScale = defaultGravity; 
                rb.bodyType = RigidbodyType2D.Dynamic; 
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
        
        UpdateUI();
    }

    void CalculateNextLevelExp()
    {
        expToNextLevel = currentLevel * 100f * (1f + currentLevel * 0.1f);
    }

    // ==========================================
    // ★ [핵심 3] UpdateUI를 public으로 열고, 연결이 끊기면 스스로 찾도록 강화했습니다.
    // ==========================================
    public void UpdateUI()
    {
        if (playerHUD == null) playerHUD = FindFirstObjectByType<PlayerHUD>();

        if (playerHUD != null)
        {
            playerHUD.UpdateHP(currentHp, TotalMaxHp);
            playerHUD.UpdateXP(currentExp, expToNextLevel);
            playerHUD.UpdateLevel(currentLevel);
        }
    }

    // ==========================================
    // ★ [핵심 고침] Evolve 함수 통째로 덮어쓰기! 
    // 불필요한 UI 끄기 로직을 삭제하여 Managers가 꺼지는 버그를 완벽 해결했습니다.
    // ==========================================
    public void Evolve(int selectedPathIndex)
    {
        // 1. 선택한 진화 특성에 따른 영구 보너스
        switch (selectedPathIndex)
        {
            case 0: bonusAttack += 10f; break;  
            case 1: bonusDefense += 10f; break; 
            case 2: bonusMaxHp += 50f; break;   
        }

        // 2. 진화 시 기본 체급 상승 (애벌레 100 -> 진화 200)
        maxHp = 200f; 
        
        // 3. 체력 100% 풀피 회복
        currentHp = TotalMaxHp; 

        // 4. GameManager에 안전하게 저장 후 내 체력바 갱신
        SaveStatsToManager();
        UpdateUI();
        
        // (기존에 있던 QuestManager나 UIManager를 건드리는 코드는 전부 삭제했습니다!)
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        Larva_PlayerController larva = GetComponent<Larva_PlayerController>();
        if (larva != null && larva.isInvincible) return;

        AntController ant = GetComponent<AntController>();
        if (ant != null && ant.isInvincible) return;

        BeetleController beetle = GetComponent<BeetleController>();
        if (beetle != null && beetle.isInvincible) return;

        float defenseFactor = 100f / (100f + TotalDefense);
        float finalDamage = damage * defenseFactor;
        finalDamage = Mathf.Max(1f, finalDamage); 
        
        currentHp -= finalDamage;

        StartCoroutine(HitFlashRoutine());
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

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
        isDead = true; 

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false; 
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
            rb.gravityScale = 0f;       
            rb.bodyType = RigidbodyType2D.Kinematic; 
        }

        Animator anim = GetComponent<Animator>();
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.1f); 

        Time.timeScale = 0.3f; 
        yield return new WaitForSecondsRealtime(1.5f); 

        Time.timeScale = 0f; 
        
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
    }

    public void Respawn()
    {
        isDead = false; 
        currentLevel = 1;
        currentExp = 0;
        SaveStatsToManager();
        CalculateNextLevelExp();
        currentHp = TotalMaxHp;
        
        transform.position = startPosition;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if(rb != null) 
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f; 
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
        if (isDead) return; 

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
            if (isDead) yield break; 

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