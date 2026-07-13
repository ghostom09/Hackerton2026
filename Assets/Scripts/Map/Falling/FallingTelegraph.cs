using UnityEngine;

[DisallowMultipleComponent]
public class FallingTelegraph : MonoBehaviour
{
    [SerializeField] private float pulseScale = 0.2f;

    private float duration = 1f;
    private float elapsed;
    private Vector3 baseScale;
    private Renderer targetRenderer;

    public void Init(float newDuration)
    {
        duration = Mathf.Max(0.05f, newDuration);
        elapsed = 0f;
    }

    private void Awake()
    {
        baseScale = transform.localScale;
        targetRenderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float ratio = Mathf.Clamp01(elapsed / duration);
        float pulse = 1f + Mathf.Sin(Time.time * 20f) * pulseScale * (1f - ratio);
        transform.localScale = baseScale * pulse;

        if (targetRenderer != null)
        {
            Color color = targetRenderer.material.color;
            color.a = Mathf.Lerp(0.35f, 0.9f, ratio);
            targetRenderer.material.color = color;
        }
    }
}
