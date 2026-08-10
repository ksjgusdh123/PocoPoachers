using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlanetSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _icon;
    [SerializeField] private Button _button;

    private PlanetData _data;
    private GameObject _lockOverlay;
    private TextMeshProUGUI _lockText;
    private bool _isUnlocked = true;

    private void Awake()
    {
        Transform lockTransform = transform.Find("Group/LockOverlay");
        if (lockTransform == null) return;

        _lockOverlay = lockTransform.gameObject;
        _lockText = lockTransform.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void OnEnable()
    {
        RefreshName();
        LocalizationManager.GetInstance().OnLanguageChanged += RefreshName;
    }

    private void OnDisable()
    {
        var manager = LocalizationManager.ExistingInstance;
        if (manager == null) return;
        manager.OnLanguageChanged -= RefreshName;
    }

    public void Setup(PlanetData data, bool isUnlocked)
    {
        _data = data;
        _isUnlocked = isUnlocked;
        _icon.sprite = ResourceManager.Instance.LoadSprite(data.IconPath);
        _button.interactable = isUnlocked;
        _lockOverlay?.SetActive(!isUnlocked);
        _button.onClick.AddListener(OnClick);
        RefreshName();
    }

    private void RefreshName()
    {
        if (_data == null) return;

        var localization = LocalizationManager.GetInstance();
        _nameText.text = localization.GetString(_data.PlanetName);

        if (_lockText == null || _isUnlocked) return;
        _lockText.text = string.Format(localization.GetString("planet.shelter_level_required"), _data.NeedShelterLevel);
    }

    private void OnClick()
    {
        int planetId = _data.Id;

        // 호스트 + 게스트면 전원 수락을 기다린 뒤 SceneMoveVote가 이동을 확정한다.
        if (SceneMoveVote.TryBeginTeamMove($"SC_Raid_{planetId}", SpawnId.FromShelter)) return;

        GameManager.Instance.SetSelectedPlanet(planetId);
        GameManager.Instance.SetSpawnId(SpawnId.FromShelter);
        SceneLoader.Instance.LoadPlanetScene(planetId);
    }
}
