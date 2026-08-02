using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// 베이크된 NavMesh의 바깥 경계선(삼각형 하나에만 속한 edge)을 따라 얇은 벽(BoxCollider)을 세운다.
// NavMesh는 타일 단위로 나눠 베이크되어 이음새마다 같은 위치의 정점이 다른 인덱스로 중복되므로,
// 먼저 좌표 기준으로 정점을 병합(welding)해 가짜 경계를 제거한다.
// 이어서 "어떤 edge 뭉치가 맵 바깥 테두리인지"는 서로 연결된 edge끼리 Union-Find로 묶은 뒤
// 그룹의 바운딩 박스 크기로 판단해서, 바위 등으로 생긴 안쪽의 작은 구멍은 제외한다.
// 마지막으로 그 뭉치 안에서만(=다른 뭉치와 섞일 위험 없이) edge를 순서대로 이어붙이고,
// 거의 일직선인 구간은 하나의 긴 벽으로 합쳐서 생성되는 GameObject 개수를 줄인다.
public static class NavMeshBoundaryWallGenerator
{
    const string ContainerName = "NavMeshBoundaryWalls";
    const float WallHeight = 5f;
    const float WallThickness = 0.3f;
    const float SimplifyAngleThreshold = 3f; // 이 각도(도) 미만으로 꺾이는 점은 직선으로 간주해 인접 edge를 하나로 합친다

    [MenuItem("Tools/Generator/NavMesh Boundary Walls")]
    static void Generate()
    {
        var triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices.Length == 0)
        {
            Debug.LogWarning("[NavMeshBoundaryWallGenerator] 베이크된 NavMesh가 없습니다. 먼저 NavMeshSurface를 Bake 하세요.");
            return;
        }

        Debug.Log($"[NavMeshBoundaryWallGenerator] 삼각형 정점 {triangulation.vertices.Length}개, 인덱스 {triangulation.indices.Length}개 (삼각형 {triangulation.indices.Length / 3}개)");

        // NavMesh는 타일 단위(Tile Size)로 나눠서 베이크되기 때문에, 타일 이음새에서는
        // 같은 위치의 점이라도 triangulation 상 인덱스가 서로 다르게 나온다.
        // 그대로 edge를 세면 이음새마다 "삼각형 하나에만 속한 edge"로 오인되어 가짜 경계가
        // 대량 생긴다. 좌표 기준으로 정점을 병합(welding)해서 이 문제를 없앤다.
        var weldMap = BuildWeldedVertexMap(triangulation.vertices);
        var weldedIndices = new int[triangulation.indices.Length];
        for (int i = 0; i < triangulation.indices.Length; i++)
            weldedIndices[i] = weldMap[triangulation.indices[i]];

        var boundaryEdges = FindBoundaryEdges(weldedIndices);
        Debug.Log($"[NavMeshBoundaryWallGenerator] 경계 edge {boundaryEdges.Count}개 발견 (정점 병합 후)");

        var groups = GroupEdgesByConnectivity(boundaryEdges);

        List<(int a, int b)> mainGroup = null;
        float bestArea = -1f;
        foreach (var group in groups)
        {
            float area = ComputeBoundsArea(group, triangulation.vertices);
            if (groups.Count <= 50)
                Debug.Log($"[NavMeshBoundaryWallGenerator] 그룹: edge {group.Count}개, 바운딩 넓이 {area:F1}");
            if (area > bestArea)
            {
                bestArea = area;
                mainGroup = group;
            }
        }
        if (groups.Count > 50)
            Debug.Log($"[NavMeshBoundaryWallGenerator] 그룹이 {groups.Count}개라 개별 로그는 생략 (여전히 많이 쪼개짐 - Bake 설정 점검 필요)");

        var existing = GameObject.Find(ContainerName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var container = new GameObject(ContainerName);
        Undo.RegisterCreatedObjectUndo(container, "Generate NavMesh Boundary Walls");

        int wallCount = mainGroup != null
            ? CreateMergedWallSegments(container.transform, mainGroup, triangulation.vertices)
            : 0;

        Debug.Log($"[NavMeshBoundaryWallGenerator] 경계 그룹 {groups.Count}개 중 가장 넓은 그룹(원본 edge {mainGroup?.Count ?? 0}개)을 병합해 벽 {wallCount}개를 생성했습니다.");
    }

    // 좌표(0.01 단위 격자)가 같은 정점은 같은 정점으로 취급해, 타일 이음새의 중복 정점을 병합한다
    static int[] BuildWeldedVertexMap(Vector3[] vertices)
    {
        const float cellSize = 0.01f;
        var canonical = new Dictionary<(int, int, int), int>();
        var map = new int[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            var key = (
                Mathf.RoundToInt(vertices[i].x / cellSize),
                Mathf.RoundToInt(vertices[i].y / cellSize),
                Mathf.RoundToInt(vertices[i].z / cellSize));

            if (!canonical.TryGetValue(key, out int canonicalIndex))
            {
                canonicalIndex = i;
                canonical[key] = canonicalIndex;
            }
            map[i] = canonicalIndex;
        }

        return map;
    }

    // 삼각형 하나에만 속한 edge = NavMesh 바깥 경계선
    static List<(int a, int b)> FindBoundaryEdges(int[] indices)
    {
        var edgeCounts = new Dictionary<(int, int), int>();
        for (int t = 0; t < indices.Length; t += 3)
        {
            CountEdge(edgeCounts, indices[t], indices[t + 1]);
            CountEdge(edgeCounts, indices[t + 1], indices[t + 2]);
            CountEdge(edgeCounts, indices[t + 2], indices[t]);
        }

        var boundary = new List<(int, int)>();
        foreach (var kv in edgeCounts)
            if (kv.Value == 1) boundary.Add(kv.Key);
        return boundary;
    }

    static void CountEdge(Dictionary<(int, int), int> edges, int a, int b)
    {
        var key = a < b ? (a, b) : (b, a);
        edges.TryGetValue(key, out int count);
        edges[key] = count + 1;
    }

    // 서로 맞닿은 edge끼리 Union-Find로 묶어서, 순서 없이도 "같은 테두리 뭉치"를 구분한다
    static List<List<(int a, int b)>> GroupEdgesByConnectivity(List<(int a, int b)> edges)
    {
        var parent = new Dictionary<int, int>();

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        void Union(int a, int b)
        {
            int ra = Find(a), rb = Find(b);
            if (ra != rb) parent[ra] = rb;
        }

        foreach (var edge in edges)
        {
            if (!parent.ContainsKey(edge.a)) parent[edge.a] = edge.a;
            if (!parent.ContainsKey(edge.b)) parent[edge.b] = edge.b;
            Union(edge.a, edge.b);
        }

        var groups = new Dictionary<int, List<(int, int)>>();
        foreach (var edge in edges)
        {
            int root = Find(edge.a);
            if (!groups.TryGetValue(root, out var list))
            {
                list = new List<(int, int)>();
                groups[root] = list;
            }
            list.Add(edge);
        }

        return new List<List<(int, int)>>(groups.Values);
    }

    // XZ 바운딩 박스 넓이 - 맵 바깥 테두리는 넓게 퍼져있고, 바위 등 안쪽 구멍은 좁다
    static float ComputeBoundsArea(List<(int a, int b)> group, Vector3[] vertices)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var edge in group)
        {
            foreach (int i in new[] { edge.a, edge.b })
            {
                Vector3 v = vertices[i];
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.z < minZ) minZ = v.z;
                if (v.z > maxZ) maxZ = v.z;
            }
        }

        return (maxX - minX) * (maxZ - minZ);
    }

    // mainGroup은 이미 Union-Find로 하나의 연결된 뭉치로 격리되어 있으므로, 여기서 순서대로
    // edge를 이어붙여도 (이전처럼) 서로 다른 뭉치끼리 엉뚱하게 연결될 위험이 없다.
    // 이어붙인 뒤 거의 일직선인 구간은 하나의 긴 벽으로 합쳐서 GameObject 개수를 줄인다.
    static int CreateMergedWallSegments(Transform parent, List<(int a, int b)> group, Vector3[] vertices)
    {
        var adjacency = new Dictionary<int, List<int>>();
        foreach (var edge in group)
        {
            AddAdjacency(adjacency, edge.a, edge.b);
            AddAdjacency(adjacency, edge.b, edge.a);
        }

        var visited = new HashSet<(int, int)>();
        int wallIndex = 0;

        foreach (var startEdge in group)
        {
            if (visited.Contains(NormalizeEdge(startEdge))) continue;

            var chain = WalkChain(startEdge.a, startEdge.b, adjacency, visited);
            var corners = SimplifyChain(chain, vertices);

            for (int i = 0; i < corners.Count - 1; i++)
            {
                CreateWallSegment(parent, vertices[corners[i]], vertices[corners[i + 1]], wallIndex);
                wallIndex++;
            }
        }

        return wallIndex;
    }

    static (int, int) NormalizeEdge((int a, int b) e) => e.a < e.b ? e : (e.b, e.a);

    static void AddAdjacency(Dictionary<int, List<int>> adjacency, int from, int to)
    {
        if (!adjacency.TryGetValue(from, out var list))
        {
            list = new List<int>();
            adjacency[from] = list;
        }
        list.Add(to);
    }

    // start->second에서 출발해 갈림길이 나올 때까지(또는 루프가 닫힐 때까지) 계속 이어간다
    static List<int> WalkChain(int start, int second, Dictionary<int, List<int>> adjacency, HashSet<(int, int)> visited)
    {
        var chain = new List<int> { start, second };
        visited.Add(NormalizeEdge((start, second)));

        int prev = start, current = second;
        while (true)
        {
            int next = -1;
            foreach (var candidate in adjacency[current])
            {
                if (candidate == prev) continue;
                var key = NormalizeEdge((current, candidate));
                if (visited.Contains(key)) continue;
                next = candidate;
                visited.Add(key);
                break;
            }

            if (next == -1) break;
            chain.Add(next);
            if (next == start) break; // 루프가 닫힘
            prev = current;
            current = next;
        }

        return chain;
    }

    // 거의 일직선인 연속 구간은 코너만 남기고 중간 점을 제거한다
    static List<int> SimplifyChain(List<int> chain, Vector3[] vertices)
    {
        if (chain.Count <= 2) return chain;

        var result = new List<int> { chain[0] };
        for (int i = 1; i < chain.Count - 1; i++)
        {
            Vector3 prev = vertices[chain[i - 1]];
            Vector3 cur = vertices[chain[i]];
            Vector3 next = vertices[chain[i + 1]];

            Vector3 dirA = (cur - prev).normalized;
            Vector3 dirB = (next - cur).normalized;
            float angle = Vector3.Angle(dirA, dirB);

            if (angle >= SimplifyAngleThreshold)
                result.Add(chain[i]);
        }
        result.Add(chain[chain.Count - 1]);

        return result;
    }

    // edge 하나를 그 자리에서 얇은 박스 벽으로 만든다 (순서/연결 없이 독립적으로 생성)
    static void CreateWallSegment(Transform parent, Vector3 a, Vector3 b, int index)
    {
        Vector3 dir = b - a;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;

        float length = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
        Vector3 mid = (a + b) * 0.5f + Vector3.up * (WallHeight * 0.5f);

        var go = new GameObject($"Wall_{index}");
        go.transform.SetParent(parent);
        go.transform.position = mid;
        go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        var collider = go.AddComponent<BoxCollider>();
        collider.size = new Vector3(WallThickness, WallHeight, length);
    }
}
