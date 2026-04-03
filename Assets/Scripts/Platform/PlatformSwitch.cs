using UnityEngine;

public class PlatformSwitch : MonoBehaviour
{
    [Header("연결할 엘리베이터")]
    public ElevatorPlatform elevator; // 작동시킬 엘리베이터를 연결할 칸

    private bool isPressed = false; // 한 번만 작동하게 막아주는 스위치 역할

    // 트리거(Is Trigger 체크된 콜라이더)에 무언가 닿았을 때 실행됨
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 닿은 것이 플레이어(개미)이고, 스위치가 아직 안 눌린 상태라면
        if (!isPressed && collision.CompareTag("Player"))
        {
            isPressed = true; // 스위치 눌림 처리
            
            // 연결된 엘리베이터가 있다면 내려오라고 명령
            if (elevator != null)
            {
                elevator.MoveDown();
            }
            
            Debug.Log("스위치 작동! 엘리베이터가 내려옵니다.");
        }
    }
}