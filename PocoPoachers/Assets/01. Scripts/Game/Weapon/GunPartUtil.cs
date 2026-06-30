// 총기 파츠 호환성 판정.
public static class GunPartUtil
{
    // compatible_gun_types는 비트마스크. 비트 인덱스 = (GunType - 1) (None=0 제외).
    // 예: 31 == 0b11111 == Pistol~SMG 전부 호환.
    public static bool IsCompatible(GunPartData part, GunType gunType)
    {
        if (part == null || gunType == GunType.None)
            return false;

        return (part.compatible_gun_types & (1 << ((int)gunType - 1))) != 0;
    }
}
