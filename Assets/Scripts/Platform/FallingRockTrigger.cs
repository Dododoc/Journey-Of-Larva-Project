using UnityEngine;

public class FallingRockTrigger : MonoBehaviour
{
    [Header("떨어질 돌멩이 연결")]
    // 떨어뜨릴 돌의 Rigidbody를 여기에 연결합니다.
    public Rigidbody2D rockRigidbody; 

    [Header("효과음 (선택사항)")]
    public AudioClip thudSound;
    private AudioSource audioSource;

    private bool hasFallen = false; // 이미 떨어졌는지 체크 (중복 방지)

    void Start()
    {
        // 효과음 재생기가 없다면 추가 (선택사항)
        if(thudSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    // 무언가가 투명 감지선에 닿았을 때 실행됨
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 닿은게 플레이어이고 + 아직 돌이 안 떨어졌다면
        if (collision.CompareTag("Player") && !hasFallen)
        {
            Fall(); // ✅ 수정됨 (느낌표 삭제)
        }
    }

    // ✅ 수정됨: 함수 이름에서 느낌표(!)를 뺐습니다.
    void Fall()
    {
        hasFallen = true; // 작동 완료 표시
        Debug.Log("쿵! 돌이 떨어집니다!");

        // ★ 핵심: 꺼뒀던 물리 시뮬레이션을 켜서 중력의 영향을 받게 함
        if (rockRigidbody != null)
        {
            rockRigidbody.simulated = true;
        }

        // 효과음 재생 (선택사항)
        if (audioSource != null && thudSound != null)
        {
            audioSource.PlayOneShot(thudSound);
        }

        // 이 감지선은 이제 할 일을 다 했으니 꺼버립니다.
        // gameObject.SetActive(false); // 원하면 주석 해제
    }
}