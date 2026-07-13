using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ElevatorPickStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MiniGameTarget[] elevators;

    private bool _complete;
    private int _safeIndex = -1;

    private void Awake()
    {
        targetCamera = MiniGameVisuals.FindCamera(targetCamera);
        if (elevators == null || elevators.Length == 0)
        {
            var list = new System.Collections.Generic.List<MiniGameTarget>();
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Elevator"))
                    continue;
                var target = child.GetComponent<MiniGameTarget>();
                if (target != null)
                    list.Add(target);
            }
            elevators = list.ToArray();
        }
        PickSafeElevator();
    }

    private void PickSafeElevator()
    {
        if (elevators == null || elevators.Length == 0)
            return;

        for (var i = 0; i < elevators.Length; i++)
        {
            if (elevators[i] != null && elevators[i].IsSafe)
            {
                _safeIndex = i;
                return;
            }
        }

        _safeIndex = Random.Range(0, elevators.Length);
        for (var i = 0; i < elevators.Length; i++)
        {
            if (elevators[i] != null)
                elevators[i].IsSafe = i == _safeIndex;
        }
    }

    private void Update()
    {
        if (_complete || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;
        if (elevators == null)
            return;

        var world = MiniGameVisuals.ScreenToWorld(targetCamera, Mouse.current.position.ReadValue(), 0f);
        for (var i = 0; i < elevators.Length; i++)
        {
            if (elevators[i] == null || !elevators[i].Contains(world))
                continue;

            if (i == _safeIndex)
            {
                var renderer = MiniGameVisuals.FindSprite(elevators[i]);
                if (renderer != null)
                    renderer.color = new Color(0.3f, 0.9f, 0.45f);
                Complete();
            }
            else
            {
                elevators[i].transform.localPosition += new Vector3(Random.Range(-0.12f, 0.12f), 0f, 0f);
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
