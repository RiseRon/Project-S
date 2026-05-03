using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 씬 이동이 필요할 때 이 함수를 호출
    public void ChangeScene(string sceneName)
    {
        SceneLoader.LoadScene(sceneName);
    }
}