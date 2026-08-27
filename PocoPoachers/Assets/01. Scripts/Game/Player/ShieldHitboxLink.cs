using UnityEngine;

// 방어막(ShieldFX) 콜라이더가 대신 맞는 총알을 실제 소유자(StatBase)에게 그대로 넘긴다.
// 방어막 반경이 플레이어 본체 콜라이더보다 커서, 총알이 몸에 닿기 전에 방어막 표면에서 먼저 막히거나
// (반사 스킬이면) 반사되게 하는 게 목적 — 데미지 판정 자체는 언제나 Owner.TakeDamage로 위임하므로
// 무적/반사 여부는 StatBase 쪽 로직을 그대로 따른다.
public class ShieldHitboxLink : MonoBehaviour, IDamageable
{
    public StatBase Owner { get; set; }

    public bool TakeDamage(float damage, GameObject attacker = null)
    {
        return Owner != null && Owner.TakeDamage(damage, attacker);
    }
}
