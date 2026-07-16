using System.Collections.Generic;
using UnityEngine;

public class ShotgunGun : GunBase
{
    protected override void Shoot()
    {
        int count = Mathf.Max(1, _stat.PelletCount);
        var dirs = new List<Vector3>(count);

        for (int i = 0; i < count; i++)
        {
            Vector3 fireDir = GetFireDirection();
            dirs.Add(fireDir);

            Bullet bullet = BulletPool.GetInstance().Get(_bulletPrefab, _muzzle.position, _muzzle.rotation);
            bullet.Initialize(
                _stat.BulletSpeed,
                _stat.Damage,
                _stat.BulletRange,
                fireDir,
                () => BulletPool.GetInstance().Release(_bulletPrefab, bullet),
                Owner,
                _stat.MuzzleColor
            );
        }

        // 방아쇠 1회 = 펠릿 방향 배열 1패킷. 호스트가 1회만 인증 후 전 펠릿을 스폰·브로드캐스트한다.
        RoomSync.Shoot(_muzzle.position, dirs[0], _stat, dirs);
    }
}
