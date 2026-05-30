using UnityEngine;
using UnityEngine.SceneManagement;

public class BarrierCheat : MonoBehaviour
{
#if UNITY_EDITOR
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
         if (Input.GetKeyDown(KeyCode.F7))
        {
            if (Barrier.Instance != null)
            {
                Barrier.Instance.SetBarrierDie();
            }
        }
    }
#endif
}
