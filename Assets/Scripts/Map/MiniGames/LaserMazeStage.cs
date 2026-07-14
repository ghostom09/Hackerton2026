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
                // SpriteRenderer.bounds is expressed in world space, so its shape
                // stays aligned with the rendered beam even when its Laser parent
                // is rotated vertically.
                if (lasers[i].bounds.Contains(player.position))
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
            var laser = lasers[i];
            if (laser == null)
                continue;

            // Lasers holds only the beam renderers for collision.  The sibling
            // Square is visual-only, so blink it without adding it to this array.
            laser.color = color;
            var square = laser.transform.parent != null ? laser.transform.parent.Find("Square") : null;
            var squareRenderer = square != null ? square.GetComponent<SpriteRenderer>() : null;
            if (squareRenderer != null)
            {
                var squareColor = squareRenderer.color;
                squareColor.a = color.a;
                squareRenderer.color = squareColor;
            }
        }
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
