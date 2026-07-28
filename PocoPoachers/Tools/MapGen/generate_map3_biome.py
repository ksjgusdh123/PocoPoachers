"""
Map3.png(사막 지형 렌더/하이트맵)에 맞춰 Biome Map을 만든다.
Map3의 픽셀을 분석해 세 영역으로 분류한다:
  - 물(Water)   : 지형 내부에 완전히 둘러싸인 검은 구덩이(오아시스 웅덩이)
  - 길(Road)    : 산맥 능선보다 안쪽(내부 안전지대)에서 국소 밝기가 주변보다
                  튀는 가늘고 밝은 선(지역들을 잇는 길)
  - 주변 지형    : 그 외 전부 (사막 모래) — 기존 팔레트의 Wasteland 색(8C603A)을 재사용

산출물: Assets/_Art/Map/Map3_Biome.png (Map3.png와 동일 해상도/좌표계)
색상은 MapLayerPalette의 기존 Water(366CD6)/Wasteland(8C603A) 값을 그대로 재사용해
팔레트 수정 없이 바로 매칭되도록 했다. Road 색(C9A66B)은 새 색상 — 아직 팔레트에
Road 항목이 없으므로(= Road Map 레이어 자체가 미구현) 현재는 시각 참고용이며,
실제로 지형에 반영하려면 MapLayerPalette.biomes에 Road 항목을 추가해야 한다.
"""

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SRC_PATH = "../../Assets/_Art/Map/Map3.png"
OUT_PATH = "../../Assets/_Art/Map/Map3_Biome.png"

DESERT = (140, 96, 58)    # 8C603A - 기존 Wasteland 팔레트 색 재사용 (사막 모래)
WATER = (54, 108, 214)    # 366CD6 - 기존 Water 팔레트 색 재사용
ROAD = (201, 166, 107)    # C9A66B - 신규 색 (Road 팔레트 항목 없음, 참고용)

DARK_THRESHOLD = 15       # 이 값 이하 = 배경/구덩이 후보
RIM_MARGIN_PX = 160       # 산맥 능선 영향권으로 간주해 길 탐지에서 제외할 경계 폭
ROAD_DIFF_THRESHOLD = 18  # 국소 블러 대비 이 값보다 밝으면 길 후보
ROAD_DILATE_PX = 3        # 길 선 두께 보정


def dilate4(mask, iters=1):
    m = mask.copy()
    for _ in range(iters):
        up = np.roll(m, -1, axis=0); up[-1, :] = False
        down = np.roll(m, 1, axis=0); down[0, :] = False
        left = np.roll(m, -1, axis=1); left[:, -1] = False
        right = np.roll(m, 1, axis=1); right[:, 0] = False
        m = m | up | down | left | right
    return m


def erode4(mask, iters=1):
    m = mask.copy()
    for _ in range(iters):
        up = np.roll(m, -1, axis=0); up[-1, :] = False
        down = np.roll(m, 1, axis=0); down[0, :] = False
        left = np.roll(m, -1, axis=1); left[:, -1] = False
        right = np.roll(m, 1, axis=1); right[:, 0] = False
        m = m & up & down & left & right
    return m


def dilate8(mask, iters=1):
    m = mask.copy()
    for _ in range(iters):
        layers = [m]
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dy == 0 and dx == 0:
                    continue
                s = np.roll(np.roll(m, dy, axis=0), dx, axis=1)
                if dy == -1: s[-1, :] = False
                if dy == 1: s[0, :] = False
                if dx == -1: s[:, -1] = False
                if dx == 1: s[:, 0] = False
                layers.append(s)
        m = np.logical_or.reduce(layers)
    return m


def main():
    im = Image.open(SRC_PATH).convert("RGB")
    arr = np.array(im)
    gray = arr.max(axis=2).astype(np.int32)
    size = gray.shape[0]

    # 1) 배경(exterior) vs 내부 구덩이(물) 분리 — 코너에서 flood fill
    dark = (gray <= DARK_THRESHOLD).astype(np.uint8) * 255
    dark_img = Image.fromarray(dark, mode="L")
    ff = dark_img.copy()
    ImageDraw.floodfill(ff, (0, 0), 128, thresh=0)
    exterior = np.array(ff) == 128
    landmass = ~exterior

    water_raw = (dark == 255) & landmass
    water = dilate4(erode4(water_raw, 2), 2)  # 경계 근처 잡티 제거

    # 2) 길 탐지: 산맥 능선 영향권 밖(interior_safe)에서 국소 밝기 대비 리지 검출
    ext_img = Image.fromarray((exterior * 255).astype(np.uint8))
    rim_zone = np.array(ext_img.filter(ImageFilter.MaxFilter(RIM_MARGIN_PX * 2 + 1))) > 0
    interior_safe = landmass & (~rim_zone) & (~water)

    blur = np.array(Image.fromarray(gray.astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius=20))).astype(np.int32)
    diff = gray - blur
    road = (diff > ROAD_DIFF_THRESHOLD) & interior_safe
    road = dilate8(road, ROAD_DILATE_PX) & landmass & (~water)

    # 3) 합성
    out = np.empty((size, size, 3), dtype=np.uint8)
    out[:, :] = DESERT
    out[road] = ROAD
    out[water] = WATER

    Image.fromarray(out, mode="RGB").save(OUT_PATH)
    print("saved", OUT_PATH)
    print(f"desert={size*size - road.sum() - water.sum()} road={road.sum()} water={water.sum()}")


if __name__ == "__main__":
    main()
