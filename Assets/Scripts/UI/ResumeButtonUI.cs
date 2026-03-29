using UnityEngine;
using UnityEngine.UI;

public class ResumeButtonUI : MonoBehaviour
{
    void Start()
    {
        // 내 몸에 있는 버튼 컴포넌트를 가져옴
        Button myButton = GetComponent<Button>();

        // 버튼을 클릭했을 때, UIManager(감독님)의 ResumeGame 기능을 곧바로 실행!
        myButton.onClick.AddListener(() => 
        {
            if (UIManager.instance != null) 
            {
                UIManager.instance.ResumeGame();
            }
        });
    }
}