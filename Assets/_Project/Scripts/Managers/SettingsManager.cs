using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트(Slider) 제어를 위해 필수

public class SettingsManager : MonoBehaviour
{
    [Header("--- Volume Sliders ---")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void Start()
    {
        // 게임 시작 시 오디오 볼륨 슬라이더들을 항상 기본값(1.0f 즉, 최대 볼륨)으로 초기화합니다.
        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.IsNewStart)
            {
                InitVolumeSliders();
                SoundManager.Instance.IsNewStart = false;
            }
            else
            {
                // 처음 켠 게 아니라면(씬을 옴겨 다닐 때), SoundManager가 현재 들고 있는 볼륨값으로 슬라이더 위치를 맞춰줍니다.
                LoadCurrentVolumeToSliders();
            }
        }
    }

    #region [사운드 볼륨 조절 기능 (Slider 연동 - 휘발성 초기화)]

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
    /// 설정창을 다시 열었을 때 SoundManager에 설정되어 있던 기존 볼륨값으로 슬라이더 UI를 갱신합니다.
    /// </summary>
    private void LoadCurrentVolumeToSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = AudioListener.volume;
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        if (bgmVolumeSlider != null && SoundManager.Instance != null)
        {
            bgmVolumeSlider.value = SoundManager.Instance.BgmVolume;
            bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        }
        if (sfxVolumeSlider != null && SoundManager.Instance != null)
        {
            sfxVolumeSlider.value = SoundManager.Instance.SfxVolume;
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
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
    /// BGM 볼륨 조절 (SoundManager와 연동)
    /// </summary>
    public void SetBGMVolume(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(value);
        }
    }

    /// <summary>
    /// SFX 볼륨 조절 (SoundManager와 연동)
    /// </summary>
    public void SetSFXVolume(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(value);
        }
    }

    #endregion

    #region [디스플레이 화면 모드 전환 기능]

    /// <summary>
    /// [전체 화면 버튼 연동] 게임을 전체 화면 모드로 전환합니다.
    /// </summary>
    public void SetFullScreen()
    {
        // 현재 모니터 해상도를 가져와 전체 화면으로 변경
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
        Debug.Log("화면 모드 변경: 전체 화면");
    }

    /// <summary>
    /// [창 모드 버튼 연동] 게임을 창 모드로 전환합니다. (1280x720 예시)
    /// </summary>
    public void SetWindowedMode()
    {
        // 1280 x 720 해상도의 창 모드로 전환
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        Debug.Log("화면 모드 변경: 창 모드 (1280x720)");
    }

    #endregion
}