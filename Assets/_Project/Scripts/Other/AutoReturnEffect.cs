using UnityEngine;

public class AutoReturnEffect : MonoBehaviour
{
    private ParticleSystem ps;
    private string effectName; // 💡 [새로 추가] 이펙트가 풀에서 태어날 때 부여받은 고유 이름

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        // 💡 [새로 추가] 프리팹 이름에서 "(Clone)" 찌꺼기를 지우고 원본 이름을 보관합니다.
        // 예: "Hit_Normal(Clone)" -> "Hit_Normal"
        effectName = gameObject.name.Replace("(Clone)", "").Trim();
    }

    private void OnEnable()
    {
        if (ps != null)
        {
            ps.Play();
            // 파티클의 지속 시간(duration) + 최대 시작 지연 시간만큼 기다렸다가 반환 요청
            Invoke(nameof(DisableSelf), ps.main.duration + ps.main.startDelay.constantMax);
        }
    }

    private void DisableSelf()
    {
        // 💡 [전면 수정] 스스로 활성화를 끄지 않고, 매니저에게 본인 이름과 자신을 넘겨주며 정상 회수를 요청합니다.
        if (EffectPoolManager.Instance != null)
        {
            EffectPoolManager.Instance.ReturnEffect(effectName, gameObject);
        }
        else
        {
            // 혹시 씬 전환 등으로 매니저가 먼저 파괴되었다면 안전하게 스스로 꺼지도록 방어벽 배치
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}