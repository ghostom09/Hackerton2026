using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Collapsing bridge: run right and jump gaps to reach the safe side.
/// </summary>
[DisallowMultipleComponent]
public class GapJumpStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform groundRoot;
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private Transform goal;

    [Header("Rules")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7.5f;
    [SerializeField] private float gravity = 18f;
    [SerializeField] private float segmentWidth = 1.5f;
    [SerializeField] private int segmentCount = 12;
    [SerializeField] private float gapChance = 0.3f;

    private readonly List<Transform> _segments = new();
    private float _vy;
    private bool _grounded = true;
    private bool _complete;
    private Vector3 _startPos;

    private void Awake()
    {
        if (groundRoot == null)
            groundRoot = transform;
        EnsurePlayer();
        EnsureGoal();
        BuildBridge();
        _startPos = player.position;
    }

    private void Update()
    {
        if (_complete || player == null)
            return;

        var inputX = 0f;
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) inputX -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) inputX += 1f;
            if (_grounded && (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame))
            {
                _vy = jumpForce;
                _grounded = false;
            }
        }

        var pos = player.position;
        pos.x += inputX * moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -6.5f, 7f);

        _vy -= gravity * Time.deltaTime;
        pos.y += _vy * Time.deltaTime;

        var floorY = FloorYAt(pos.x);
        if (floorY.HasValue)
        {
            if (pos.y <= floorY.Value && _vy <= 0f)
            {
                pos.y = floorY.Value;
                _vy = 0f;
                _grounded = true;
            }
        }
        else if (pos.y < -4.5f)
        {
            player.position = _startPos;
            _vy = 0f;
            _grounded = true;
            return;
        }

        player.position = pos;

        if (goal != null && Vector2.Distance(player.position, goal.position) < 0.85f)
            Complete();
    }

    private float? FloorYAt(float x)
    {
        foreach (var seg in _segments)
        {
            if (seg == null || !seg.gameObject.activeSelf)
                continue;
            var half = Mathf.Abs(seg.localScale.x) * 0.5f;
            if (Mathf.Abs(x - seg.position.x) <= half + 0.05f)
                return seg.position.y + Mathf.Abs(seg.localScale.y) * 0.5f + 0.28f;
        }
        return null;
    }

    private void BuildBridge()
    {
        for (var i = groundRoot.childCount - 1; i >= 0; i--)
            Destroy(groundRoot.GetChild(i).gameObject);
        _segments.Clear();

        var x = -6f;
        for (var i = 0; i < segmentCount; i++)
        {
            var isGap = i > 1 && i < segmentCount - 2 && Random.value < gapChance;
            if (!isGap)
                _segments.Add(SpawnSegment(new Vector3(x, -2.1f, 0f)).transform);
            x += segmentWidth;
        }

        var endX = 5.6f;
        _segments.Add(SpawnSegment(new Vector3(endX, -2.1f, 0f)).transform);
        if (goal != null)
            goal.position = new Vector3(endX + 0.9f, -1.2f, 0f);
    }

    private GameObject SpawnSegment(Vector3 pos)
    {
        GameObject go;
        if (segmentPrefab != null)
        {
            go = Instantiate(segmentPrefab, pos, Quaternion.identity, groundRoot);
            go.transform.localScale = new Vector3(segmentWidth * 0.9f, 0.4f, 1f);
        }
        else
        {
            go = new GameObject("Segment");
            go.transform.SetParent(groundRoot, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(segmentWidth * 0.9f, 0.4f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = new Color(0.45f, 0.42f, 0.38f);
            sr.sortingOrder = 2;
        }
        go.SetActive(true);
        return go;
    }

    private void EnsurePlayer()
    {
        if (player != null)
            return;
        var go = new GameObject("Player");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(-5.5f, -1.2f, 0f);
        go.transform.localScale = Vector3.one * 0.55f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
        sr.color = new Color(0.35f, 0.75f, 1f);
        sr.sortingOrder = 6;
        player = go.transform;
    }

    private void EnsureGoal()
    {
        if (goal != null)
            return;
        var g = new GameObject("Goal");
        g.transform.SetParent(transform, false);
        g.transform.position = new Vector3(6.2f, -1.2f, 0f);
        g.transform.localScale = Vector3.one * 0.9f;
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
        sr.color = new Color(0.3f, 0.9f, 0.45f);
        sr.sortingOrder = 5;
        goal = g.transform;
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
