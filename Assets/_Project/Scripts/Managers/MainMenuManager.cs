using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private void Start()
    {
        // 게임 시작 시 타이틀 BGM을 재생합니다.
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM("BGM_Title");
        }
    }

    #region [씬 전환 및 게임 종료 기능]

    /// <summary>
    /// [스타트 버튼 연동] 스테이지 선택 화면(Scene)으로 넘어갑니다.
    /// </summary>
    public void OnClickStart()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_UI_Click");
        }
        // 빌드 세팅에 등록된 스테이지 선택 씬의 정확한 이름을 적어주세요.
        SceneManager.LoadScene("Scene_StageSelect");
    }

    /// <summary>
    /// [나가기 버튼 연동] 게임을 완전히 종료합니다.
    /// </summary>
    public void OnClickExitGame()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_UI_Click");
        }
        Debug.Log("게임 종료 요청");

#if UNITY_EDITOR
        // 유니티 에디터 환경에서 테스트할 때 종료 확인용
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 빌드된 게임(PC/모바일)이 종료됨
        Application.Quit();
#endif
    }

    #endregion
}