using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[DisallowMultipleComponent]
public class ElevatorPickStage : MonoBehaviour
{
    private const float WrongElevatorTimePenaltyFraction = 1f / 3f;

    [Header("Refs")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MiniGameTarget[] elevators;
    [SerializeField] private TextMeshProUGUI[] doorLabels;
    [SerializeField] private GameObject checkPrefab;

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

            SetDoorLabel(i, isSafe);
        }
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
                StartCoroutine(ShowCheckAndComplete(elevators[i]));
            }
            else
            {
                elevators[i].transform.localPosition += new Vector3(Random.Range(-0.12f, 0.12f), 0f, 0f);
                GameManager.Instance?.ReduceCurrentMapTimeByRemainingFraction(WrongElevatorTimePenaltyFraction);
            }
            return;
        }
    }

    private System.Collections.IEnumerator ShowCheckAndComplete(MiniGameTarget elevator)
    {
        _complete = true;

        if (checkPrefab != null && elevator != null)
        {
            var check = Instantiate(checkPrefab, elevator.transform.position, Quaternion.identity, transform);
            var checkRenderer = check.GetComponentInChildren<SpriteRenderer>(true);
            if (checkRenderer != null)
                checkRenderer.sortingOrder = 5;
        }

        yield return new WaitForSeconds(0.45f);
        Complete();
    }

    private void Complete()
    {
        _complete = true;
        MiniGameClear.RequestNext();
    }
}
