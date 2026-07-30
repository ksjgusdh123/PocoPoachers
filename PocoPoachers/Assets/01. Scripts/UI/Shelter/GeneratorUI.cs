using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneratorUI : MonoBehaviour
{
    private static readonly Color CriticalColor = new Color(1f, 0.231f, 0.231f); // < 10%
    private static readonly Color LowColor = new Color(1f, 0.549f, 0f);          // < 40%
    private static readonly Color MediumColor = new Color(1f, 0.878f, 0.1f);     // < 70%
    private static readonly Color HighColor = new Color(0.2f, 0.898f, 0.4f);     // >= 70%

    [SerializeField] private Slider _powerBar;
    [SerializeField] private TextMeshProUGUI _powerText;
    [SerializeField] private GeneratorFuelDropHandler _fuelSlot;
    [SerializeField] private Button _insertButton;

    private ItemData _pendingFuelItem;
    private int _pendingFuelAmount;

    private void Awake()
    {
        _insertButton?.onClick.AddListener(OnClickInsert);

        if (_fuelSlot != null)
        {
            _fuelSlot.OnItemSet += HandleFuelSet;
            _fuelSlot.OnItemCleared += HandleFuelCleared;
        }
    }

    private void OnEnable()
    {
        if (Generator.Instance == null) return;

        Generator.Instance.OnPowerChanged += Refresh;
        Refresh(Generator.Instance.CurrentPower, Generator.Instance.MaxPowerCapacity);
    }

    private void OnDisable()
    {
        if (Generator.Instance == null) return;

        Generator.Instance.OnPowerChanged -= Refresh;
    }

    public void Open(PlayerController player)
    {
        _fuelSlot?.BindInventoryUI(player.PlayerBagInventoryUI);

        if (Generator.Instance == null) return;

        Refresh(Generator.Instance.CurrentPower, Generator.Instance.MaxPowerCapacity);
    }

    // 슬롯에 연료를 올려놓는 즉시 미리보기 시작 — 아직 발전기에 투입된 건 아님
    private void HandleFuelSet(ItemData item, int amount)
    {
        _pendingFuelItem = item;
        _pendingFuelAmount = amount;

        if (Generator.Instance != null)
            Refresh(Generator.Instance.CurrentPower, Generator.Instance.MaxPowerCapacity);
    }

    // 슬롯이 비면(넣기 확정 또는 우클릭 취소) 미리보기 종료
    private void HandleFuelCleared()
    {
        _pendingFuelItem = null;
        _pendingFuelAmount = 0;

        if (Generator.Instance != null)
            Refresh(Generator.Instance.CurrentPower, Generator.Instance.MaxPowerCapacity);
    }

    private void Refresh(float current, float max)
    {
        float ratio = max > 0f ? current / max : 0f;

        if (_powerBar != null)
            _powerBar.value = ratio;

        if (_powerText == null) return;

        if (_pendingFuelItem != null)
            ShowPendingPreview(current, max);
        else
            SetPlainText(ratio);
    }

    private void SetPlainText(float ratio)
    {
        string hex = ColorUtility.ToHtmlStringRGB(GetColorForRatio(ratio));
        _powerText.text = $"<color=#{hex}>{Mathf.RoundToInt(ratio * 100f)}</color><color=#B8C7D9>%</color>";
    }

    // 슬롯에 연료가 올라와 있는 동안, 지금 넣으면 도달할 퍼센트를 "52 -> 57%" 형태로 실시간으로 보여준다.
    // 방전이 계속 진행되므로 매 갱신마다 현재값 기준으로 다시 계산한다.
    private void ShowPendingPreview(float current, float max)
    {
        var fuel = GeneratorFuelTable.Instance.Get(_pendingFuelItem.id);
        float potential = fuel != null ? Mathf.Min(max, current + fuel.power_seconds * _pendingFuelAmount) : current;

        float beforeRatio = max > 0f ? current / max : 0f;
        float afterRatio = max > 0f ? potential / max : 0f;

        int before = Mathf.RoundToInt(beforeRatio * 100f);
        int after = Mathf.RoundToInt(afterRatio * 100f);
        string beforeHex = ColorUtility.ToHtmlStringRGB(GetColorForRatio(beforeRatio));
        string afterHex = ColorUtility.ToHtmlStringRGB(GetColorForRatio(afterRatio));

        _powerText.text = $"<color=#{beforeHex}>{before}</color> <color=#B8C7D9>-></color> <color=#{afterHex}>{after}</color><color=#B8C7D9>%</color>";
    }

    private static Color GetColorForRatio(float ratio)
    {
        if (ratio < 0.1f) return CriticalColor;
        if (ratio < 0.4f) return LowColor;
        if (ratio < 0.7f) return MediumColor;
        return HighColor;
    }

    private void OnClickInsert()
    {
        _fuelSlot?.TryInsertToGenerator();
    }
}
