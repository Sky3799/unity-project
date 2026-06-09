using UnityEngine;
using TMPro;

public class StageTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private int stageNumber = 1;

    private float elapsed = 0f;
    private bool running = false;

    private void Start()
    {
        if (stageText != null)
            stageText.text = $"스테이지 {stageNumber}";
        running = true;
    }

    private void Update()
    {
        if (!running) return;
        elapsed += Time.deltaTime;
        int min = (int)(elapsed / 60f);
        int sec = (int)(elapsed % 60f);
        if (timerText != null)
            timerText.text = $"{min:00}:{sec:00}";
    }

    public void Stop()
    {
        running = false;
    }

    public float GetElapsed() => elapsed;
}

