using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Slider timeSlider;

    private float remainingTime;
    private float maxTime;
    private bool isRunning;

    public int RemainingSeconds => Mathf.CeilToInt(remainingTime);
    public float RemainingNormalized => maxTime <= 0f ? 0f : Mathf.Clamp01(remainingTime / maxTime);

    private void Awake()
    {
        if (timerText == null)
            timerText = GetComponent<TMP_Text>();

        if (timeSlider == null)
            timeSlider = GetComponentInChildren<Slider>(true);
    }

    private void Update()
    {
        if (!isRunning)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateUI();

        if (remainingTime <= 0f)
            isRunning = false;
    }

    public void BeginOrder(float seconds)
    {
        maxTime = Mathf.Max(0f, seconds);
        remainingTime = maxTime;
        isRunning = maxTime > 0f;

        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.wholeNumbers = false;
        }

        UpdateUI();
    }

    public void SetTime(int seconds)
    {
        BeginOrder(seconds);
    }

    public void StartTimer()
    {
        isRunning = remainingTime > 0f;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    private void UpdateUI()
    {
        if (timerText != null)
            timerText.text = FormatTime(RemainingSeconds);

        if (timeSlider != null)
            timeSlider.value = RemainingNormalized;
    }

    private static string FormatTime(int totalSeconds)
    {
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes:00} : {seconds:00}";
    }
}
