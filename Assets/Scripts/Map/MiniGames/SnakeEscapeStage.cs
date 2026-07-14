using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Snake: crawl the escape tunnel, collect safety kits, avoid fire cells.
/// </summary>
[DisallowMultipleComponent]
public class SnakeEscapeStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private SpriteRenderer bg;

    [Header("Rules")]
    [SerializeField] private int width = 12;
    [SerializeField] private int height = 8;
    [SerializeField] private float cellSize = 0.55f;
    [SerializeField] private float stepInterval = 0.18f;
    [SerializeField] private int kitsToCollect = 5;
    [SerializeField, Min(0f)] private float wallHitTimePenalty = 1f;

    private readonly List<Vector2Int> _snake = new();
    private readonly HashSet<Vector2Int> _fires = new();
    private readonly Dictionary<Vector2Int, Transform> _visuals = new();
    private Vector2Int _dir = Vector2Int.right;
    private Vector2Int _nextDir = Vector2Int.right;
    private Vector2Int _kit;
    private Transform _kitBox;
    private float _timer;
    private int _collected;
    private bool _complete;
    private bool _ready;

    private void Awake()
    {
        if (boardRoot == null)
            boardRoot = transform;
        BuildBoard();
        ResetSnake();
        PlaceKit();
        _ready = true;
    }

    private void Update()
    {
        if (!_ready || _complete)
            return;

        ReadInput();
        _timer -= Time.deltaTime;
        if (_timer > 0f)
            return;

        _timer = stepInterval;
        Step();
    }

    private void ReadInput()
    {
        var k = Keyboard.current;
        if (k == null)
            return;

        if ((k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) && _dir != Vector2Int.down)
            _nextDir = Vector2Int.up;
        else if ((k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) && _dir != Vector2Int.up)
            _nextDir = Vector2Int.down;
        else if ((k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) && _dir != Vector2Int.right)
            _nextDir = Vector2Int.left;
        else if ((k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) && _dir != Vector2Int.left)
            _nextDir = Vector2Int.right;
    }

    private void Step()
    {
        _dir = _nextDir;
        var head = _snake[0] + _dir;

        bool hitWall = head.x < 0 || head.y < 0 || head.x >= width || head.y >= height;
        if (hitWall)
        {
            GameManager.Instance?.ReduceCurrentMapTime(wallHitTimePenalty);
            ResetSnake();
            return;
        }

        if (_fires.Contains(head) || _snake.Contains(head))
        {
            ResetSnake();
            return;
        }

        _snake.Insert(0, head);
        if (head == _kit)
        {
            RemoveKitBox();
            _collected++;
            if (_collected >= kitsToCollect)
            {
                Complete();
                return;
            }
            PlaceKit();
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }

        RefreshSnakeVisuals();
    }

    private void ResetSnake()
    {
        _snake.Clear();
        var start = new Vector2Int(2, height / 2);
        _snake.Add(start);
        _snake.Add(start + Vector2Int.left);
        _snake.Add(start + Vector2Int.left * 2);
        _dir = Vector2Int.right;
        _nextDir = Vector2Int.right;
        _timer = stepInterval;
        RefreshSnakeVisuals();
    }

    private void PlaceKit()
    {
        for (var n = 0; n < 80; n++)
        {
            var p = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            if (_fires.Contains(p) || _snake.Contains(p))
                continue;
            _kit = p;
            SetCell(_kit, new Color(0.18f, 0.2f, 0.22f), "Kit");
            SpawnKitBox();
            return;
        }
    }

    private void BuildBoard()
    {
        ClearBoard();
        var origin = new Vector2(-(width - 1) * cellSize * 0.5f, -(height - 1) * cellSize * 0.5f);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var p = new Vector2Int(x, y);
            var world = origin + new Vector2(x * cellSize, y * cellSize);
            var cell = SpawnCell(world, new Color(0.18f, 0.2f, 0.22f), $"Floor_{x}_{y}");
            _visuals[p] = cell;

            /*if ((x + y) % 7 == 0 && !(x < 4 && Mathf.Abs(y - height / 2) < 2))
            {
                _fires.Add(p);
                SetCell(p, new Color(1f, 0.35f, 0.1f), "Fire");
            }*/
        }
    }

    private void RefreshSnakeVisuals()
    {
        foreach (var kv in _visuals)
        {
            if (_fires.Contains(kv.Key))
            {
                SetCell(kv.Key, new Color(1f, 0.35f, 0.1f), "Fire");
                continue;
            }
            if (kv.Key == _kit)
            {
                SetCell(kv.Key, new Color(0.18f, 0.2f, 0.22f), "Kit");
                continue;
            }
            SetCell(kv.Key, new Color(0.18f, 0.2f, 0.22f), "Floor");
        }

        for (var i = 0; i < _snake.Count; i++)
        {
            var c = i == 0 ? new Color(0.35f, 0.85f, 0.45f) : new Color(0.25f, 0.65f, 0.35f);
            SetCell(_snake[i], c, "Snake", 3);
        }
    }

    private void SpawnKitBox()
    {
        if (boxPrefab == null || !_visuals.TryGetValue(_kit, out var kitCell) || kitCell == null)
            return;

        _kitBox = Instantiate(boxPrefab, kitCell).transform;
        _kitBox.name = $"Box_{_kit.x}_{_kit.y}";
        _kitBox.localPosition = Vector3.zero;

        foreach (var sprite in _kitBox.GetComponentsInChildren<SpriteRenderer>(true))
            sprite.sortingOrder = 2;
    }

    private void RemoveKitBox()
    {
        if (_kitBox == null)
            return;

        Destroy(_kitBox.gameObject);
        _kitBox = null;
    }

    private void SetCell(Vector2Int p, Color color, string label, int sortingOrder = 1)
    {
        if (!_visuals.TryGetValue(p, out var t) || t == null)
            return;
        t.name = $"{label}_{p.x}_{p.y}";
        var sr = MiniGameVisuals.FindSprite(t);
        if (sr != null)
        {
            sr.color = color;
            sr.sortingOrder = sortingOrder;
        }
    }

    private Transform SpawnCell(Vector2 pos, Color color, string name)
    {
        GameObject go;
        if (cellPrefab != null)
        {
            go = Instantiate(cellPrefab, boardRoot);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * cellSize;
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(boardRoot, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * cellSize;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sr.color = color;
            sr.sortingOrder = 2;
        }

        go.name = name;
        go.SetActive(true);
        var sprite = MiniGameVisuals.FindSprite(go.transform);
        if (sprite != null)
            sprite.color = color;
        return go.transform;
    }

    private void ClearBoard()
    {
        _kitBox = null;
        _visuals.Clear();
        _fires.Clear();
        for (var i = boardRoot.childCount - 1; i >= 0; i--)
            Destroy(boardRoot.GetChild(i).gameObject);
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
