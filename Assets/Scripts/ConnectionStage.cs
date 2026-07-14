using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configures the three connection pairs for one round.
/// Each pair receives a unique shape, while the wire and code positions are
/// shuffled independently.
/// </summary>
public class ConnectionStage : MonoBehaviour
{
    [SerializeField] private OutletHead[] outletHeads;
    [SerializeField] private Plug[] plugs;

    private void Awake()
    {
        FindPiecesIfNeeded();
        ShufflePositions(outletHeads);
        ShufflePositions(plugs);
        ResetOutletOrigins();
        AssignMatchingShapes();
    }

    private void FindPiecesIfNeeded()
    {
        if (outletHeads == null || outletHeads.Length == 0)
            outletHeads = GetComponentsInChildren<OutletHead>(true);

        if (plugs == null || plugs.Length == 0)
            plugs = GetComponentsInChildren<Plug>(true);
    }

    private void AssignMatchingShapes()
    {
        if (outletHeads == null || plugs == null || outletHeads.Length != 3 || plugs.Length != 3)
        {
            Debug.LogError("ConnectionStage requires exactly three wires and three codes.", this);
            return;
        }

        var shapes = new List<ConnectionShape>
        {
            ConnectionShape.Circle,
            ConnectionShape.Triangle,
            ConnectionShape.Square
        };
        Shuffle(shapes);

        var plugByFrequency = new Dictionary<int, Plug>();
        foreach (var plug in plugs)
        {
            if (plug == null || !plugByFrequency.TryAdd(plug.Frequency, plug))
            {
                Debug.LogError("Each connection code must have a unique frequency.", this);
                return;
            }
        }

        var usedFrequencies = new HashSet<int>();
        for (var i = 0; i < outletHeads.Length; i++)
        {
            var outlet = outletHeads[i];
            if (outlet == null || !usedFrequencies.Add(outlet.Frequency) ||
                !plugByFrequency.TryGetValue(outlet.Frequency, out var plug))
            {
                Debug.LogError("Every wire must have exactly one matching code frequency.", this);
                return;
            }

            outlet.SetShape(shapes[i]);
            plug.SetShape(shapes[i]);
        }
    }

    private void ResetOutletOrigins()
    {
        if (outletHeads == null)
            return;

        foreach (var outlet in outletHeads)
        {
            if (outlet != null)
                outlet.ResetOrigin();
        }
    }

    private static void ShufflePositions<T>(IReadOnlyList<T> pieces) where T : Component
    {
        if (pieces == null)
            return;

        var positions = new List<Vector3>(pieces.Count);
        foreach (var piece in pieces)
            positions.Add(piece.transform.localPosition);

        Shuffle(positions);

        for (var i = 0; i < pieces.Count; i++)
            pieces[i].transform.localPosition = positions[i];
    }

    private static void Shuffle<T>(IList<T> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var randomIndex = Random.Range(0, i + 1);
            (items[i], items[randomIndex]) = (items[randomIndex], items[i]);
        }
    }
}

public enum ConnectionShape
{
    Circle,
    Triangle,
    Square
}

/// <summary>Creates the simple white matching symbols without requiring art assets.</summary>
public static class ConnectionShapeSprite
{
    private const int TextureSize = 128;
    private static readonly Dictionary<ConnectionShape, Sprite> Sprites = new();

    public static void Apply(GameObject target, ConnectionShape shape)
    {
        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.sprite = Get(shape);
    }

    private static Sprite Get(ConnectionShape shape)
    {
        if (Sprites.TryGetValue(shape, out var sprite))
            return sprite;

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = $"Connection{shape}"
        };

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                var point = new Vector2(
                    (x + 0.5f) / TextureSize * 2f - 1f,
                    (y + 0.5f) / TextureSize * 2f - 1f);
                texture.SetPixel(x, y, IsInside(shape, point) ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), 128f);
        Sprites.Add(shape, sprite);
        return sprite;
    }

    private static bool IsInside(ConnectionShape shape, Vector2 point)
    {
        return shape switch
        {
            ConnectionShape.Circle => point.sqrMagnitude <= 0.68f * 0.68f,
            ConnectionShape.Square => Mathf.Abs(point.x) <= 0.58f && Mathf.Abs(point.y) <= 0.58f,
            ConnectionShape.Triangle => point.y >= -0.58f && point.y <= 0.62f &&
                                        Mathf.Abs(point.x) <= (0.62f - point.y) * 0.96f,
            _ => false
        };
    }
}
