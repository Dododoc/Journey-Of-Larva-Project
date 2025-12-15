using UnityEngine;
using Unity.Cinemachine; // 시네머신 3.0 네임스페이스

public class EvolutionItem : MonoBehaviour
{
    public GameObject nextCharacterPrefab; // 변신할 캐릭터 프리팹
    public GameManager.CharacterType typeToChange; // 변신할 타입

    private bool canEvolve = false; // 변신 가능 여부 체크
    private GameObject targetPlayer; // 닿아있는 플레이어 저장

    private void Update()
    {
        // 플레이어가 아이템 범위 안에 있고, R키를 눌렀다면?
        if (canEvolve && Input.GetKeyDown(KeyCode.R))
        {
            Evolve();
        }
    }

    // 아이템 범위에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canEvolve = true;
            targetPlayer = collision.gameObject;
            Debug.Log("변신하려면 R 키를 누르세요!"); // 테스트용 로그
        }
    }

    // 아이템 범위에서 나갔을 때
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canEvolve = false;
            targetPlayer = null;
        }
    }

    // 실제 변신 로직 (R키 누르면 실행됨)
    private void Evolve()
    {
        if (targetPlayer == null) return;

        // 1. 위치와 회전값 저장
        Vector3 spawnPos = targetPlayer.transform.position;
        Quaternion spawnRot = targetPlayer.transform.rotation;

        // 2. 현재 플레이어(라바) 삭제
        Destroy(targetPlayer);

        // 3. 새로운 캐릭터 생성
        GameObject newPlayer = Instantiate(nextCharacterPrefab, spawnPos, spawnRot);
        newPlayer.tag = "Player";

        // 4. 시네머신 카메라에게 새 주인 알려주기
        var cineCam = FindObjectOfType<CinemachineCamera>();
        if (cineCam != null)
        {
            cineCam.Follow = newPlayer.transform;
        }

        // 5. 게임 매니저 상태 업데이트
        if (GameManager.instance != null)
        {
            GameManager.instance.currentCharacter = typeToChange;
        }

        // 6. 아이템 삭제
        Destroy(gameObject);
    }
}