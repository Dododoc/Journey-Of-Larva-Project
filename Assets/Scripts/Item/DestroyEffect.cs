using UnityEngine;

public class DestroyEffect : MonoBehaviour
{
    public float lifeTime = 1.0f; // 애니메이션이 끝나는 시간 (초)

    void Start()
    {
        // 태어나고 lifeTime 초 뒤에 자기 자신을 파괴합니다.
        Destroy(gameObject, lifeTime); 
    }
}