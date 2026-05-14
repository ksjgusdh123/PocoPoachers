using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class Includer : AssetPostprocessor
{
    /// <summary>
    /// 유니티가 .csproj 파일을 생성한 직후 호출되어 내용을 수정합니다.
    /// </summary>
    private static string OnGeneratedCSProject(string path, string content)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);

        if (fileName != "Assembly-CSharp-Editor")
        {
            return content;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        var targetFolders = new List<string>
        {
            Path.Combine(projectRoot, "FlatBuffer"),
            Path.Combine(projectRoot, "DataTable")
        };

        StringBuilder sb = new StringBuilder();
        bool hasAddedAnyFile = false;

        foreach (var folder in targetFolders)
        {
            if (!Directory.Exists(folder)) continue;

            var allFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                string ext = Path.GetExtension(file).ToLower();

                if (ext == ".fbs" || ext == ".csv")
                {
                    string relativePath = Path.GetRelativePath(projectRoot, file);

                    if (content.Contains($"Include=\"{relativePath}\""))
                        continue;

                    if (!hasAddedAnyFile)
                    {
                        sb.AppendLine("\n  <ItemGroup>");
                        hasAddedAnyFile = true;
                    }

                    sb.AppendLine($"    <None Include=\"{relativePath}\" />");
                }
            }
        }

        if (hasAddedAnyFile)
        {
            sb.AppendLine("  </ItemGroup>");
            return content.Replace("</Project>", sb.ToString() + "</Project>");
        }

        return content;
    }
}