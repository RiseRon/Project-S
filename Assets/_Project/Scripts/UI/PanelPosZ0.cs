using UnityEngine;

[ExecuteAlways]
public class PanelPosZ0 : MonoBehaviour
{
    // 자식 오브젝트가 새로 들어오거나 나갈 때만 유니티가 호출해줍니다.
    private void OnTransformChildrenChanged()
    {
        // 게임 실행 중이 아닐 때도 작동하도록 설정
        FixAllChildrenZ();
    }

    // 에디터에서 스크립트가 처음 붙거나 리셋될 때를 위한 대비
    private void OnValidate()
    {
        FixAllChildrenZ();
    }

    private void FixAllChildrenZ()
    {
        foreach (Transform child in transform)
        {
            // Z축이 0이 아닌 놈들만 골라서 0으로 세팅
            if (child.localPosition.z != 0)
            {
                Vector3 pos = child.localPosition;
                pos.z = 0;
                child.localPosition = pos;

                // 에디터 상에서 변경사항을 즉시 저장 (Scene View 업데이트)
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(child);
                }
#endif
            }
        }
    }
}