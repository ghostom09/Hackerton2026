using UnityEngine;

[DisallowMultipleComponent]
public class FallingCover : MonoBehaviour
{
    private FallingRoomController owner;
    private Vector3 landingPosition;
    private float fallSpeed;
    private float hitRadius;
    private float lifetimeAfterLanding;
    private bool landed;
    private bool hasHitPlayer;

    public void Init(FallingRoomController newOwner, Vector3 newLandingPosition, float newFallSpeed, float newHitRadius, float newLifetimeAfterLanding)
    {
        owner = newOwner;
        landingPosition = newLandingPosition;
        fallSpeed = Mathf.Max(0.1f, newFallSpeed);
        hitRadius = Mathf.Max(0.05f, newHitRadius);
        lifetimeAfterLanding = Mathf.Max(0f, newLifetimeAfterLanding);
        landed = false;
        hasHitPlayer = false;
    }

    private void Update()
    {
        if (landed)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, landingPosition, fallSpeed * Time.deltaTime);
        CheckPlayerHit();

        if (Vector3.Distance(transform.position, landingPosition) <= 0.01f)
        {
            Land();
        }
    }

    private void CheckPlayerHit()
    {
        if (hasHitPlayer || owner == null || owner.Player == null)
        {
            return;
        }

        if (Vector2.Distance(transform.position, owner.Player.position) > hitRadius)
        {
            return;
        }

        hasHitPlayer = true;
        owner.NotifyPlayerHit();
    }

    private void Land()
    {
        landed = true;
        transform.position = landingPosition;
        CheckPlayerHit();

        if (lifetimeAfterLanding <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject, lifetimeAfterLanding);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
