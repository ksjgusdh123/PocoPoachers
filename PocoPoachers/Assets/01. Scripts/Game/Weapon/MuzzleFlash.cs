using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class MuzzleFlash : MonoBehaviour
{
    private VisualEffect _visualEffect;

    private void Awake()
    {
        _visualEffect = GetComponent<VisualEffect>();
    }

    public void Play()
    {
        if (_visualEffect == null) return;

        _visualEffect.Stop();
        _visualEffect.Reinit();
        _visualEffect.SendEvent("OnPlay");
    }
}
