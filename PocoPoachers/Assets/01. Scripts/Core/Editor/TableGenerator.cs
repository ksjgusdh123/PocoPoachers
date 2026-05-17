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

        var types = InferTypes(headers, rows);
        var enumColumns = BuildEnumColumns(className, headers, rows);
        foreach (var pair in enumColumns)
            types[pair.Key] = pair.Value.Name;

        RegisterAndWriteEnums(enumColumns.Values, generatedEnums, clientCsOut);

        WriteClientCs(clientCsOut, fileName, className, headers, types);
        WriteJson(fileName, headers, types, rows, enumColumns, clientJsonOut);

        Debug.Log($"[{ToolName}] 변환 완료: {Path.GetFileName(csvPath)}");
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
            if (cols.Length > 0) rows.Add(cols);
        }
        return (headers, rows);
    }

    private static string[] SplitCsvLine(string line)
    {
        return line.Split(',').Select(s => s.Trim()).ToArray();
    }

    private static string[] InferTypes(string[] headers, List<string[]> rows)
    {
        var types = new string[headers.Length];
        for (int col = 0; col < headers.Length; col++)
        {
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
            if (!(header.Equals("type", StringComparison.OrdinalIgnoreCase) || header.EndsWith("_type", StringComparison.OrdinalIgnoreCase))) continue;

            var rawValues = new List<string>();
            var rawSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                string raw = col < row.Length ? row[col].Trim() : "";
                if (!string.IsNullOrWhiteSpace(raw) && rawSet.Add(raw)) rawValues.Add(raw);
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
                else obj[headers[i]] = raw;
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