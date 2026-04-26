namespace Server;

public static class ServerPaths
{
    static string? _projectDir;

    public static string ProjectDir =>
        _projectDir ??= Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    public static string GeneratedJson(string fileName) =>
        Path.GetFullPath(Path.Combine(ProjectDir, "Generated", "JsonData", fileName));
}
