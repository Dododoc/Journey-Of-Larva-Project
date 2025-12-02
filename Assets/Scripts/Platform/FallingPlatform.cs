using UnityEngine;
using System.Collections;

public class FallingPlatform : MonoBehaviour
{
    [Header("함정 설정")]
    public float fallDelay = 0.5f;    // 밟고나서 떨어지기 전까지 버티는 시간 (덜컹!)
    public float stayTime = 2.0f;     // 바닥에서 머무르는 시간
    public float fallDistance = 5f;   // 아래로 떨어질 깊이
    
    [Header("속도 설정")]
    public float fallSpeed = 10f;     // 떨어지는 속도 (빠름)
    public float returnSpeed = 2f;    // 다시 올라오는 속도 (천천히)

    private Vector3 startPos;         // 원래 위치 저장
    private bool isWorking = false;   // 현재 작동 중인지 확인 (중복 발동 방지)

    void Start()
    {
        startPos = transform.position; // 시작 위치 기억
    }

    // 캐릭터가 밟으면 발동!
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 작동 중이 아니고, 플레이어가 밟았을 때만
        if (!isWorking && collision.gameObject.CompareTag("Player"))
        {
            // 캐릭터가 발판보다 위에 있을 때만 발동 (머리로 박았을 땐 안 떨어지게)
            if (collision.transform.position.y > transform.position.y)
            {
                StartCoroutine(FallAndReturn());
            }
        }
    }

    IEnumerator FallAndReturn()
    {
        isWorking = true;

        // 1. 밟자마자 잠시 대기 (덜컹거리는 연출 시간)
        yield return new WaitForSeconds(fallDelay);

        // 2. 아래로 뚝 떨어짐
        Vector3 targetPos = startPos + Vector3.down * fallDistance;
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, fallSpeed * Time.deltaTime);
            yield return null; // 한 프레임 대기
        }

        // 3. 바닥에서 잠시 대기
        yield return new WaitForSeconds(stayTime);

        // 4. 다시 원래 위치로 천천히 복귀
        while (Vector3.Distance(transform.position, startPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPos, returnSpeed * Time.deltaTime);
            yield return null;
        }

        // 위치 보정 및 초기화
        transform.position = startPos;
        isWorking = false;
    }
}