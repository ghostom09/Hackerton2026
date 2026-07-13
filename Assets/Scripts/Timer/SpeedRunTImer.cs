using UnityEngine;

public class SpeedRunTImer : MonoBehaviour
{
    private float elapsedTime;
    private bool isRunning;

    public float ElapsedTime => elapsedTime;
    public int ElapsedSeconds => Mathf.FloorToInt(elapsedTime);
    public string FormattedTime => FormatTime(elapsedTime);

    void Start()
    {
        StartTimer();
    }

    void Update()
    {
        if (!isRunning)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
    }

    private static string FormatTime(float totalSeconds)
    {
        totalSeconds = Mathf.Max(0f, totalSeconds);
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        int centiseconds = Mathf.FloorToInt((totalSeconds - Mathf.Floor(totalSeconds)) * 100f);
        return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }
}
