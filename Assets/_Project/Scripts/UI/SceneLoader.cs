using UnityEngine.SceneManagement;

public static class SceneLoader
{
    // 로드 할 타겟 씬 이름을 저장
    public static string TargetSceneName;

    public static void LoadScene(string sceneName)
    {
        TargetSceneName = sceneName;
        SceneManager.LoadScene("Scene_Loading");
    }

}
