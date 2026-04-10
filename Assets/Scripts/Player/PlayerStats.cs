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
    // ★ [핵심 4] 레벨이 리셋되지 않도록 Evolve 로직에서 강제 1레벨 변환 코드를 지웠습니다!
    // ==========================================
    public void Evolve(int selectedPathIndex)
    {
        switch (selectedPathIndex)
        {
            case 0: bonusAttack += 10f; break;  
            case 1: bonusDefense += 10f; break; 
            case 2: bonusMaxHp += 50f; break;   
        }

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