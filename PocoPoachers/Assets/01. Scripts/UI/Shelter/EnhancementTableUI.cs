using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 강화대 상위 UI — 상단 탭(레벨/스탯/스킬)으로 하위 패널을 전환한다.
// 실제 레벨업·강화·스킬 장착 로직은 각 하위 패널이 담당.
public class EnhancementTableUI : MonoBehaviour
{
    private enum Tab { Level, Stat, Skill }

    [Header("탭 메뉴")]
    [SerializeField] private Button _levelTabButton;
    [SerializeField] private Button _statTabButton;
    [SerializeField] private Button _skillTabButton;

    [Header("하위 패널")]
    [SerializeField] private GameObject _levelPanel;
    [SerializeField] private GameObject _statPanel;
    [SerializeField] private GameObject _skillPanel;
    [SerializeField] private EnhancementLevelUpUI _levelUpUI;
    [SerializeField] private EnhancementStatUI _statUI;
    [SerializeField] private SkillEquipPanel _skillUI;

    private PlayerController _player;
    private PlayerEnhancement _playerEnhancement;
    private Tab _selectedTab = Tab.Level;

    private void Awake()
    {
        _levelTabButton?.onClick.AddListener(() => SelectTab(Tab.Level));
        _statTabButton?.onClick.AddListener(() => SelectTab(Tab.Stat));
        _skillTabButton?.onClick.AddListener(() => SelectTab(Tab.Skill));
    }

    public void Open(PlayerController player)
    {
        _player = player;
        _playerEnhancement = player != null ? player.GetComponent<PlayerEnhancement>() : null;

        if (_playerEnhancement == null)
            Debug.LogWarning("EnhancementTableUI requires PlayerEnhancement on player.");

        _levelUpUI?.Open(_playerEnhancement, _player != null ? _player.PlayerInventory : null);
        _statUI?.Open(_playerEnhancement);

        // 스킬 매니저는 PlayerSkillManager가 밀어주는 단독 창과 달리, 상호작용한 플레이어에게서 직접 받는다.
        _skillUI?.Setup(player != null ? player.GetComponent<PlayerSkillManager>() : null);

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
        if (_skillPanel != null) _skillPanel.SetActive(tab == Tab.Skill);

        RefreshTabSelection();
    }

    private void RefreshTabSelection()
    {
        SetTabHighlight(_levelTabButton, _selectedTab == Tab.Level);
        SetTabHighlight(_statTabButton, _selectedTab == Tab.Stat);
        SetTabHighlight(_skillTabButton, _selectedTab == Tab.Skill);
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
