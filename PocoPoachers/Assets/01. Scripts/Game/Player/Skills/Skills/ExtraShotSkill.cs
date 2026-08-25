using UnityEngine;

// duration 동안 플레이어 주변에 드론을 띄운다.
// 드론은 내 총알이 적을 맞출 때마다 그 대상에게 유도탄을 한 발 더 쏜다.
// 유도탄 데미지는 테이블의 power, 발사 간격 등은 드론 프리팹 인스펙터에서 조정한다.
public class ExtraShotSkill : PlayerSkillBase
{
    private const string DronePrefabPath = "Skill/CombatDrone";

    public override PlayerSkillId Id => PlayerSkillId.ExtraShot;

    private float _elapsed;
    private GameObject _drone;

    public ExtraShotSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx)
    {
        return base.CanUse(ctx) && ctx.Weapon != null;
    }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;

        var prefab = Resources.Load<GameObject>(DronePrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[ExtraShotSkill] 드론 프리팹을 찾을 수 없습니다: Resources/{DronePrefabPath}");
            return;
        }

        _drone = Object.Instantiate(prefab, ctx.Transform.position, Quaternion.identity);
        // 발사 판단은 호스트만 한다. 게스트의 드론은 H_DroneShoot을 받아서 그리기만 하므로
        // 로컬 명중을 듣지 않는다 — 그래야 연출과 실제 피해가 어긋나지 않는다.
        _drone.GetComponent<CombatDrone>()?.Setup(ctx.Transform, RoomSync.MyPlayerId, Data.power,
                                                  ctx.Weapon.CurrentGun?.BulletPrefab,
                                                  listenLocalHits: RoomManager.IsHost);

        // 다른 플레이어 화면에도 드론을 띄운다. 게스트라면 호스트가 이 상태를 보고
        // 내 총알이 맞을 때 대신 유도탄을 쏴줘야 실제 데미지가 들어간다.
        RoomSync.DroneState(true, Data.power);
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        if (_drone == null) return;

        Object.Destroy(_drone);
        _drone = null;

        RoomSync.DroneState(false, 0f);
    }
}
