using System.Collections.Generic;
using UnityEngine;

// AI마다 1개. 보유 스킬 인스턴스 + 쿨다운을 일원 관리하고, 발동/진행/종료를 중개한다.
// "언제 쓸지"는 BT(UseSkill/CanUseSkill 노드)가 결정하고, 여기서는 실행과 상태만 책임진다.
[RequireComponent(typeof(EnemyStat))]
public class SkillManager : MonoBehaviour
{
    // 이 AI가 보유할 스킬 행 id (skill.csv). 같은 동작(SkillId)당 1개만 등록 권장.
    [SerializeField] private int[] _skillIds;

    private SkillContext _context;
    private readonly Dictionary<SkillId, ISkill> _skills = new();
    private readonly Dictionary<SkillId, float> _lastUsedTime = new();
    private ISkill _active;

    private void Awake()
    {
        _context = new SkillContext(gameObject);
        RegisterSkills();
    }

    // skill.csv에서 지정된 id 행을 읽어 스킬 인스턴스를 생성·등록한다.
    private void RegisterSkills()
    {
        if (_skillIds == null)
            return;

        foreach (int id in _skillIds)
        {
            SkillData data = SkillTable.Instance.Get(id);
            if (data == null)
            {
                Debug.LogWarning($"[SkillManager] skill.csv에 없는 id: {id}");
                continue;
            }

            ISkill skill = SkillFactory.Create(data);
            if (skill != null)
                Register(skill);
        }
    }

    private void Register(ISkill skill)
    {
        _skills[skill.Id] = skill;
        _lastUsedTime[skill.Id] = float.NegativeInfinity;
    }

    public bool Has(SkillId id) => _skills.ContainsKey(id);

    // 쿨다운 경과 + 스킬 자체 조건을 모두 만족해야 사용 가능
    public bool CanUse(SkillId id)
    {
        if (!_skills.TryGetValue(id, out ISkill skill))
            return false;
        if (Time.time < _lastUsedTime[id] + skill.Cooldown)
            return false;
        return skill.CanUse(_context);
    }

    // 스킬 발동 시작 — 성공 시 활성 스킬로 등록하고 쿨다운을 시작한다.
    public bool TryBegin(SkillId id)
    {
        if (!CanUse(id))
            return false;

        _active = _skills[id];
        _lastUsedTime[id] = Time.time;
        _active.Begin(_context);
        return true;
    }

    // 활성 스킬 진행 — true면 계속 진행 중, false면 종료(자동 정리)
    public bool Tick()
    {
        if (_active == null)
            return false;

        if (_active.Tick(_context))
            return true;

        End();
        return false;
    }

    // 활성 스킬 종료 정리 (중복 호출 안전)
    public void End()
    {
        if (_active == null)
            return;

        _active.End(_context);
        _active = null;
    }

    // 외부에서 스킬 실행에 필요한 상황 정보를 전달 (타겟은 SkillContext가 TargetDetector에서 직접 읽음)
    public void SetAttacker(GameObject attacker) => _context.Attacker = attacker;
}
