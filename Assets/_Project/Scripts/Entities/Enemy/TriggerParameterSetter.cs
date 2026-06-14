using UnityEngine;

// 이 스크립트는 기존 Enemy.cs를 수정하지 않고 트리거 전용으로 사용합니다.
public class TriggerParameterSetter : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        // 부모나 자신에게 있는 애니메이터를 찾습니다.
        animator = GetComponentInParent<Animator>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Barrier"))
        {
            animator.SetBool("isAttacking", true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Barrier"))
        {
            animator.SetBool("isAttacking", false);
        }
    }
}