"""
맵 자동 생성 시스템 제안서에 정의된 레이어 예시 텍스처를 생성한다.
- Height Map, Biome Map, Object Map, Resource Map, Enemy Spawn Map
각 이미지는 512x512, 제안서에 명시된 색상 규칙을 따르는 샘플 마스크다.
"""

import numpy as np
from PIL import Image, ImageDraw
import math

SIZE = 512
rng = np.random.default_rng(42)


def value_noise(size, scale, octaves=4, persistence=0.5, seed=0):
    r = np.random.default_rng(seed)
    total = np.zeros((size, size), dtype=np.float64)
    amp = 1.0
    amp_sum = 0.0
    for o in range(octaves):
        cell = max(2, scale // (2 ** o))
        grid_n = size // cell + 2
        grid = r.random((grid_n, grid_n)).astype(np.float32)
        # smooth upsample grid to size x size via PIL bicubic resize
        layer = np.asarray(
            Image.fromarray(grid, mode="F").resize((size, size), Image.BICUBIC),
            dtype=np.float64,
        )
        total += layer * amp
        amp_sum += amp
        amp *= persistence
    total /= amp_sum
    return total


def save(img, name):
    path = f"{name}.png"
    img.save(path)
    print("saved", path)


# ---------------------------------------------------------------
# 1) Height Map : 흑(낮은 지형) -> 회(언덕) -> 백(산악)
# ---------------------------------------------------------------
h = value_noise(SIZE, 128, octaves=5, persistence=0.55, seed=1)
h = (h - h.min()) / (h.max() - h.min())
h = np.power(h, 1.3)  # push down mid values a bit for more low terrain
height_img = (h * 255).astype(np.uint8)
Image.fromarray(height_img, mode="L").convert("RGB").save(
    "../../Assets/_Art/Map/height_map.png"
)

# ---------------------------------------------------------------
# 2) Biome Map : green forest, yellow grassland, brown wasteland,
#    white snow, blue lake/river, using height + moisture noise
# ---------------------------------------------------------------
moisture = value_noise(SIZE, 96, octaves=4, persistence=0.5, seed=2)
moisture = (moisture - moisture.min()) / (moisture.max() - moisture.min())

biome_rgb = np.zeros((SIZE, SIZE, 3), dtype=np.uint8)
FOREST = (52, 143, 60)
GRASS = (222, 200, 60)
WASTE = (140, 96, 58)
SNOW = (245, 245, 245)
WATER = (54, 108, 214)

snow_mask = h > 0.78
water_mask = (~snow_mask) & (h < 0.22)
forest_mask = (~snow_mask) & (~water_mask) & (moisture > 0.55)
grass_mask = (~snow_mask) & (~water_mask) & (~forest_mask) & (moisture > 0.35)
waste_mask = (~snow_mask) & (~water_mask) & (~forest_mask) & (~grass_mask)

for mask, color in [
    (waste_mask, WASTE),
    (grass_mask, GRASS),
    (forest_mask, FOREST),
    (water_mask, WATER),
    (snow_mask, SNOW),
]:
    biome_rgb[mask] = color

Image.fromarray(biome_rgb, mode="RGB").save(
    "../../Assets/_Art/Map/biome_map.png"
)

# ---------------------------------------------------------------
# 3) Object Map : black background, colored dot/blob = prefab marker
#    red=tree, gray=rock, yellow=building, purple=dungeon entrance
# ---------------------------------------------------------------
obj_img = Image.new("RGB", (SIZE, SIZE), (0, 0, 0))
draw = ImageDraw.Draw(obj_img)

TREE = (230, 40, 40)
ROCK = (150, 150, 150)
BUILDING = (235, 210, 40)
DUNGEON = (150, 40, 200)


def scatter_points(mask, density, min_r, max_r):
    ys, xs = np.where(mask)
    if len(xs) == 0:
        return []
    idx = rng.choice(len(xs), size=max(1, int(len(xs) * density)), replace=False)
    return [(xs[i], ys[i], rng.integers(min_r, max_r + 1)) for i in idx]


# trees cluster in forest, rocks in wasteland, buildings in grassland,
# a few dungeon entrances scattered rarely
for x, y, r in scatter_points(forest_mask, 0.012, 3, 6):
    draw.ellipse([x - r, y - r, x + r, y + r], fill=TREE)
for x, y, r in scatter_points(waste_mask, 0.006, 3, 7):
    draw.ellipse([x - r, y - r, x + r, y + r], fill=ROCK)
for x, y, r in scatter_points(grass_mask, 0.002, 5, 9):
    draw.rectangle([x - r, y - r, x + r, y + r], fill=BUILDING)

dungeon_candidates = np.where(~water_mask)
n_dungeon = 4
idx = rng.choice(len(dungeon_candidates[0]), size=n_dungeon, replace=False)
for i in idx:
    y, x = dungeon_candidates[0][i], dungeon_candidates[1][i]
    r = 10
    draw.ellipse([x - r, y - r, x + r, y + r], fill=DUNGEON)

obj_img.save("../../Assets/_Art/Map/object_map.png")

# ---------------------------------------------------------------
# 4) Resource Map : black background,
#    green=plant, blue=mineral, yellow=rare
# ---------------------------------------------------------------
res_img = Image.new("RGB", (SIZE, SIZE), (0, 0, 0))
draw = ImageDraw.Draw(res_img)

PLANT = (60, 200, 90)
MINERAL = (60, 130, 230)
RARE = (240, 220, 50)

for x, y, r in scatter_points(forest_mask | grass_mask, 0.01, 3, 5):
    draw.ellipse([x - r, y - r, x + r, y + r], fill=PLANT)
for x, y, r in scatter_points(waste_mask | snow_mask, 0.008, 3, 6):
    draw.ellipse([x - r, y - r, x + r, y + r], fill=MINERAL)

rare_mask = h > 0.85
for x, y, r in scatter_points(rare_mask, 0.05, 4, 7):
    draw.ellipse([x - r, y - r, x + r, y + r], fill=RARE)

res_img.save("../../Assets/_Art/Map/resource_map.png")

# ---------------------------------------------------------------
# 5) Enemy Spawn Map : white = spawn allowed, black = forbidden
#    (forbidden near water and on steep mountain peaks)
# ---------------------------------------------------------------
enemy_noise = value_noise(SIZE, 64, octaves=3, persistence=0.5, seed=5)
forbidden = water_mask | snow_mask | (enemy_noise < 0.35)
enemy_gray = np.where(forbidden, 0, 255).astype(np.uint8)
Image.fromarray(enemy_gray, mode="L").convert("RGB").save(
    "../../Assets/_Art/Map/enemy_spawn_map.png"
)

print("done")
