using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlanetSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _icon;
    [SerializeField] private Button _button;

    private PlanetData _data;

    private void OnEnable()
    {
        RefreshName();
        LocalizationManager.GetInstance().OnLanguageChanged += RefreshName;
    }

    private void OnDisable()
    {
        var manager = LocalizationManager.GetInstance();
        if (manager == null) return;
        manager.OnLanguageChanged -= RefreshName;
    }

    public void Setup(PlanetData data, bool isUnlocked)
    {
        _data = data;
        RefreshName();
        _icon.sprite = ResourceManager.Instance.LoadSprite(data.IconPath);
        _button.interactable = isUnlocked;
        _button.onClick.AddListener(OnClick);
    }

    private void RefreshName()
    {
        if (_data == null) return;
        _nameText.text = LocalizationManager.GetInstance().GetString(_data.PlanetName);
    }

    private void OnClick()
    {
        GameManager.Instance.SetSelectedPlanet(_data.Id);
        SceneLoader.Instance.LoadPlanetScene(_data.Id);
    }
}
