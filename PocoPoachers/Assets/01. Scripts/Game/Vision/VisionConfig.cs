using UnityEngine;

[CreateAssetMenu(fileName = "VisionConfig", menuName = "Game/Vision Config")]
public class VisionConfig : ScriptableObject
{
    public float detectRange = 15f;
    public float fovAngle = 90f;
    public int arcSegments = 30;
    public float circleRadius = 3f;
}
