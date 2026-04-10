using UnityEngine;

public class CameraAutoFollow : MonoBehaviour
{
    void Start()
    {
        // 1. 씬에 있는 플레이어(태그가 Player인 녀석)를 무조건 찾습니다.
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // 2. 구버전 유니티 (CinemachineVirtualCamera)를 사용할 경우
            var vcamOld = GetComponent("CinemachineVirtualCamera");
            if (vcamOld != null)
            {
                // Reflection을 사용해 버전 충돌 없이 깔끔하게 대입합니다.
                vcamOld.GetType().GetProperty("Follow")?.SetValue(vcamOld, player.transform);
                vcamOld.GetType().GetProperty("LookAt")?.SetValue(vcamOld, player.transform);
            }

            // 3. 최신 유니티 6 (CinemachineCamera)를 사용할 경우
            var vcamNew = GetComponent("CinemachineCamera");
            if (vcamNew != null)
            {
                vcamNew.GetType().GetProperty("Follow")?.SetValue(vcamNew, player.transform);
                vcamNew.GetType().GetProperty("LookAt")?.SetValue(vcamNew, player.transform);
            }
        }
    }
}