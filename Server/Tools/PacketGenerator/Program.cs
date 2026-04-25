using System.Diagnostics;

string schemasDir = Path.GetFullPath("Schemas");
string flatcExe   = Path.GetFullPath("flatc.exe");
string serverOut  = Path.GetFullPath(Path.Combine("..", "..", "Server", "Generated", "Packet"));
string clientOut  = Path.GetFullPath(Path.Combine("..", "..", "..", "PocoPoachers", "Assets", "01. Scripts", "Generated", "Packet"));

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--schemas" && i + 1 < args.Length) schemasDir = args[++i];
    if (args[i] == "--server"  && i + 1 < args.Length) serverOut  = args[++i];
    if (args[i] == "--client"  && i + 1 < args.Length) clientOut  = args[++i];
}

if (!File.Exists(flatcExe)) { Console.Error.WriteLine($"[Error] flatc.exe not found: {flatcExe}"); return 1; }

Directory.CreateDirectory(serverOut);
Directory.CreateDirectory(clientOut);

var fbsFiles = Directory.GetFiles(schemasDir, "*.fbs");
if (fbsFiles.Length == 0) { Console.Error.WriteLine("[Error] No .fbs files found."); return 1; }

Console.WriteLine($"Server output : {serverOut}");
Console.WriteLine($"Client output : {clientOut}");
Console.WriteLine();

foreach (var fbs in fbsFiles)
{
    Console.WriteLine(Path.GetFileName(fbs));
    if (RunFlatc(flatcExe, schemasDir, fbs, serverOut) != 0) return 1;
    if (RunFlatc(flatcExe, schemasDir, fbs, clientOut) != 0) return 1;
}

Console.WriteLine("Done.");
return 0;

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
    if (p.ExitCode != 0) Console.Error.WriteLine($"[Error] {err}");
    return p.ExitCode;
}
