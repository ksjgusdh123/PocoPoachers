using UnityEngine;
using UnityEditor;
using System.IO;

public static class BulletMaterialCreator
{
    private const string SavePath = "Assets/03. Materials/Weapon/Bullet.mat";

    [MenuItem("Tools/Create Bullet Material")]
    public static void CreateBulletMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("URP Lit 셰이더를 찾을 수 없습니다.");
            return;
        }

        string directory = Path.GetDirectoryName(SavePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        Material material = new Material(shader);
        material.SetColor("_BaseColor", new Color(1f, 0.85f, 0f));
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f) * 2f);

        AssetDatabase.CreateAsset(material, SavePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Bullet 머터리얼 생성 완료: {SavePath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = material;
    }
}
