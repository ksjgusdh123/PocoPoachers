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

    protected override void Awake()
    {
        // 씬에 새로 배치된 인스턴스가 중복 싱글턴으로 파괴되기 전에, 이 씬의 프리팹 지정을
        // 살아남는 인스턴스에 넘겨준다 (ObjectManager.ApplySceneEntries와 같은 이유).
        if (_instance != null && _instance != this)
            _instance.ApplyScenePrefabs(_podPrefab, _beamPrefab);

        base.Awake();
    }

    private void ApplyScenePrefabs(GameObject pod, GameObject beam)
    {
        if (pod != null) _podPrefab = pod;
        if (beam != null) _beamPrefab = beam;
    }
}
