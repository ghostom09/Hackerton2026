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
        EnsureOneSafeBottle();
    }

    private void EnsureOneSafeBottle()
    {
        if (_initialized || bottles == null || bottles.Length == 0)
            return;

        var hasSafe = false;
        for (var i = 0; i < bottles.Length; i++)
        {
            if (bottles[i] != null && bottles[i].IsSafe)
                hasSafe = true;
        }

        if (!hasSafe)
        {
            var safe = Random.Range(0, bottles.Length);
            for (var i = 0; i < bottles.Length; i++)
            {
                if (bottles[i] != null)
                    bottles[i].IsSafe = i == safe;
            }
        }

        _initialized = true;
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
