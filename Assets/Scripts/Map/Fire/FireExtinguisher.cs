using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FireExtinguisher : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float holdExtinguishPowerPerSecond = 4f;

    private void Awake()
    {
        FindTargetCamera();
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.isPressed)
        {
            TryExtinguish(screenPosition, holdExtinguishPowerPerSecond * Time.deltaTime);
        }
    }

    private void OnValidate()
    {
        holdExtinguishPowerPerSecond = Mathf.Max(0.01f, holdExtinguishPowerPerSecond);
    }

    public bool TryExtinguish(Vector2 screenPosition, float amount)
    {
        FireHealth fireHealth = FindFire(screenPosition);
        if (fireHealth == null)
        {
            return false;
        }

        fireHealth.TakeExtinguishDamage(amount);
        return true;
    }

    private FireHealth FindFire(Vector2 screenPosition)
    {
        if (targetCamera == null)
        {
            FindTargetCamera();
        }

        Camera cameraToUse = targetCamera;
        if (cameraToUse == null)
        {
            return null;
        }

        FireHealth fire2D = Find2DFire(cameraToUse, screenPosition);
        if (fire2D != null)
        {
            return fire2D;
        }

        return Find3DFire(cameraToUse, screenPosition);
    }

    private void FindTargetCamera()
    {
        if (targetCamera != null)
        {
            return;
        }

        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindAnyObjectByType<Camera>();
        }
    }

    private FireHealth Find2DFire(Camera cameraToUse, Vector2 screenPosition)
    {
        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, cameraToUse.farClipPlane);

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            FireHealth fireHealth = hitCollider.GetComponentInParent<FireHealth>();
            if (fireHealth != null)
            {
                return fireHealth;
            }
        }

        return null;
    }

    private FireHealth Find3DFire(Camera cameraToUse, Vector2 screenPosition)
    {
        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, cameraToUse.farClipPlane, ~0, QueryTriggerInteraction.Collide);

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            FireHealth fireHealth = hits[i].collider.GetComponentInParent<FireHealth>();
            if (fireHealth != null)
            {
                return fireHealth;
            }
        }

        return null;
    }
}
