using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어마다 1개. 장착 슬롯별 스킬과 쿨다운을 관리하고 발동/진행/종료를 중개한다.
[RequireComponent(typeof(PlayerStat))]
public class PlayerSkillManager : MonoBehaviour
{
    public const int SlotCount = 3;

    [SerializeField] private int[] _startSkillIds = new int[SlotCount]; // 장착 시스템 연결 전 임시 세팅

    private PlayerSkillContext _context;
    private readonly IPlayerSkill[] _slots = new IPlayerSkill[SlotCount];
    private readonly float[] _lastUsedTime = new float[SlotCount];
    private readonly List<int> _activeSlots = new();

    public event Action<int, IPlayerSkill> OnSlotChanged;
    public event Action<int> OnSkillUsed;

    // 대시처럼 스킬이 직접 이동을 처리하는 동안 PlayerMovement의 수평 이동을 막는다
    public bool IsMovementLocked { get; private set; }

    private PlayerInputHandler _inputHandler;

    private void Awake()
    {
        _context = new PlayerSkillContext(gameObject);
        _inputHandler = GetComponent<PlayerInputHandler>();

        for (int i = 0; i < SlotCount; i++)
            _lastUsedTime[i] = float.NegativeInfinity;
    }

    private void Start()
    {
        if (_inputHandler != null)
            _inputHandler.SkillUse += HandleSkillInput;

        for (int i = 0; i < SlotCount && i < _startSkillIds.Length; i++)
        {
            if (_startSkillIds[i] > 0)
                Equip(i, _startSkillIds[i]);
        }

        FindAnyObjectByType<SkillHudUI>(FindObjectsInactive.Include)?.Setup(this);
        FindAnyObjectByType<SkillEquipUI>(FindObjectsInactive.Include)?.Setup(this);
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
            _inputHandler.SkillUse -= HandleSkillInput;
    }

    private void HandleSkillInput(int slotIndex) => TryUse(slotIndex);

    public bool Equip(int slotIndex, int skillId)
    {
        if (!IsValidSlot(slotIndex)) return false;

        PlayerSkillData data = PlayerSkillTable.Instance.Get(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[PlayerSkillManager] player_skill.csv에 없는 id: {skillId}");
            return false;
        }

        IPlayerSkill skill = PlayerSkillFactory.Create(data);
        if (skill == null) return false;

        Unequip(slotIndex);
        _slots[slotIndex] = skill;
        OnSlotChanged?.Invoke(slotIndex, skill);
        return true;
    }

    public bool IsEquipped(int skillId)
    {
        foreach (IPlayerSkill skill in _slots)
        {
            if (skill != null && skill.Data.id == skillId)
                return true;
        }
        return false;
    }

    public int FindEmptySlot()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i] == null)
                return i;
        }
        return -1;
    }

    // 빈 슬롯에 자동 장착 — 성공하면 장착된 슬롯 인덱스, 실패하면 -1
    public int EquipToEmptySlot(int skillId)
    {
        if (IsEquipped(skillId)) return -1;

        int slotIndex = FindEmptySlot();
        if (slotIndex < 0) return -1;

        return Equip(slotIndex, skillId) ? slotIndex : -1;
    }

    public void Unequip(int slotIndex)
    {
        if (!IsValidSlot(slotIndex) || _slots[slotIndex] == null) return;

        EndSkill(slotIndex);
        _slots[slotIndex] = null;
        OnSlotChanged?.Invoke(slotIndex, null);
    }

    public IPlayerSkill GetSkill(int slotIndex) => IsValidSlot(slotIndex) ? _slots[slotIndex] : null;

    public bool IsActive(int slotIndex) => _activeSlots.Contains(slotIndex);

    public float GetCooldownRemaining(int slotIndex)
    {
        IPlayerSkill skill = GetSkill(slotIndex);
        if (skill == null) return 0f;

        return Mathf.Max(0f, _lastUsedTime[slotIndex] + skill.Cooldown - Time.time);
    }

    public bool CanUse(int slotIndex)
    {
        IPlayerSkill skill = GetSkill(slotIndex);
        if (skill == null) return false;
        if (_activeSlots.Contains(slotIndex)) return false;
        if (GetCooldownRemaining(slotIndex) > 0f) return false;

        return skill.CanUse(_context);
    }

    public bool TryUse(int slotIndex)
    {
        if (!CanUse(slotIndex)) return false;

        IPlayerSkill skill = _slots[slotIndex];
        _lastUsedTime[slotIndex] = Time.time;
        _activeSlots.Add(slotIndex);
        skill.Begin(_context);
        RefreshMovementLock();
        OnSkillUsed?.Invoke(slotIndex);
        return true;
    }

    private void Update()
    {
        for (int i = _activeSlots.Count - 1; i >= 0; i--)
        {
            int slotIndex = _activeSlots[i];
            if (!_slots[slotIndex].Tick(_context))
                EndSkill(slotIndex);
        }
    }

    private void EndSkill(int slotIndex)
    {
        if (!_activeSlots.Remove(slotIndex)) return;

        _slots[slotIndex]?.End(_context);
        RefreshMovementLock();
    }

    private void RefreshMovementLock()
    {
        IsMovementLocked = false;
        foreach (int slotIndex in _activeSlots)
        {
            if (_slots[slotIndex].LocksMovement)
            {
                IsMovementLocked = true;
                return;
            }
        }
    }

    private static bool IsValidSlot(int slotIndex) => slotIndex >= 0 && slotIndex < SlotCount;
}
