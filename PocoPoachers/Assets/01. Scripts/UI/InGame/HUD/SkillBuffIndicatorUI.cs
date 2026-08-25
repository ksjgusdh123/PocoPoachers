using UnityEngine;

// 지속시간이 있는 스킬이 켜져 있는 동안만 뜨는 표시.
// 루트는 계속 활성인 채로 두고 칸을 각각 켜고 끈다 — 꺼져 있으면 Update가 돌지 않아 다시 켜줄 수가 없다.
// PlayerSkillManager가 스폰 직후 Setup으로 연결한다 (SkillHudUI와 같은 경로).
public class SkillBuffIndicatorUI : MonoBehaviour
{
    [SerializeField] private SkillBuffEntryUI[] _entries;

    [SerializeField, Tooltip("이 시간보다 짧은 스킬은 표시하지 않는다. 대시처럼 순간적인 건 깜빡이기만 하고 읽히지 않는다.")]
    private float _minDuration = 1f;

    private PlayerSkillManager _manager;

    public void Setup(PlayerSkillManager manager)
    {
        _manager = manager;
        HideAll();
    }

    private void OnDisable() => HideAll();

    private void Update()
    {
        if (_manager == null)
        {
            HideAll();
            return;
        }

        // 켜져 있는 스킬만 앞에서부터 채운다 — 슬롯 번호와 칸 번호를 맞추면 중간이 비어 보인다
        int used = 0;
        for (int slot = 0; slot < PlayerSkillManager.SlotCount && used < _entries.Length; slot++)
        {
            IPlayerSkill skill = _manager.GetSkill(slot);
            if (skill == null || skill.Data.duration < _minDuration) continue;

            float remaining = _manager.GetDurationRemaining(slot);
            if (remaining <= 0f) continue;

            _entries[used]?.Show(skill, remaining, skill.Data.duration);
            used++;
        }

        for (int i = used; i < _entries.Length; i++)
            _entries[i]?.Hide();
    }

    private void HideAll()
    {
        if (_entries == null) return;

        foreach (var entry in _entries)
            entry?.Hide();
    }
}
