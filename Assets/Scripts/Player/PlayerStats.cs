using UnityEngine;
using System.Collections; 

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public PlayerHUD playerHUD; 
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

    [Header("Evolution Bonus Stats")]
    public float bonusAttack = 0f;
    public float bonusDefense = 0f;
    public float bonusMaxHp = 0f;

    private Vector3 startPosition;
    private SpriteRenderer sr; 

    public float TotalAttack => (baseAttack * currentLevel) + bonusAttack;
    public float TotalDefense => (baseDefense * currentLevel) + bonusDefense;
    public float TotalMaxHp => (maxHp * currentLevel) + bonusMaxHp;

    void Start()
    {
        if (playerHUD == null) playerHUD = FindObjectOfType<PlayerHUD>();
        sr = GetComponent<SpriteRenderer>(); 

        startPosition = transform.position;

        // ★ [데이터 로드 로직]
        if (GameManager.instance != null)
        {
            currentLevel = GameManager.instance.globalLevel;
            currentExp = GameManager.instance.globalXP;

            // [방어 코드] 진화 상태인데 1레벨 경험치 잔존 시 초기화
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
            
            Debug.Log($"[PlayerStats] 로드 완료: Lv.{currentLevel}, XP.{currentExp}");
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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) GainExp(50); 
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    public void Heal(float amount)
    {
        currentHp += amount;
        if (currentHp > TotalMaxHp) currentHp = TotalMaxHp; 
        UpdateUI();
    }

    public void GainExp(float amount)
    {
        currentExp += amount;
        if (currentExp >= expToNextLevel) LevelUp();
        
        SaveStatsToManager(); // ★ [추가] 경험치 얻으면 저장!
        UpdateUI(); 
    }

    void LevelUp()
    {
        currentLevel++;
        currentExp -= expToNextLevel; 
        CalculateNextLevelExp(); 
        currentHp = TotalMaxHp;  
        
        SaveStatsToManager(); // ★ [추가] 레벨업하면 저장!

        if(currentExp >= expToNextLevel) LevelUp();
    }

    // ★ [추가] 중요: 매니저에 현재 상태를 저장하는 함수
    void SaveStatsToManager()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.globalLevel = currentLevel;
            GameManager.instance.globalXP = currentExp;
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

        SaveStatsToManager(); // ★ 진화 후 초기화된 정보 저장
        UpdateUI();

        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateEvolutionUI(selectedPathIndex + 1);
            uiManager.CloseEvolutionPopup();
        }
    }
    
    public void TakeDamage(float damage)
    {
        float defenseFactor = 100f / (100f + TotalDefense);
        float finalDamage = damage * defenseFactor;
        finalDamage = Mathf.Max(1f, finalDamage);
        
        currentHp -= finalDamage;

        StartCoroutine(HitFlashRoutine());
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (currentHp < 0) currentHp = 0;
        UpdateUI(); 

        if (currentHp <= 0) Die();
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
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null) uiManager.ShowGameOver();
    }

    public void Respawn()
    {
        currentLevel = 1;
        currentExp = 0;
        
        SaveStatsToManager(); // ★ 부활 시 초기화 정보 저장

        CalculateNextLevelExp();
        currentHp = TotalMaxHp;
        transform.position = startPosition;
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        // ★ [수정] 중복된 코드 삭제 및 Unity 버전 호환성 유지
        if(rb != null) rb.linearVelocity = Vector2.zero; 

        if (sr != null) sr.color = Color.white;

        UpdateUI();
    }

    public void ApplyPoison(float totalDamage, float duration)
    {
        StopCoroutine("PoisonRoutine");
        StartCoroutine(PoisonRoutine(totalDamage, duration));
    }

    IEnumerator PoisonRoutine(float totalDamage, float duration)
    {
        float tickInterval = 0.5f; 
        int ticks = Mathf.FloorToInt(duration / tickInterval);
        float damagePerTick = totalDamage / ticks;
        
        SpriteRenderer playerSr = GetComponent<SpriteRenderer>();
        Color originalColor = (playerSr != null) ? playerSr.color : Color.white;

        for (int i = 0; i < ticks; i++)
        {
            if (playerSr != null) playerSr.color = new Color(0.4f, 1f, 0.4f); 
            TakeDamage(damagePerTick); 
            yield return new WaitForSeconds(0.1f);
            if (playerSr != null) playerSr.color = originalColor; 
            yield return new WaitForSeconds(tickInterval - 0.1f);
        }
    }
}