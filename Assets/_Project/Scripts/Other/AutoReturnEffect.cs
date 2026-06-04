using UnityEngine;

public class AutoReturnEffect : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        // 이펙트가 켜지는 순간 실행
        if (ps != null)
        {
            ps.Play();
            // 파티클의 지속 시간(duration) + 메인 루프 시간만큼 기다렸다가 끄기
            Invoke(nameof(DisableSelf), ps.main.duration + ps.main.startDelay.constantMax);
        }
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false); // 꺼지면 풀매니저가 재사용 가능한 상태가 됨
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}