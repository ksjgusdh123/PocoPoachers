using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

const string ToolName = "TableGenerator";

return Run(args);

int Run(string[] args)
{
    string dataDir        = "Data";
    string serverCsOut    = Path.Combine("..", "..", "Server", "Server", "Generated", "DataTable");
    string clientCsOut    = Path.Combine("..", "..", "PocoPoachers", "Assets", "01. Scripts", "DataTable");
    string serverJsonOut  = Path.Combine("..", "..", "Server", "Server", "Generated", "JsonData");
    string clientJsonOut  = Path.Combine("..", "..", "PocoPoachers", "Assets", "Resources", "JsonData");

    if (!Directory.Exists(dataDir) && Directory.Exists("DataTable"))
        dataDir = "DataTable";

    for (int i = 0; i < args.Length; i++)
    {
        string a = args[i];
        if (a == "--data")
        {
            if (!TryReadValue(args, ref i, out var v)) return 1;
            dataDir = v;
        }
        else if (a == "--cs-server")
        {
            if (!TryReadValue(args, ref i, out var v)) return 1;
            serverCsOut = v;
        }
        else if (a == "--cs-client")
        {
            if (!TryReadValue(args, ref i, out var v)) return 1;
            clientCsOut = v;
        }
        else if (a == "--json-server")
        {
            if (!TryReadValue(args, ref i, out var v)) return 1;
            serverJsonOut = v;
        }
        else if (a == "--json-client")
        {
            if (!TryReadValue(args, ref i, out var v)) return 1;
            clientJsonOut = v;
        }
        else
        {
            LogError($"Unknown argument: {a}");
            return 1;
        }
    }

    serverCsOut   = Path.GetFullPath(serverCsOut);
    clientCsOut   = Path.GetFullPath(clientCsOut);
    serverJsonOut = Path.GetFullPath(serverJsonOut);
    clientJsonOut = Path.GetFullPath(clientJsonOut);

    Directory.CreateDirectory(serverCsOut);
    Directory.CreateDirectory(clientCsOut);
    Directory.CreateDirectory(serverJsonOut);
    Directory.CreateDirectory(clientJsonOut);

    string dataDirFull = Path.GetFullPath(dataDir);
    if (!Directory.Exists(dataDirFull))
    {
        LogError($"data directory not found: {dataDirFull}");
        return 1;
    }

    LogPath("data (csv)", dataDirFull);
    LogPath("server (csharp)", serverCsOut);
    LogPath("client (csharp)", clientCsOut);
    LogPath("server (json)", serverJsonOut);
    LogPath("client (json)", clientJsonOut);
    Console.WriteLine();

    var generatedEnums = new Dictionary<string, EnumInfo>(StringComparer.Ordinal);

    var csvFiles = Directory.GetFiles(dataDirFull, "*.csv");
    if (csvFiles.Length == 0)
    {
        LogError("No .csv files found.");
        return 1;
    }

    foreach (var csvPath in csvFiles)
    {
        try
        {
            ProcessCsv(csvPath, generatedEnums, serverCsOut, clientCsOut, serverJsonOut, clientJsonOut);
        }
        catch (Exception ex)
        {
            LogError($"{Path.GetFileName(csvPath)}: {ex.Message}");
            return 1;
        }
    }

    Console.WriteLine($"[{ToolName}] Done.");
    return 0;
}

static bool TryReadValue(string[] args, ref int i, out string value)
{
    value = "";
    if (i + 1 >= args.Length)
    {
        LogError($"Missing value for {args[i]}");
        return false;
    }

    value = args[++i];
    return true;
}

static void LogError(string message) =>
    Console.Error.WriteLine($"[Error][{ToolName}] {message}");

static void LogPath(string key, string path) =>
    Console.WriteLine($"[{ToolName}] {key,-18} {path}");

// ────────────────────────────────────────────────
void ProcessCsv(
    string csvPath,
    Dictionary<string, EnumInfo> generatedEnums,
    string serverCsOut,
    string clientCsOut,
    string serverJsonOut,
    string clientJsonOut)
{
    string fileName  = Path.GetFileNameWithoutExtension(csvPath);
    string className = ToPascalCase(fileName);

    var (headers, rows) = ReadCsv(csvPath);
    if (headers.Length == 0 || rows.Count == 0) return;

    var types = InferTypes(headers, rows);
    var enumColumns = BuildEnumColumns(className, headers, rows);
    foreach (var pair in enumColumns)
        types[pair.Key] = pair.Value.Name;

    RegisterAndWriteEnums(enumColumns.Values, generatedEnums, serverCsOut, clientCsOut);

    WriteServerCs(serverCsOut, className, headers, types);
    WriteClientCs(clientCsOut, fileName, className, headers, types);
    WriteJson(fileName, headers, types, rows, enumColumns, serverJsonOut, clientJsonOut);

    Console.WriteLine($"[{ToolName}] table: {Path.GetFileName(csvPath)}");
}

// ── CSV 파싱 ─────────────────────────────────────
(string[] headers, List<string[]> rows) ReadCsv(string path)
{
    var lines = File.ReadAllLines(path, Encoding.UTF8);
    if (lines.Length < 2) return ([], []);

    var headers = SplitCsvLine(lines[0]);
    var rows    = new List<string[]>();
    for (int i = 1; i < lines.Length; i++)
    {
        var cols = SplitCsvLine(lines[i]);
        if (cols.Length > 0) rows.Add(cols);
    }
    return (headers, rows);
}

string[] SplitCsvLine(string line)
{
    var parts = line.Split(',');
    for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
    return parts;
}

// ── 타입 추론 ─────────────────────────────────────
string[] InferTypes(string[] headers, List<string[]> rows)
{
    var types = new string[headers.Length];
    for (int col = 0; col < headers.Length; col++)
    {
        bool allInt = true, allFloat = true;
        foreach (var row in rows)
        {
            string val = col < row.Length ? row[col] : "";
            if (!int.TryParse(val, out _))   allInt   = false;
            if (!float.TryParse(val, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out _))
                allFloat = false;
        }
        types[col] = allInt ? "int" : allFloat ? "float" : "string";
    }
    return types;
}

// ── Enum 컬럼 탐지 ────────────────────────────────
Dictionary<int, EnumInfo> BuildEnumColumns(string className, string[] headers, List<string[]> rows)
{
    var result = new Dictionary<int, EnumInfo>();
    for (int col = 0; col < headers.Length; col++)
    {
        string header = headers[col];
        if (!IsEnumColumnName(header)) continue;

        var rawValues = new List<string>();
        var rawSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            string raw = col < row.Length ? row[col].Trim() : "";
            if (!string.IsNullOrWhiteSpace(raw) && rawSet.Add(raw))
                rawValues.Add(raw);
        }
        if (rawValues.Count == 0) continue;

        string enumName = header.Equals("type", StringComparison.OrdinalIgnoreCase)
            ? $"{className}Type" : ToPascalCase(header);

        var members = new List<EnumMember> { new EnumMember("None", 0) };
        var memberSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "None" };
        var rawToValue = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rawValues.Count; i++)
        {
            string raw = rawValues[i];
            string member = ToPascalIdentifier(raw);
            if (string.IsNullOrWhiteSpace(member))
                throw new InvalidOperationException($"Enum member name is empty. ({enumName}, raw='{raw}')");
            if (!memberSet.Add(member))
                throw new InvalidOperationException($"Duplicate enum member '{member}' in {enumName}.");
            int value = i + 1;
            members.Add(new EnumMember(member, value));
            rawToValue[raw] = value;
        }
        result[col] = new EnumInfo(enumName, members, rawToValue);
    }
    return result;
}

bool IsEnumColumnName(string header) =>
    header.Equals("type", StringComparison.OrdinalIgnoreCase) ||
    header.EndsWith("_type", StringComparison.OrdinalIgnoreCase);

bool IsEnumType(string type) => type != "int" && type != "float" && type != "string";

void RegisterAndWriteEnums(IEnumerable<EnumInfo> enums, Dictionary<string, EnumInfo> registry, string serverOutDir, string clientOutDir)
{
    foreach (var enumInfo in enums)
    {
        if (registry.TryGetValue(enumInfo.Name, out var existing))
        {
            if (!EnumEquals(existing, enumInfo))
                throw new InvalidOperationException($"Enum '{enumInfo.Name}' has conflicting definitions.");
            continue;
        }
        registry[enumInfo.Name] = enumInfo;
        WriteEnumCs(serverOutDir, enumInfo);
        WriteEnumCs(clientOutDir, enumInfo);
    }
}

bool EnumEquals(EnumInfo a, EnumInfo b)
{
    if (a.Members.Count != b.Members.Count) return false;
    for (int i = 0; i < a.Members.Count; i++)
        if (a.Members[i].Name != b.Members[i].Name || a.Members[i].Value != b.Members[i].Value)
            return false;
    return true;
}

void WriteEnumCs(string outDir, EnumInfo enumInfo)
{
    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated>");
    sb.AppendLine($"public enum {enumInfo.Name} : int");
    sb.AppendLine("{");
    foreach (var member in enumInfo.Members)
        sb.AppendLine($"    {member.Name} = {member.Value},");
    sb.AppendLine("}");
    File.WriteAllText(Path.Combine(outDir, $"{enumInfo.Name}.cs"), sb.ToString(), Encoding.UTF8);
}

// ── 서버용 C# 코드 생성 ───────────────────────────
void WriteServerCs(string outDir, string className, string[] headers, string[] types)
{
    int keyIdx     = Array.FindIndex(types, t => t == "int" || IsEnumType(t));
    string keyType = keyIdx >= 0 ? types[keyIdx] : "string";
    string keyProp = keyIdx >= 0 ? ToPascalCase(headers[keyIdx]) : ToPascalCase(headers[0]);

    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated>");
    sb.AppendLine("using System.Text.Json.Serialization;");
    sb.AppendLine();
    sb.AppendLine($"public record {className}Data");
    sb.AppendLine("{");
    for (int i = 0; i < headers.Length; i++)
    {
        string prop   = ToPascalCase(headers[i]);
        string type   = types[i];
        string defVal = type == "string" ? " = \"\";" : "";
        sb.AppendLine($"    [JsonPropertyName(\"{headers[i]}\")] public {type} {prop} {{ get; init; }}{defVal}");
    }
    sb.AppendLine("}");
    File.WriteAllText(Path.Combine(outDir, $"{className}Data.cs"), sb.ToString(), Encoding.UTF8);

    sb.Clear();
    sb.AppendLine("// <auto-generated>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine("using System.Text.Json;");
    sb.AppendLine();
    sb.AppendLine($"public static class {className}Table");
    sb.AppendLine("{");
    sb.AppendLine($"    static readonly Dictionary<{keyType}, {className}Data> _map = new();");
    sb.AppendLine();
    sb.AppendLine($"    public static void Load(string json)");
    sb.AppendLine("    {");
    sb.AppendLine($"        var list = JsonSerializer.Deserialize<List<{className}Data>>(json)!;");
    sb.AppendLine("        _map.Clear();");
    sb.AppendLine($"        foreach (var d in list) _map[d.{keyProp}] = d;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine($"    public static {className}Data? Get({keyType} key) => _map.GetValueOrDefault(key);");
    sb.AppendLine($"    public static IReadOnlyCollection<{className}Data> All => _map.Values;");
    sb.AppendLine("}");
    File.WriteAllText(Path.Combine(outDir, $"{className}Table.cs"), sb.ToString(), Encoding.UTF8);
}

// ── Unity 클라이언트용 C# 코드 생성 ──────────────
void WriteClientCs(string outDir, string fileName, string className, string[] headers, string[] types)
{
    int keyIdx      = Array.FindIndex(types, t => t == "int" || IsEnumType(t));
    string keyType  = keyIdx >= 0 ? types[keyIdx] : "string";
    string keyField = keyIdx >= 0 ? headers[keyIdx] : headers[0];
    string resPath  = $"JsonData/{fileName}";  // Resources/JsonData/{fileName}

    // *Data.cs
    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated>");
    sb.AppendLine("using System;");
    bool needsUnityEngine = false;
    bool hasPrefabString = false;
    for (int i = 0; i < headers.Length; i++)
    {
        if (headers[i].Equals("icon", StringComparison.OrdinalIgnoreCase) && types[i] == "string")
        {
            needsUnityEngine = true;
        }
        if (headers[i].Equals("prefab", StringComparison.OrdinalIgnoreCase) && types[i] == "string")
        {
            needsUnityEngine = true;
            hasPrefabString = true;
        }
    }
    if (needsUnityEngine)
        sb.AppendLine("using UnityEngine;");
    bool hasIconString = false;
    for (int i = 0; i < headers.Length; i++)
    {
        if (headers[i].Equals("icon", StringComparison.OrdinalIgnoreCase) && types[i] == "string")
        {
            hasIconString = true;
            break;
        }
    }
    if (hasIconString)
        sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine();
    sb.AppendLine("[Serializable]");
    sb.AppendLine($"public partial class {className}Data");
    sb.AppendLine("{");
    for (int i = 0; i < headers.Length; i++)
    {
        string type   = types[i];
        string defVal = type == "string" ? " = \"\"" : "";
        sb.AppendLine($"    public {type} {headers[i]}{defVal};");
    }
    sb.AppendLine();

    // Compatibility properties (PascalCase + legacy ItemName/ItemType) while keeping JsonUtility-friendly public fields above.
    for (int i = 0; i < headers.Length; i++)
    {
        string type = types[i];
        string rawField = headers[i];
        string propName = ToPascalCase(rawField);

        if (rawField.Equals("icon", StringComparison.OrdinalIgnoreCase) && type == "string")
        {
            sb.AppendLine("    public string IconPath");
            sb.AppendLine("    {");
            sb.AppendLine($"        get => {rawField};");
            sb.AppendLine($"        set => {rawField} = value;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    static readonly Dictionary<string, Sprite> _iconSpriteCache = new Dictionary<string, Sprite>();");
            sb.AppendLine();
            sb.AppendLine("    public Sprite Icon");
            sb.AppendLine("    {");
            sb.AppendLine("        get");
            sb.AppendLine("        {");
            sb.AppendLine("            if (string.IsNullOrWhiteSpace(icon))");
            sb.AppendLine("                return null;");
            sb.AppendLine("            if (_iconSpriteCache.TryGetValue(icon, out var cached))");
            sb.AppendLine("                return cached;");
            sb.AppendLine();
            sb.AppendLine("            var sprite = Resources.Load<Sprite>(icon);");
            sb.AppendLine("            if (sprite != null)");
            sb.AppendLine("            {");
            sb.AppendLine("                _iconSpriteCache[icon] = sprite;");
            sb.AppendLine("                return sprite;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            var tex = Resources.Load<Texture2D>(icon);");
            sb.AppendLine("            if (tex == null)");
            sb.AppendLine("                return null;");
            sb.AppendLine();
            sb.AppendLine("            var created = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);");
            sb.AppendLine("            _iconSpriteCache[icon] = created;");
            sb.AppendLine("            return created;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            continue;
        }

        sb.AppendLine($"    public {type} {propName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        get => {rawField};");
        sb.AppendLine($"        set => {rawField} = value;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (rawField.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            string legacyName = $"{className}Name";
            sb.AppendLine($"    public {type} {legacyName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        get => {propName};");
            sb.AppendLine($"        set => {propName} = value;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        else if (rawField.Equals("type", StringComparison.OrdinalIgnoreCase))
        {
            string legacyType = $"{className}Type";
            sb.AppendLine($"    public {type} {legacyType}");
            sb.AppendLine("    {");
            sb.AppendLine($"        get => {propName};");
            sb.AppendLine($"        set => {propName} = value;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    if (hasPrefabString)
    {
        sb.AppendLine("    public GameObject LoadPrefab()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (string.IsNullOrWhiteSpace(prefab))");
        sb.AppendLine("            return null;");
        sb.AppendLine("        return Resources.Load<GameObject>(prefab);");
        sb.AppendLine("    }");
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
    sb.AppendLine();
    sb.AppendLine($"    readonly Dictionary<{keyType}, {className}Data> _map = new Dictionary<{keyType}, {className}Data>();");
    sb.AppendLine();
    sb.AppendLine($"    static {className}Table Load()");
    sb.AppendLine("    {");
    sb.AppendLine($"        var asset = Resources.Load<TextAsset>(\"{resPath}\");");
    sb.AppendLine($"        if (asset == null) {{ Debug.LogError(\"[{className}Table] not found: Resources/{resPath}.json\"); return new {className}Table(); }}");
    sb.AppendLine($"        var table = new {className}Table();");
    sb.AppendLine($"        string wrapped = \"{{\\\"items\\\":\" + asset.text + \"}}\";");
    sb.AppendLine($"        var wrapper = JsonUtility.FromJson<Wrapper>(wrapped);");
    sb.AppendLine($"        if (wrapper == null || wrapper.items == null) {{ Debug.LogError(\"[{className}Table] JSON 파싱 실패\"); return table; }}");
    sb.AppendLine($"        foreach (var d in wrapper.items) table._map[d.{keyField}] = d;");
    sb.AppendLine($"        Debug.Log($\"[{className}Table] {{table._map.Count}}개 로드 완료\");");
    sb.AppendLine("        return table;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine($"    public {className}Data Get({keyType} key) => _map.TryGetValue(key, out var v) ? v : null;");
    sb.AppendLine($"    public IReadOnlyCollection<{className}Data> All => _map.Values;");
    sb.AppendLine();
    sb.AppendLine("    [System.Serializable]");
    sb.AppendLine($"    class Wrapper {{ public List<{className}Data> items; }}");
    sb.AppendLine("}");
    File.WriteAllText(Path.Combine(outDir, $"{className}Table.cs"), sb.ToString(), Encoding.UTF8);
}

// ── JSON 생성 (서버 + 클라 공용) ──────────────────
void WriteJson(
    string fileName,
    string[] headers,
    string[] types,
    List<string[]> rows,
    Dictionary<int, EnumInfo> enumColumns,
    string serverJsonOut,
    string clientJsonOut)
{
    var array = new JsonArray();
    foreach (var row in rows)
    {
        var obj = new JsonObject();
        for (int i = 0; i < headers.Length; i++)
        {
            string raw = i < row.Length ? row[i] : "";
            if (enumColumns.TryGetValue(i, out var enumInfo))
            {
                if (string.IsNullOrWhiteSpace(raw)) obj[headers[i]] = 0;
                else if (enumInfo.RawToValue.TryGetValue(raw, out int ev)) obj[headers[i]] = ev;
                else throw new InvalidOperationException($"Unknown enum value '{raw}' in {fileName}.csv column '{headers[i]}'.");
            }
            else if (types[i] == "int" && int.TryParse(raw, out int iv))
                obj[headers[i]] = iv;
            else if (types[i] == "float" && float.TryParse(raw,
                     System.Globalization.NumberStyles.Float,
                     System.Globalization.CultureInfo.InvariantCulture, out float fv))
                obj[headers[i]] = fv;
            else
                obj[headers[i]] = raw;
        }
        array.Add(obj);
    }

    var opts = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    string json = array.ToJsonString(opts);

    // 서버와 클라 동일한 JSON 사용
    File.WriteAllText(Path.Combine(serverJsonOut, $"{fileName}.json"), json, Encoding.UTF8);
    File.WriteAllText(Path.Combine(clientJsonOut,  $"{fileName}.json"), json, Encoding.UTF8);
}

// ── 유틸 ──────────────────────────────────────────
string ToPascalCase(string s)
{
    var sb = new StringBuilder();
    bool upper = true;
    foreach (char c in s)
    {
        if (c == '_') { upper = true; continue; }
        sb.Append(upper ? char.ToUpper(c) : c);
        upper = false;
    }
    return sb.ToString();
}

string ToPascalIdentifier(string s)
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

record EnumMember(string Name, int Value);
record EnumInfo(string Name, List<EnumMember> Members, Dictionary<string, int> RawToValue);
