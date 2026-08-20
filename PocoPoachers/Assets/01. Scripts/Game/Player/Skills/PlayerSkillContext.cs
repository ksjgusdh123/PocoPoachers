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
    }

    // 이동 입력 방향(월드 기준), 입력이 없으면 바라보는 방향
    public Vector3 MoveDirectionOrForward()
    {
        Vector2 input = Input != null ? Input.MoveInput : Vector2.zero;
        if (input.sqrMagnitude < 0.01f)
            return Transform.forward;

        return new Vector3(input.x, 0f, input.y).normalized;
    }
}
