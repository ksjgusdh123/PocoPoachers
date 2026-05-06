using Unity.Behavior;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    [SerializeField] private float _detectRange = 10f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private string _blackboardTarget = "Target";

    private BehaviorGraphAgent _behaviorAgent;
    private GameObject _currentTarget;


    private void Awake()
    {
        _behaviorAgent = GetComponent<BehaviorGraphAgent>();
    }

    private void Update()
    {
    }

    public bool TryDetect()
    {
        var colliders = Physics.OverlapSphere(transform.position, _detectRange, _targetLayer);

        if (colliders.Length == 0)
        {
            if (_currentTarget != null)
            {
                _behaviorAgent.BlackboardReference.SetVariableValue(_blackboardTarget, (GameObject)null);
                _currentTarget = null;
                Debug.Log("«ÿ¡¶");
            }   
            return false;
        }
        _currentTarget = colliders[0].gameObject;
        _behaviorAgent.BlackboardReference.SetVariableValue(_blackboardTarget, _currentTarget);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _currentTarget != null ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
    }
}
