using UnityEngine;

public class SoundDetector : MonoBehaviour
{
    private TargetDetector _targetDetector;

    private void Awake()
    {
        _targetDetector = GetComponent<TargetDetector>();
    }

    private void OnEnable()
    {
        SoundEvent.OnSoundEmitted += OnSoundHeard;
    }

    private void OnDisable()
    {
        SoundEvent.OnSoundEmitted -= OnSoundHeard;
    }

    private void OnSoundHeard(Vector3 soundPosition, float soundRange, GameObject source)
    {
        if (source == gameObject) return;

        float distance = Vector3.Distance(transform.position, soundPosition);
        if (distance > soundRange) return;

        _targetDetector?.ForceSetTarget(source);
    }
}
