using UnityEngine;

[DisallowMultipleComponent]
public class LaserMazeStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private SpriteRenderer[] lasers;
    [SerializeField] private Vector2 roomClamp = new(5.8f, 3.4f);

    [Header("Rules")]
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float blinkInterval = 1.1f;
    [SerializeField] private float exitRadius = 0.8f;
    [SerializeField] private Color laserOnColor = new(1f, 0.15f, 0.2f, 0.9f);
    [SerializeField] private Color laserOffColor = new(0.4f, 0.1f, 0.12f, 0.25f);

    private Vector3 _spawnPos;
    private float _blinkTimer;
    private bool _lasersOn = true;
    private bool _complete;

    private void Awake()
    {
        if (player != null)
            _spawnPos = player.position;

        if (lasers == null || lasers.Length == 0)
        {
            var list = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Laser"))
                {
                    var sr = MiniGameVisuals.FindSprite(child);
                    if (sr != null)
                        list.Add(sr);
                }
            }
            lasers = list.ToArray();
        }

        ApplyLaserColors();
    }

    private void Update()
    {
        if (_complete || player == null || exitPoint == null)
            return;

        var input = MiniGameVisuals.ReadWasd();
        player.position += (Vector3)(input * (moveSpeed * Time.deltaTime));
        var p = player.position;
        p.x = Mathf.Clamp(p.x, -roomClamp.x, roomClamp.x);
        p.y = Mathf.Clamp(p.y, -roomClamp.y, roomClamp.y);
        player.position = p;

        _blinkTimer -= Time.deltaTime;
        if (_blinkTimer <= 0f)
        {
            _lasersOn = !_lasersOn;
            _blinkTimer = blinkInterval;
            ApplyLaserColors();
        }

        if (_lasersOn && lasers != null)
        {
            for (var i = 0; i < lasers.Length; i++)
            {
                if (lasers[i] == null)
                    continue;
                if (MiniGameVisuals.ContainsPoint(lasers[i].transform, MiniGameVisuals.HalfFromScale(lasers[i].transform), player.position))
                {
                    player.position = _spawnPos;
                    break;
                }
            }
        }

        if (Vector2.Distance(player.position, exitPoint.position) < exitRadius)
            Complete();
    }

    private void ApplyLaserColors()
    {
        if (lasers == null)
            return;
        var color = _lasersOn ? laserOnColor : laserOffColor;
        for (var i = 0; i < lasers.Length; i++)
        {
            if (lasers[i] != null)
                lasers[i].color = color;
        }
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
