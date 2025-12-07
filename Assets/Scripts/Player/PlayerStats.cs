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

    // 시작 위치 저장용 변수
    private Vector3 startPosition;

    public float TotalAttack => (baseAttack * currentLevel) + bonusAttack;
    public float TotalDefense => (baseDefense * currentLevel) + bonusDefense;
    public float TotalMaxHp => (maxHp * currentLevel) + bonusMaxHp;

    void Start()
    {
        if (playerHUD == null)
            playerHUD = FindObjectOfType<PlayerHUD>();

        // ★ 게임 시작 시점의 위치를 기억해둡니다 (리스폰 용)
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
        // Debug.Log($"레벨 업! Lv.{currentLevel}");
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
        // ... (기존 진화 로직 유지) ...
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
        float finalDamage = Mathf.Max(1f, damage - TotalDefense);
        currentHp -= finalDamage;

        if (currentHp < 0) currentHp = 0;
        UpdateUI(); 

        // 체력이 0이 되면 사망 처리
        if (currentHp <= 0)
        {
            Die();
        }
    }

    // ★ 사망 처리 함수
    void Die()
    {
        Debug.Log("플레이어 사망!");
        
        // 1. 캐릭터 조작 비활성화 (선택 사항: 원하시면 추가)
        // GetComponent<PlayerController>().enabled = false;

        // 2. UIManager에게 게임 오버 창 띄우라고 지시
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
    }

    // ★ 부활(리스폰) 함수 - UIManager가 호출함
    public void Respawn()
    {
        Debug.Log("플레이어 부활!");

        // 1. 레벨 1로 초기화 (하지만 bonusStats는 초기화 안 함 -> 진화 유지됨)
        currentLevel = 1;
        currentExp = 0;
        CalculateNextLevelExp();

        // 2. 체력 풀회복
        currentHp = TotalMaxHp;

        // 3. 시작 위치로 이동
        transform.position = startPosition;
        
        // 4. 리지드바디 속도 초기화 (낙하 중이었으면 멈추게)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if(rb != null) rb.linearVelocity = Vector2.zero;

        // 5. UI 갱신
        UpdateUI();
    }
}