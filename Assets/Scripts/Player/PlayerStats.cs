using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 필수

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public PlayerHUD playerHUD; 
    
    // ★ [추가] 플레이어가 맞았을 때 터질 이펙트 (피 튀기는 효과 등)
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
    private SpriteRenderer sr; // ★ 색상 변경을 위해 추가

    public float TotalAttack => (baseAttack * currentLevel) + bonusAttack;
    public float TotalDefense => (baseDefense * currentLevel) + bonusDefense;
    public float TotalMaxHp => (maxHp * currentLevel) + bonusMaxHp;

    void Start()
    {
        if (playerHUD == null) playerHUD = FindObjectOfType<PlayerHUD>();
        sr = GetComponent<SpriteRenderer>(); // ★ 컴포넌트 가져오기

        startPosition = transform.position;

        currentHp = TotalMaxHp;
        CalculateNextLevelExp();
        UpdateUI(); 

        
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null) uiManager.UpdateEvolutionUI(0);


        // ★ [데이터 로드 및 초기화 로직]
        if (GameManager.instance != null)
        {
            // 1. 매니저 데이터 로드
            currentLevel = GameManager.instance.globalLevel;
            currentExp = GameManager.instance.globalXP;

            // 2. [방어 코드] 진화 상태인데 1레벨 경험치가 남아있다면 초기화
            if (GameManager.instance.currentCharacter != GameManager.CharacterType.Larva)
            {
                if (currentLevel == 1 && currentExp > 0)
                {
                    Debug.LogWarning("⚠️ 진화 직후 잔여 경험치 감지! 강제로 0으로 초기화합니다.");
                    currentExp = 0;
                    GameManager.instance.globalXP = 0;
                }
            }
            
            // 3. 스탯 세팅
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

        // ★ [통합] 팀원 코드의 로직 채택 (저장된 캐릭터 타입에 맞춰 UI 갱신)

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
    

    // ★ [핵심] 데미지 입는 함수 수정

    public void TakeDamage(float damage)
    {
        // 1. 데미지 계산
        float defenseFactor = 100f / (100f + TotalDefense);
        float finalDamage = damage * defenseFactor;
        finalDamage = Mathf.Max(1f, finalDamage);
        
        currentHp -= finalDamage;

        // 2. ★ 피격 연출 (빨간맛 + 이펙트) 자동 실행!
        StartCoroutine(HitFlashRoutine());
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (currentHp < 0) currentHp = 0;
        UpdateUI(); 

        if (currentHp <= 0) Die();
    }

    // ★ [신규] 빨갛게 깜빡이는 코루틴
    IEnumerator HitFlashRoutine()
    {
        if (sr != null)
        {
            sr.color = new Color(1f, 0.4f, 0.4f); // 붉은색
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white; // 원래대로
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
        CalculateNextLevelExp();
        currentHp = TotalMaxHp;
        transform.position = startPosition;
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if(rb != null) rb.linearVelocity = Vector2.zero;

        // 부활 시 색상 초기화

        if(rb != null) rb.linearVelocity = Vector2.zero; // Unity 6+ (구버전은 rb.velocity)

        if (sr != null) sr.color = Color.white;

        UpdateUI();
    }

    // ★ [통합] 독 데미지 관련 로직 추가 (내 코드 유지)
    public void ApplyPoison(float totalDamage, float duration)
    {
        // 이미 독에 걸려있다면 새로 갱신
        StopCoroutine("PoisonRoutine");
        StartCoroutine(PoisonRoutine(totalDamage, duration));
    }

    IEnumerator PoisonRoutine(float totalDamage, float duration)
    {
        // 예: 3초 동안 10데미지 -> 0.5초마다 나눠서 데미지
        float tickInterval = 0.5f; 
        int ticks = Mathf.FloorToInt(duration / tickInterval);
        float damagePerTick = totalDamage / ticks;
        
        SpriteRenderer playerSr = GetComponent<SpriteRenderer>();
        Color originalColor = (playerSr != null) ? playerSr.color : Color.white;

        for (int i = 0; i < ticks; i++)
        {
            if (playerSr != null) playerSr.color = new Color(0.4f, 1f, 0.4f); // 초록색 티닝
            TakeDamage(damagePerTick); 
            yield return new WaitForSeconds(0.1f);
            if (playerSr != null) playerSr.color = originalColor; // 색 복구
            yield return new WaitForSeconds(tickInterval - 0.1f);
        }
    }
}