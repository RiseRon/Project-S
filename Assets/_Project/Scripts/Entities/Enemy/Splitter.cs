using UnityEngine;

public class Splitter : Enemy
{
    protected override void Die()
    {
        // 1. 이미 죽은 상태라면 중복 실행 방지
        if (isDead) return;

        // 2. 분열 로직 실행
        SpawnSplits();

        // 3. 부모의 사망 로직 호출 (보상 지급, 풀 반납 등)
        base.Die();
    }

    private void SpawnSplits()
    {
        if (enemyData == null || enemyData.splitID <= 0) return;

        Vector3 deathPosition = transform.position;
        float parentDistance = TotalDistanceTraveled;
        Vector3 forwardDir = transform.forward;

        // 설정값
        float minSpreadDistance = 1.0f; // [추가] 최소 이만큼은 떨어져서 소환
        float maxSpreadRadius = 2.0f;   // 최대 흩어질 반경

        for (int i = 0; i < enemyData.splitSpawnCount; i++)
        {
            Vector3 offset = Vector3.zero;
            bool validPos = false;
            int safetyNet = 0; // 무한 루프 방지용

            // 1. 유효한 위치가 나올 때까지 반복 계산
            while (!validPos && safetyNet < 10)
            {
                safetyNet++;

                // 랜덤 좌표 생성
                Vector2 randomPoint = Random.insideUnitCircle * maxSpreadRadius;
                offset = new Vector3(randomPoint.x, 0, randomPoint.y);

                // 앞쪽이면 뒤로 반전
                if (Vector3.Dot(offset, forwardDir) > 0)
                {
                    offset = Vector3.Reflect(offset, forwardDir) * -1f;
                }

                // [핵심] 거리가 최소 거리보다 멀 때만 OK
                if (offset.magnitude >= minSpreadDistance)
                {
                    validPos = true;
                }
            }

            Vector3 spawnPos = deathPosition + offset;

            // 2. 소환 및 데이터 설정
            GameObject splitObj = PoolManager.Instance.SpawnFromPool(enemyData.splitID, spawnPos, Quaternion.identity);

            if (splitObj != null && splitObj.TryGetComponent<Enemy>(out var newEnemy))
            {
                SO_EnemyData splitData = SpawnManager.Instance.enemyDataList.Find(x => x.id == enemyData.splitID);

                if (splitData != null)
                {
                    newEnemy.Setup(waypoints, savedHpGrowthRate, splitData);
                    newEnemy.SetWaypointIndex(currentWaypointIndex);
                    if (WaveManager.Instance != null)
                    {
                        // 자식이 태어났으므로 activeEnemies를 증가시켜야 
                        // 얘네까지 다 잡아야 다음 웨이브가 시작됩니다.
                        WaveManager.Instance.AddActiveEnemy(1);
                    }

                    // 뒤로 밀려난 만큼 누적 이동 거리 차감
                    float distanceOffset = offset.magnitude;
                    newEnemy.SetTotalDistance(parentDistance - distanceOffset);

                    newEnemy.transform.position = spawnPos;
                }
            }
        }
    }
}