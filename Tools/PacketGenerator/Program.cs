using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PocoPoachers.Tools.PacketGenerator;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 1 && (args[0] == "-h" || args[0] == "--help"))
        {
            PrintHelp();
            return 0;
        }

        string? outputOverride = null;
        var paths = new List<string>();
        var all = false;
        var interactive = false;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "-o" or "--output")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("-o needs directory");
                    return 1;
                }
                outputOverride = args[++i];
                continue;
            }
            if (a is "-a" or "--all")
            {
                all = true;
                continue;
            }
            if (a is "-i" or "--interactive")
            {
                interactive = true;
                continue;
            }
            if (a.StartsWith('-'))
            {
                Console.Error.WriteLine($"unknown: {a}");
                PrintHelp();
                return 1;
            }
            paths.Add(a);
        }

        string repoRoot;
        try
        {
            repoRoot = FindRepoRoot();
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var schemasDir = Path.Combine(repoRoot, "Tools", "PacketGenerator", "Schemas");
        if (!Directory.Exists(schemasDir))
        {
            Console.Error.WriteLine($"no Schemas: {schemasDir}");
            return 1;
        }

        var fbsList = Directory.EnumerateFiles(schemasDir, "*.fbs", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (fbsList.Count == 0)
        {
            Console.Error.WriteLine("no .fbs");
            return 1;
        }

        var outputDir = string.IsNullOrEmpty(outputOverride)
            ? Path.Combine(repoRoot, "Shared", "Generated")
            : Path.GetFullPath(outputOverride);

        var flatc = ResolveFlatc(repoRoot);
        if (flatc != "flatc" && !File.Exists(flatc))
        {
            Console.Error.WriteLine($"no flatc: {flatc}");
            return 1;
        }

        List<string> selected;
        if (all)
        {
            selected = fbsList;
        }
        else if (interactive || paths.Count == 0)
        {
            selected = PromptSelection(fbsList, schemasDir);
            if (selected.Count == 0)
            {
                Console.WriteLine("cancel");
                return 0;
            }
        }
        else
        {
            selected = [];
            foreach (var p in paths)
            {
                var full = Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), p));
                if (File.Exists(full) && full.EndsWith(".fbs", StringComparison.OrdinalIgnoreCase))
                {
                    selected.Add(full);
                    continue;
                }
                var rel = Path.Combine(schemasDir, p.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (File.Exists(rel))
                {
                    selected.Add(rel);
                    continue;
                }
                var byName = fbsList.FirstOrDefault(f =>
                    string.Equals(Path.GetFileName(f), p, StringComparison.OrdinalIgnoreCase));
                if (byName is not null)
                {
                    selected.Add(byName);
                    continue;
                }
                Console.Error.WriteLine($"not found: {p}");
                return 1;
            }
        }

        Directory.CreateDirectory(outputDir);
        var failed = 0;
        foreach (var fbs in selected.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine(Path.GetRelativePath(repoRoot, fbs));
            var code = RunFlatc(flatc, outputDir, fbs, repoRoot);
            if (code != 0)
            {
                Console.Error.WriteLine($"fail {code}");
                failed++;
            }
            else
                Console.WriteLine(outputDir);
        }

        return failed > 0 ? 1 : 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            "dotnet run --project Tools/PacketGenerator -- [-a|-i] [-o dir] [*.fbs ...]\n" +
            "  -a  all   -i  pick   -o  out (default Shared/Generated)   -h  help");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var packetGenProj = Path.Combine(dir.FullName, "Tools", "PacketGenerator", "PacketGenerator.csproj");
            if (File.Exists(packetGenProj) && Directory.Exists(Path.Combine(dir.FullName, "Shared")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found (need Tools/PacketGenerator/PacketGenerator.csproj and Shared/).");
    }

    private static string ResolveFlatc(string repoRoot)
    {
        var generatorDir = Path.Combine(repoRoot, "Tools", "PacketGenerator");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var bundled = Path.Combine(generatorDir, "flatc.exe");
            if (File.Exists(bundled))
                return bundled;
        }
        else
        {
            var bundled = Path.Combine(generatorDir, "flatc");
            if (File.Exists(bundled))
                return bundled;
        }

        return "flatc";
    }

    private static List<string> PromptSelection(List<string> fbsList, string schemasDir)
    {
        for (var i = 0; i < fbsList.Count; i++)
            Console.WriteLine($"[{i}] {Path.GetRelativePath(schemasDir, fbsList[i])}");
        Console.WriteLine("[a] all");
        Console.Write("> ");
        var line = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(line))
            return [];

        var t = line.Trim();
        if (t.Equals("a", StringComparison.OrdinalIgnoreCase) || t == "*")
            return fbsList;

        var result = new List<string>();
        foreach (var part in t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var idx) || idx < 0 || idx >= fbsList.Count)
            {
                Console.Error.WriteLine($"bad index: {part}");
                return [];
            }
            result.Add(fbsList[idx]);
        }
        return result;
    }

    private static int RunFlatc(string flatcExe, string outputDir, string schemaPath, string workingDir)
    {
        using var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = flatcExe,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        p.StartInfo.ArgumentList.Add("--csharp");
        p.StartInfo.ArgumentList.Add("-o");
        p.StartInfo.ArgumentList.Add(outputDir);
        p.StartInfo.ArgumentList.Add(schemaPath);
        p.Start();
        var err = p.StandardError.ReadToEnd();
        var std = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        if (!string.IsNullOrEmpty(std))
            Console.Write(std);
        if (!string.IsNullOrEmpty(err))
            Console.Error.Write(err);
        return p.ExitCode;
    }
}
