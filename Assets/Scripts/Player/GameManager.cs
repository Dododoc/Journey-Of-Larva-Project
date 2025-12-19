using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 어디서든 접근할 수 있게 static으로 만듭니다 
    public static GameManager instance;

    public enum CharacterType { Larva, Ant, Beetle } // 캐릭터 종류 정의
    public CharacterType currentCharacter = CharacterType.Larva; // 현재 캐릭터 상태

    [Header("Game Info")]
    public float playTime = 0f;
    private bool isGameRunning = true;

    void Awake()
    {
<<<<<<< HEAD
        // 게임 시작 시 이 매니저가 중복 생성되지 않도록 설정
        if (instance == null) instance = this;
        else Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
=======
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
>>>>>>> 3af38861f8e43197c32a6576f845799e2b4d9d92
    }
}