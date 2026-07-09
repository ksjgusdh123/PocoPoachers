using System;
using UnityEngine;

// 맵 자동 생성 시스템 제안서(map_plan/)의 레이어별 색상 규칙을 데이터로 관리하는 팔레트.
[CreateAssetMenu(fileName = "MapLayerPalette", menuName = "Game/Map/Layer Palette")]
public class MapLayerPalette : ScriptableObject
{
    [Serializable]
    public class BiomeEntry
    {
        public string name;
        public Color color = Color.white;
        public TerrainLayer terrainLayer;
    }

    [Serializable]
    public class PlacementEntry
    {
        public string name;
        public Color color = Color.white;
        public GameObject prefab;
        [Range(0f, 1f)] public float density = 1f;
        public Vector2 uniformScaleRange = new Vector2(0.9f, 1.1f);
        public bool randomYRotation = true;
    }

    [Tooltip("픽셀 색상과 팔레트 색상 사이 허용 오차 (0~1, RGB 거리 정규화 기준)")]
    [Range(0.01f, 0.5f)]
    public float colorMatchTolerance = 0.15f;

    public BiomeEntry[] biomes = Array.Empty<BiomeEntry>();
    public PlacementEntry[] objects = Array.Empty<PlacementEntry>();
    public PlacementEntry[] resources = Array.Empty<PlacementEntry>();

    public BiomeEntry MatchBiome(Color pixel) => FindClosest(biomes, e => e.color, pixel);
    public PlacementEntry MatchObject(Color pixel) => FindClosest(objects, e => e.color, pixel);
    public PlacementEntry MatchResource(Color pixel) => FindClosest(resources, e => e.color, pixel);

    T FindClosest<T>(T[] entries, Func<T, Color> colorOf, Color pixel) where T : class
    {
        if (entries == null || entries.Length == 0) return null;

        T best = null;
        float bestDist = colorMatchTolerance;
        foreach (var entry in entries)
        {
            float dist = ColorDistance(colorOf(entry), pixel);
            if (dist <= bestDist)
            {
                bestDist = dist;
                best = entry;
            }
        }
        return best;
    }

    // 0(동일) ~ 1(정반대) 범위로 정규화한 RGB 유클리드 거리
    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db) / 1.7320508f;
    }
}
