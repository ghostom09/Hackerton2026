using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Classic stacking: drop sandbags column-by-column to block the flood line.
/// </summary>
[DisallowMultipleComponent]
public class SandbagStackStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private GameObject bagPrefab;
    [SerializeField] private Transform floodLine;

    [Header("Rules")]
    [SerializeField] private int columns = 6;
    [SerializeField] private int rowsNeeded = 4;
    [SerializeField] private float cellSize = 0.7f;
    [SerializeField] private float dropSpeed = 8f;

    private int[] _heights;
    private int _cursor;
    private Transform _falling;
    private bool _dropping;
    private bool _complete;

    private void Awake()
    {
        if (boardRoot == null)
            boardRoot = transform;
        _heights = new int[columns];
        _cursor = columns / 2;
        SpawnFalling();
    }

    private void Update()
    {
        if (_complete)
            return;

        var k = Keyboard.current;
        if (!_dropping && k != null)
        {
            if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame)
                _cursor = Mathf.Max(0, _cursor - 1);
            if (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame)
                _cursor = Mathf.Min(columns - 1, _cursor + 1);
            if (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)
                _dropping = true;

            if (_falling != null)
                _falling.position = ColumnWorld(_cursor, _heights[_cursor] + 6);
        }

        if (_dropping && _falling != null)
        {
            var target = ColumnWorld(_cursor, _heights[_cursor]);
            _falling.position = Vector3.MoveTowards(_falling.position, target, dropSpeed * Time.deltaTime);
            if (Vector3.Distance(_falling.position, target) < 0.02f)
            {
                _falling.position = target;
                _heights[_cursor]++;
                _falling = null;
                _dropping = false;

                if (IsWallComplete())
                {
                    Complete();
                    return;
                }

                if (_heights[_cursor] >= rowsNeeded + 2)
                    _heights[_cursor] = rowsNeeded;
                SpawnFalling();
            }
        }

        if (floodLine != null)
        {
            var filled = 0;
            for (var i = 0; i < columns; i++)
                if (_heights[i] >= rowsNeeded)
                    filled++;
            var t = filled / (float)columns;
            floodLine.localScale = new Vector3(columns * cellSize, Mathf.Lerp(2.5f, 0.2f, t), 1f);
        }
    }

    private bool IsWallComplete()
    {
        for (var i = 0; i < columns; i++)
        {
            if (_heights[i] < rowsNeeded)
                return false;
        }
        return true;
    }

    private void SpawnFalling()
    {
        var pos = ColumnWorld(_cursor, _heights[_cursor] + 6);
        GameObject go;
        if (bagPrefab != null)
        {
            go = Instantiate(bagPrefab, pos, Quaternion.identity, boardRoot);
            go.transform.localScale = Vector3.one * (cellSize * 0.9f);
        }
        else
        {
            go = new GameObject("Sandbag");
            go.transform.SetParent(boardRoot, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * (cellSize * 0.9f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = new Color(0.72f, 0.55f, 0.28f);
            sr.sortingOrder = 5;
        }
        go.SetActive(true);
        var sprite = MiniGameVisuals.FindSprite(go.transform);
        if (sprite != null)
            sprite.color = new Color(0.72f, 0.55f, 0.28f);
        _falling = go.transform;
    }

    private Vector3 ColumnWorld(int col, int row)
    {
        var x = (col - (columns - 1) * 0.5f) * cellSize;
        var y = -2.2f + row * cellSize;
        return boardRoot.TransformPoint(new Vector3(x, y, 0f));
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
