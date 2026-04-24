using UnityEngine;

public enum ObjectKind
{
    Player,
    Npc,
    WorldItem,
}

public class WorldObject : MonoBehaviour
{
    [SerializeField] float _smooth = 14f;

    public int Id { get; private set; }
    public ObjectKind Kind { get; private set; }

    Vector3 _targetPos;
    float _targetYaw;
    bool _hasTarget;

    public void Initialize(ObjectKind kind, int id)
    {
        Kind = kind;
        Id = id;
    }

    public void SetMoveTarget(Vector3 worldPos, float yawDegrees)
    {
        _targetPos = worldPos;
        _targetYaw = yawDegrees;
        _hasTarget = true;
    }

    void Update()
    {
        if (!_hasTarget) return;

        float t = 1f - Mathf.Exp(-_smooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, _targetPos, t);
        float y = Mathf.LerpAngle(transform.eulerAngles.y, _targetYaw, t);
        transform.rotation = Quaternion.Euler(0f, y, 0f);
    }
}
