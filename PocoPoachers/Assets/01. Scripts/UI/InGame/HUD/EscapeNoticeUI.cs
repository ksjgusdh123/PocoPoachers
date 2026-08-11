using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  EscapeNoticeUI                     ← 루트는 항상 켜둔다 (구독 유지)
//  └── Panel [_content]               ← 실제 표시는 이 자식을 켜고 끈다
//      ├── outline
//      ├── Txt_Title  [TextMeshProUGUI]
//      ├── Roster     [HorizontalLayoutGroup]  ← MemberIcon 프리팹을 인원 수만큼 생성
//      ├── Txt_Count  [TextMeshProUGUI]
//      └── Gauge      [Slider]
// ────────────────────────────────────────────────────────────────────────

// 탈출 구역 집결 알림. 메인 게임 UI와 함께 떠 있는 HUD라 UIManager 패널 스택에 넣지 않는다
// (넣으면 크로스헤어·커서가 바뀌고 ESC로 닫혀버린다).
public class EscapeNoticeUI : MonoBehaviour
{
    [SerializeField] private GameObject      _content;
    [SerializeField] private RectTransform   _roster;
    [SerializeField] private MemberIconUI    _memberIconPrefab;
    [SerializeField] private TextMeshProUGUI _txtCount;
    [SerializeField] private Slider          _gauge;

    // 생성한 아이콘은 파괴하지 않고 재사용한다.
    private readonly List<MemberIconUI> _icons = new();

    private float _duration;
    private float _chargeStartTime;
    private bool  _charging;

    // 지금 표시 중인 구역. 탈출 구역이 여러 개일 때 다른 구역의 신호로 배너가 꺼지면 안 된다.
    private int _shownZoneId = NoZone;

    private const int NoZone = -1;

    // 구독은 루트 생명주기에 건다. 표시 여부로 켜고 끄면 숨은 동안 갱신을 못 받는다.
    private void Awake()
    {
        if (_content != null) _content.SetActive(false);

        EscapeZone.OnStatusChanged += HandleStatusChanged;
        EscapeZone.OnStatusCleared += HandleStatusCleared;
    }

    private void OnDestroy()
    {
        EscapeZone.OnStatusChanged -= HandleStatusChanged;
        EscapeZone.OnStatusCleared -= HandleStatusCleared;
    }

    private void Update()
    {
        if (!_charging || _duration <= 0f) return;

        _gauge.value = Mathf.Clamp01((Time.time - _chargeStartTime) / _duration);
    }

    private void HandleStatusChanged(EscapeStatus status)
    {
        if (_content != null) _content.SetActive(true);
        _shownZoneId = status.ZoneId;

        BuildIcons(status.Inside);
        if (_txtCount != null) _txtCount.text = $"{CountInside(status.Inside)} / {status.Inside.Count}";

        _duration = status.Duration;

        // 충전이 새로 시작될 때만 기준 시각을 잡는다. 이미 차는 중이면 게이지가 되감기지 않게 둔다.
        if (status.Charging && !_charging) _chargeStartTime = Time.time;
        if (!status.Charging && _gauge != null) _gauge.value = 0f;

        _charging = status.Charging;
    }

    // 내가 보고 있던 구역이 아니면 무시한다 — 다른 구역의 인원 변화로 내 배너가 꺼지지 않게.
    private void HandleStatusCleared(int zoneId)
    {
        if (_shownZoneId != NoZone && _shownZoneId != zoneId) return;

        _shownZoneId = NoZone;
        _charging = false;
        if (_gauge != null) _gauge.value = 0f;
        if (_content != null) _content.SetActive(false);
    }

    private void BuildIcons(IReadOnlyList<bool> inside)
    {
        if (_memberIconPrefab == null || _roster == null) return;

        while (_icons.Count < inside.Count)
            _icons.Add(Instantiate(_memberIconPrefab, _roster));

        for (int i = 0; i < _icons.Count; i++)
        {
            bool used = i < inside.Count;
            _icons[i].gameObject.SetActive(used);
            if (used) _icons[i].SetHighlighted(inside[i]);
        }
    }

    private static int CountInside(IReadOnlyList<bool> inside)
    {
        int count = 0;
        for (int i = 0; i < inside.Count; i++)
            if (inside[i]) count++;
        return count;
    }
}
