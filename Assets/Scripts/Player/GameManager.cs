using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환 감지를 위해 필요!

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
    public float globalCurrentHp = -1f; 
    public float globalBonusAttack = 0f;
    public float globalBonusDefense = 0f;
    public float globalBonusMaxHp = 0f;
    [Header("Portal System")]
    public string targetPortalID = "";

    [Header("Game Info")]
    public float playTime = 0f;
    public int killCount = 0; 
    private bool isGameRunning = true;
    
    // ==========================================
    // ★ [추가] 씬 이동 시 자동 스폰을 위한 프리팹 모음
    // ==========================================
    [Header("Player Prefabs for Spawning")]
    public GameObject larvaPrefab;
    public GameObject antPrefab;
    public GameObject beetlePrefab;

    void Awake()
    {
        if (instance == null) 
        { 
            instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    // ==========================================
    // ★ [핵심 추가] 씬이 바뀔 때마다 자동으로 실행되는 이벤트 등록
    // ==========================================
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 새로운 씬이 로드되면 이 함수가 무조건 실행됩니다.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 타이틀 화면이나 메인 메뉴라면 플레이어를 스폰하지 않음
        if (scene.name == "TitleScene" || scene.name == "StartScene") return;

        Debug.Log($"새로운 씬({scene.name}) 로드 완료! 플레이어를 소환합니다.");
        SpawnPlayerAtPortal();

        // UI 매니저에게 현재 캐릭터에 맞는 UI를 켜라고 명령합니다.
        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateSkillUI((int)currentCharacter);
            UIManager.instance.UpdateEvolutionUI((int)currentCharacter);
        }
    }

    // 포탈 위치를 찾아서, 현재 진화 상태에 맞는 프리팹을 소환하는 함수
    void SpawnPlayerAtPortal()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null) return;

        Vector3 spawnPos = Vector3.zero;

        // 1. 맵에 있는 '모든' 포탈을 다 가져옵니다.
        ConditionalPortal[] allPortals = FindObjectsByType<ConditionalPortal>(FindObjectsSortMode.None);
        ConditionalPortal targetPortal = null;

        // 2. 티켓(targetPortalID)에 적힌 이름과 똑같은 이름표를 가진 포탈을 찾습니다!
        if (!string.IsNullOrEmpty(targetPortalID))
        {
            foreach (var portal in allPortals)
            {
                if (portal.myPortalID == targetPortalID)
                {
                    targetPortal = portal;
                    break; // 찾았으면 더 이상 안 찾아도 됨
                }
            }
        }

        // ==========================================
        // ★ [핵심 추가] 3. 포탈을 찾지 못한 경우 (게임 첫 시작 시)
        // ==========================================
        if (targetPortal == null)
        {
            // 맵에서 "InitialSpawnPoint"라는 이름을 가진 오브젝트를 찾습니다.
            GameObject initialSpawn = GameObject.Find("InitialSpawnPoint");
            
            if (initialSpawn != null)
            {
                // 찾았다면 그 위치를 소환 위치로 결정! (포탈이 아니므로 Y축 +1을 안 해도 됩니다)
                spawnPos = initialSpawn.transform.position; 
                Debug.Log("게임을 처음 시작하여 'InitialSpawnPoint' 위치에 소환합니다.");
            }
            else if (allPortals.Length > 0)
            {
                // 시작 지점도 못 찾았는데 포탈은 있다면, 어쩔 수 없이 첫 번째 포탈에서 소환
                targetPortal = allPortals[0];
                spawnPos = targetPortal.transform.position + new Vector3(0, 1f, 0);
            }
        }
        else
        {
            // 정상적으로 포탈을 찾았을 경우 살짝 위로 소환
            spawnPos = targetPortal.transform.position + new Vector3(0, 1f, 0);
        }

        // 4. 플레이어 소환
        GameObject prefabToSpawn = larvaPrefab;
        if (currentCharacter == CharacterType.Ant) prefabToSpawn = antPrefab;
        else if (currentCharacter == CharacterType.Beetle) prefabToSpawn = beetlePrefab;

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            
            string spawnLog = targetPortal != null ? $"'{targetPortalID}' 포탈" : "시작 지점";
            Debug.Log($"[{currentCharacter}] 플레이어를 {spawnLog}에 스폰했습니다!");
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
    }

    public void ResetGlobalStats()
    {
        globalLevel = 1;
        globalXP = 0;
        globalMaxXP = 100; 
        Debug.Log("★ [GameManager] 데이터 강제 리셋 완료!");
    }

    public void ResetGameData()
    {
        currentCharacter = CharacterType.Larva; 
        globalLevel = 1;
        globalXP = 0;
        playTime = 0f;
        killCount = 0; 

        globalCurrentHp = -1f; 
        globalBonusAttack = 0f;
        globalBonusDefense = 0f;
        globalBonusMaxHp = 0f;

        Debug.Log("GameManager 데이터 완벽 리셋! 다시 애벌레로 돌아갑니다.");
    }
}