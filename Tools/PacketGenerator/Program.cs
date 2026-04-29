using System.Diagnostics;

const string ToolName = "PacketGenerator";

return Run(args);

static int Run(string[] args)
{
    string schemasDir = Path.GetFullPath("Schemas");
    string flatcExe   = Path.GetFullPath("flatc.exe");
    string serverOut  = Path.GetFullPath(Path.Combine("..", "..", "Server", "Server", "Generated", "FlatBuffer"));
    string clientOut  = Path.GetFullPath(Path.Combine("..", "..", "PocoPoachers", "Assets", "01. Scripts", "Generated", "FlatBuffer"));

    for (int i = 0; i < args.Length; i++)
    {
        string a = args[i];
        if (a == "--schemas")
        {
            if (!TryReadValue(args, ref i, out var v)) return 1;
            schemasDir = v;
        }
        else if (a == "--server")
        {
            if (!TryReadValue(args, ref i, out var v)) return 1;
            serverOut = v;
        }
        else if (a == "--client")
        {
            if (!TryReadValue(args, ref i, out var v)) return 1;
            clientOut = v;
        }
        else
        {
            LogError($"Unknown argument: {a}");
            return 1;
        }
    }

    if (!File.Exists(flatcExe))
    {
        LogError($"flatc.exe not found: {flatcExe}");
        return 1;
    }

    Directory.CreateDirectory(serverOut);
    Directory.CreateDirectory(clientOut);

    if (!Directory.Exists(schemasDir))
    {
        LogError($"schemas directory not found: {schemasDir}");
        return 1;
    }

    var fbsFiles = Directory.GetFiles(schemasDir, "*.fbs");
    if (fbsFiles.Length == 0)
    {
        LogError("No .fbs files found.");
        return 1;
    }

    LogPath("schemas", schemasDir);
    LogPath("flatc", flatcExe);
    LogPath("server (csharp)", serverOut);
    LogPath("client (csharp)", clientOut);
    Console.WriteLine();

    foreach (var fbs in fbsFiles)
    {
        Console.WriteLine($"[{ToolName}] schema: {Path.GetFileName(fbs)}");
        if (RunFlatc(flatcExe, schemasDir, fbs, serverOut) != 0) return 1;
        if (RunFlatc(flatcExe, schemasDir, fbs, clientOut) != 0) return 1;
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

static int RunFlatc(string flatc, string schemasDir, string fbsPath, string outDir)
{
    var psi = new ProcessStartInfo(flatc, $"--csharp -I \"{schemasDir}\" -o \"{outDir}\" \"{fbsPath}\"")
    {
        UseShellExecute = false,
        RedirectStandardError = true,
    };
    using var p = Process.Start(psi)!;
    string err = p.StandardError.ReadToEnd();
    p.WaitForExit();
    if (p.ExitCode != 0)
    {
        LogError($"flatc failed ({Path.GetFileName(fbsPath)}): exit={p.ExitCode}");
        if (!string.IsNullOrWhiteSpace(err))
            Console.Error.WriteLine(err);
    }
    return p.ExitCode;
}
