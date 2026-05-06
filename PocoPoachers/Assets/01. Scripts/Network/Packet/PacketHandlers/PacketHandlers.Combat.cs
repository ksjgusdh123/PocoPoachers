using Google.FlatBuffers;
using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Shoot(FlatPacket root)
    {
        var pkt = root.TypeAsG_Shoot();
        int playerId = pkt.PlayerId;
        var o = pkt.Origin;
        var d = pkt.Direction;
        if (!o.HasValue || !d.HasValue)
            return;

        float ox = o.Value.X, oy = o.Value.Y, oz = o.Value.Z;
        float dx = d.Value.X, dy = d.Value.Y, dz = d.Value.Z;
        float bulletSpeed = pkt.BulletSpeed;
        float damage = pkt.Damage;
        float maxRange = pkt.MaxRange;

        MainThreadDispatcher.Enqueue(() =>
        {
            var nm = NetworkManager.Instance;
            if (nm != null && playerId == nm.MyPlayerId)
                return;

            Vector3 origin = new Vector3(ox, oy, oz);
            Vector3 dir = new Vector3(dx, dy, dz);
            if (dir.sqrMagnitude > 1e-6f)
                dir.Normalize();

            GameObject prefab = nm != null ? nm.RemoteBulletPrefab : null;
            if (prefab == null)
            {
                Debug.LogWarning("[PacketHandlers] S_ShootNtf: RemoteBulletPrefab 미할당 — 원격 탄환 생략");
                return;
            }

            Quaternion rot = dir.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(dir) : Quaternion.identity;
            Bullet bullet = BulletPool.GetInstance().Get(prefab, origin, rot);
            bullet.Initialize(
                bulletSpeed,
                damage,
                maxRange,
                dir,
                () => BulletPool.GetInstance().Release(prefab, bullet),
                applyDamage: false);
        });
    }
    public static void OnH_Shoot(FlatPacket root)
    {
        // TODO
    }
}
