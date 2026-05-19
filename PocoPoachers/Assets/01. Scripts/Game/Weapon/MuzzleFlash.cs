using UnityEngine;
using UnityEngine.VFX;

public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private VisualEffect _visualEffect;

    public static MuzzleFlash Create(Transform parent, VisualEffectAsset asset)
    {
        GameObject muzzleFlashObject = new GameObject("MuzzleFlash");
        Transform muzzleFlashTransform = muzzleFlashObject.transform;
        muzzleFlashTransform.SetParent(parent, false);
        muzzleFlashTransform.localPosition = Vector3.zero;
        muzzleFlashTransform.localRotation = Quaternion.identity;
        muzzleFlashTransform.localScale = Vector3.one;

        VisualEffect visualEffect = muzzleFlashObject.AddComponent<VisualEffect>();
        visualEffect.visualEffectAsset = asset;
        visualEffect.initialEventName = string.Empty;
        visualEffect.Stop();

        MuzzleFlash muzzleFlash = muzzleFlashObject.AddComponent<MuzzleFlash>();
        muzzleFlash._visualEffect = visualEffect;
        return muzzleFlash;
    }

    private void Awake()
    {
        if (_visualEffect == null)
            _visualEffect = GetComponent<VisualEffect>();
    }

    public void Play()
    {
        if (_visualEffect == null) return;

        _visualEffect.Stop();
        _visualEffect.Play();
    }
}
