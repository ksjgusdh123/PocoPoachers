using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private Light _flashLight;
    [SerializeField] private float _flashDuration = 0.05f;
    [SerializeField] private float _intensity = 5f;

    private VisualEffect _visualEffect;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        _visualEffect = GetComponent<VisualEffect>();
        if (_flashLight != null) _flashLight.enabled = false;
    }

    public void Play()
    {
        if (_visualEffect == null) return;

        _visualEffect.Stop();
        _visualEffect.Reinit();
        _visualEffect.SendEvent("OnPlay");

        if (_flashLight == null) return;

        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        _flashLight.intensity = _intensity;
        _flashLight.enabled = true;
        yield return new WaitForSeconds(_flashDuration);
        _flashLight.enabled = false;
    }
}
