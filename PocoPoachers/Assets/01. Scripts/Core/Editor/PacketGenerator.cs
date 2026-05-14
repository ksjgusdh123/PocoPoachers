using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Debug = UnityEngine.Debug; // System.Diagnostics.Debug와 충돌 방지

public class PacketGenerator
{
    private const string ToolName = "PacketGenerator";

    [MenuItem("Tools/Generator/Packets")]
    public static void Generate()
    {
        // 1. 경로 설정 (유니티 프로젝트 루트 기준)
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string flatBufferRoot = Path.Combine(projectRoot, "FlatBuffer");
        string schemasDir = Path.Combine(flatBufferRoot, "Schemas");
        string flatcExe = Path.Combine(flatBufferRoot, "flatc.exe");

        // 출력 경로 설정
        string serverOut = Path.GetFullPath(Path.Combine(projectRoot, "..", "Server", "Server", "Generated", "FlatBuffer"));
        string clientOut = Path.Combine(Application.dataPath, "01. Scripts", "Generated", "FlatBuffer");

        if (!File.Exists(flatcExe))
        {
            Debug.LogError($"[{ToolName}] flatc.exe를 찾을 수 없습니다: {flatcExe}");
            return;
        }

        if (!Directory.Exists(schemasDir))
        {
            Debug.LogError($"[{ToolName}] Schemas 폴더가 없습니다: {schemasDir}");
            return;
        }

        // 진행률 표시
        EditorUtility.DisplayProgressBar(ToolName, "Generating Packets...", 0.1f);

        try
        {
            Directory.CreateDirectory(serverOut);
            Directory.CreateDirectory(clientOut);

            var fbsFiles = Directory.GetFiles(schemasDir, "*.fbs", SearchOption.AllDirectories);
            if (fbsFiles.Length == 0)
            {
                Debug.LogWarning($"[{ToolName}] .fbs 파일을 찾을 수 없습니다.");
                return;
            }

            // 2. flatc 실행
            foreach (var fbs in fbsFiles)
            {
                RunFlatc(flatcExe, schemasDir, fbs, serverOut);
                RunFlatc(flatcExe, schemasDir, fbs, clientOut);
            }

            // 3. PacketManager 및 Handler 생성
            if (ParseMainFbs(schemasDir, out var cPackets, out var sPackets))
            {
                GeneratePacketManagers(serverOut, clientOut, cPackets, sPackets);
                GenerateHandlerStubs(schemasDir, serverOut, clientOut);
            }

            // 유니티 데이터베이스 갱신 (생성된 파일 반영)
            AssetDatabase.Refresh();
            Debug.Log($"[{ToolName}] 모든 패킷 생성이 완료되었습니다.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{ToolName}] 생성 중 오류 발생: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // --- 기존 로직 이식 (상태 메시지만 Debug.Log로 변경) ---

    private static void RunFlatc(string flatc, string schemasDir, string fbsPath, string outDir)
    {
        var psi = new ProcessStartInfo(flatc, $"--csharp --gen-object-api -I \"{schemasDir}\" -o \"{outDir}\" \"{fbsPath}\"")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi);
        string err = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (p.ExitCode != 0)
            Debug.LogError($"[{ToolName}] flatc 에러 ({Path.GetFileName(fbsPath)}): {err}");
    }

    private static bool ParseMainFbs(string schemasDir, out List<string> cPackets, out List<string> sPackets)
    {
        cPackets = new List<string>();
        sPackets = new List<string>();
        var mainFbs = Path.Combine(schemasDir, "Main.fbs");
        if (!File.Exists(mainFbs)) return false;

        bool inUnion = false;
        foreach (var line in File.ReadAllLines(mainFbs))
        {
            string t = line.Trim();
            if (t.StartsWith("union PacketType")) { inUnion = true; continue; }
            if (!inUnion) continue;
            if (t == "}") break;
            string name = t.Split("//")[0].Trim().TrimEnd(',');
            if (string.IsNullOrEmpty(name)) continue;

            if (name.StartsWith("C_")) cPackets.Add(name);
            else if (name.StartsWith("S_") || name.StartsWith("G_") || name.StartsWith("H_")) sPackets.Add(name);
        }
        return true;
    }

    private static void GeneratePacketManagers(string serverOut, string clientOut, List<string> cPackets, List<string> sPackets)
    {
        string serverPacketDir = Path.GetFullPath(Path.Combine(serverOut, "..", "..", "Net", "Packet"));
        string clientPacketDir = Path.GetFullPath(Path.Combine(clientOut, "..", "..", "Network", "Packet"));

        Directory.CreateDirectory(serverPacketDir);
        Directory.CreateDirectory(clientPacketDir);

        File.WriteAllText(Path.Combine(serverPacketDir, "PacketManager.Generated.cs"), BuildServerPacketManager(cPackets));
        File.WriteAllText(Path.Combine(clientPacketDir, "PacketManager.Generated.cs"), BuildClientPacketManager(sPackets));
    }

    private static string BuildServerPacketManager(List<string> cPackets)
    {
        int pad = cPackets.Count > 0 ? cPackets.Max(n => n.Length) : 0;
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>\n//  generated by PacketGenerator — do not modify\n// </auto-generated>\n");
        sb.AppendLine("namespace Server;\n");
        sb.AppendLine("public static partial class PacketManager\n{");
        sb.AppendLine("    static void Register()\n    {");
        foreach (var name in cPackets)
            sb.AppendLine($"        _onRecv.Add(PacketType.{name.PadRight(pad)}, _handler.On{name});");
        sb.AppendLine("    }\n}");
        return sb.ToString();
    }

    private static string BuildClientPacketManager(List<string> sPackets)
    {
        int pad = sPackets.Count > 0 ? sPackets.Max(n => n.Length) : 0;
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>\n//  generated by PacketGenerator\n// </auto-generated>\n");
        sb.AppendLine("public static partial class PacketManager\n{");
        sb.AppendLine("    static void Register()\n    {");
        foreach (var name in sPackets)
            sb.AppendLine($"        _onRecv.Add(PacketType.{name.PadRight(pad)}, PacketHandlers.On{name});");
        sb.AppendLine("    }\n}");
        return sb.ToString();
    }

    private static void GenerateHandlerStubs(string schemasDir, string serverOut, string clientOut)
    {
        string serverHandlerDir = Path.GetFullPath(Path.Combine(serverOut, "..", "..", "Net", "Packet", "PacketHandler"));
        string clientBase = Path.GetFullPath(Path.Combine(clientOut, "..", "..", "Network", "Packet"));

        var dirs = new Dictionary<string, string> {
            { "C_", serverHandlerDir },
            { "S_", Path.Combine(clientBase, "SPacketHandlers") },
            { "G_", Path.Combine(clientBase, "GPacketHandlers") },
            { "H_", Path.Combine(clientBase, "HPacketHandlers") }
        };

        foreach (var d in dirs.Values) Directory.CreateDirectory(d);

        var existingServer = CollectImplementedMethods(serverHandlerDir, "OnC_");
        var existingClient = new HashSet<string>();
        existingClient.UnionWith(CollectImplementedMethods(dirs["S_"], "OnS_"));
        existingClient.UnionWith(CollectImplementedMethods(dirs["G_"], "OnG_"));
        existingClient.UnionWith(CollectImplementedMethods(dirs["H_"], "OnH_"));

        foreach (var fbsPath in Directory.GetFiles(schemasDir, "*.fbs"))
        {
            string fileName = Path.GetFileName(fbsPath);
            if (fileName == "Main.fbs" || fileName == "Common.fbs") continue;

            string schema = Path.GetFileNameWithoutExtension(fbsPath);
            var packets = ParsePacketsInFbs(fbsPath);

            // 서버 전용 (C_)
            WriteHandler(dirs["C_"], schema, packets.Where(n => n.StartsWith("C_") && !existingServer.Contains($"On{n}")).ToList(), true);
            // 클라 전용 (S, G, H)
            WriteHandler(dirs["S_"], schema, packets.Where(n => n.StartsWith("S_") && !existingClient.Contains($"On{n}")).ToList(), false);
            WriteHandler(dirs["G_"], schema, packets.Where(n => n.StartsWith("G_") && !existingClient.Contains($"On{n}")).ToList(), false);
            WriteHandler(dirs["H_"], schema, packets.Where(n => n.StartsWith("H_") && !existingClient.Contains($"On{n}")).ToList(), false);
        }
    }

    private static List<string> ParsePacketsInFbs(string path)
    {
        return File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("table "))
            .Select(l => l["table ".Length..].TrimEnd('{').Trim())
            .ToList();
    }

    private static void WriteHandler(string dir, string schema, List<string> pkts, bool isServer)
    {
        if (pkts.Count == 0) return;
        string content = isServer ? BuildServerHandlerStubs(schema, pkts) : BuildClientHandlerStubs(schema, pkts);

        string path = Path.Combine(dir, $"PacketHandler.{schema}.cs");
        if (File.Exists(path)) path = Path.Combine(dir, $"PacketHandler.{schema}.temp.cs");

        File.WriteAllText(path, content);
    }

    private static HashSet<string> CollectImplementedMethods(string dir, string prefix)
    {
        var result = new HashSet<string>();
        if (!Directory.Exists(dir)) return result;
        foreach (var file in Directory.GetFiles(dir, "*.cs"))
        {
            if (file.EndsWith(".temp.cs")) continue;
            foreach (var line in File.ReadAllLines(file))
            {
                int idx = line.IndexOf(prefix);
                if (idx < 0 || !line.Contains('(')) continue;
                int end = line.IndexOf('(', idx);
                result.Add(line[idx..end].Trim());
            }
        }
        return result;
    }

    private static string BuildServerHandlerStubs(string schema, List<string> packets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>\nnamespace Server;\npublic partial class PacketHandler\n{");
        foreach (var p in packets) sb.AppendLine($"    public void On{p}(ClientSession session, FlatPacket root)\n    {{\n        // TODO\n    }}\n");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildClientHandlerStubs(string schema, List<string> packets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>\npublic static partial class PacketHandlers\n{");
        foreach (var p in packets) sb.AppendLine($"    public static void On{p}(FlatPacket root)\n    {{\n        // TODO\n    }}\n");
        sb.AppendLine("}");
        return sb.ToString();
    }
}