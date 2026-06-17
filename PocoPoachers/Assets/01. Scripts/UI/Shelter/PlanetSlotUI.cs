using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlanetSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Button _button;

    private PlanetData _data;

    public void Setup(PlanetData data, bool isUnlocked)
    {
        _data = data;
        _nameText.text = LocalizationManager.GetInstance().GetString(data.PlanetName);
        _button.interactable = isUnlocked;
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        GameManager.Instance.SetSelectedPlanet(_data.Id);
        SceneLoader.Instance.LoadPlanetScene(_data.Id);
    }
}
