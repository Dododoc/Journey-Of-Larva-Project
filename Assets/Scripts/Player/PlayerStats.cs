using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public PlayerHUD playerHUD; 

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

    public float TotalAttack => (baseAttack * currentLevel) + bonusAttack;
    public float TotalDefense => (baseDefense * currentLevel) + bonusDefense;
    public float TotalMaxHp => (maxHp * currentLevel) + bonusMaxHp;

    void Start()
    {
        if (playerHUD == null) playerHUD = FindObjectOfType<PlayerHUD>();
        startPosition = transform.position;
        currentHp = TotalMaxHp;
        CalculateNextLevelExp();
        UpdateUI(); 
        
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null) uiManager.UpdateEvolutionUI(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) GainExp(50); 
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    // ★ [추가] 체력 회복 함수 (흡혈용)
    public void Heal(float amount)
    {
        currentHp += amount;
        if (currentHp > TotalMaxHp) currentHp = TotalMaxHp; // 최대 체력 초과 방지
        
        Debug.Log($"흡혈! 체력 {amount} 회복");
        UpdateUI();
    }

    public void GainExp(float amount)
    {
        currentExp += amount;
        if (currentExp >= expToNextLevel) LevelUp();
        UpdateUI(); 
    }

    void LevelUp()
    {
        currentLevel++;
        currentExp -= expToNextLevel; 
        CalculateNextLevelExp(); 
        currentHp = TotalMaxHp;  
        if(currentExp >= expToNextLevel) LevelUp();
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
        // 퍼센트 감소 공식
        float defenseFactor = 100f / (100f + TotalDefense);
        float finalDamage = damage * defenseFactor;
        finalDamage = Mathf.Max(1f, finalDamage);
        
        currentHp -= finalDamage;

        if (currentHp < 0) currentHp = 0;
        UpdateUI(); 

        if (currentHp <= 0) Die();
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
        CalculateNextLevelExp();
        currentHp = TotalMaxHp;
        transform.position = startPosition;
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if(rb != null) rb.linearVelocity = Vector2.zero;

        UpdateUI();
    }
}