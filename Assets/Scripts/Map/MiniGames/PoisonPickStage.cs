using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PoisonPickStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MiniGameTarget[] bottles;

    private bool _complete;
    private bool _initialized;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
        if (bottles == null || bottles.Length == 0)
        {
            var list = new System.Collections.Generic.List<MiniGameTarget>();
            foreach (Transform child in transform)
            {
                if (!(child.name.Contains("Bottle")))
                    continue;
                var target = child.GetComponent<MiniGameTarget>();
                if (target != null)
                    list.Add(target);
            }
            bottles = list.ToArray();
        }
        RandomizeSafeBottlePosition();
    }

    private void RandomizeSafeBottlePosition()
    {
        if (_initialized || bottles == null || bottles.Length == 0)
            return;

        var safeIndex = -1;
        for (var i = 0; i < bottles.Length; i++)
        {
            if (bottles[i] != null && bottles[i].IsSafe)
            {
                safeIndex = i;
                break;
            }
        }

        // Keep the authored safe-bottle visuals and move that whole bottle to a
        // random slot so its appearance and answer state never get out of sync.
        if (safeIndex < 0)
        {
            for (var i = 0; i < bottles.Length; i++)
            {
                if (bottles[i] != null)
                {
                    safeIndex = i;
                    break;
                }
            }
        }

        if (safeIndex >= 0)
        {
            for (var i = 0; i < bottles.Length; i++)
            {
                if (bottles[i] != null)
                    bottles[i].IsSafe = i == safeIndex;
            }

            var destinationIndex = RandomBottleIndex(safeIndex);
            if (destinationIndex >= 0 && destinationIndex != safeIndex)
            {
                var safePosition = bottles[safeIndex].transform.localPosition;
                bottles[safeIndex].transform.localPosition = bottles[destinationIndex].transform.localPosition;
                bottles[destinationIndex].transform.localPosition = safePosition;
            }
        }

        _initialized = true;
    }

    private int RandomBottleIndex(int excludedIndex)
    {
        var candidates = new System.Collections.Generic.List<int>();
        for (var i = 0; i < bottles.Length; i++)
        {
            if (bottles[i] != null && i != excludedIndex)
                candidates.Add(i);
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : excludedIndex;
    }

    private void Update()
    {
        if (_complete || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        var world = MiniGameVisuals.ScreenToWorld(targetCamera, Mouse.current.position.ReadValue(), 0f);
        if (bottles == null)
            return;

        for (var i = 0; i < bottles.Length; i++)
        {
            var bottle = bottles[i];
            if (bottle == null || !bottle.Contains(world))
                continue;

            if (bottle.IsSafe)
            {
                bottle.transform.localScale *= 1.1f;
                Complete();
            }
            else
            {
                var renderer = MiniGameVisuals.FindSprite(bottle);
                if (renderer != null)
                    renderer.color = new Color(0.3f, 0.8f, 0.2f);
                bottle.transform.localPosition += new Vector3(Random.Range(-0.08f, 0.08f), 0f, 0f);
            }
            return;
        }
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
