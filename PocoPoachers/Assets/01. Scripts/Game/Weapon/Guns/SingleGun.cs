using UnityEngine;

public class SingleGun : GunBase
{
    protected override void Shoot(bool isHeadshot)
    {
        Vector3 fireDir = GetFireDirection();

        Bullet bullet = BulletPool.GetInstance().Get(_bulletPrefab, _muzzle.position, _muzzle.rotation);
        bullet.Initialize(
            _stat.BulletSpeed,
            _stat.Damage,
            EffectiveBulletRange,
            fireDir,
            () => BulletPool.GetInstance().Release(_bulletPrefab, bullet),
            Owner,
            _stat.MuzzleColor,
            isHeadshot
        );

        var seqs = new System.Collections.Generic.List<int> { PrepareBullet(bullet) };
        BroadcastShoot(_muzzle.position, fireDir, isHeadshot: isHeadshot, bulletSeqs: seqs);
    }
}
