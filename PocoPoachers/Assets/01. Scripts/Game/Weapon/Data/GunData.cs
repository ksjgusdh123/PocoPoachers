using UnityEngine;

public enum FireMode { Single, Auto }

[CreateAssetMenu(fileName = "GunData", menuName = "Weapon/GunData")]
public class GunData : ScriptableObject
{
    [Header("기본 스펙")]
    public float damage = 10f;
    public float fireRate = 1f;
    public int magazineSize = 30;
    public float reloadTime = 2f;

    [Header("탄환")]
    public float bulletSpeed = 20f;
    public float range = 50f;
    public GameObject bulletPrefab;

    [Header("발사 모드")]
    public FireMode fireMode = FireMode.Single;

    [Header("반동")]
    public float recoilDistance = 0.3f;
    public float recoilReturnSpeed = 1f;

    [Header("카메라 흔들림")]
    public float shakeIntensity = 0.2f;
    public float shakeDuration = 0.1f;

    [Header("탄 퍼짐")]
    public float spreadAngle = 5f;
    public float aimSpreadAngle = 5f;
    
    [Header("조준")]
    public float aimFOV = 57f;

    [Header("이동속도 계수")]
    public float moveSpeedMultiplier = 1f;
    public float aimMoveSpeedMultiplier = 0.5f;

}
