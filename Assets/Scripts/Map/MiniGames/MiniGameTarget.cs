using UnityEngine;

/// <summary>
/// Marker for clickable / hazard objects placed in stage prefabs.
/// </summary>
[DisallowMultipleComponent]
public class MiniGameTarget : MonoBehaviour
{
    [SerializeField] private bool isSafe;
    [SerializeField] private Vector2 halfSize;

    public bool IsSafe
    {
        get => isSafe;
        set => isSafe = value;
    }

    public Vector2 HalfSize
    {
        get
        {
            if (halfSize.sqrMagnitude > 0.0001f)
                return halfSize;
            return MiniGameVisuals.HalfFromScale(transform);
        }
        set => halfSize = value;
    }

    public bool Contains(Vector3 world) => MiniGameVisuals.ContainsPoint(transform, HalfSize, world);

    private void OnValidate()
    {
        if (halfSize.sqrMagnitude < 0.0001f)
            halfSize = MiniGameVisuals.HalfFromScale(transform);
    }

    private void Reset()
    {
        halfSize = MiniGameVisuals.HalfFromScale(transform);
    }
}
