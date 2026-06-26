using System;
using UnityEngine;

public class BulletDecal : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private Vector3 _baseScale;
    private Color _cachedBaseColor = Color.white;
    private Color _cachedEmissionColor = Color.black;
    private Color _baseColor = Color.white;
    private Color _emissionColor = Color.black;
    private float _lifeTimer;
    private float _lifetime;
    private float _popDuration;
    private float _popScale;
    private float _fadeDuration;
    private Action _onRelease;

    public bool IsSpawned { get; private set; }

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        CacheBaseColor();
    }

    public void Place(
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        float size,
        float lifetime,
        float popDuration,
        float popScale,
        float fadeDuration,
        Action onRelease,
        Color color = default)
    {
        transform.SetParent(parent, true);
        transform.SetPositionAndRotation(position, rotation);

        Vector3 ls = parent != null ? parent.lossyScale : Vector3.one;
        _baseScale = new Vector3(size / ls.x, size / ls.y, size / ls.z);
        _lifeTimer = lifetime;
        _lifetime = lifetime;
        _popDuration = popDuration;
        _popScale = popScale;
        _fadeDuration = fadeDuration;
        _onRelease = onRelease;
        IsSpawned = true;

        if (color != default)
        {
            _baseColor = new Color(color.r, color.g, color.b, _cachedBaseColor.a);
            float intensity = Mathf.Max(_cachedEmissionColor.r, _cachedEmissionColor.g, _cachedEmissionColor.b, 1f);
            _emissionColor = new Color(color.r * intensity, color.g * intensity, color.b * intensity, _cachedEmissionColor.a);
        }
        else
        {
            _baseColor = _cachedBaseColor;
            _emissionColor = _cachedEmissionColor;
        }

        ApplyScale();
        ApplyAlpha(1f);
    }

    private void Update()
    {
        if (!IsSpawned) return;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
        {
            Release();
            return;
        }

        ApplyScale();
        ApplyAlpha(GetAlpha());
    }

    private void ApplyScale()
    {
        float elapsed = _lifetime - _lifeTimer;
        float scaleMultiplier = 1f;

        if (_popDuration > 0f && elapsed < _popDuration)
        {
            float t = elapsed / _popDuration;
            scaleMultiplier = Mathf.Lerp(_popScale, 1f, 1f - Mathf.Pow(1f - t, 2f));
        }

        transform.localScale = _baseScale * scaleMultiplier;
    }

    private float GetAlpha()
    {
        if (_fadeDuration <= 0f) return 1f;
        if (_lifeTimer >= _fadeDuration) return 1f;
        return Mathf.Clamp01(_lifeTimer / _fadeDuration);
    }

    private void ApplyAlpha(float alpha)
    {
        if (_renderer == null) return;

        Color color = _baseColor;
        color.a *= alpha;
        Color emission = _emissionColor * alpha;

        _renderer.GetPropertyBlock(_propertyBlock);
        if (_renderer.sharedMaterial != null)
        {
            if (_renderer.sharedMaterial.HasProperty(BaseColorId))
                _propertyBlock.SetColor(BaseColorId, color);
            if (_renderer.sharedMaterial.HasProperty(ColorId))
                _propertyBlock.SetColor(ColorId, color);
            if (_renderer.sharedMaterial.HasProperty(EmissionColorId))
                _propertyBlock.SetColor(EmissionColorId, emission);
        }
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    private void CacheBaseColor()
    {
        if (_renderer == null || _renderer.sharedMaterial == null) return;

        Material material = _renderer.sharedMaterial;
        if (material.HasProperty(BaseColorId))
            _cachedBaseColor = material.GetColor(BaseColorId);
        else if (material.HasProperty(ColorId))
            _cachedBaseColor = material.GetColor(ColorId);

        if (material.HasProperty(EmissionColorId))
            _cachedEmissionColor = material.GetColor(EmissionColorId);

        _baseColor = _cachedBaseColor;
        _emissionColor = _cachedEmissionColor;
    }

    public void Release()
    {
        if (!IsSpawned) return;

        IsSpawned = false;
        ApplyAlpha(1f);
        transform.SetParent(null, true);
        _onRelease?.Invoke();
        _onRelease = null;
    }
}
