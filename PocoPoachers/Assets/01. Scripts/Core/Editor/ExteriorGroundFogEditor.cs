using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExteriorGroundFog))]
public sealed class ExteriorGroundFogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var fog = (ExteriorGroundFog)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(fog.Status, MessageType.Info);
        if (GUILayout.Button("안개 다시 생성 및 상태 확인"))
        {
            fog.Rebuild();
            SceneView.RepaintAll();
        }
    }
}
