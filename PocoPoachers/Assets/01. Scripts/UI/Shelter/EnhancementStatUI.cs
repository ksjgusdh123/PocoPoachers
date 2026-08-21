using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스탯 포인트 강화 전용 패널.
// StatRow 프리팹을 스탯 개수만큼 Instantiate해 (이름 / 강화 수치 / -버튼 / 10칸 블록바 / ＋버튼) 행을 만든다.
// 블록바는 "이미 저장된 레벨 + 이번 세션에 예약(pending)한 만큼"을 합쳐서 채운다 — 스탯당 최대 PlayerEnhancement.MaxStatLevel(10)칸.
// ＋는 포인트를 즉시 소비하지 않고 예약만 하며, 저장 버튼을 눌러야 PlayerEnhancement에 반영된다.
// 저장해도 채워진 블록은 그대로 유지된다(예약분이 레벨로 흡수될 뿐이라 다음에 켜도 채워진 채로 보인다).
// 기체 레벨업은 EnhancementLevelUpUI가 담당.
public class EnhancementStatUI : MonoBehaviour
{
    [Header("Row Prefab")]
    [SerializeField] private EnhancementStatRowUI _rowPrefab;
    [SerializeField] private Transform _rowContainer;

    [Header("Footer")]
    [SerializeField] private TextMeshProUGUI _statPointsText;
    [SerializeField] private Button _saveButton;

    private readonly Dictionary<EnhancementStatType, EnhancementStatRowUI> _rows = new();

    // 저장 전까지의 예약 포인트 — 이미 저장된 레벨과 합쳐서 블록바에 표시된다.
    private readonly Dictionary<EnhancementStatType, int> _pending = new();

    private PlayerEnhancement _playerEnhancement;
    private bool _rowsBuilt;

    private void Awake()
    {
        _saveButton?.onClick.AddListener(OnClickSave);
    }

    // StatPanel은 탭 전환마다 SetActive로 켜진다 — 레벨 탭에서 포인트를 얻고 스탯 탭으로 돌아왔을 때도
    // 남은 포인트 표시가 갱신되도록 여기서 새로고침한다.
    private void OnEnable() => RefreshAll();

    private void BuildRows()
    {
        if (_rowsBuilt || _rowPrefab == null || _rowContainer == null || _playerEnhancement == null) return;

        foreach (EnhancementStatType statType in _playerEnhancement.ConfiguredStatTypes)
        {
            EnhancementStatRowUI row = Instantiate(_rowPrefab, _rowContainer);
            row.Setup(statType, GetDisplayName(statType), () => OnClickPlus(statType), () => OnClickMinus(statType));
            _rows[statType] = row;
        }

        _rowsBuilt = true;
    }

    public void Open(PlayerEnhancement playerEnhancement)
    {
        _playerEnhancement = playerEnhancement;
        BuildRows();
        _pending.Clear();
        RefreshAll();
    }

    public void Refresh() => RefreshAll();

    private int GetPending(EnhancementStatType statType) =>
        _pending.TryGetValue(statType, out int value) ? value : 0;

    private int TotalPending() => _pending.Values.Sum();

    private void OnClickPlus(EnhancementStatType statType)
    {
        if (_playerEnhancement == null) return;

        int remaining = _playerEnhancement.StatPoints - TotalPending();
        if (remaining <= 0) return;

        int filled = _playerEnhancement.GetStatLevel(statType) + GetPending(statType);
        if (filled >= PlayerEnhancement.MaxStatLevel) return;

        _pending[statType] = GetPending(statType) + 1;
        RefreshAll();
    }

    private void OnClickMinus(EnhancementStatType statType)
    {
        int current = GetPending(statType);
        if (current <= 0) return;

        _pending[statType] = current - 1;
        RefreshAll();
    }

    private void OnClickSave()
    {
        if (_playerEnhancement == null) return;

        foreach (var kv in _pending)
        {
            if (kv.Value <= 0) continue;
            _playerEnhancement.TrySpendPoints(kv.Key, kv.Value);
        }

        _pending.Clear();
        RefreshAll();
    }

    private void RefreshAll()
    {
        bool available = _playerEnhancement != null;
        int remaining = available ? _playerEnhancement.StatPoints - TotalPending() : 0;

        foreach (var kv in _rows)
        {
            EnhancementStatRowUI row = kv.Value;
            int pending = GetPending(kv.Key);
            int committed = available ? _playerEnhancement.GetStatLevel(kv.Key) : 0;
            int filled = committed + pending;

            row.SetFilled(filled);
            row.SetValue(available ? _playerEnhancement.FormatBonus(kv.Key, _playerEnhancement.GetBonusAtLevel(kv.Key, filled)) : "-");
            row.SetInteractable(available && remaining > 0 && filled < PlayerEnhancement.MaxStatLevel, pending > 0);
        }

        if (_statPointsText != null)
        {
            _statPointsText.text = available
                ? $"{LocalizationManager.GetInstance().GetString("enhancement.stat_points")}: {remaining}"
                : "Missing PlayerEnhancement";
        }

        if (_saveButton != null)
            _saveButton.interactable = available && TotalPending() > 0;
    }

    private static string GetDisplayName(EnhancementStatType statType)
    {
        return statType switch
        {
            EnhancementStatType.AttackPower => "공격력",
            EnhancementStatType.MoveSpeed => "이동속도",
            EnhancementStatType.MaxHp => "체력",
            EnhancementStatType.DefenseRate => "방어력",
            EnhancementStatType.VisionRange => "시야",
            EnhancementStatType.AttackSpeed => "공격속도",
            _ => statType.ToString()
        };
    }
}
