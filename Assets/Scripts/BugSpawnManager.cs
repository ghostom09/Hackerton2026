using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stage3-KillAllBug의 벌레들을 게임 시작 시 카메라 화면 안의 서로 다른 위치에 배치합니다.
/// </summary>
public class BugSpawnManager : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Range(0f, 0.45f)] private float viewportPadding = 0.12f;
    [SerializeField, Min(0f)] private float minimumDistance = 1.5f;
    [SerializeField, Min(1)] private int attemptsPerBug = 20;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning("BugSpawnManager needs a camera to randomize bug positions.", this);
            return;
        }

        var occupiedPositions = new List<Vector2>();

        // 프리팹 안의 Bug 컴포넌트를 매번 직접 찾습니다.
        // Inspector에 남은 오래된 Transform 참조를 사용하지 않으므로,
        // Bug 프리팹을 교체하거나 재생성해도 MissingReferenceException이 발생하지 않습니다.
        var spawnTargets = GetComponentsInChildren<Bug>(true);

        foreach (var spawnTarget in spawnTargets)
        {
            if (spawnTarget == null)
                continue;

            var position = FindSpawnPosition(occupiedPositions);
            spawnTarget.transform.position = position;
            occupiedPositions.Add(position);
        }
    }

    private Vector2 FindSpawnPosition(List<Vector2> occupiedPositions)
    {
        var fallbackPosition = GetRandomViewportPosition();

        for (var attempt = 0; attempt < attemptsPerBug; attempt++)
        {
            var candidate = GetRandomViewportPosition();
            fallbackPosition = candidate;

            var isFarEnough = true;
            foreach (var occupiedPosition in occupiedPositions)
            {
                if (Vector2.Distance(candidate, occupiedPosition) < minimumDistance)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough)
                return candidate;
        }

        return fallbackPosition;
    }

    private Vector2 GetRandomViewportPosition()
    {
        var viewportPosition = new Vector3(
            Random.Range(viewportPadding, 1f - viewportPadding),
            Random.Range(viewportPadding, 1f - viewportPadding),
            -targetCamera.transform.position.z);

        return targetCamera.ViewportToWorldPoint(viewportPosition);
    }
}
