using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 게임 전체의 사운드(BGM, SFX)를 중앙 집중 제어하는 매니저입니다.
/// Resources 로드 방식을 채택하여 인스펙터 등록 노가다가 필요 없으며,
/// 코루틴 분산 풀링을 통해 메인 씬 진입 시의 프리징(렉)을 방지합니다.
/// 1. 동일한 BGM은 중복 재생을 차단합니다.
/// 2. 동일한 SFX는 씬에 동시에 최대 5개까지만 재생되도록 제한합니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // 어디서든 SoundManager.Instance로 접근할 수 있도록 싱글톤 선언
    public static SoundManager Instance { get; private set; }

    [Header("--- Audio Sources ---")]
    [Tooltip("배경음악을 재생할 오디오 소스입니다. (없으면 Awake에서 자동 생성됩니다)")]
    [SerializeField] private AudioSource bgmSource;

    [Header("--- Optimization Settings ---")]
    [Tooltip("동시에 재생될 수 있는 최대 효과음(SFX)의 개수입니다.")]
    [SerializeField] private int sfxPoolSize = 35;
    [SerializeField] private int maxSameSFXCount = 5;

    // 빠른 검색(Key-Value)을 위해 파일 이름을 Key로 사용하는 딕셔너리
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> bgmDictionary = new Dictionary<string, AudioClip>();

    // 효과음 재생기(AudioSource)들을 담아두고 돌려막기할 오브젝트 풀 큐
    private Queue<AudioSource> sfxPool = new Queue<AudioSource>();

    private Dictionary<string, int> activeSFXCount = new Dictionary<string, int>();
    public float BgmVolume { get; private set; } = 1.0f;
    public float SfxVolume { get; private set; } = 1.0f;

    public bool IsNewStart { get; set; } = true;
    private void Awake()
    {
        // --- 싱글톤 및 DontDestroyOnLoad 세팅 ---
        // 씬이 바뀌어도 음악이 끊기지 않고 게임 전체에서 유지됩니다.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 메인 씬 프리징 렉을 방지하기 위해 코루틴으로 초기화 시작
            StartCoroutine(InitializeManagerRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Resources 폴더에서 에셋을 로드하고 오디오 소스 풀을 분산 생성합니다.
    /// </summary>
    private IEnumerator InitializeManagerRoutine()
    {
        // 1. Assets/Resources/BGM 및 Assets/Resources/SFX 폴더 내의 모든 오디오 클립을 긁어옵니다.
        AudioClip[] loadedBgm = Resources.LoadAll<AudioClip>("BGM");
        AudioClip[] loadedSfx = Resources.LoadAll<AudioClip>("SFX");

        // 2. 긁어온 사운드 파일들의 '실제 파일 이름'을 Key값으로 하여 딕셔너리에 자동 등록합니다.
        foreach (var clip in loadedBgm) bgmDictionary[clip.name] = clip;
        foreach (var clip in loadedSfx) sfxDictionary[clip.name] = clip;

        Debug.Log($"<color=cyan>[SoundManager]</color> 리소스 자동 로드 완료! (BGM: {loadedBgm.Length}개, SFX: {loadedSfx.Length}개)");

        // 가벼운 로드 연산 후 한 프레임 쉬어가기 (초기 구동 체증 방지)
        yield return null;

        // 3. 배경음 전용 소스가 인스펙터에 비어있다면 컴포넌트를 직접 추가하고 루프를 켭니다.
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = BgmVolume; // 현재 설정된 BGM 볼륨 초기 세팅

        // 4. 효과음 플레이어(AudioSource) 풀링 생성 작업 시작
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sfxObj = new GameObject($"SFX_Player_{i}");
            sfxObj.transform.SetParent(this.transform); // Hierarchy 정리를 위해 매니저 자식으로 설정

            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.volume = SfxVolume; // ★ [추가] 초기 생성 단계에서 실시간 설정된 SFX 볼륨을 주입
            sfxObj.SetActive(false); // 평소엔 꺼둠
            sfxPool.Enqueue(source);

            // [★최적화 핵심] 2개를 생성할 때마다 다음 프레임으로 연산을 양보합니다.
            // 이 덕분에 메인 타이틀 화면이 켜질 때 순간적으로 화면이 툭 끊기는 초기 프리징이 완벽하게 사라집니다.
            if (i % 2 == 0)
            {
                yield return null;
            }
        }

        Debug.Log("<color=lime>[SoundManager]</color> 사운드 플레이어 분산 풀링 완료!");
    }

    /// <summary>
    /// [배경음악 재생] 외부 스크립트에서 파일 이름 문자열로 호출합니다.
    /// 예시: SoundManager.Instance.PlayBGM("BGM_MainTitle");
    /// </summary>
    public void PlayBGM(string bgmName)
    {
        if (!bgmDictionary.TryGetValue(bgmName, out AudioClip clip))
        {
            Debug.LogWarning($"[SoundManager] BGM 파일을 찾을 수 없습니다: {bgmName}");
            return;
        }

        // =========================================================================
        // [조건 1] 같은 이름의 BGM이 이미 재생 중이라면 중복 플레이를 무시합니다.
        // =========================================================================
        if (bgmSource.clip != null && bgmSource.clip.name == bgmName && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    /// <summary>
    /// 현재 재생 중인 배경음악을 정지합니다.
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// <summary>
    /// [효과음 재생] 외부 스크립트에서 파일 이름 문자열로 호출합니다. (오브젝트 풀 자동 순환)
    /// 예시: SoundManager.Instance.PlaySFX("Button_Click");
    /// </summary>
    public void PlaySFX(string sfxName)
    {
        if (!sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
        {
            Debug.LogWarning($"[SoundManager] SFX 파일을 찾을 수 없습니다: {sfxName}");
            return;
        }

        // 장부에 해당 효과음 카운트가 없다면 방을 만들어줌
        if (!activeSFXCount.ContainsKey(sfxName))
        {
            activeSFXCount.Add(sfxName, 0);
        }

        // =========================================================================
        // [조건 2] 같은 이름의 SFX가 이미 5개 재생 중이라면 더 이상 재생하지 않고 차단
        // =========================================================================
        if (activeSFXCount[sfxName] >= maxSameSFXCount)
        {
            // 의도된 제한이므로 경고창 대신 로그만 남기거나 아예 리턴시킵니다.
            // Debug.Log($"[SoundManager] {sfxName}의 동시 발음 수 제한(5개)으로 재생이 차단되었습니다.");
            return;
        }

        // 풀(큐)에서 재생기 하나 꺼내기
        AudioSource availableSource = GetAvailableSFXSource();

        if (availableSource != null)
        {
            // 재생 시작 직전, 장부의 카운트를 1 올림
            activeSFXCount[sfxName]++;

            availableSource.gameObject.SetActive(true);
            availableSource.clip = clip;
            availableSource.volume = SfxVolume; // ★ [버그 수정] 소리를 재생하기 직전에 실시간 볼륨 설정을 무조건 주입합니다.
            availableSource.Play();

            // 효과음 재생 시간만큼 기다렸다가 카운트를 깎아줄 코루틴 실행
            StartCoroutine(ReturnSourceToPoolRoutine(availableSource, sfxName, clip.length));
        }
    }

    /// <summary>
    /// 큐의 맨 앞 소스를 빼서 쓰고, 쓴 소스는 즉시 맨 뒤로 다시 집어넣어 계속 순환시키는 구조입니다.
    /// </summary>
    private AudioSource GetAvailableSFXSource()
    {
        AudioSource source = sfxPool.Dequeue();
        sfxPool.Enqueue(source);
        return source;
    }

    /// <summary>
    /// 사운드 재생이 끝난 시점에 오디오 소스 오브젝트를 꺼서 다음 사운드가 깨끗하게 재생될 수 있게 대기시킵니다.
    /// </summary>
    private IEnumerator ReturnSourceToPoolRoutine(AudioSource source, string sfxName, float delay)
    {
        yield return new WaitForSeconds(delay);

        // =========================================================================
        // [카운트 환원] 사운드가 정상 종료되었으므로 동시 재생 카운트를 1 감소시킵니다.
        // =========================================================================
        if (activeSFXCount.ContainsKey(sfxName))
        {
            activeSFXCount[sfxName] = Mathf.Max(0, activeSFXCount[sfxName] - 1);
        }

        if (source != null && !source.isPlaying)
        {
            source.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 외부(설정창)에서 BGM 볼륨을 실시간으로 변경할 때 호출합니다.
    /// </summary>
    public void SetBGMVolume(float value)
    {
        BgmVolume = value;

        // 현재 BGM을 재생 중인 오디오 소스의 볼륨을 즉시 변경합니다.
        if (bgmSource != null)
        {
            bgmSource.volume = value;
        }
    }

    /// <summary>
    /// 외부(설정창)에서 SFX 볼륨을 실시간으로 변경할 때 호출합니다.
    /// </summary>
    public void SetSFXVolume(float value)
    {
        SfxVolume = value;

        // 풀에 들어있는 모든 효과음 플레이어들의 볼륨도 실시간으로 같이 변경해줍니다.
        foreach (var source in sfxPool)
        {
            if (source != null)
            {
                source.volume = value;
            }
        }
    }
}