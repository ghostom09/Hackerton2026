using UnityEngine;

public class MainMenuBackgroundEffects : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform fallingGirl;
    [SerializeField] private GameObject[] hideWhenPaused;
    [SerializeField] private GameObject[] pauseWhenActive;

    [Header("Camera")]
    [SerializeField] private float idleZoomAmount = 0.28f;
    [SerializeField] private float idleZoomSpeed = 0.12f;
    [SerializeField] private float startPunchAmount = 1.25f;
    [SerializeField] private float startPunchDuration = 0.45f;
    [SerializeField] private bool zoomCamera = false;

    [Header("Particles")]
    [SerializeField] private bool createFallingParticles = true;
    [SerializeField] private float particleRate = 32f;
    [SerializeField] private float particleYOffset = 6.8f;
    [SerializeField] private float particleWidth = 16f;

    [Header("Light Beam")]
    [SerializeField] private bool createLightBeam = true;
    [SerializeField] private float beamYOffset = 5.2f;
    [SerializeField] private float beamWidth = 7f;
    [SerializeField] private float beamHeight = 14f;

    private ParticleSystem _particles;
    private GameObject _particleObject;
    private SpriteRenderer _beamRenderer;
    private GameObject _beamObject;
    private Vector3 _fallingGirlBaseScale;
    private bool _fallingGirlInitialActiveSelf;
    private bool[] _hideTargetInitialActiveSelf;
    private float _baseOrthographicSize;
    private float _time;
    private bool _paused;

    private void Awake()
    {
        ResolveReferences();

        if (targetCamera != null)
            _baseOrthographicSize = targetCamera.orthographicSize;

        if (fallingGirl != null)
        {
            _fallingGirlBaseScale = fallingGirl.localScale;
            _fallingGirlInitialActiveSelf = fallingGirl.gameObject.activeSelf;
        }

        CacheHideTargetState();

        if (createFallingParticles)
            CreateFallingParticles();

        if (createLightBeam)
            CreateLightBeam();
    }

    private void OnEnable()
    {
        _time = 0f;
    }

    private void Update()
    {
        var shouldPause = ShouldPauseForUI();
        SetPaused(shouldPause);

        if (_paused || targetCamera == null)
            return;

        _time += Time.deltaTime;
        UpdateBackgroundZoom();
        UpdateEffectPositions();
    }

    public void SetPaused(bool paused)
    {
        if (_paused == paused)
            return;

        _paused = paused;

        if (targetCamera != null)
            targetCamera.orthographicSize = _baseOrthographicSize;

        if (fallingGirl != null)
        {
            fallingGirl.localScale = _fallingGirlBaseScale;
            fallingGirl.gameObject.SetActive(!_paused && _fallingGirlInitialActiveSelf);
        }

        SetHideTargetsVisible(!_paused);
        SetEffectsVisible(!_paused);

        if (_particles != null)
        {
            if (_paused) _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else _particles.Play(true);
        }
    }

    public void SetPauseTargets(params GameObject[] targets)
    {
        pauseWhenActive = targets;
    }

    public void SetHideTargets(params GameObject[] targets)
    {
        hideWhenPaused = targets ?? System.Array.Empty<GameObject>();
        CacheHideTargetState();
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (fallingGirl == null)
            fallingGirl = FindTransformByNamePart("falling_girl");

        if (hideWhenPaused == null || hideWhenPaused.Length == 0)
        {
            hideWhenPaused = FindGameObjectsByNameParts("Title", "Logo", "ChatGPT Image");
        }
    }

    private bool ShouldPauseForUI()
    {
        if (pauseWhenActive == null)
            return false;

        foreach (var target in pauseWhenActive)
        {
            if (target != null && target.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void UpdateBackgroundZoom()
    {
        var punch = GetStartPunchOffset();
        var idle = (1f - Mathf.Cos(_time * Mathf.PI * 2f * idleZoomSpeed)) * 0.5f * idleZoomAmount;
        var zoomOffset = punch + idle;

        if (zoomCamera)
        {
            targetCamera.orthographicSize = Mathf.Max(0.1f, _baseOrthographicSize + zoomOffset);
            return;
        }

        if (fallingGirl != null)
            fallingGirl.localScale = _fallingGirlBaseScale * Mathf.Max(0.1f, 1f - (zoomOffset * 0.08f));
    }

    private float GetStartPunchOffset()
    {
        if (_time >= startPunchDuration)
            return 0f;

        var t = Mathf.Clamp01(_time / Mathf.Max(0.01f, startPunchDuration));
        return -Mathf.Sin((1f - t) * Mathf.PI * 0.5f) * startPunchAmount;
    }

    private void UpdateEffectPositions()
    {
        var center = targetCamera != null ? targetCamera.transform.position : (fallingGirl != null ? fallingGirl.position : transform.position);
        center.z = fallingGirl != null ? fallingGirl.position.z : 0f;

        if (_particles != null)
            _particles.transform.position = center + Vector3.up * particleYOffset;

        if (_beamRenderer != null)
        {
            var pos = center + Vector3.up * beamYOffset;
            pos.z = center.z + 0.15f;
            _beamRenderer.transform.position = pos;
        }
    }

    private void CreateFallingParticles()
    {
        _particleObject = new GameObject("Falling Girl Particles");
        _particleObject.transform.SetParent(transform, false);
        _particleObject.transform.position = transform.position + Vector3.up * particleYOffset;
        _particles = _particleObject.AddComponent<ParticleSystem>();

        var main = _particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.2f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.35f),
            new Color(1f, 1f, 1f, 0.85f));
        main.gravityModifier = 0.12f;

        var emission = _particles.emission;
        emission.rateOverTime = particleRate;

        var shape = _particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(particleWidth, 0.2f, 0.1f);

        var velocity = _particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(-0.35f, -1.15f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var renderer = _particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 4;
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null)
            renderer.material = new Material(shader);
    }

    private void CreateLightBeam()
    {
        _beamObject = new GameObject("Falling Girl Light Beam");
        _beamObject.transform.SetParent(transform, false);
        _beamObject.transform.localScale = new Vector3(beamWidth, beamHeight, 1f);

        _beamRenderer = _beamObject.AddComponent<SpriteRenderer>();
        _beamRenderer.sprite = CreateBeamSprite();
        _beamRenderer.color = new Color(0.78f, 0.86f, 1f, 0.46f);
        _beamRenderer.sortingOrder = 1;

        UpdateEffectPositions();
    }

    private void SetEffectsVisible(bool visible)
    {
        if (_particleObject != null)
            _particleObject.SetActive(visible);

        if (_beamObject != null)
            _beamObject.SetActive(visible);
    }

    private void CacheHideTargetState()
    {
        if (hideWhenPaused == null)
        {
            _hideTargetInitialActiveSelf = System.Array.Empty<bool>();
            return;
        }

        _hideTargetInitialActiveSelf = new bool[hideWhenPaused.Length];
        for (var i = 0; i < hideWhenPaused.Length; i++)
            _hideTargetInitialActiveSelf[i] = hideWhenPaused[i] != null && hideWhenPaused[i].activeSelf;
    }

    private void SetHideTargetsVisible(bool visible)
    {
        if (hideWhenPaused == null)
            return;

        for (var i = 0; i < hideWhenPaused.Length; i++)
        {
            var target = hideWhenPaused[i];
            if (target == null)
                continue;

            var initialActive = _hideTargetInitialActiveSelf == null ||
                                i >= _hideTargetInitialActiveSelf.Length ||
                                _hideTargetInitialActiveSelf[i];
            target.SetActive(visible && initialActive);
        }
    }

    private static Sprite CreateBeamSprite()
    {
        const int width = 32;
        const int height = 128;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (var y = 0; y < height; y++)
        {
            var vertical = y / (height - 1f);
            for (var x = 0; x < width; x++)
            {
                var horizontal = Mathf.Abs((x / (width - 1f)) - 0.5f) * 2f;
                var topSpread = Mathf.Lerp(0.58f, 1f, vertical);
                var centerFade = Mathf.Pow(1f - Mathf.Clamp01(horizontal * topSpread), 2.2f);
                var verticalFade = Mathf.SmoothStep(0f, 0.35f, vertical) * Mathf.Lerp(0.55f, 1f, vertical);
                var core = Mathf.Pow(1f - Mathf.Clamp01(horizontal * 1.9f), 2f) * 0.35f;
                var alpha = Mathf.Clamp01((centerFade + core) * verticalFade);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 1f), height);
    }

    private static Transform FindTransformByNamePart(string namePart)
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in transforms)
        {
            if (item.name.ToLowerInvariant().Contains(namePart))
                return item;
        }

        return null;
    }

    private static Transform FindFirstTransformByNameParts(params string[] nameParts)
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var namePart in nameParts)
        {
            foreach (var item in transforms)
            {
                if (item.name.ToLowerInvariant().Contains(namePart.ToLowerInvariant()))
                    return item;
            }
        }

        return null;
    }

    private static GameObject[] FindGameObjectsByNameParts(params string[] nameParts)
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var results = new System.Collections.Generic.List<GameObject>();

        foreach (var namePart in nameParts)
        {
            var loweredPart = namePart.ToLowerInvariant();
            foreach (var item in transforms)
            {
                if (!item.name.ToLowerInvariant().Contains(loweredPart))
                    continue;

                var gameObject = item.gameObject;
                if (!results.Contains(gameObject))
                    results.Add(gameObject);
            }
        }

        return results.ToArray();
    }
}
