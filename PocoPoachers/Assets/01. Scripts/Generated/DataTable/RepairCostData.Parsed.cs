// 손으로 작성한 partial 확장 — Tools/Generator/Tables로 재생성해도 지워지지 않는다 (ShelterData.Parsed.cs와 동일한 방식).
// need_item_ids/need_item_counts는 "802|803" 형태의 '|' 구분 문자열이라 재료 개수 제한이 없다.
using System;
using System.Collections.Generic;

public partial class RepairCostData
{
    public IReadOnlyList<(int itemId, int count)> NeedItems => ParsePairs(need_item_ids, need_item_counts);

    // ids/counts 개수가 안 맞거나 항목이 숫자가 아니면 그 항목만 건너뛴다 (행 전체를 깨뜨리지 않음)
    private static IReadOnlyList<(int itemId, int count)> ParsePairs(string idsRaw, string countsRaw)
    {
        var result = new List<(int, int)>();
        if (string.IsNullOrEmpty(idsRaw)) return result;

        string[] ids = idsRaw.Split('|');
        string[] counts = string.IsNullOrEmpty(countsRaw) ? Array.Empty<string>() : countsRaw.Split('|');

        int n = Math.Min(ids.Length, counts.Length);
        for (int i = 0; i < n; i++)
        {
            if (int.TryParse(ids[i], out int itemId) && int.TryParse(counts[i], out int count) && itemId > 0 && count > 0)
                result.Add((itemId, count));
        }
        return result;
    }
}
