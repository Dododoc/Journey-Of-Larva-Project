using UnityEngine;

public class FixUIFlip : MonoBehaviour
{
    private Vector3 originalScale;

    void Start()
    {
        // 처음 시작할 때 UI의 원래 크기(비율)를 기억해 둡니다.
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (transform.parent != null)
        {
            // 부모(플레이어)가 왼쪽을 봐서 음수(-1)가 되면, 
            // UI의 크기에도 음수를 곱해버립니다. (음수 * 음수 = 양수!)
            // 이렇게 하면 부모가 뒤집혀도 UI는 항상 똑바로 보이게 됩니다.
            float parentSign = Mathf.Sign(transform.parent.localScale.x);
            transform.localScale = new Vector3(originalScale.x * parentSign, originalScale.y, originalScale.z);
        }
    }
}