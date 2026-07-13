using UnityEngine;

public class Plug : MonoBehaviour
{
    [SerializeField] private int frequency;
    [SerializeField] private float snapDistance = 0.5f;
    [SerializeField] private ConnectionCounter connectionCounter;

    public int Frequency => frequency;

    private void Update()
    {
        var heads = FindObjectsByType<OutletHead>(FindObjectsSortMode.None);

        foreach (var head in heads)
        {
            if (head.IsConnected)
                continue;

            if (Vector2.Distance(head.transform.position, transform.position) > snapDistance)
                continue;

            if (head.Frequency != frequency)
                continue;

            head.ConnectTo(transform.position);

            if (connectionCounter != null)
                connectionCounter.OnConnected();
        }
    }
}
