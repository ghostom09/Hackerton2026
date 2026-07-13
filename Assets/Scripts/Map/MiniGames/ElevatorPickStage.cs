using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[DisallowMultipleComponent]
public class ElevatorPickStage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MiniGameTarget[] elevators;
    [SerializeField] private TextMeshProUGUI[] doorLabels;

    private bool _complete;
    private int _safeIndex = -1;

    private static readonly Color UnsafeDoorColor = new(0.55f, 0.25f, 0.25f);
    private static readonly Color UnsafeLightColor = new(0.95f, 0.25f, 0.2f);

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

        if (doorLabels == null || doorLabels.Length == 0)
        {
            var labelCanvas = transform.Find("DoorLabelsCanvas");
            doorLabels = labelCanvas != null
                ? labelCanvas.GetComponentsInChildren<TextMeshProUGUI>(true)
                : System.Array.Empty<TextMeshProUGUI>();
        }

        PickSafeElevator();
    }

    private void PickSafeElevator()
    {
        if (elevators == null || elevators.Length == 0)
            return;

        var elevatorCount = 0;
        for (var i = 0; i < elevators.Length; i++)
        {
            if (elevators[i] != null)
                elevatorCount++;
        }

        if (elevatorCount == 0)
            return;

        var safeElevator = Random.Range(0, elevatorCount);
        for (var i = 0; i < elevators.Length; i++)
        {
            var elevator = elevators[i];
            if (elevator == null)
                continue;

            var isSafe = safeElevator-- == 0;
            elevator.IsSafe = isSafe;
            if (isSafe)
                _safeIndex = i;

            SetElevatorVisual(elevator);
            SetDoorLabel(i, isSafe);
        }
    }

    private static void SetElevatorVisual(MiniGameTarget elevator)
    {
        var doorRenderer = MiniGameVisuals.FindSprite(elevator);
        if (doorRenderer != null)
            doorRenderer.color = UnsafeDoorColor;

        var light = elevator.transform.Find("Light");
        var lightRenderer = light != null ? MiniGameVisuals.FindSprite(light) : null;
        if (lightRenderer != null)
            lightRenderer.color = UnsafeLightColor;

    }

    private void SetDoorLabel(int index, bool isSafe)
    {
        if (index < 0 || doorLabels == null || index >= doorLabels.Length)
            return;

        var label = doorLabels[index];
        if (label == null)
            return;

        label.text = isSafe ? "Exit" : "Trap";
        label.gameObject.SetActive(true);
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
