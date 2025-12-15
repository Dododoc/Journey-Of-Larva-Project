using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 어디서든 접근할 수 있게 static으로 만듭니다 
    public static GameManager instance;

    public enum CharacterType { Larva, Ant, Beetle } // 캐릭터 종류 정의
    public CharacterType currentCharacter = CharacterType.Larva; // 현재 캐릭터 상태

    void Awake()
    {
        // 게임 시작 시 이 매니저가 중복 생성되지 않도록 설정
        if (instance == null) instance = this;
        else Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
    }
}