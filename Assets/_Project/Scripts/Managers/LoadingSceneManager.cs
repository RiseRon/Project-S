using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private Slider progressBar; // 로딩바 UI
    public static string nextSceneName; // 다음 스테이지 이름 저장용

    private void Start()
    {
        StartCoroutine(LoadSceneProcess());
    }

    // 로딩 씬으로 이동할 때 호출할 정적 함수
    public static void LoadScene()
    {
        SceneManager.LoadScene("Scene_Loading"); // 로딩 전용 씬 이름
    }

    private IEnumerator LoadSceneProcess()
    {
        // 0. 초기화
        progressBar.value = 0f;

        // 1. 비동기 씬 로드 시작
        AsyncOperation op = SceneManager.LoadSceneAsync("Scene_Stage");

        // 씬이 다 불러와져도 바로 화면을 넘기지 않게 설정 (부드러운 연출을 위해)
        op.allowSceneActivation = false;

        // 2. [핵심] PoolManager의 오브젝트 생성 작업 (비동기)
        // 씬 로드와 병행하여 몬스터들을 미리 만듭니다.
        if (PoolManager.Instance != null)
        {
            // PoolManager에 만든 코루틴이 끝날 때까지 대기
            yield return StartCoroutine(PoolManager.Instance.Co_PreWarmPools());
        }

        // 3. 페이크 로딩 및 씬 로드 진행률 연출
        // 실제 로드율(op.progress)과 시간을 섞어 부드럽게 연출합니다.
        float timer = 0f;
        while (!op.isDone)
        {
            yield return null;
            timer += Time.unscaledDeltaTime;

            if (op.progress < 0.9f)
            {
                // 실제 씬 리소스 로드 중
                progressBar.value = Mathf.Lerp(progressBar.value, op.progress, timer);
                if (progressBar.value >= op.progress) timer = 0f;
            }
            else
            {
                // 리소스 로드 완료 후 마지막 연출 (100%까지 채우기)
                progressBar.value = Mathf.Lerp(progressBar.value, 1f, timer);

                if (progressBar.value >= 0.99f)
                {
                    progressBar.value = 1f;
                    yield return new WaitForSecondsRealtime(0.5f); // 마지막 여운
                    op.allowSceneActivation = true; // 실제 씬 전환 승인
                    yield break;
                }
            }
        }
    }
}