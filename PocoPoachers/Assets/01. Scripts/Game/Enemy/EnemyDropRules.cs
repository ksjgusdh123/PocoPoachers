using System;
using System.Globalization;

// 에디터 생성과 런타임에서 동일한 드롭 규칙 검증을 사용한다.
public static class EnemyDropRules
{
    public static (int[] ids, float[] chances, int[] mins, int[] maxs) Parse(
        string itemIds, string dropChances, string minCounts, string maxCounts)
    {
        string[] Split(string value) => string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(';');
        var ids = Split(itemIds);
        var chances = Split(dropChances);
        var mins = Split(minCounts);
        var maxs = Split(maxCounts);
        if (ids.Length != chances.Length || ids.Length != mins.Length || ids.Length != maxs.Length)
            throw new FormatException("아이템·확률·최소 수량·최대 수량의 항목 개수가 다릅니다.");
        var result = (ids: new int[ids.Length], chances: new float[ids.Length], mins: new int[ids.Length], maxs: new int[ids.Length]);
        var unique = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < ids.Length; i++)
        {
            if (!int.TryParse(ids[i], out result.ids[i]) || result.ids[i] <= 0 || !unique.Add(result.ids[i]))
                throw new FormatException($"{i + 1}번째 아이템 ID가 잘못되었거나 중복입니다.");
            if (!float.TryParse(chances[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result.chances[i])
                || float.IsNaN(result.chances[i]) || result.chances[i] < 0f || result.chances[i] > 1f)
                throw new FormatException($"{i + 1}번째 확률은 0~1이어야 합니다.");
            if (!int.TryParse(mins[i], out result.mins[i]) || !int.TryParse(maxs[i], out result.maxs[i])
                || result.mins[i] < 1 || result.maxs[i] < result.mins[i] || result.maxs[i] == int.MaxValue)
                throw new FormatException($"{i + 1}번째 수량은 1 이상의 최소~최대 범위여야 합니다.");
        }
        return result;
    }
}
