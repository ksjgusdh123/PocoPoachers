using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스탯 포인트 강화 전용 패널.
// StatRow 프리팹을 스탯 개수만큼 Instantiate해 (이름 / 10칸 블록바 / ＋버튼) 행을 만든다.
// ＋는 포인트를 즉시 소비하지 않고 예약(pending)만 하며, 예약 수만큼 블록바가 왼쪽부터 채워진다.
// 저장 버튼을 눌러야 PlayerEnhancement에 일괄 반영된다.
// 기체 레벨업은 EnhancementLevelUpUI가 담당.
public class EnhancementStatUI : MonoBehaviour
{
    private const int MaxPendingPerStat = 10;

    [Header("Row Prefab")]
    [SerializeField] private EnhancementStatRowUI _rowPrefab;
    [SerializeField] private Transform _rowContainer;

    [Header("Footer")]
    [SerializeField] private TextMeshProUGUI _statPointsText;
    [SerializeField] private Button _saveButton;

    private static readonly EnhancementStatType[] StatTypes =
    {
        EnhancementStatType.AttackPower,
        EnhancementStatType.MoveSpeed,
        EnhancementStatType.MaxHp,
        EnhancementStatType.DefenseRate,
        EnhancementStatType.VisionRange,
        EnhancementStatType.AttackSpeed,
    };

    private readonly Dictionary<EnhancementStatType, EnhancementStatRowUI> _rows = new();

    // 저장 전까지의 예약 포인트. 스탯당 최대 MaxPendingPerStat개.
    private readonly Dictionary<EnhancementStatType, int> _pending = new();

    private PlayerEnhancement _playerEnhancement;
    private bool _rowsBuilt;

    private void Awake()
    {
        BuildRows();
        _saveButton?.onClick.AddListener(OnClickSave);
    }

    private void BuildRows()
    {
        if (_rowsBuilt || _rowPrefab == null || _rowContainer == null) return;

        foreach (EnhancementStatType statType in StatTypes)
        {
            EnhancementStatRowUI row = Instantiate(_rowPrefab, _rowContainer);
            row.Setup(statType, GetDisplayName(statType), () => OnClickPlus(statType));
            _rows[statType] = row;
        }

        _rowsBuilt = true;
    }

    public void Open(PlayerEnhancement playerEnhancement)
    {
        _playerEnhancement = playerEnhancement;
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

        int current = GetPending(statType);
        if (current >= MaxPendingPerStat) return;

        _pending[statType] = current + 1;
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

            row.SetFilled(pending);
            row.SetInteractable(available && remaining > 0 && pending < MaxPendingPerStat);
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
