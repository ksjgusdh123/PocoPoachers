using UnityEngine;

// RescueBeamEffect가 사용할 포드(호송선)/빔 프리팹을 인스펙터로 지정해두는 곳.
// 씬에 미리 하나 배치하고 인스펙터에서 프리팹을 연결해야 한다.
// 빔 프리팹은 피벗(로컬 y=0)이 포드 위치에 붙고 아래(-Y)로 1유닛 길이로 모델링되어 있어야 한다 —
// RescueBeamEffect가 localScale.y로 길이를 조절해 자라나는 연출을 만든다.
public class RescueEffectPrefabs : Singleton<RescueEffectPrefabs>
{
    [SerializeField] private GameObject _podPrefab;
    [SerializeField] private GameObject _beamPrefab;

    public GameObject PodPrefab => _podPrefab;
    public GameObject BeamPrefab => _beamPrefab;
}
