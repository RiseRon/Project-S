using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // UI 컴포넌트(Slider) 제어를 위해 필수

public class MainMenuManager : MonoBehaviour
{
    [Header("--- Volume Sliders ---")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM("BGM_title 1");
        }

        // 게임 시작 시 오디오 볼륨 슬라이더들을 항상 기본값(1.0f 즉, 최대 볼륨)으로 초기화합니다.
        InitVolumeSliders();
    }

    #region [1. 씬 전환 및 게임 종료 기능]

    /// <summary>
    /// [스타트 버튼 연동] 스테이지 선택 화면(Scene)으로 넘어갑니다.
    /// </summary>
    public void OnClickStart()
    {
        // if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UI_Button_Click");

        // 빌드 세팅에 등록된 스테이지 선택 씬의 정확한 이름을 적어주세요.
        SceneManager.LoadScene("Scene_StageSelect");
    }

    /// <summary>
    /// [나가기 버튼 연동] 게임을 완전히 종료합니다.
    /// </summary>
    public void OnClickExitGame()
    {
        // if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UI_Button_Click");

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


    #region [2. 사운드 볼륨 조절 기능 (Slider 연동 - 휘발성 초기화)]

    private void InitVolumeSliders()
    {
        // 1. 마스터 볼륨 슬라이더 초기화
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = 1.0f;
            AudioListener.volume = 1.0f; // 오디오 리스너 자체도 기본값 세팅

            masterVolumeSlider.onValueChanged.RemoveAllListeners(); // 중복 등록 방지 안전장치
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        // 2. BGM 볼륨 슬라이더 초기화
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = 1.0f;
            if (SoundManager.Instance != null) SoundManager.Instance.SetBGMVolume(1.0f);

            bgmVolumeSlider.onValueChanged.RemoveAllListeners(); // 중복 등록 방지 안전장치
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        // 3. SFX 볼륨 슬라이더 초기화
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = 1.0f;
            if (SoundManager.Instance != null) SoundManager.Instance.SetSFXVolume(1.0f);

            sfxVolumeSlider.onValueChanged.RemoveAllListeners(); // 중복 등록 방지 안전장치
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    /// <summary>
    /// 전체 마스터 볼륨 조절 (유니티 오디오 리스너 자체의 볼륨을 조절합니다)
    /// </summary>
    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
    }

    /// <summary>
    /// BGM 볼륨 조절 (SoundManager 또는 오디오 믹서와 연동 가능하도록 자리를 비워둡니다)
    /// </summary>
    public void SetBGMVolume(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(value);
        }
    }

    /// <summary>
    /// SFX 볼륨 조절 (SoundManager 또는 오디오 믹서와 연동 가능하도록 자리를 비워둡니다)
    /// </summary>
    public void SetSFXVolume(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(value);
        }
    }

    #endregion


    #region [3. 디스플레이 화면 모드 전환 기능]

    /// <summary>
    /// [전체 화면 버튼 연동] 게임을 전체 화면 모드로 전환합니다.
    /// </summary>
    public void SetFullScreen()
    {
        // if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UI_Button_Click");

        // 현재 모니터 해상도를 가져와 전체 화면으로 변경
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
        Debug.Log("화면 모드 변경: 전체 화면");
    }

    /// <summary>
    /// [창 모드 버튼 연동] 게임을 창 모드로 전환합니다. (디펜스 게임 표준 해상도인 16:9인 1280x720 예시)
    /// </summary>
    public void SetWindowedMode()
    {
        // if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UI_Button_Click");

        // 1280 x 720 해상도의 창 모드로 전환
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        Debug.Log("화면 모드 변경: 창 모드 (1280x720)");
    }

    #endregion
}