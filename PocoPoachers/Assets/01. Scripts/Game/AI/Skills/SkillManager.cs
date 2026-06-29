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
    private ISkill _active;

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
        if (!CanUse(id))
            return false;

        _active = _skills[id];
        _lastUsedTime[id] = Time.time;
        _active.Begin(_context);
        return true;
    }

    public bool Tick()
    {
        if (_active == null)
            return false;

        if (_active.Tick(_context))
            return true;

        End();
        return false;
    }

    public void End()
    {
        if (_active == null)
            return;

        _active.End(_context);
        _active = null;
    }

    public void SetAttacker(GameObject attacker) => _context.Attacker = attacker;
}
