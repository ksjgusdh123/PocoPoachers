using UnityEngine;

// duration 동안 피해를 받지 않는다 (StatBase.SetInvincible → RoomSync로 네트워크 전파까지 자동 처리됨).
// 방어막 연출(ShieldFX)은 아직 로컬(사용자 본인 화면)에만 붙는다 — 다른 클라이언트에게도 보여주려면
// Stealth/DroneState처럼 상태를 전파하는 전용 패킷이 있어야 한다(현재는 없음).
public class InvincibleSkill : PlayerSkillBase
{
    private const string ShieldFxPrefabPath = "Skill/ShieldFX";
    private const float ShieldHeightOffset = 0.3f; // 캐릭터 몸통 중심쯤에 방어막을 띄운다

    public override PlayerSkillId Id => PlayerSkillId.Invincible;

    private float _elapsed;
    private GameObject _shieldFx;

    public InvincibleSkill(PlayerSkillData data) : base(data) { }

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        ctx.Stat.SetInvincible(true);
        SpawnShieldFx(ctx);
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        if (ctx.Stat != null) ctx.Stat.SetInvincible(false);
        DespawnShieldFx();
    }

    private void SpawnShieldFx(PlayerSkillContext ctx)
    {
        var prefab = Resources.Load<GameObject>(ShieldFxPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[InvincibleSkill] 방어막 프리팹을 찾을 수 없습니다: Resources/{ShieldFxPrefabPath}");
            return;
        }

        _shieldFx = Object.Instantiate(prefab, ctx.Transform);
        _shieldFx.transform.localPosition = Vector3.up * ShieldHeightOffset;
        _shieldFx.transform.localRotation = Quaternion.identity;

        // 에셋 원본의 SphereCollider는 데모용 — 플레이어 몸에 붙었을 때 물리 판정에 끼어들면 안 된다.
        foreach (var col in _shieldFx.GetComponentsInChildren<Collider>())
            col.enabled = false;

        _shieldFx.AddComponent<ShieldFxVisual>(); // Awake에서 0으로 시작해 OnEnable에서 페이드인
    }

    private void DespawnShieldFx()
    {
        if (_shieldFx == null) return;

        // 즉시 파괴하지 않고 페이드아웃이 끝난 뒤 스스로 파괴하도록 맡긴다
        if (_shieldFx.TryGetComponent<ShieldFxVisual>(out var visual))
            visual.FadeOutAndDestroy();
        else
            Object.Destroy(_shieldFx);

        _shieldFx = null;
    }
}
