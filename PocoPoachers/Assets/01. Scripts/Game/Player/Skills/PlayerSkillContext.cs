using UnityEngine;

// 스킬 실행에 필요한 참조 묶음 — PlayerSkillManager가 1회 생성해 모든 스킬에 전달.
public class PlayerSkillContext
{
    public GameObject Self { get; }
    public Transform Transform { get; }
    public CharacterController Controller { get; }
    public PlayerStat Stat { get; }
    public Animator Animator { get; }
    public PlayerInputHandler Input { get; }
    public WeaponController Weapon { get; }
    public PlayerDodge Dodge { get; }
    public PlayerEnhancement Enhancement { get; }

    public PlayerSkillContext(GameObject self)
    {
        Self = self;
        Transform = self.transform;
        Controller = self.GetComponent<CharacterController>();
        Stat = self.GetComponent<PlayerStat>();
        Animator = self.GetComponentInChildren<Animator>();
        Input = self.GetComponent<PlayerInputHandler>();
        Weapon = self.GetComponent<WeaponController>();
        Dodge = self.GetComponent<PlayerDodge>();
        Enhancement = self.GetComponent<PlayerEnhancement>();
    }

    // 이동 입력 방향(화면 기준), 입력이 없으면 바라보는 방향
    public Vector3 MoveDirectionOrForward()
    {
        Vector2 input = Input != null ? Input.MoveInput : Vector2.zero;
        if (input.sqrMagnitude < 0.01f)
            return Transform.forward;

        return CameraSpace.InputToWorld(input).normalized;
    }

    // 크로스헤어가 가리키는 지면 지점(PlayerRotation.RotateTowardMouse와 같은 방식) — maxDistance를 넘으면 그 방향으로 clamp.
    public Vector3 AimGroundPoint(float maxDistance)
    {
        Vector3 origin = Transform.position;
        if (CrosshairUI.Instance == null || Camera.main == null)
            return origin + Transform.forward * maxDistance;

        Ray ray = Camera.main.ScreenPointToRay(CrosshairUI.Instance.ScreenPosition);
        Transform muzzle = Weapon != null ? Weapon.CurrentGun?.Muzzle : null;
        float planeHeight = muzzle != null ? muzzle.position.y : origin.y;
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));

        Vector3 point = aimPlane.Raycast(ray, out float distance)
            ? ray.GetPoint(distance)
            : origin + Transform.forward * maxDistance;

        Vector3 offset = point - origin;
        offset.y = 0f;
        if (offset.magnitude > maxDistance)
            offset = offset.normalized * maxDistance;

        return origin + offset;
    }
}
