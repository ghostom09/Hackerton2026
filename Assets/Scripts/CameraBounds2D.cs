using UnityEngine;

public static class CameraBounds2D
{
    public static Vector2 ClampPosition(Camera targetCamera, Vector3 worldPosition, Vector2 objectExtents, float padding = 0.02f)
    {
        if (targetCamera == null)
            return worldPosition;

        var cameraDepth = Vector3.Dot(worldPosition - targetCamera.transform.position, targetCamera.transform.forward);
        var bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, cameraDepth));
        var topRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, cameraDepth));

        var minX = bottomLeft.x + objectExtents.x + padding;
        var maxX = topRight.x - objectExtents.x - padding;
        var minY = bottomLeft.y + objectExtents.y + padding;
        var maxY = topRight.y - objectExtents.y - padding;

        if (minX > maxX)
            minX = maxX = (bottomLeft.x + topRight.x) * 0.5f;

        if (minY > maxY)
            minY = maxY = (bottomLeft.y + topRight.y) * 0.5f;

        return new Vector2(
            Mathf.Clamp(worldPosition.x, minX, maxX),
            Mathf.Clamp(worldPosition.y, minY, maxY));
    }
}
