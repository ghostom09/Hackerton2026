using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pac-Man lite: collect first-aid kits while avoiding roaming hazards.
/// </summary>
[DisallowMultipleComponent]
public class KitCollectStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private GameObject cellPrefab;

    [Header("Rules")]
    [SerializeField] private int width = 9;
    [SerializeField] private int height = 7;
    [SerializeField] private float cellSize = 0.6f;
    [SerializeField] private int kitCount = 6;
    [SerializeField] private float hazardStep = 0.45f;

    private bool[,] _wall;
    private bool[,] _kit;
    private Transform[,] _cells;
    private Vector2Int _player;
    private readonly List<Vector2Int> _hazards = new();
    private float _hazardTimer;
    private int _remaining;
    private bool _complete;

    private void Awake()
    {
        if (boardRoot == null)
            boardRoot = transform;
        Build();
    }

    private void Update()
    {
        if (_complete)
            return;

        var k = Keyboard.current;
        if (k != null)
        {
            if (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) TryMove(Vector2Int.up);
            else if (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) TryMove(Vector2Int.down);
            else if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) TryMove(Vector2Int.left);
            else if (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) TryMove(Vector2Int.right);
        }

        _hazardTimer -= Time.deltaTime;
        if (_hazardTimer <= 0f)
        {
            _hazardTimer = hazardStep;
            MoveHazards();
        }

        if (HitHazard())
        {
            _player = new Vector2Int(1, height / 2);
            Refresh();
        }
    }

    private void TryMove(Vector2Int dir)
    {
        var next = _player + dir;
        if (!InBounds(next) || _wall[next.x, next.y])
            return;
        _player = next;
        if (_kit[_player.x, _player.y])
        {
            _kit[_player.x, _player.y] = false;
            _remaining--;
            if (_remaining <= 0)
            {
                Complete();
                return;
            }
        }
        Refresh();
    }

    private void MoveHazards()
    {
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (var i = 0; i < _hazards.Count; i++)
        {
            var d = dirs[Random.Range(0, dirs.Length)];
            var next = _hazards[i] + d;
            if (!InBounds(next) || _wall[next.x, next.y])
                continue;
            _hazards[i] = next;
        }
        Refresh();
    }

    private bool HitHazard()
    {
        for (var i = 0; i < _hazards.Count; i++)
        {
            if (_hazards[i] == _player)
                return true;
        }
        return false;
    }

    private void Build()
    {
        _wall = new bool[width, height];
        _kit = new bool[width, height];
        _cells = new Transform[width, height];
        _hazards.Clear();

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            _wall[x, y] = x == 0 || y == 0 || x == width - 1 || y == height - 1 || ((x % 3 == 0) && (y % 2 == 0) && x > 1 && x < width - 2);

        _player = new Vector2Int(1, height / 2);
        _hazards.Add(new Vector2Int(width - 2, 1));
        _hazards.Add(new Vector2Int(width - 2, height - 2));

        _remaining = 0;
        while (_remaining < kitCount)
        {
            var x = Random.Range(1, width - 1);
            var y = Random.Range(1, height - 1);
            if (_wall[x, y] || _kit[x, y] || (x == _player.x && y == _player.y))
                continue;
            _kit[x, y] = true;
            _remaining++;
        }

        var origin = new Vector2(-(width - 1) * cellSize * 0.5f, -(height - 1) * cellSize * 0.5f);
        for (var i = boardRoot.childCount - 1; i >= 0; i--)
            Destroy(boardRoot.GetChild(i).gameObject);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            _cells[x, y] = Spawn(origin + new Vector2(x * cellSize, y * cellSize), $"Cell_{x}_{y}").transform;

        Refresh();
    }

    private void Refresh()
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            Color c;
            if (_player.x == x && _player.y == y) c = new Color(0.35f, 0.8f, 1f);
            else if (IsHazard(x, y)) c = new Color(0.95f, 0.25f, 0.2f);
            else if (_wall[x, y]) c = new Color(0.22f, 0.22f, 0.25f);
            else if (_kit[x, y]) c = new Color(0.35f, 0.95f, 0.55f);
            else c = new Color(0.14f, 0.16f, 0.18f);

            var sr = MiniGameVisuals.FindSprite(_cells[x, y]);
            if (sr != null)
                sr.color = c;
        }
    }

    private bool IsHazard(int x, int y)
    {
        for (var i = 0; i < _hazards.Count; i++)
        {
            if (_hazards[i].x == x && _hazards[i].y == y)
                return true;
        }
        return false;
    }

    private GameObject Spawn(Vector2 pos, string name)
    {
        GameObject go;
        if (cellPrefab != null)
        {
            go = Instantiate(cellPrefab, boardRoot);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * (cellSize * 0.9f);
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(boardRoot, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * (cellSize * 0.9f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.sortingOrder = 2;
        }
        go.name = name;
        go.SetActive(true);
        return go;
    }

    private bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
