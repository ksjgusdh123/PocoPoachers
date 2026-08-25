using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 좌측 투입 슬롯 -> 가운데 진행 게이지 -> 우측 결과 칸 + 가져오기 버튼.
// 표시값은 전부 Furnace가 들고 있는 상태에서 읽어오고, 이 UI는 상태를 직접 바꾸지 않는다.
public class FurnaceUI : MonoBehaviour
{
    [SerializeField] private FurnaceInputDropHandler _inputSlot;
    [SerializeField] private Slider _progressBar;

    [Header("Output")]
    [SerializeField] private FurnaceOutputSlotUI _outputSlot;
    [SerializeField] private Image _outputIcon;
    [SerializeField] private TextMeshProUGUI _outputNameText;
    [SerializeField] private TextMeshProUGUI _outputCountText;
    [SerializeField] private Button _takeButton;

    private Inventory _inventory;
    private int _shownOutputId = -1;   // 매 프레임 스프라이트를 다시 로드하지 않기 위한 캐시. -1이면 아직 한 번도 안 그림

    private void Awake()
    {
        _takeButton?.onClick.AddListener(OnClickTake);
    }

    private void OnEnable()
    {
        if (Furnace.Instance == null) return;

        Furnace.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (Furnace.Instance == null) return;

        Furnace.Instance.OnStateChanged -= Refresh;
    }

    public void Open(PlayerController player)
    {
        _inventory = player?.PlayerInventory;
        _inputSlot?.BindInventoryUI(player?.PlayerBagInventoryUI);
        _outputSlot?.Bind(_inventory);

        Refresh();
    }

    private void Refresh()
    {
        var furnace = Furnace.Instance;
        if (furnace == null) return;

        _inputSlot?.Refresh(furnace.InputItem, furnace.InputCount);

        if (_progressBar != null)
            _progressBar.value = furnace.Progress;

        RefreshOutput(furnace);
    }

    private void RefreshOutput(Furnace furnace)
    {
        bool hasOutput = furnace.OutputItem != null && furnace.OutputCount > 0;

        int outputId = hasOutput ? furnace.OutputItem.id : 0;
        if (_outputIcon != null && outputId != _shownOutputId)
        {
            _outputIcon.gameObject.SetActive(hasOutput);
            if (hasOutput) _outputIcon.sprite = ResourceManager.Instance.LoadSprite(furnace.OutputItem.icon);
        }
        _shownOutputId = outputId;

        if (_outputNameText != null)
            _outputNameText.text = hasOutput ? LocalizationManager.GetInstance().GetString(furnace.OutputItem.ItemName) : "";

        if (_outputCountText != null)
            _outputCountText.text = hasOutput ? $"x{furnace.OutputCount}" : "";

        if (_takeButton != null)
            _takeButton.interactable = hasOutput;
    }

    private void OnClickTake()
    {
        Furnace.Instance?.TryTakeOutput(_inventory);
    }
}
