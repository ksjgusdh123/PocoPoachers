using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    [SerializeField] private float _detectRange = 10f;
    [SerializeField] private LayerMask _targetLayer;

    public GameObject DetectedTarget { get; private set; }
    public bool IsDetected => DetectedTarget != null;

    private void Awake()
    {
    }

    private void Update()
    {
        TryDetect();
    }
    public bool TryDetect()
    {
        var colliders = Physics.OverlapSphere(transform.position, _detectRange, _targetLayer);

        if (colliders.Length == 0)
        {
            DetectedTarget = null;
            return false;
        }

        DetectedTarget = colliders[0].gameObject;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
    }
}
