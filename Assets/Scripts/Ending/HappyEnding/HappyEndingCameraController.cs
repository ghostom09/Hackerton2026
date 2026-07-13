using System.Collections;
using UnityEngine;

public class HappyEndingCameraController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private Coroutine _shakeRoutine;
    private Vector3 _restPosition;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        _restPosition = transform.localPosition;
    }

    public void Shake(float duration, float strength)
    {
        if (_shakeRoutine != null)
        {
            StopCoroutine(_shakeRoutine);
            transform.localPosition = _restPosition;
        }

        _restPosition = transform.localPosition;
        _shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    public IEnumerator ZoomTo(Transform target, float targetOrthographicSize, float duration)
    {
        if (targetCamera == null || target == null)
            yield break;

        if (_shakeRoutine != null)
        {
            StopCoroutine(_shakeRoutine);
            transform.localPosition = _restPosition;
            _shakeRoutine = null;
        }

        var startPosition = transform.position;
        var destination = new Vector3(target.position.x, target.position.y, startPosition.z);
        var startSize = targetCamera.orthographicSize;
        duration = Mathf.Max(duration, 0.01f);

        for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, destination, progress);
            targetCamera.orthographicSize = Mathf.Lerp(startSize, targetOrthographicSize, progress);
            yield return null;
        }

        transform.position = destination;
        targetCamera.orthographicSize = targetOrthographicSize;
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        duration = Mathf.Max(duration, 0.01f);

        for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            var falloff = 1f - elapsed / duration;
            var offset = Random.insideUnitCircle * (strength * falloff);
            transform.localPosition = _restPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        transform.localPosition = _restPosition;
        _shakeRoutine = null;
    }
}
