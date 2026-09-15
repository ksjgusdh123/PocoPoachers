using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

public class TableGeneratorTool
{
    private const string ToolName = "TableGenerator";

    [MenuItem("Tools/Generator/Tables")]
    public static void Generate()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        string dataDir = Path.Combine(projectRoot, "DataTable");
        if (!Directory.Exists(dataDir)) dataDir = Path.Combine(projectRoot, "Data");

        string clientCsOut = Path.Combine(Application.dataPath, "01. Scripts", "Generated", "DataTable");
        string clientJsonOut = Path.Combine(Application.dataPath, "_Data", "Resources", "JsonData");

        if (!Directory.Exists(dataDir))
        {
            Debug.LogError($"[{ToolName}] CSV 데이터 폴더를 찾을 수 없습니다: {dataDir}");
            return;
        }

        EditorUtility.DisplayProgressBar(ToolName, "Processing CSV Tables...", 0.1f);

        try
        {
            Directory.CreateDirectory(clientCsOut);
            Directory.CreateDirectory(clientJsonOut);

            var csvFiles = Directory.GetFiles(dataDir, "*.csv");
            if (csvFiles.Length == 0)
            {
                Debug.LogWarning($"[{ToolName}] .csv 파일이 없습니다.");
                return;
            }

            var generatedEnums = new Dictionary<string, EnumInfo>(StringComparer.Ordinal);

            foreach (var csvPath in csvFiles)
            {
                ProcessCsv(csvPath, generatedEnums, clientCsOut, clientJsonOut);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[{ToolName}] 테이블 생성이 완료되었습니다. (JSON & C#)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{ToolName}] 오류 발생: {ex.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void ProcessCsv(string csvPath, Dictionary<string, EnumInfo> generatedEnums, string clientCsOut, string clientJsonOut)
    {
        string fileName = Path.GetFileNameWithoutExtension(csvPath);
        string className = ToPascalCase(fileName);

        var (headers, rows) = ReadCsv(csvPath);
        if (headers.Length == 0 || rows.Count == 0) return;

        (headers, rows) = StripDesignerColumns(headers, rows);
        if (headers.Length == 0) return;

        if (fileName == "enemy_drop") ValidateEnemyDrops(csvPath, headers, rows);
        if (fileName == "quest") { ValidateQuestPrerequisites(headers, rows); ValidateQuestDialogues(csvPath, headers, rows); }
        var types = InferTypes(headers, rows);
        if (fileName == "quest") types[Array.IndexOf(headers, "prerequisite_quest_ids")] = "string";
        if (fileName == "enemy_drop")
            foreach (string column in new[] { "item_ids", "drop_chances", "min_counts", "max_counts" })
                types[Array.IndexOf(headers, column)] = "string";
        var enumColumns = BuildEnumColumns(className, headers, rows);
        foreach (var pair in enumColumns)
            types[pair.Key] = pair.Value.Name;

        RegisterAndWriteEnums(enumColumns.Values, generatedEnums, clientCsOut);

        WriteClientCs(clientCsOut, fileName, className, headers, types);
        WriteJson(fileName, headers, types, rows, enumColumns, clientJsonOut);

        Debug.Log($"[{ToolName}] 변환 완료: {Path.GetFileName(csvPath)}");
    }

    private static void ValidateQuestPrerequisites(string[] headers, List<string[]> rows)
    {
        int idColumn = Array.IndexOf(headers, "id");
        int modeColumn = Array.IndexOf(headers, "progress_mode");
        int prerequisiteColumn = Array.IndexOf(headers, "prerequisite_quest_ids");
        if (idColumn < 0 || modeColumn < 0 || prerequisiteColumn < 0)
            throw new FormatException("quest.csv: id, progress_mode, prerequisite_quest_ids 컬럼이 필요합니다.");
        var graph = new Dictionary<int, List<int>>();
        var modes = new Dictionary<int, string>();
        foreach (var row in rows)
        {
            if (row.Length != headers.Length || !int.TryParse(row[idColumn], out int id) || id <= 0 || graph.ContainsKey(id))
                throw new FormatException("quest.csv: 컬럼 수 또는 퀘스트 ID가 잘못되었거나 중복입니다.");
            var prerequisites = new List<int>();
            if (!string.IsNullOrWhiteSpace(row[prerequisiteColumn]))
                foreach (string token in row[prerequisiteColumn].Split(';'))
                {
                    if (!int.TryParse(token.Trim(), out int prior) || prior == id || prerequisites.Contains(prior))
                        throw new FormatException($"quest.csv {id}: 선행 ID가 잘못되었거나 자기 자신/중복입니다.");
                    prerequisites.Add(prior);
                }
            graph.Add(id, prerequisites);
            modes.Add(id, row[modeColumn].Trim());
        }
        foreach (var pair in graph)
            foreach (int prior in pair.Value)
            {
                if (!graph.ContainsKey(prior)) throw new FormatException($"quest.csv {pair.Key}: 없는 선행 퀘스트 {prior}");
                if (string.Equals(modes[pair.Key], "Shared", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(modes[prior], "Personal", StringComparison.OrdinalIgnoreCase))
                    throw new FormatException($"quest.csv {pair.Key}: 공유 퀘스트의 선행 조건으로 개인 퀘스트를 지정할 수 없습니다.");
            }
        var visiting = new HashSet<int>();
        var done = new HashSet<int>();
        void Visit(int id)
        {
            if (done.Contains(id)) return;
            if (!visiting.Add(id)) throw new FormatException($"quest.csv {id}: 선행 퀘스트가 순환합니다.");
            foreach (int prior in graph[id]) Visit(prior);
            visiting.Remove(id);
            done.Add(id);
        }
        foreach (int id in graph.Keys) Visit(id);
    }

    // 제안/진행/완료 대사는 DialogueUI가 퀘스트 메뉴에서 바로 타고 들어간다 - 없는 id를 가리키면 대화가 조용히 끝난다.
    private static void ValidateQuestDialogues(string path, string[] headers, List<string[]> rows)
    {
        string[] columns = { "offer_dialogue_id", "progress_dialogue_id", "complete_dialogue_id" };
        foreach (string column in columns)
            if (Array.IndexOf(headers, column) < 0) throw new FormatException($"quest.csv: {column} 컬럼이 필요합니다.");

        var dialogueIds = new HashSet<int>();
        var npcByDialogue = new Dictionary<int, int>();
        string dialoguePath = Path.Combine(Path.GetDirectoryName(path), "dialogue.csv");
        if (!File.Exists(dialoguePath)) return;

        var (dialogueHeaders, dialogueRows) = ReadCsv(dialoguePath);
        int dialogueIdColumn = Array.IndexOf(dialogueHeaders, "id");
        int dialogueNpcColumn = Array.IndexOf(dialogueHeaders, "npc_id");
        if (dialogueIdColumn < 0) return;
        foreach (var row in dialogueRows)
        {
            if (dialogueIdColumn >= row.Length || !int.TryParse(row[dialogueIdColumn], out int id)) continue;
            dialogueIds.Add(id);
            if (dialogueNpcColumn >= 0 && dialogueNpcColumn < row.Length && int.TryParse(row[dialogueNpcColumn], out int npc))
                npcByDialogue[id] = npc;
        }

        int npcColumn = Array.IndexOf(headers, "npc_id");
        foreach (var row in rows)
        {
            int questId = int.Parse(row[Array.IndexOf(headers, "id")]);
            foreach (string column in columns)
            {
                string raw = row[Array.IndexOf(headers, column)].Trim();
                if (string.IsNullOrEmpty(raw) || raw == "0") continue;
                if (!int.TryParse(raw, out int dialogueId) || !dialogueIds.Contains(dialogueId))
                    throw new FormatException($"quest.csv {questId}: {column}가 가리키는 대사 {raw}가 dialogue.csv에 없습니다.");
                if (npcColumn >= 0 && int.TryParse(row[npcColumn], out int questNpc)
                    && npcByDialogue.TryGetValue(dialogueId, out int dialogueNpc) && questNpc != dialogueNpc)
                    throw new FormatException($"quest.csv {questId}: {column}의 대사 {dialogueId}는 npc_id가 {dialogueNpc}라 퀘스트 npc_id {questNpc}와 다릅니다.");
            }
        }
    }

    private static void ValidateEnemyDrops(string path, string[] headers, List<string[]> rows)
    {
        string[] required = { "enemy_id", "item_ids", "drop_chances", "min_counts", "max_counts" };
        foreach (var column in required)
            if (Array.IndexOf(headers, column) < 0) throw new FormatException($"enemy_drop.csv: {column} 컬럼이 없습니다.");
        var directory = Path.GetDirectoryName(path);
        HashSet<int> ReadIds(string name)
        {
            var (sourceHeaders, sourceRows) = ReadCsv(Path.Combine(directory, name + ".csv"));
            int index = Array.IndexOf(sourceHeaders, "id");
            return new HashSet<int>(sourceRows.Select(row => int.Parse(row[index])));
        }
        var enemyIds = ReadIds("enemy");
        var itemIds = ReadIds("item");
        var seen = new HashSet<int>();
        for (int i = 0; i < rows.Count; i++)
        {
            try
            {
                var row = rows[i];
                if (row.Length != headers.Length) throw new FormatException("컬럼 개수가 다릅니다.");
                string Value(string column) => row[Array.IndexOf(headers, column)];
                if (!int.TryParse(Value("enemy_id"), out int id) || !enemyIds.Contains(id) || !seen.Add(id))
                    throw new FormatException("적 ID가 없거나 중복되었습니다.");
                var rules = EnemyDropRules.Parse(Value("item_ids"), Value("drop_chances"), Value("min_counts"), Value("max_counts"));
                foreach (int itemId in rules.ids)
                    if (!itemIds.Contains(itemId)) throw new FormatException($"없는 아이템 ID: {itemId}");
            }
            catch (FormatException ex) { throw new FormatException($"enemy_drop.csv {i + 2}행: {ex.Message}"); }
        }
    }

    // 헤더가 '_'로 시작하는 컬럼은 기획용 메모로 간주하고 C#/JSON 생성에서 제외한다.
    private static (string[] headers, List<string[]> rows) StripDesignerColumns(string[] headers, List<string[]> rows)
    {
        var keepCols = Enumerable.Range(0, headers.Length).Where(i => !headers[i].StartsWith("_")).ToArray();
        if (keepCols.Length == headers.Length) return (headers, rows);

        var newHeaders = keepCols.Select(i => headers[i]).ToArray();
        var newRows = rows.Select(row => keepCols.Select(i => i < row.Length ? row[i] : "").ToArray()).ToList();
        return (newHeaders, newRows);
    }

    private static (string[] headers, List<string[]> rows) ReadCsv(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2) return (new string[0], new List<string[]>());

        var headers = SplitCsvLine(lines[0]);
        var rows = new List<string[]>();
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            if (cols.Length == 0) continue;

            if (cols.Length != headers.Length)
                Debug.LogWarning($"[{ToolName}] {Path.GetFileName(path)} {i + 1}번째 줄의 컬럼이 {headers.Length}개가 아니라 {cols.Length}개입니다. " +
                                 "값에 쉼표가 들어갔다면 그 칸을 큰따옴표로 감싸세요.");

            rows.Add(cols);
        }
        return (headers, rows);
    }

    // 따옴표로 감싼 필드 안의 쉼표는 구분자가 아니다. 대사·설명문에는 쉼표가 흔한데
    // 그냥 Split(',')으로 자르면 그 행만 컬럼이 늘어나 뒤쪽 값이 통째로 밀리고,
    // 숫자 컬럼에 문장이 들어가 InferTypes가 int를 string으로 판정해버린다.
    // 필드 안에 따옴표를 쓰려면 ""로 두 번 적는다(RFC 4180 — 엑셀이 저장할 때 쓰는 규칙과 같다).
    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c != '"') { field.Append(c); continue; }

                // ""는 따옴표 한 글자, 홀로 선 "는 인용 종료
                if (i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
                else inQuotes = false;
                continue;
            }

            if (c == '"') inQuotes = true;
            else if (c == ',') { fields.Add(field.ToString().Trim()); field.Clear(); }
            else field.Append(c);
        }

        fields.Add(field.ToString().Trim());
        return fields.ToArray();
    }

    private static string[] InferTypes(string[] headers, List<string[]> rows)
    {
        var types = new string[headers.Length];
        for (int col = 0; col < headers.Length; col++)
        {
            // ID 후보 목록은 값이 하나만 있어도 문자열 타입을 유지한다.
            if (headers[col].EndsWith("_item_ids", StringComparison.OrdinalIgnoreCase))
            {
                types[col] = "string";
                continue;
            }
            bool allInt = true, allFloat = true;
            foreach (var row in rows)
            {
                string val = col < row.Length ? row[col] : "";
                if (!int.TryParse(val, out _)) allInt = false;
                if (!float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                    allFloat = false;
            }
            types[col] = allInt ? "int" : allFloat ? "float" : "string";
        }
        return types;
    }

    private static Dictionary<int, EnumInfo> BuildEnumColumns(string className, string[] headers, List<string[]> rows)
    {
        var result = new Dictionary<int, EnumInfo>();
        for (int col = 0; col < headers.Length; col++)
        {
            string header = headers[col];
            if (!(header.Equals("type", StringComparison.OrdinalIgnoreCase)
                || header.EndsWith("_type", StringComparison.OrdinalIgnoreCase)
                || header.EndsWith("_mode", StringComparison.OrdinalIgnoreCase))) continue;

            var rawValues = new List<string>();
            var rawSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                string raw = col < row.Length ? row[col].Trim() : "";
                if (!string.IsNullOrWhiteSpace(raw) && rawSet.Add(raw)) rawValues.Add(raw);
            }
            // 퀘스트 모드는 현재 CSV에 한 종류만 있어도 두 값을 고정 순서로 생성한다.
            if (className == "Quest" && header == "progress_mode")
            {
                if (rows.Any(row => col >= row.Length ||
                    (!string.Equals(row[col].Trim(), "Shared", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(row[col].Trim(), "Personal", StringComparison.OrdinalIgnoreCase))))
                    throw new FormatException("quest.csv progress_mode는 Shared 또는 Personal이어야 합니다.");
                rawValues = new List<string> { "Shared", "Personal" };
            }
            if (rawValues.Count == 0) continue;

            string enumName = header.Equals("type", StringComparison.OrdinalIgnoreCase) ? $"{className}Type" : ToPascalCase(header);
            var members = new List<EnumMember> { new EnumMember("None", 0) };
            var rawToValue = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["None"] = 0 };

            int nextValue = 1;
            foreach (string raw in rawValues)
            {
                if (raw.Equals("None", StringComparison.OrdinalIgnoreCase)) continue;
                string member = ToPascalIdentifier(raw);
                members.Add(new EnumMember(member, nextValue));
                rawToValue[raw] = nextValue++;
            }
            result[col] = new EnumInfo(enumName, members, rawToValue);
        }
        return result;
    }

    private static void RegisterAndWriteEnums(IEnumerable<EnumInfo> enums, Dictionary<string, EnumInfo> registry, string clientOutDir)
    {
        foreach (var enumInfo in enums)
        {
            if (registry.ContainsKey(enumInfo.Name)) continue;
            registry[enumInfo.Name] = enumInfo;

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>\npublic enum " + enumInfo.Name + " : int\n{");
            foreach (var m in enumInfo.Members) sb.AppendLine($"    {m.Name} = {m.Value},");
            sb.AppendLine("}");
            File.WriteAllText(Path.Combine(clientOutDir, $"{enumInfo.Name}.cs"), sb.ToString(), Encoding.UTF8);
        }
    }

    private static void WriteClientCs(string outDir, string fileName, string className, string[] headers, string[] types)
    {
        int keyIdx = Array.FindIndex(types, t => t == "int");
        if (fileName == "enemy_drop") keyIdx = Array.IndexOf(headers, "enemy_id");
        string keyType = keyIdx >= 0 ? types[keyIdx] : "string";
        string keyField = keyIdx >= 0 ? headers[keyIdx] : headers[0];
        string resPath = $"JsonData/{fileName}";  // Resources/JsonData/{fileName}

        // *Data.cs
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("[Serializable]");
        sb.AppendLine($"public partial class {className}Data");
        sb.AppendLine("{");
        for (int i = 0; i < headers.Length; i++)
        {
            string type = types[i];
            string defVal = type == "string" ? " = \"\"" : "";
            sb.AppendLine($"    public {type} {headers[i]}{defVal};");
        }
        sb.AppendLine();

        for (int i = 0; i < headers.Length; i++)
        {
            string type = types[i];
            string rawField = headers[i];
            string propName = ToPascalCase(rawField);

            // 1. Icon 필드는 IconPath 프로퍼티만 생성
            if (rawField.Equals("icon", StringComparison.OrdinalIgnoreCase) && type == "string")
            {
                sb.AppendLine($"    public string IconPath {{ get => {rawField}; set => {rawField} = value; }}");
                sb.AppendLine();
                continue;
            }

            // 2. 일반 필드 처리 (PascalCase 프로퍼티 생성)
            sb.AppendLine($"    public {type} {propName} {{ get => {rawField}; set => {rawField} = value; }}");

            // 3. 레거시 호환용 프로퍼티 (ItemName, ItemType 등)
            if (rawField.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"    public {type} {className}Name {{ get => {propName}; set => {propName} = value; }}");
            }
            else if (rawField.Equals("type", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"    public {type} {className}Type {{ get => {propName}; set => {propName} = value; }}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(outDir, $"{className}Data.cs"), sb.ToString(), Encoding.UTF8);

        // *Table.cs — JSON 로드
        sb.Clear();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine($"public class {className}Table");
        sb.AppendLine("{");
        sb.AppendLine($"    static {className}Table _instance;");
        sb.AppendLine($"    public static {className}Table Instance => _instance ??= Load();");
        sb.AppendLine($"    readonly Dictionary<{keyType}, {className}Data> _map = new Dictionary<{keyType}, {className}Data>();");
        sb.AppendLine();
        sb.AppendLine($"    static {className}Table Load()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var asset = ResourceManager.Instance.Load<TextAsset>(\"{resPath}\");");
        sb.AppendLine($"        if (asset == null) {{ Debug.LogError(\"[{className}Table] not found: Resources/{resPath}.json\"); return new {className}Table(); }}");
        sb.AppendLine($"        var table = new {className}Table();");
        sb.AppendLine($"        string wrapped = \"{{\\\"items\\\":\" + asset.text + \"}}\";");
        sb.AppendLine($"        var wrapper = JsonUtility.FromJson<Wrapper>(wrapped);");
        sb.AppendLine($"        if (wrapper == null || wrapper.items == null) return table;");
        sb.AppendLine($"        foreach (var d in wrapper.items) table._map[d.{keyField}] = d;");
        sb.AppendLine("        return table;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public {className}Data Get({keyType} key) => _map.TryGetValue(key, out var v) ? v : null;");
        sb.AppendLine($"    public IReadOnlyCollection<{className}Data> All => _map.Values;");
        sb.AppendLine();
        sb.AppendLine($"    [System.Serializable] class Wrapper {{ public List<{className}Data> items = new List<{className}Data>(); }}");
        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(outDir, $"{className}Table.cs"), sb.ToString(), Encoding.UTF8);
    }

    private static void WriteJson(string fileName, string[] headers, string[] types, List<string[]> rows, Dictionary<int, EnumInfo> enumColumns, string clientJsonOut)
    {
        var list = new List<Dictionary<string, object>>();
        foreach (var row in rows)
        {
            var obj = new Dictionary<string, object>();
            for (int i = 0; i < headers.Length; i++)
            {
                string raw = i < row.Length ? row[i] : "";
                if (enumColumns.TryGetValue(i, out var enumInfo))
                {
                    obj[headers[i]] = enumInfo.RawToValue.TryGetValue(raw, out int ev) ? ev : 0;
                }
                else if (types[i] == "int" && int.TryParse(raw, out int iv)) obj[headers[i]] = iv;
                else if (types[i] == "float" && float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fv)) obj[headers[i]] = fv;
                else obj[headers[i]] = raw.Replace("\\n", "\n");
            }
            list.Add(obj);
        }

        string json = JsonConvert.SerializeObject(list, Formatting.Indented);
        File.WriteAllText(Path.Combine(clientJsonOut, $"{fileName}.json"), json, Encoding.UTF8);
    }

    private static string ToPascalCase(string s) => string.Concat(s.Split('_').Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1) : p));
    private static string ToPascalIdentifier(string s)
    {
        var sb = new StringBuilder();
        bool upper = true;
        foreach (char c in s.Trim())
        {
            if (!char.IsLetterOrDigit(c)) { upper = true; continue; }
            if (sb.Length == 0 && char.IsDigit(c)) sb.Append('_');
            sb.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }
        return sb.ToString();
    }

    class EnumMember { public string Name; public int Value; public EnumMember(string n, int v) { Name = n; Value = v; } }
    class EnumInfo { public string Name; public List<EnumMember> Members; public Dictionary<string, int> RawToValue; public EnumInfo(string n, List<EnumMember> m, Dictionary<string, int> r) { Name = n; Members = m; RawToValue = r; } }
}
