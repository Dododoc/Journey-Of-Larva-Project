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

    [Header("Game Info")]
    public float playTime = 0f;
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
}