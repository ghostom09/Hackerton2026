using UnityEngine;

public class SpeedRunTImer : MonoBehaviour
{
    private float elapsedTime;
    private bool isRunning;

    public float ElapsedTime => elapsedTime;
    public int ElapsedSeconds => Mathf.FloorToInt(elapsedTime);
    public string FormattedTime => FormatTime(ElapsedSeconds);

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

    private static string FormatTime(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00} : {seconds:00}";
    }
}
