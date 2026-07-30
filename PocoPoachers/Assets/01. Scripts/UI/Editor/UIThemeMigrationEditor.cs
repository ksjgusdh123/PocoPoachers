using System;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class UIThemeMigrationEditor
{
    private const string PrefabRoot = "Assets/02. Prefabs/UI";
    private const float MaximumManagedFontSize = 36.01f;
    private const int CurrentMigrationVersion = 1;

    static UIThemeMigrationEditor()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += ApplyPendingMigration;
    }

    private static void ApplyPendingMigration()
    {
        UITheme theme = UITheme.Default;
        if (theme == null || theme.DesignSystemVersion >= CurrentMigrationVersion) return;

        ApplyDesignSystem();
    }

    private sealed class MigrationStats
    {
        public int PrefabsChanged;
        public int TextComponentsAdded;
        public int TextComponentsUpdated;
        public int OversizedTextsSkipped;
        public int SelectablesAdded;
        public int SpacingValuesSnapped;
        public int PaddingValuesSnapped;

        public override string ToString()
        {
            return $"prefabs={PrefabsChanged}, textAdded={TextComponentsAdded}, " +
                   $"textUpdated={TextComponentsUpdated}, oversizedSkipped={OversizedTextsSkipped}, " +
                   $"selectablesAdded={SelectablesAdded}, spacingSnapped={SpacingValuesSnapped}, " +
                   $"paddingSnapped={PaddingValuesSnapped}";
        }
    }

    private sealed class AuditStats
    {
        public int ManagedTexts;
        public int UnmanagedTexts;
        public int OversizedTexts;
        public int InvalidAutoSizeRanges;
        public int StyledSelectables;
        public int UnstyledSelectables;
        public int OffGridSpacing;
        public int OffGridPadding;

        public override string ToString()
        {
            return $"managedTexts={ManagedTexts}, unmanagedTexts={UnmanagedTexts}, " +
                   $"oversizedTexts={OversizedTexts}, invalidAutoSize={InvalidAutoSizeRanges}, " +
                   $"styledSelectables={StyledSelectables}, unstyledSelectables={UnstyledSelectables}, " +
                   $"offGridSpacing={OffGridSpacing}, offGridPadding={OffGridPadding}";
        }
    }

    [MenuItem("PocoPoachers/UI/Apply Design System")]
    public static void ApplyDesignSystem()
    {
        UITheme theme = UITheme.Default;
        if (theme == null)
            throw new InvalidOperationException("Resources/UITheme.asset을 찾을 수 없습니다.");

        int grid = Mathf.Max(1, theme.SpacingGrid);
        var stats = new MigrationStats();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            bool changed = ApplyToPrefab(root, theme, grid, stats);
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                stats.PrefabsChanged++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        theme.DesignSystemVersion = CurrentMigrationVersion;
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        Debug.Log($"[UI Design System] 적용 완료: {stats}");
        AuditDesignSystem();
    }

    // Unity batchmode -executeMethod 진입점.
    public static void ApplyDesignSystemBatch() => ApplyDesignSystem();

    [MenuItem("PocoPoachers/UI/Audit Design System")]
    public static void AuditDesignSystem()
    {
        UITheme theme = UITheme.Default;
        if (theme == null)
            throw new InvalidOperationException("Resources/UITheme.asset을 찾을 수 없습니다.");

        int grid = Mathf.Max(1, theme.SpacingGrid);
        var stats = new AuditStats();
        var details = new StringBuilder();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            AuditPrefab(prefab, path, theme, grid, stats, details);
        }

        if (details.Length > 0)
            Debug.LogWarning($"[UI Design System] 감사 상세\n{details}");
        Debug.Log($"[UI Design System] 감사 완료: {stats}");
    }

    public static void AuditDesignSystemBatch() => AuditDesignSystem();

    private static bool ApplyToPrefab(GameObject root, UITheme theme, int grid, MigrationStats stats)
    {
        bool changed = false;

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (IsNestedPrefabObject(root, text.gameObject)) continue;

            float referenceSize = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
            if (referenceSize > MaximumManagedFontSize)
            {
                stats.OversizedTextsSkipped++;
                continue;
            }

            UITheme.TypographyRole role = InferRole(referenceSize);
            ThemedTextUI themedText = text.GetComponent<ThemedTextUI>();
            bool added = themedText == null;
            if (added)
            {
                themedText = text.gameObject.AddComponent<ThemedTextUI>();
                stats.TextComponentsAdded++;
            }

            float expectedSize = theme.GetFontSize(role);
            Vector2 expectedRange = theme.GetAutoSizeRange(role);
            bool needsUpdate = themedText.Role != role ||
                               !Mathf.Approximately(text.fontSize, expectedSize) ||
                               text.enableAutoSizing &&
                               (!Mathf.Approximately(text.fontSizeMin, expectedRange.x) ||
                                !Mathf.Approximately(text.fontSizeMax, expectedRange.y));

            themedText.Configure(role);
            if (needsUpdate) stats.TextComponentsUpdated++;
            changed |= added || needsUpdate;
        }

        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            if (selectable is Button || IsNestedPrefabObject(root, selectable.gameObject)) continue;

            ThemedSelectableUI themedSelectable = selectable.GetComponent<ThemedSelectableUI>();
            if (themedSelectable == null)
            {
                themedSelectable = selectable.gameObject.AddComponent<ThemedSelectableUI>();
                stats.SelectablesAdded++;
                changed = true;
            }

            ColorBlock before = selectable.colors;
            Selectable.Transition transition = selectable.transition;
            themedSelectable.Apply();
            if (transition != selectable.transition || !ColorBlocksEqual(before, selectable.colors))
                changed = true;
        }

        foreach (HorizontalOrVerticalLayoutGroup layout in root.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true))
        {
            float spacing = SnapPositive(layout.spacing, grid);
            if (!Mathf.Approximately(spacing, layout.spacing))
            {
                layout.spacing = spacing;
                stats.SpacingValuesSnapped++;
                changed = true;
            }

            changed |= SnapPadding(layout, grid, stats);
        }

        foreach (GridLayoutGroup layout in root.GetComponentsInChildren<GridLayoutGroup>(true))
        {
            Vector2 spacing = new(
                SnapPositive(layout.spacing.x, grid),
                SnapPositive(layout.spacing.y, grid));
            if (spacing != layout.spacing)
            {
                if (!Mathf.Approximately(spacing.x, layout.spacing.x)) stats.SpacingValuesSnapped++;
                if (!Mathf.Approximately(spacing.y, layout.spacing.y)) stats.SpacingValuesSnapped++;
                layout.spacing = spacing;
                changed = true;
            }

            changed |= SnapPadding(layout, grid, stats);
        }

        return changed;
    }

    private static void AuditPrefab(
        GameObject root,
        string path,
        UITheme theme,
        int grid,
        AuditStats stats,
        StringBuilder details)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            float referenceSize = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
            if (referenceSize > MaximumManagedFontSize)
            {
                stats.OversizedTexts++;
                continue;
            }

            ThemedTextUI themedText = text.GetComponent<ThemedTextUI>();
            if (themedText == null)
            {
                stats.UnmanagedTexts++;
                details.AppendLine($"unmanaged text: {path}/{GetPath(root.transform, text.transform)}");
                continue;
            }

            stats.ManagedTexts++;
            if (!text.enableAutoSizing) continue;

            Vector2 expected = theme.GetAutoSizeRange(themedText.Role);
            if (!Mathf.Approximately(text.fontSizeMin, expected.x) ||
                !Mathf.Approximately(text.fontSizeMax, expected.y))
            {
                stats.InvalidAutoSizeRanges++;
                details.AppendLine($"invalid auto size: {path}/{GetPath(root.transform, text.transform)} " +
                                   $"{text.fontSizeMin}-{text.fontSizeMax}, expected {expected.x}-{expected.y}");
            }
        }

        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            if (selectable is Button) continue;
            if (selectable.GetComponent<ThemedSelectableUI>() != null)
                stats.StyledSelectables++;
            else
            {
                stats.UnstyledSelectables++;
                details.AppendLine($"unstyled selectable: {path}/{GetPath(root.transform, selectable.transform)}");
            }
        }

        foreach (HorizontalOrVerticalLayoutGroup layout in root.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true))
        {
            if (layout.spacing > 0f && !IsOnGrid(layout.spacing, grid)) stats.OffGridSpacing++;
            stats.OffGridPadding += CountOffGridPadding(layout.padding, grid);
        }

        foreach (GridLayoutGroup layout in root.GetComponentsInChildren<GridLayoutGroup>(true))
        {
            if (layout.spacing.x > 0f && !IsOnGrid(layout.spacing.x, grid)) stats.OffGridSpacing++;
            if (layout.spacing.y > 0f && !IsOnGrid(layout.spacing.y, grid)) stats.OffGridSpacing++;
            stats.OffGridPadding += CountOffGridPadding(layout.padding, grid);
        }
    }

    private static UITheme.TypographyRole InferRole(float fontSize)
    {
        if (fontSize <= 15.5f) return UITheme.TypographyRole.Caption;
        if (fontSize <= 21f) return UITheme.TypographyRole.Body;
        if (fontSize <= 27f) return UITheme.TypographyRole.Subtitle;
        if (fontSize <= 33f) return UITheme.TypographyRole.Title;
        return UITheme.TypographyRole.Display;
    }

    private static bool SnapPadding(LayoutGroup layout, int grid, MigrationStats stats)
    {
        RectOffset padding = layout.padding;
        int left = SnapPositive(padding.left, grid);
        int right = SnapPositive(padding.right, grid);
        int top = SnapPositive(padding.top, grid);
        int bottom = SnapPositive(padding.bottom, grid);

        int changedValues = 0;
        if (left != padding.left) changedValues++;
        if (right != padding.right) changedValues++;
        if (top != padding.top) changedValues++;
        if (bottom != padding.bottom) changedValues++;
        if (changedValues == 0) return false;

        layout.padding = new RectOffset(left, right, top, bottom);
        stats.PaddingValuesSnapped += changedValues;
        return true;
    }

    private static int SnapPositive(int value, int grid)
    {
        if (value <= 0) return value;
        return Mathf.Max(grid, Mathf.FloorToInt(value / (float)grid + 0.5f) * grid);
    }

    private static float SnapPositive(float value, int grid)
    {
        if (value <= 0f) return value;
        return Mathf.Max(grid, Mathf.Floor(value / grid + 0.5f) * grid);
    }

    private static bool IsOnGrid(float value, int grid) =>
        Mathf.Approximately(value / grid, Mathf.Round(value / grid));

    private static int CountOffGridPadding(RectOffset padding, int grid)
    {
        int count = 0;
        if (padding.left > 0 && padding.left % grid != 0) count++;
        if (padding.right > 0 && padding.right % grid != 0) count++;
        if (padding.top > 0 && padding.top % grid != 0) count++;
        if (padding.bottom > 0 && padding.bottom % grid != 0) count++;
        return count;
    }

    private static bool IsNestedPrefabObject(GameObject root, GameObject target)
    {
        GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(target);
        return instanceRoot != null && instanceRoot != root;
    }

    private static string GetPath(Transform root, Transform target)
    {
        if (target == root) return root.name;

        string path = target.name;
        Transform current = target.parent;
        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return root.name + "/" + path;
    }

    private static bool ColorBlocksEqual(ColorBlock left, ColorBlock right)
    {
        return left.normalColor == right.normalColor &&
               left.highlightedColor == right.highlightedColor &&
               left.pressedColor == right.pressedColor &&
               left.selectedColor == right.selectedColor &&
               left.disabledColor == right.disabledColor &&
               Mathf.Approximately(left.colorMultiplier, right.colorMultiplier) &&
               Mathf.Approximately(left.fadeDuration, right.fadeDuration);
    }
}
