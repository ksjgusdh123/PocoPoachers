using UnityEngine;
using UnityEngine.AI;

public class AIRotator : MonoBehaviour
{
    public float RotationSpeed = 10f;

    private Transform _target;
    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (_target == null) return;

        Vector3 dir = _target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            RotationSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        _agent.updateRotation = false;
    }

    public void ClearTarget()
    {
        _target = null;
        _agent.updateRotation = true;
    }
}
