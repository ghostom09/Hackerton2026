using UnityEngine;

/// <summary>
/// Runtime helpers only. Visual objects belong in prefabs / scene, not code.
/// </summary>
public static class MiniGameVisuals
{
    public static Camera FindCamera(Camera current = null)
    {
        if (current != null)
            return current;
        if (Camera.main != null)
            return Camera.main;
        return Object.FindAnyObjectByType<Camera>();
    }

    public static Vector3 ScreenToWorld(Camera camera, Vector2 screenPosition, float z)
    {
        if (camera == null)
            return Vector3.zero;

        var depth = Mathf.Abs(z - camera.transform.position.z);
        var world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        world.z = z;
        return world;
    }

    public static Vector2 ReadWasd()
    {
        var input = Vector2.zero;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null)
            return input;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;

        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    public static bool ContainsPoint(Transform root, Vector2 halfSize, Vector3 world)
    {
        if (root == null)
            return false;
        var local = (Vector2)world - (Vector2)root.position;
        return Mathf.Abs(local.x) <= halfSize.x && Mathf.Abs(local.y) <= halfSize.y;
    }

    public static Vector2 HalfFromScale(Transform t)
    {
        if (t == null)
            return Vector2.one * 0.5f;
        return new Vector2(Mathf.Abs(t.lossyScale.x) * 0.5f, Mathf.Abs(t.lossyScale.y) * 0.5f);
    }

    public static SpriteRenderer FindSprite(Component c)
    {
        if (c == null)
            return null;
        return c.GetComponentInChildren<SpriteRenderer>(true);
    }

    public static T[] FindTargets<T>(Transform root) where T : Component
    {
        if (root == null)
            return System.Array.Empty<T>();
        return root.GetComponentsInChildren<T>(true);
    }
}

public static class MiniGameClear
{
    public static void RequestNext()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RequestNextMap();
    }
}
