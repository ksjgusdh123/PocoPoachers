using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// item 테이블의 icon / prefab 키가 Resources에서 로드되는지 검사합니다.
/// </summary>
public static class ItemResourceTools
{
    [MenuItem("Tools/PocoPoachers/아이템 리소스 검증 창")]
    public static void OpenWindow() => EditorWindow.GetWindow<ItemResourceValidatorWindow>(true, "아이템 리소스", true);

    static ValidationReport BuildReport()
    {
        var table = ItemTable.Instance;
        var items = table.All.OrderBy(d => d.id).ToList();
        var lines = new List<ValidationLine>();

        foreach (var d in items)
        {
            var iconOk = TryResolveIcon(d.icon, out var iconKind);
            var prefabOk = TryResolvePrefab(d.prefab, out var prefabKind);
            lines.Add(new ValidationLine(d.id, d.ItemName, d.icon, iconOk, iconKind, d.prefab, prefabOk, prefabKind));
        }

        return new ValidationReport(items.Count, lines);
    }

    static bool TryResolveIcon(string path, out string kind)
    {
        kind = "";
        if (string.IsNullOrWhiteSpace(path))
        {
            kind = "(비움)";
            return true;
        }

        if (Resources.Load<Sprite>(path) != null)
        {
            kind = "Sprite";
            return true;
        }

        if (Resources.Load<Texture2D>(path) != null)
        {
            kind = "Texture2D";
            return true;
        }

        kind = "없음";
        return false;
    }

    static bool TryResolvePrefab(string path, out string kind)
    {
        kind = "";
        if (string.IsNullOrWhiteSpace(path))
        {
            kind = "(비움)";
            return true;
        }

        if (Resources.Load<GameObject>(path) != null)
        {
            kind = "Prefab";
            return true;
        }

        kind = "없음";
        return false;
    }

    sealed class ValidationReport
    {
        public int Total { get; }
        public IReadOnlyList<ValidationLine> Lines { get; }
        public int MissingIconCount => Lines.Count(l => !l.IconOk && !string.IsNullOrWhiteSpace(l.IconPath));
        public int MissingPrefabCount => Lines.Count(l => !l.PrefabOk && !string.IsNullOrWhiteSpace(l.PrefabPath));

        public ValidationReport(int total, IReadOnlyList<ValidationLine> lines)
        {
            Total = total;
            Lines = lines;
        }
    }

    readonly struct ValidationLine
    {
        public readonly int Id;
        public readonly string Name;
        public readonly string IconPath;
        public readonly bool IconOk;
        public readonly string IconKind;
        public readonly string PrefabPath;
        public readonly bool PrefabOk;
        public readonly string PrefabKind;

        public ValidationLine(int id, string name, string iconPath, bool iconOk, string iconKind, string prefabPath, bool prefabOk, string prefabKind)
        {
            Id = id;
            Name = name;
            IconPath = iconPath ?? "";
            IconOk = iconOk;
            IconKind = iconKind ?? "";
            PrefabPath = prefabPath ?? "";
            PrefabOk = prefabOk;
            PrefabKind = prefabKind ?? "";
        }
    }

    sealed class ItemResourceValidatorWindow : EditorWindow
    {
        Vector2 _scroll;
        ValidationReport _report;

        void OnEnable() => Refresh();

        void Refresh() => _report = BuildReport();

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "JsonData/item 기준으로 icon(Sprite→Texture2D) / prefab(GameObject)를 Resources.Load와 동일하게 검사합니다. " +
                "에셋을 추가·이동한 뒤에는 아래 새로고침을 누르세요.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("새로고침", GUILayout.Height(24)))
                    Refresh();

                GUILayout.FlexibleSpace();
                GUILayout.Label($"아이템 {_report.Total}개", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStat("icon 누락", _report.MissingIconCount);
                DrawStat("prefab 누락", _report.MissingPrefabCount);
            }

            EditorGUILayout.Space(6);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var l in _report.Lines)
                DrawRow(l);
            EditorGUILayout.EndScrollView();
        }

        static void DrawStat(string label, int missing)
        {
            var ok = missing == 0;
            var prev = GUI.color;
            GUI.color = ok ? new Color(0.55f, 0.85f, 0.55f) : new Color(1f, 0.65f, 0.45f);
            GUILayout.Label($"{label}: {(ok ? "없음" : missing + "건")}", EditorStyles.miniLabel, GUILayout.Width(160));
            GUI.color = prev;
        }

        static void DrawRow(ValidationLine l)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{l.Id} — {l.Name}", EditorStyles.boldLabel);

                DrawCell("icon", l.IconPath, l.IconOk, l.IconKind);
                DrawCell("prefab", l.PrefabPath, l.PrefabOk, l.PrefabKind);
            }
        }

        static void DrawCell(string title, string path, bool ok, string kind)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var prev = GUI.color;
                GUI.color = ok ? Color.white : new Color(1f, 0.55f, 0.55f);
                GUILayout.Label(title, GUILayout.Width(52));
                GUI.color = prev;

                GUILayout.Label(string.IsNullOrEmpty(path) ? "—" : path, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.LabelField($"  → {kind}", EditorStyles.miniLabel);
        }
    }
}
