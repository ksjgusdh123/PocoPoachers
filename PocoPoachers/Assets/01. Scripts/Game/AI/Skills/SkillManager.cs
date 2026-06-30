using System.Collections.Generic;
using UnityEngine;

// AI마다 1개. 보유 스킬과 쿨다운을 관리하고 발동/진행/종료를 중개한다. "언제 쓸지"는 BT가 결정.
[RequireComponent(typeof(EnemyStat))]
public class SkillManager : MonoBehaviour
{
    [SerializeField] private int[] _skillIds; // 보유 스킬 행 id (skill.csv)

    private SkillContext _context;
    private readonly Dictionary<SkillId, ISkill> _skills = new();
    private readonly Dictionary<SkillId, float> _lastUsedTime = new();
    // 동시에 여러 스킬이 진행될 수 있으므로 활성 스킬을 집합으로 관리
    private readonly HashSet<SkillId> _active = new();

    private void Awake()
    {
        _context = new SkillContext(gameObject);
        RegisterSkills();
    }

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

    public bool CanUse(SkillId id)
    {
        if (!_skills.TryGetValue(id, out ISkill skill))
            return false;
        if (Time.time < _lastUsedTime[id] + skill.Cooldown)
            return false;
        return skill.CanUse(_context);
    }

    public bool TryBegin(SkillId id)
    {
        if (_active.Contains(id)) // 이미 진행 중인 스킬은 중복 발동 금지
            return false;
        if (!CanUse(id))
            return false;

        _lastUsedTime[id] = Time.time;
        _active.Add(id);
        _skills[id].Begin(_context);
        return true;
    }

    // 해당 스킬 진행 — true면 계속 진행 중, false면 종료(자동 정리)
    public bool Tick(SkillId id)
    {
        if (!_active.Contains(id))
            return false;

        if (_skills[id].Tick(_context))
            return true;

        End(id);
        return false;
    }

    public void End(SkillId id)
    {
        if (!_active.Remove(id))
            return;

        _skills[id].End(_context);
    }

    public void SetAttacker(GameObject attacker) => _context.Attacker = attacker;
}
