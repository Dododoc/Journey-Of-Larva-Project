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
    public float hpPerLevel = 20f;      // 레벨당 체력 +20
    public float attackPerLevel = 2f;   // 레벨당 공격력 +2
    public float defensePerLevel = 1f;  // 레벨당 방어력 +1

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

        // ==========================================
        // ★ [수정됨] GameManager가 정확한 포탈 위치에 소환해 주었으므로, 
        // 이제 엉뚱한 곳으로 순간이동하는 코드를 지우고 그 자리표를 시작 위치로 저장만 합니다.
        // ==========================================
        startPosition = transform.position;

        if (GameManager.instance != null)
        {
            if (GameManager.instance.currentCharacter != GameManager.CharacterType.Larva)
            {
                maxHp = 200f; // 진화체는 기본 체력이 200부터 시작
            }
            else
            {
                maxHp = 100f; 
            }

            currentLevel = GameManager.instance.globalLevel;
            currentExp = GameManager.instance.globalXP;

            bonusAttack = GameManager.instance.globalBonusAttack;
            bonusDefense = GameManager.instance.globalBonusDefense;
            bonusMaxHp = GameManager.instance.globalBonusMaxHp;

            if (GameManager.instance.currentCharacter != GameManager.CharacterType.Larva)
            {
                if (currentLevel == 1 && currentExp > 0)
                {
                    currentExp = 0;
                    GameManager.instance.globalXP = 0;
                }
            }
            
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
        if (Input.GetKeyDown(KeyCode.J)) GainExp(50); 
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);

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
        
        // 레벨업 시 최대 체력으로 회복!
        currentHp = TotalMaxHp;  
        
        SaveStatsToManager();

        if (currentLevel >= 3 && QuestManager.instance != null)
        {
            QuestManager.instance.CompleteQuest(3);
        }

        if(currentExp >= expToNextLevel) LevelUp();
        
        // 레벨업 시 HUD에 증가한 최대 체력 반영을 위해 UI 갱신!
        UpdateUI();
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
        bonusMaxHp += (currentLevel - 1) * hpPerLevel;
        bonusAttack += (currentLevel - 1) * attackPerLevel;
        bonusDefense += (currentLevel - 1) * defensePerLevel;

        switch (selectedPathIndex)
        {
            case 0: bonusAttack += 10f; break;  
            case 1: bonusDefense += 10f; break; 
            case 2: bonusMaxHp += 50f; break;   
        }

        currentLevel = 1;
        currentExp = 0;
        CalculateNextLevelExp();
        
        maxHp = 200f; 
        currentHp = TotalMaxHp; 

        SaveStatsToManager();
        UpdateUI();

        if (QuestManager.instance != null)
        {
            QuestManager.instance.gameObject.SetActive(false);
        }
        else
        {
            GameObject questUI = GameObject.Find("QuestCanvas"); 
            if (questUI != null) questUI.SetActive(false);
        }

        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateEvolutionUI(selectedPathIndex + 1);
            uiManager.CloseEvolutionPopup();
        }
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