using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 강화대 상위 UI — 상단 탭(레벨/스탯)으로 EnhancementLevelUpUI / EnhancementStatUI 패널을 전환한다.
// 실제 레벨업·강화 로직은 각 하위 패널이 담당.
public class EnhancementTableUI : MonoBehaviour
{
    private enum Tab { Level, Stat }

    [Header("탭 메뉴")]
    [SerializeField] private Button _levelTabButton;
    [SerializeField] private Button _statTabButton;

    [Header("하위 패널")]
    [SerializeField] private GameObject _levelPanel;
    [SerializeField] private GameObject _statPanel;
    [SerializeField] private EnhancementLevelUpUI _levelUpUI;
    [SerializeField] private EnhancementStatUI _statUI;

    private PlayerController _player;
    private PlayerEnhancement _playerEnhancement;
    private Tab _selectedTab = Tab.Level;

    private void Awake()
    {
        _levelTabButton?.onClick.AddListener(() => SelectTab(Tab.Level));
        _statTabButton?.onClick.AddListener(() => SelectTab(Tab.Stat));
    }

    public void Open(PlayerController player)
    {
        _player = player;
        _playerEnhancement = player != null ? player.GetComponent<PlayerEnhancement>() : null;

        if (_playerEnhancement == null)
            Debug.LogWarning("EnhancementTableUI requires PlayerEnhancement on player.");

        _levelUpUI?.Open(_playerEnhancement);
        _statUI?.Open(_playerEnhancement);

        SelectTab(_selectedTab);
    }

    public void Refresh()
    {
        _levelUpUI?.Refresh();
        _statUI?.Refresh();
    }

    private void SelectTab(Tab tab)
    {
        _selectedTab = tab;

        if (_levelPanel != null) _levelPanel.SetActive(tab == Tab.Level);
        if (_statPanel != null) _statPanel.SetActive(tab == Tab.Stat);

        RefreshTabSelection();
    }

    private void RefreshTabSelection()
    {
        SetTabHighlight(_levelTabButton, _selectedTab == Tab.Level);
        SetTabHighlight(_statTabButton, _selectedTab == Tab.Stat);
    }

    private static void SetTabHighlight(Button button, bool selected)
    {
        if (button == null) return;

        Transform accent = button.transform.Find("SelectionAccent");
        if (accent != null) accent.gameObject.SetActive(selected);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.color = selected ? UITheme.InkPrimary : UITheme.InkSecondary;
    }
}
