using UnityEngine;

public class StageSelectTest : MonoBehaviour
{
    // 버튼의 OnClick 이벤트에서 호출할 함수
    public void SelectStage(string sceneName)
    {
        StageManager.Instance.SetNextStage(501);
        Debug.Log($"[Test] {sceneName} 로딩 시작");

        // 작성하신 로딩 매니저 코드 실행
        LoadingSceneManager.LoadScene(sceneName);
    }
}