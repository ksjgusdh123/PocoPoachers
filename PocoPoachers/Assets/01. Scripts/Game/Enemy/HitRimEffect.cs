using System.Collections;
using UnityEngine;

public class HitRimEffect : MonoBehaviour
{
    [SerializeField, ColorUsage(true, true)] Color _rimColor    = new Color(2f, 2f, 2f, 1f);
    [SerializeField, Range(1f, 10f)]         float _rimPower    = 3f;
    [SerializeField, Range(0.05f, 1f)]       float _duration    = 0.2f;

    static readonly int RimColorID    = Shader.PropertyToID("_RimColor");
    static readonly int RimPowerID    = Shader.PropertyToID("_RimPower");
    static readonly int HitIntensityID = Shader.PropertyToID("_HitIntensity");

    MaterialPropertyBlock _mpb;
    SkinnedMeshRenderer[] _smrs;
    Material              _rimMat;
    Coroutine             _flashCoroutine;

    void Awake()
    {
        _mpb  = new MaterialPropertyBlock();
        _smrs = GetComponentsInChildren<SkinnedMeshRenderer>();

        _rimMat = new Material(Shader.Find("Custom/HitRimFlash"));
        _rimMat.SetColor(RimColorID,  _rimColor);
        _rimMat.SetFloat(RimPowerID,  _rimPower);

        foreach (var smr in _smrs)
        {
            var mats    = smr.sharedMaterials;
            var newMats = new Material[mats.Length + 1];
            mats.CopyTo(newMats, 0);
            newMats[^1]    = _rimMat;
            smr.materials  = newMats;
        }

        var stat = GetComponent<StatBase>();
        if (stat != null)
            stat.OnDamaged += OnDamaged;
    }

    void OnDestroy()
    {
        var stat = GetComponent<StatBase>();
        if (stat != null)
            stat.OnDamaged -= OnDamaged;

        if (_rimMat != null)
            Destroy(_rimMat);
    }

    void OnDamaged(float damage, Vector3 pos, GameObject attacker)
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            float t = 1f - (elapsed / _duration);
            SetIntensity(t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetIntensity(0f);
        _flashCoroutine = null;
    }

    void SetIntensity(float intensity)
    {
        _mpb.SetFloat(HitIntensityID, intensity);
        foreach (var smr in _smrs)
            smr.SetPropertyBlock(_mpb);
    }
}
