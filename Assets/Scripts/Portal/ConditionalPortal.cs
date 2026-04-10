using UnityEngine;
using UnityEngine.SceneManagement;

public class ConditionalPortal : MonoBehaviour
{
    [Header("이 포탈의 정보")]
    public string myPortalID = "Start"; // 내 이름표 (예: "Start", "End")

    // ==========================================
    // ★ [핵심 변경] 캐릭터별로 도착지를 따로 설정할 수 있습니다!
    // ==========================================
    [Header("애벌레(Larva) 전용 도착지")]
    public string larvaTargetScene = ""; 
    public string larvaTargetPortalID = "Start";

    [Header("개미(Ant) 전용 도착지")]
    public string antTargetScene = "Ant Middle Stage"; 
    public string antTargetPortalID = "Start";

    [Header("풍뎅이(Beetle) 전용 도착지")]
    public string beetleTargetScene = "Beetle Middle Stage"; 
    public string beetleTargetPortalID = "Start";

    private bool isPlayerNearby = false;

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            ActivatePortal();
        }
    }

    void ActivatePortal()
    {
        // 1. 현재 내 캐릭터가 무엇인지 파악하고, 목적지를 결정합니다.
        string finalSceneName = "";
        string finalPortalID = "";

        if (GameManager.instance != null)
        {
            if (GameManager.instance.currentCharacter == GameManager.CharacterType.Larva)
            {
                finalSceneName = larvaTargetScene;
                finalPortalID = larvaTargetPortalID;
            }
            else if (GameManager.instance.currentCharacter == GameManager.CharacterType.Ant)
            {
                finalSceneName = antTargetScene;
                finalPortalID = antTargetPortalID;
            }
            else if (GameManager.instance.currentCharacter == GameManager.CharacterType.Beetle)
            {
                finalSceneName = beetleTargetScene;
                finalPortalID = beetleTargetPortalID;
            }
        }

        // 2. 만약 결정된 목적지 맵 이름이 비어있다면? -> 아직 못 가는 문!
        if (string.IsNullOrEmpty(finalSceneName)) 
        {
            Debug.Log("이 캐릭터로는 아직 들어갈 수 없는 문입니다!");
            return; 
        }

        // 3. 이동 로직 실행
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null) player.SaveStatsToManager();

        if (GameManager.instance != null)
        {
            GameManager.instance.targetPortalID = finalPortalID;
        }

        Debug.Log($"{finalSceneName} 맵의 '{finalPortalID}' 포탈로 이동합니다!");
        SceneManager.LoadScene(finalSceneName); 
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.AddNearbyItem();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.RemoveNearbyItem();
        }
    }
}