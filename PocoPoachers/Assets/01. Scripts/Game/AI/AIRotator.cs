using UnityEngine;
using UnityEngine.AI;

public class AIRotator : MonoBehaviour
{
    public float RotationSpeed = 10f;

    private Transform _target;
    private NavMeshAgent _agent;
    // 이동 방향 바라보기 우선 모드 — 켜져 있으면 타겟 조준보다 우선해서 NavMeshAgent의 이동 방향을 바라봄
    private bool _faceMovement;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // 이동 방향 우선 모드: 병렬 브랜치의 RotateToTarget이 타겟을 물고 있어도 이동 방향을 바라봄
        if (_faceMovement)
        {
            Vector3 vel = _agent.velocity;
            vel.y = 0f;
            if (vel.sqrMagnitude < 0.001f) return;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(vel),
                RotationSpeed * Time.deltaTime);
            return;
        }

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

    // 후퇴 등에서 이동 방향을 바라보게 함 (타겟 조준보다 우선)
    public void BeginFaceMovement()
    {
        _faceMovement = true;
        _agent.updateRotation = false;
    }

    public void EndFaceMovement()
    {
        _faceMovement = false;
    }
}
