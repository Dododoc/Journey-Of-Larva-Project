using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public PlayerHUD playerHUD; // UI 매니저 연결용

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

    // 프로퍼티 (최종 스탯 계산)
    public float TotalAttack => (baseAttack * currentLevel) + bonusAttack;
    public float TotalDefense => (baseDefense * currentLevel) + bonusDefense;
    public float TotalMaxHp => (maxHp * currentLevel) + bonusMaxHp;

    void Start()
    {
        // UI 매니저가 연결 안 되어 있으면 자동으로 찾기
        if (playerHUD == null)
            playerHUD = FindObjectOfType<PlayerHUD>();

        currentHp = TotalMaxHp;
        CalculateNextLevelExp();
        UpdateUI(); // 시작하자마자 UI 한번 그림
    }

    // ★ 테스트용 업데이트 함수 추가
    void Update()
    {
        // 'J' 키를 누르면 경험치 50 획득
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("치트키 작동: 경험치 +50");
            GainExp(50); 
        }
        
        // (참고) 'K' 키를 누르면 데미지 입는 테스트도 가능
        if (Input.GetKeyDown(KeyCode.K))
        {
            currentHp -= 10;
            if(currentHp < 0) currentHp = 0;
            UpdateUI();
        }
    }

    public void GainExp(float amount)
    {
        currentExp += amount;
        
        // 경험치가 목표치보다 많으면 레벨업
        if (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
        UpdateUI(); // 경험치 변했으니 UI 갱신
    }

    void LevelUp()
    {
        currentLevel++;
        currentExp -= expToNextLevel; // 남은 경험치 다음 레벨로 이월
        
        CalculateNextLevelExp(); // 다음 레벨 필요 경험치 재계산
        currentHp = TotalMaxHp;  // 레벨업 축하: 체력 풀회복
        
        Debug.Log($"레벨 업! Lv.{currentLevel}");

        // 경험치가 너무 많아서 한 번에 2업 이상 할 경우 처리
        if(currentExp >= expToNextLevel) LevelUp();
    }

    void CalculateNextLevelExp()
    {
        expToNextLevel = currentLevel * 100f * (1f + currentLevel * 0.1f);
    }

    // UI 갱신 함수 (HUD 스크립트에게 값 전달)
    void UpdateUI()
    {
        if (playerHUD != null)
        {
            // HUD에게 현재 값들을 다 던져줍니다.
            playerHUD.UpdateHP(currentHp, TotalMaxHp);
            playerHUD.UpdateXP(currentExp, expToNextLevel);
            playerHUD.UpdateLevel(currentLevel);
            Debug.Log($"[UI갱신] 현재: {currentHp} / 최대: {TotalMaxHp} (비율: {currentHp/TotalMaxHp})");
        }
    }

    // --- 진화 시스템 (핵심) ---

    // 진화 'R' 버튼을 눌러 선택을 마쳤을 때 호출되는 함수
    public void Evolve(int selectedPathIndex)
    {
        Debug.Log($"진화 루트 {selectedPathIndex} 선택됨! 진화 시작!");

        // 1. 현재 레벨에 따른 보너스 스탯 계산 (높은 레벨일수록 많이 받음)
        // 예: 레벨당 공격력 2, 방어력 1, 체력 10 씩 영구 보너스 추가
        float bonusMultiplier = currentLevel * 0.5f; // 밸런스 조절 필요

        bonusAttack += 2f * bonusMultiplier;
        bonusDefense += 1f * bonusMultiplier;
        bonusMaxHp += 10f * bonusMultiplier;

        Debug.Log($"진화 보너스 획득! Atk+{2f * bonusMultiplier}, Def+{1f * bonusMultiplier}");

        // 2. 루트별 특수 능력치 부여 (예시)
        switch (selectedPathIndex)
        {
            case 0: // 공격형 루트
                bonusAttack += 10f;
                break;
            case 1: // 방어형 루트
                bonusDefense += 10f;
                break;
            case 2: // 스피드/유틸 루트
                bonusMaxHp += 50f;
                break;
        }

        // 3. 레벨 및 경험치 초기화
        currentLevel = 1;
        currentExp = 0;
        CalculateNextLevelExp();
        currentHp = TotalMaxHp;

        Debug.Log("진화 완료! 1레벨로 돌아갔지만 더 강해졌습니다.");
        UpdateUI();

        // 진화 창 닫기 요청
        FindObjectOfType<UIManager>().CloseEvolutionPopup();
    }

}