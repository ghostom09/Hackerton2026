using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private float remainingTime;
    private bool isRunning;

    public int RemainingSeconds => Mathf.CeilToInt(remainingTime);

    void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
        }
    }

    void Start()
    {
        int startTime = GameManager.Instance != null ? GameManager.Instance.time : 0;
        SetTime(startTime);
        StartTimer();
    }

    void Update()
    {
        if (!isRunning)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateText();

        if (remainingTime <= 0f)
        {
            isRunning = false;
        }
    }

    public void SetTime(int seconds)
    {
        remainingTime = Mathf.Max(0, seconds);
        UpdateText();
    }

    public void StartTimer()
    {
        isRunning = remainingTime > 0f;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    private void UpdateText()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text = FormatTime(RemainingSeconds);
    }

    private static string FormatTime(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00} : {seconds:00}";
    }
}
