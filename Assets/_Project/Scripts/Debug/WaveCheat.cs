using UnityEngine;

public class WaveCheat : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        // F5 키: 다음 웨이브 강제 스킵
        if (Input.GetKeyDown(KeyCode.F5))
        {
            WaveManager.Instance?.ForceSkipToNextWave();
        }

        // F6 키: 필드의 모든 적 처치
        if (Input.GetKeyDown(KeyCode.F6))
        {
            KillAllEnemies();
        }
    }

    private void KillAllEnemies()
    {
        // 씬 내의 모든 Enemy 컴포넌트를 찾습니다.
        Enemy[] activeEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        if (activeEnemies.Length == 0)
        {
            Debug.Log("<color=yellow>[Cheat]</color> 처치할 적이 없습니다.");
            return;
        }

        foreach (Enemy enemy in activeEnemies)
        {
            // TakeDamage를 크게 주어 자연스럽게 죽이거나, 
            // 보상 없이 바로 지우고 싶다면 아래 코드를 사용하세요.
            enemy.TakeDamage(999999f);
        }

        Debug.Log($"<color=red>[Cheat]</color> 필드의 적 {activeEnemies.Length}마리를 모두 처치했습니다.");
    }
#endif
}