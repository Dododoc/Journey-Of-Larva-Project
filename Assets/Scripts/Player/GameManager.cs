using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum CharacterType { Larva, Ant, Beetle }
    [Header("Current Status")]
    public CharacterType currentCharacter = CharacterType.Larva;

    [Header("Player Data")]
    public int globalXP = 0;
    public int globalMaxXP = 100;
    public int globalLevel = 1;
    // GameManager.cs 안에 추가할 내용
    public float globalCurrentHp = -1f; // -1이면 처음 시작이라는 뜻
    public float globalBonusAttack = 0f;
    public float globalBonusDefense = 0f;
    public float globalBonusMaxHp = 0f;

    [Header("Game Info")]
    public float playTime = 0f;
    public int killCount = 0; // ★ [추가] 처치한 적의 수를 저장할 변수
    private bool isGameRunning = true;
    

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }
    void Update()
    {
        if (isGameRunning)
        {
            playTime += Time.deltaTime;
        }
    }
    public void StopGameTimer()
    {
        isGameRunning = false;
    }


    public void ChangeCharacter(CharacterType newCharacter)
    {
        currentCharacter = newCharacter;
        // 캐릭터가 바뀌면 자동으로 스탯을 초기화하고 싶다면 아래 주석을 해제하세요.
        ResetGlobalStats(); 
    }

    // ★ [추가] 강제로 경험치와 레벨을 초기화하는 함수
    public void ResetGlobalStats()
    {
        globalLevel = 1;
        globalXP = 0;
        globalMaxXP = 100; // 필요하다면 초기 경험치통 크기도 리셋

        Debug.Log("★ [GameManager] 데이터 강제 리셋 완료! (Lv.1 / XP.0)");
    }
    // GameManager.cs 안에 이 함수를 추가해 주세요.
    public void ResetGameData()
    {
        // 1. 캐릭터를 기본 상태(애벌레)로 되돌림
        currentCharacter = CharacterType.Larva; 

        // 2. 레벨과 경험치, 플레이 타임 등 초기화
        globalLevel = 1;
        globalXP = 0;
        playTime = 0f;
        
        // (만약 킬 카운트가 있다면)
        // killCount = 0; 

        // 3. 체력과 보너스 스탯도 싹 날림
        globalCurrentHp = -1f; 
        globalBonusAttack = 0f;
        globalBonusDefense = 0f;
        globalBonusMaxHp = 0f;

        Debug.Log("GameManager 데이터 완벽 리셋! 다시 애벌레로 돌아갑니다.");
    }
}