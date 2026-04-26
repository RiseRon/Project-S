using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SO_SlimeData))] // 어떤 스크립트를 편집할지 지정
public class SlimeDataEditor : Editor
{
    public override void OnInspectorGUI() // 커스텀 인스팩터
    {
        // 원본 타겟 데이터 가져오기
        SO_SlimeData data = (SO_SlimeData)target;

        // 기본 정보 섹션
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        data.slimeID = EditorGUILayout.TextField("슬라임 ID", data.slimeID);
        data.slimeName = EditorGUILayout.TextField("슬라임 이름", data.slimeName);
        data.elementType = (SlimeElementType)EditorGUILayout.EnumPopup("슬라임 속성", data.elementType);
        data.MasteryLevel = EditorGUILayout.IntField("마스터리 레벨", data.MasteryLevel);

        EditorGUILayout.Space();

        // 전투 스텟 섹션
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        data.attackDamage = EditorGUILayout.FloatField("공격력", data.attackDamage);
        data.attackRange = EditorGUILayout.FloatField("공격 사거리", data.attackRange);
        data.attackInterval = EditorGUILayout.FloatField("공격 주기", data.attackInterval);

        EditorGUILayout.Space();

        // 투사체 설정 섹션
        EditorGUILayout.LabelField("투사체 설정", EditorStyles.boldLabel);
        data.trajectoryType = (TrajectoryType)EditorGUILayout.EnumPopup("궤도 타입", data.trajectoryType);
        data.projectileSpeed = EditorGUILayout.FloatField("투사체 속도", data.projectileSpeed);
        // [조건부 표시] 포물선일 때만 arcHeight 표시
        if (data.trajectoryType == TrajectoryType.Parabolic)
        {
            data.arcHeight = EditorGUILayout.FloatField("포물선 높이", data.arcHeight);
        }

        EditorGUILayout.Space();

        // 특수 효과 섹션
        EditorGUILayout.LabelField("특수 효과", EditorStyles.boldLabel);
        data.projectileType = (ProjectileType)EditorGUILayout.EnumPopup("투사체 타입", data.projectileType);

        if (data.projectileType == ProjectileType.Single)
        {
            // 직선형(Single)일 때는 스턴 확률만 표시
            data.stunChance = EditorGUILayout.Slider("스턴 확률 (%)", data.stunChance, 0, 100);
        }
        else
        {
            // 장판형(Area/Floor)일 때는 슬로우와 도트뎀 표시
            data.areaPrefabID = EditorGUILayout.IntField("장판 프리팹 ID", data.areaPrefabID);
            data.slowRate = EditorGUILayout.Slider("슬로우 비율 (%)", data.slowRate, 0, 100);
            data.dotDamage = EditorGUILayout.FloatField("도트 데미지", data.dotDamage);
            data.damageInterval = EditorGUILayout.FloatField("도트 데미지 주기", data.damageInterval);
            data.areaDuration = EditorGUILayout.FloatField("장판 유지 시간", data.areaDuration);  
        }

        // 변경사항 저장 (이걸 안 하면 인스펙터 값이 저장이 안 됨)
        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }
}