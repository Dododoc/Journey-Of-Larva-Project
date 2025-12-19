using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 어디서든 접근할 수 있게 static으로 만듭니다 
    public static GameManager instance;

    [Header("Global Data")]
    public int globalLevel = 1;      // 전역 레벨
    public float globalXP = 0;       // 전역 경험치
    public float globalMaxXP = 100;

    public enum CharacterType { Larva, Ant, Beetle } // 캐릭터 종류 정의
    public CharacterType currentCharacter = CharacterType.Larva; // 현재 캐릭터 상태

    [Header("Game Info")]
    public float playTime = 0f;
    private bool isGameRunning = true;

    void Awake()
    {
        // ★ [수정] 중복된 싱글톤 코드를 하나로 정리했습니다.
        if (instance == null) 
        { 
            instance = this; 
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else 
        { 
            Destroy(gameObject); // 이미 있으면 자신을 파괴
        }
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
        // 캐릭터가 바뀌면 스탯을 초기화
        ResetGlobalStats(); 
    }

    // 강제로 경험치와 레벨을 초기화하는 함수
    public void ResetGlobalStats()
    {
        globalLevel = 1;
        globalXP = 0;
        globalMaxXP = 100; 

        Debug.Log("★ [GameManager] 데이터 강제 리셋 완료! (Lv.1 / XP.0)");
    }
}