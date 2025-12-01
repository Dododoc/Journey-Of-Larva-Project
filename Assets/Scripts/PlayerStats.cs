using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Level Info")]
    public int currentLevel = 1;
    public float currentExp = 0;
    public float expToNextLevel;

    [Header("Base Stats (1레벨 기준)")]
    public float baseAttack = 10f;
    public float baseDefense = 5f;
    public float maxHp = 100f;
    public float currentHp;

    [Header("Evolution Bonus Stats (진화로 얻은 영구 스탯)")]
    public float bonusAttack = 0f;
    public float bonusDefense = 0f;
    public float bonusMaxHp = 0f;

    // 실제 적용되는 최종 스탯 (외부에서 이 값을 가져다 씀)
    public float TotalAttack => (baseAttack * currentLevel) + bonusAttack;
    public float TotalDefense => (baseDefense * currentLevel) + bonusDefense;
    public float TotalMaxHp => (maxHp * currentLevel) + bonusMaxHp;

    void Start()
    {
        currentHp = TotalMaxHp;
        CalculateNextLevelExp();
        UpdateUI(); // 시작할 때 UI 한번 갱신
    }

    // --- 레벨업 시스템 ---

    // 경험치 획득 함수 (몹 잡았을 때 호출)
    public void GainExp(float amount)
    {
        currentExp += amount;
        Debug.Log($"{amount} 경험치 획득! 현재: {currentExp}/{expToNextLevel}");

        if (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
        UpdateUI();
    }

    void LevelUp()
    {
        currentLevel++;
        currentExp -= expToNextLevel; // 남은 경험치 이월
        
        // 레벨이 오를수록 필요 경험치 증가 (포켓몬 방식 - 간단한 제곱 공식 사용)
        CalculateNextLevelExp();

        currentHp = TotalMaxHp; // 레벨업 시 체력 회복
        Debug.Log($"레벨 업! 현재 레벨: {currentLevel}");
        
        // 만약 경험치가 너무 많아서 한 번에 여러 레벨이 오를 경우를 대비한 재귀 호출
        if(currentExp >= expToNextLevel) LevelUp();
    }

    void CalculateNextLevelExp()
    {
        // 예시 공식: 레벨 * 100 * 약간의 증가폭. 원하는 대로 수정 가능.
        expToNextLevel = currentLevel * 100f * (1f + currentLevel * 0.1f);
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

    // UI 갱신용 임시 함수 (나중에 UIManager와 연결)
    void UpdateUI()
    {
        // 여기에 스테이터스 창의 텍스트들을 업데이트하는 코드가 들어갑니다.
        // 예: statusText.text = $"Lv.{currentLevel} (Exp: {currentExp}/{expToNextLevel})\nAtk: {TotalAttack}";
    }
}