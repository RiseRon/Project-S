using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    private void Start()
    {
        // 씬이 시작되자마자 비동기 로딩 코루틴 실행
        StartCoroutine(LoadSceneProcess());
    }

    private IEnumerator LoadSceneProcess()
    {
        // 비동기 방식으로 목표 씬 로드 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneLoader.TargetSceneName);

        // 로딩이 완료되어도 즉시 씬을 바꾸지 않도록 설정
        op.allowSceneActivation = false;

        float timer = 0f;
        while (!op.isDone)
        {
            yield return null;
            timer += Time.unscaledDeltaTime;

            // 로딩 진행률이 90%(0.9f)를 넘고, 최소 로딩 시간 동안 로딩 화면을 보여준 뒤 전환
            if (op.progress >= 0.9f && timer >= 2.0f)
            {
                op.allowSceneActivation = true;
                yield break;
            }
        }
    }
}