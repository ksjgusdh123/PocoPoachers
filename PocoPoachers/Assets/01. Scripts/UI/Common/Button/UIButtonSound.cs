using UnityEngine;
using UnityEngine.UI;

// 이 버튼만 다른 호버/클릭 효과음을 내고 싶을 때 버튼과 같은 오브젝트에 붙인다.
// 비워 둔 키는 기본음(ui_hover / ui_click)을 그대로 쓴다.
// 재생은 UISoundManager가 하고, 이 컴포넌트는 어떤 키를 쓸지만 들고 있는다.
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [SerializeField, Tooltip("sound.csv의 key. 비우면 기본 호버음(ui_hover)")]
    private string _hoverKey;

    [SerializeField, Tooltip("sound.csv의 key. 비우면 기본 클릭음(ui_click)")]
    private string _clickKey;

    public string HoverKey => _hoverKey;
    public string ClickKey => _clickKey;
}
