using UnityEngine;

#if UNITY_EDITOR
public class DebugOverlay : MonoBehaviour
{
    private bool isVisible = false;

    // FPS 업데이트용 변수
    private float fpsUpdateInterval = 1.0f; // 1초 간격
    private float fpsAccumulator = 0;
    private int frameCounter = 0;
    private float timeLeft;
    private float currentFPS = 0;

    [Header("설정")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F10;
    [SerializeField] private Font korenFont;

    private void Start()
    {
        timeLeft = fpsUpdateInterval;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) isVisible = !isVisible;

        if (isVisible)
        {
            // FPS 계산 로직 (1초마다 업데이트)
            timeLeft -= Time.unscaledDeltaTime;
            fpsAccumulator += Time.unscaledDeltaTime;
            frameCounter++;

            if (timeLeft <= 0.0)
            {
                currentFPS = frameCounter / fpsAccumulator;
                timeLeft = fpsUpdateInterval;
                fpsAccumulator = 0.0f;
                frameCounter = 0;
            }
        }
    }

    private void OnGUI()
    {
        if (!isVisible || WaveManager.Instance == null) return;

        // 스타일 및 레이아웃 설정
        float width = 450;
        float height = 220;
        Rect rect = new Rect(20, Screen.height - height - 20, width, height);

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(25, 25, 25, 25);
        if (korenFont != null) style.font = korenFont;

        // 배경색 설정
        Texture2D background = new Texture2D(1, 1);
        background.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
        background.Apply();
        style.normal.background = background;

        // 데이터 로드
        int enemies = WaveManager.Instance.ActiveEnemyCount;
        float waveTimeLeft = WaveManager.Instance.WaveRemainingTime;

        string text = string.Format(
            "◈ 디버그 시스템 ({0})\n\n" +
            "남은 적 수      : {1} 마리\n" +
            "웨이브 남은 시간 : {2:F1} 초\n" +
            "현재 성능       : {3:F0} FPS",
            toggleKey.ToString(), enemies, waveTimeLeft, currentFPS);

        GUI.Label(rect, text, style);
    }
}
#endif