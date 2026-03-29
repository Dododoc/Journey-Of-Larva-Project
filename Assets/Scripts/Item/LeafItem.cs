using UnityEngine;

// 이 스크립트는 MonoBehaviour를 상속받아야 작동합니다.
public class LeafItem : MonoBehaviour
{
    // ★ [추가됨] 부유(둥둥) 효과 설정
    [Header("부유(둥둥) 효과")]
    public bool useFloating = true;   
    public float floatSpeed = 2.0f;    // 나뭇잎은 좀 더 천천히 (2f)
    public float floatAmplitude = 0.05f; // 나뭇잎은 좀 더 작게 (0.05f) 움직이게 설정해봤습니다.

    private float startY; // 나뭇잎의 원래 Y축 높이

    // 만약 기존 코드에 Start 함수가 없다면 아래를 추가해주세요.
    void Start()
    {
        // ★ [추가됨] 시작할 때의 Y축 위치를 기억해둡니다.
        startY = transform.position.y;
    }

    // 만약 기존 코드에 Update 함수가 없다면 아래를 추가해주세요.
    void Update()
    {
        // ==========================================
        // ★ [추가됨] 부유(둥둥) 효과 처리
        // ==========================================
        if (useFloating)
        {
            float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    // 기존에 있던 나뭇잎 줍기 기능 (예시)
    public void Collect(PlayerStats stats)
    {
        if (stats != null)
        {
            // 예: stats.AddXP(5);
            Debug.Log("나뭇잎 획득!");
        }
        Destroy(gameObject); // 주우면 파괴
    }
}