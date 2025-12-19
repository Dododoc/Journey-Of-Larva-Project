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

        // ★ [수정 핵심] 데이터 로드 및 "진화 직후 초기화" 로직
        if (GameManager.instance != null)
        {
            // 1. 일단 매니저의 데이터를 가져옵니다.
            currentLevel = GameManager.instance.globalLevel;
            currentExp = GameManager.instance.globalXP;

            // 2. [방어 코드] 만약 캐릭터가 'Larva(애벌레)'가 아닌데(진화했음),
            //    레벨은 1이고, 경험치가 남아있다면? -> 이건 라바 시절 경험치다! 삭제하자.
            if (GameManager.instance.currentCharacter != GameManager.CharacterType.Larva)
            {
                if (currentLevel == 1 && currentExp > 0)
                {
                    Debug.LogWarning("⚠️ 진화 직후 잔여 경험치 감지! 강제로 0으로 초기화합니다.");
                    currentExp = 0;
                    
                    // 매니저에도 즉시 반영 (중요)
                    GameManager.instance.globalXP = 0;
                }
            }
            
            // 3. 정리된 데이터로 세팅
            CalculateNextLevelExp();
            currentHp = TotalMaxHp; 
            
            Debug.Log($"[PlayerStats] 최종 로드 완료: Lv.{currentLevel}, XP.{currentExp}");
        }
        else
        {
            currentHp = TotalMaxHp;
            CalculateNextLevelExp();
        }

        UpdateUI(); 
        
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null) uiManager.UpdateEvolutionUI(0); 
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
        
        SaveStatsToManager();
        UpdateUI(); 
    }

    void LevelUp()
    {
        currentLevel++;
        currentExp -= expToNextLevel; 
        CalculateNextLevelExp(); 
        currentHp = TotalMaxHp;  
        
        SaveStatsToManager();

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

    // 이 함수는 혹시 버튼으로 호출될 때를 대비해 유지합니다.
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

        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateEvolutionUI(selectedPathIndex + 1);
            uiManager.CloseEvolutionPopup();
        }
    }
    
    // ... (이하 데미지, 사망 관련 코드는 기존과 동일) ...
    public void TakeDamage(float damage)
    {
        float defenseFactor = 100f / (100f + TotalDefense);
        float finalDamage = damage * defenseFactor;
        finalDamage = Mathf.Max(1f, finalDamage);
        
        currentHp -= finalDamage;

        StartCoroutine(HitFlashRoutine());
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

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
        SaveStatsToManager();
        CalculateNextLevelExp();
        currentHp = TotalMaxHp;
        transform.position = startPosition;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if(rb != null) rb.linearVelocity = Vector2.zero;
        if (sr != null) sr.color = Color.white;
        UpdateUI();
    }
}