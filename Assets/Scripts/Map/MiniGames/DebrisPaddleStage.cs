using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pong-style: move a safety net and catch falling debris before it hits people.
/// </summary>
[DisallowMultipleComponent]
public class DebrisPaddleStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform paddle;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private Transform groundLine;

    [Header("Rules")]
    [SerializeField] private int requiredCatches = 8;
    [SerializeField] private float paddleSpeed = 9f;
    [SerializeField] private float spawnInterval = 0.85f;
    [SerializeField] private float fallSpeed = 3.8f;
    [SerializeField] private float paddleY = -3f;
    [SerializeField] private float xClamp = 5.5f;

    [Header("Penalty")]
    [Range(0f, 1f)] [SerializeField] private float missedDebrisTimePenaltyFraction = 1f / 3f;

    private readonly List<Transform> _debris = new();
    private float _spawnTimer;
    private int _catches;
    private bool _complete;

    private void Awake()
    {
        if (spawnParent == null)
            spawnParent = transform;
        if (paddle == null)
        {
            var go = new GameObject("Paddle");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, paddleY, 0f);
            go.transform.localScale = new Vector3(1.8f, 0.35f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = new Color(0.3f, 0.8f, 0.45f);
            sr.sortingOrder = 5;
            paddle = go.transform;
        }
    }

    private void Update()
    {
        if (_complete || paddle == null)
            return;

        var input = 0f;
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) input -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) input += 1f;
        }

        var p = paddle.position;
        p.x = Mathf.Clamp(p.x + input * paddleSpeed * Time.deltaTime, -xClamp, xClamp);
        p.y = paddleY;
        paddle.position = p;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnDebris();
            _spawnTimer = spawnInterval;
        }

        var groundY = groundLine != null ? groundLine.position.y : -3.6f;
        for (var i = _debris.Count - 1; i >= 0; i--)
        {
            var d = _debris[i];
            if (d == null)
            {
                _debris.RemoveAt(i);
                continue;
            }

            d.position += Vector3.down * (fallSpeed * Time.deltaTime);

            if (Mathf.Abs(d.position.x - paddle.position.x) < 1.1f &&
                Mathf.Abs(d.position.y - paddle.position.y) < 0.45f)
            {
                Destroy(d.gameObject);
                _debris.RemoveAt(i);
                _catches++;
                if (_catches >= requiredCatches)
                    Complete();
                continue;
            }

            if (d.position.y <= groundY)
            {
                Destroy(d.gameObject);
                _debris.RemoveAt(i);
                _catches = Mathf.Max(0, _catches - 1);
                GameManager.Instance?.ReduceCurrentMapTimeByRemainingFraction(missedDebrisTimePenaltyFraction);
            }
        }
    }

    private void SpawnDebris()
    {
        var x = Random.Range(-5f, 5f);
        var pos = new Vector3(x, 4.2f, 0f);
        GameObject go;
        if (debrisPrefab != null)
        {
            go = Instantiate(debrisPrefab, pos, Quaternion.identity, spawnParent);
            go.transform.localScale = Vector3.one * Random.Range(0.45f, 0.75f);
        }
        else
        {
            go = new GameObject("Debris");
            go.transform.SetParent(spawnParent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.45f, 0.75f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            sr.color = new Color(0.55f, 0.5f, 0.45f);
            sr.sortingOrder = 4;
        }
        go.SetActive(true);
        _debris.Add(go.transform);
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
