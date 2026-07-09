"""
'행성 대역별 3D 세션 기본 스펙 테이블'에 정의된 4개 섹터의 컨셉 이미지를 생성한다.
안개 밀도 / 가시거리 / 수직성 테마 / 광물 색상 / 위험도를 절차적 레이어 합성으로 표현한다.
"""

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

W, H = 800, 450
FONT_TITLE = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 18)
FONT_SUB = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 15)


def vgrad(size, top, bottom):
    w, h = size
    t = np.array(top, dtype=np.float64)
    b = np.array(bottom, dtype=np.float64)
    ys = np.linspace(0, 1, h)[:, None]
    row = t[None, :] * (1 - ys) + b[None, :] * ys
    arr = np.repeat(row[:, None, :], w, axis=1).astype(np.uint8)
    return Image.fromarray(arr, mode="RGB")


def jagged_layer(width, base_y, amp, n_pts, seed, roughness=0.5):
    r = np.random.default_rng(seed)
    xs = np.linspace(0, width, n_pts)
    ys = base_y - np.abs(np.cumsum(r.normal(0, amp * roughness, n_pts)))
    ys = ys - ys.min()
    ys = base_y - (ys / ys.max() if ys.max() > 0 else ys) * amp
    return list(zip(xs.tolist(), ys.tolist()))


def draw_silhouette(canvas_size, points, color, bottom):
    layer = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    poly = [(0, bottom)] + points + [(canvas_size[0], bottom)]
    d.polygon(poly, fill=color + (255,))
    return layer


def blend_fog_rows(img, fog_color, horizon, fade, gamma):
    """지평선(horizon)에서 안개가 가장 짙고, 위(하늘)/아래(카메라 쪽)로
    멀어질수록 fade 픽셀 거리에 걸쳐 옅어진다. fade가 클수록(가시거리가
    짧을수록) 안개가 더 멀리까지 남아 전경도 흐려진다."""
    arr = np.asarray(img.convert("RGB")).astype(np.float64)
    h = arr.shape[0]
    dist = np.abs(np.arange(h) - horizon)
    depth = np.clip(1 - dist / max(1, fade), 0, 1)
    mix = depth ** gamma
    fog = np.array(fog_color, dtype=np.float64)
    mixed = arr * (1 - mix[:, None, None]) + fog[None, None, :] * mix[:, None, None]
    return Image.fromarray(mixed.astype(np.uint8), mode="RGB")


def add_glow(base, points, color, radius, blur):
    glow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(glow)
    for (x, y), r in points:
        d.ellipse([x - r, y - r, x + r, y + r], fill=color + (255,))
    glow = glow.filter(ImageFilter.GaussianBlur(blur))
    return Image.alpha_composite(base.convert("RGBA"), glow)


def scatter_glow_points(rng, n, x_range, y_range, r_range):
    pts = []
    for _ in range(n):
        x = rng.uniform(*x_range)
        y = rng.uniform(*y_range)
        r = rng.uniform(*r_range)
        pts.append(((x, y), r))
    return pts


def vignette(base, center, strength, softness):
    arr = np.asarray(base.convert("RGB")).astype(np.float64)
    h, w = arr.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy = center
    dist = np.sqrt((xx - cx) ** 2 + ((yy - cy) * 1.3) ** 2)
    dist = dist / dist.max()
    mask = np.clip((dist - softness) / max(1e-3, (1 - softness)), 0, 1) ** 1.5
    arr = arr * (1 - mask[:, :, None] * strength)
    return Image.fromarray(arr.astype(np.uint8), mode="RGB")


def enemy_eyes(base, n, y_range, seed, color=(255, 40, 40)):
    r = np.random.default_rng(seed)
    glow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(glow)
    for _ in range(n):
        cx = r.uniform(30, W - 30)
        cy = r.uniform(*y_range)
        gap = r.uniform(3, 6)
        rr = r.uniform(1.6, 2.6)
        d.ellipse([cx - gap - rr, cy - rr, cx - gap + rr, cy + rr], fill=color + (255,))
        d.ellipse([cx + gap - rr, cy - rr, cx + gap + rr, cy + rr], fill=color + (255,))
    glow = glow.filter(ImageFilter.GaussianBlur(1.2))
    return Image.alpha_composite(base.convert("RGBA"), glow)


def label_band(base, title, subtitle, band_color, text_color):
    from PIL import ImageDraw as D
    img = base.convert("RGB")
    band_h = 54
    band = Image.new("RGB", (W, band_h), band_color)
    d = D.Draw(band)
    d.text((16, 6), title, fill=text_color, font=FONT_TITLE)
    d.text((16, 29), subtitle, fill=text_color, font=FONT_SUB)
    out = Image.new("RGB", (W, H + band_h))
    out.paste(band, (0, 0))
    out.paste(img, (0, band_h))
    return out


# ----------------------------------------------------------------------
# 섹터 01: 폐기 황무지 (Tier 1) — 안개 0.01 / 가시거리 150m, 평면 고철장 사막
# ----------------------------------------------------------------------
def sector_01():
    horizon = 300
    sky = vgrad((W, horizon), (214, 178, 120), (238, 214, 168))
    ground = vgrad((W, H - horizon), (168, 128, 82), (120, 90, 58))
    img = Image.new("RGB", (W, H))
    img.paste(sky, (0, 0))
    img.paste(ground, (0, horizon))

    far = draw_silhouette((W, H), jagged_layer(W, horizon, 14, 9, seed=1), (196, 160, 108), horizon)
    img = Image.alpha_composite(img.convert("RGBA"), far)

    rng = np.random.default_rng(11)
    scrap = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(scrap)
    for _ in range(16):
        y = rng.uniform(horizon + 15, H - 20)
        depth = (y - horizon) / (H - horizon)
        x = rng.uniform(20, W - 20)
        s = rng.uniform(18, 40) * (0.6 + depth)
        rust = (rng.integers(60, 90), rng.integers(38, 54), rng.integers(22, 34))
        outline = (28, 18, 10)
        d.rectangle([x, y, x + s, y + s * 0.35], fill=rust + (255,), outline=outline + (255,), width=2)
        if rng.random() < 0.6:
            d.polygon([(x, y), (x + s * 0.5, y - s * 0.7), (x + s, y)],
                      fill=rust + (255,), outline=outline + (255,), width=2)
    img = Image.alpha_composite(img, scrap)

    img = blend_fog_rows(img, (222, 198, 150), horizon, fade=35, gamma=3.0)
    img = img.convert("RGB")
    return label_band(img, "SECTOR 01 : 폐기 황무지 (Tier 1)",
                       "안개 0.01 / 가시거리 150m · 평면 고철장 사막 · Fe/Cu/Coal · 위험 최하(12)",
                       (60, 46, 32), (240, 224, 190))


# ----------------------------------------------------------------------
# 섹터 02: 동결 정점 (Tier 2) — 안개 0.15 / 가시거리 60m, 복층 빙하 절벽·미로 얼음동굴
# ----------------------------------------------------------------------
def sector_02():
    horizon = 260
    sky = vgrad((W, horizon), (150, 178, 210), (206, 224, 236))
    ground = vgrad((W, H - horizon), (210, 226, 236), (150, 172, 190))
    img = Image.new("RGB", (W, H))
    img.paste(sky, (0, 0))
    img.paste(ground, (0, horizon))
    img = img.convert("RGBA")

    layers = [
        (jagged_layer(W, horizon + 10, 90, 7, seed=2, roughness=0.9), (188, 206, 222), horizon + 30),
        (jagged_layer(W, horizon + 60, 130, 6, seed=3, roughness=1.1), (150, 178, 202), horizon + 90),
        (jagged_layer(W, horizon + 140, 160, 5, seed=4, roughness=1.2), (108, 142, 176), H - 10),
    ]
    for pts, color, base in layers:
        img = Image.alpha_composite(img, draw_silhouette((W, H), pts, color, base))

    arch = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(arch)
    d.polygon([(0, H), (0, H - 170), (90, H - 230), (170, H - 150), (170, H)], fill=(70, 100, 130, 255))
    img = Image.alpha_composite(img, arch)

    rng = np.random.default_rng(22)
    crystal_pts = scatter_glow_points(rng, 12, (40, W - 40), (horizon + 30, H - 20), (5, 11))
    img = add_glow(img, crystal_pts, (90, 210, 255), radius=0, blur=4)
    img = add_glow(img, crystal_pts, (200, 240, 255), radius=0, blur=10)

    img = blend_fog_rows(img, (214, 226, 236), horizon, fade=150, gamma=1.6)
    img = img.convert("RGB")
    return label_band(img, "SECTOR 02 : 동결 정점 (Tier 2)",
                       "안개 0.15 / 가시거리 60m · 복층 빙하 절벽·미로 얼음동굴 · Ti/Blue Crystal · 위험 중(20)",
                       (26, 46, 62), (200, 226, 240))


# ----------------------------------------------------------------------
# 섹터 03: 화산 원자로 (Tier 3) — 안개 0.25 / 가시거리 40m, 고저차 심한 용암대지·폐공장
# ----------------------------------------------------------------------
def sector_03():
    horizon = 250
    sky = vgrad((W, horizon), (40, 26, 30), (110, 52, 34))
    ground = vgrad((W, H - horizon), (46, 24, 18), (18, 10, 8))
    img = Image.new("RGB", (W, H))
    img.paste(sky, (0, 0))
    img.paste(ground, (0, horizon))
    img = img.convert("RGBA")

    ridge = jagged_layer(W, horizon + 30, 170, 8, seed=5, roughness=1.4)
    img = Image.alpha_composite(img, draw_silhouette((W, H), ridge, (30, 16, 14), H - 10))

    factory = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(factory)
    fx = W * 0.62
    d.rectangle([fx, horizon - 10, fx + 140, horizon + 120], fill=(20, 16, 16, 255))
    for i, cx in enumerate([fx + 15, fx + 55, fx + 95]):
        d.rectangle([cx, horizon - 70 - i * 10, cx + 14, horizon - 10], fill=(16, 12, 12, 255))
    img = Image.alpha_composite(img, factory)

    rng = np.random.default_rng(33)
    lava_pts = scatter_glow_points(rng, 16, (10, W - 10), (horizon + 10, H - 5), (4, 12))
    img = add_glow(img, lava_pts, (255, 90, 20), radius=0, blur=8)
    vent_pts = scatter_glow_points(rng, 4, (fx, fx + 120), (horizon - 60, horizon), (3, 6))
    img = add_glow(img, vent_pts, (255, 60, 30), radius=0, blur=5)

    img = blend_fog_rows(img, (70, 40, 30), horizon, fade=190, gamma=1.3)
    img = enemy_eyes(img, 3, (horizon + 60, H - 40), seed=333)
    img = img.convert("RGB")
    return label_band(img, "SECTOR 03 : 화산 원자로 (Tier 3)",
                       "안개 0.25 / 가시거리 40m · 고저차 심한 용암대지·폐공장 · U/Red Plasma · 위험 상(28)",
                       (52, 22, 14), (250, 200, 170))


# ----------------------------------------------------------------------
# 섹터 04: 미지의 심연 (Tier 4) — 안개 0.60 / 가시거리 10m, 완전 암전·고대 3D 미로 유적
# ----------------------------------------------------------------------
def sector_04():
    img = Image.new("RGB", (W, H), (6, 4, 10))
    img = img.convert("RGBA")

    horizon = 260
    pillars = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(pillars)
    rng = np.random.default_rng(4)
    for i in range(7):
        x = i * (W / 7) + rng.uniform(-15, 15)
        w_ = rng.uniform(26, 44)
        top = horizon - rng.uniform(0, 60)
        d.rectangle([x, top, x + w_, H], fill=(14, 10, 20, 255))
        d.rectangle([x - 10, top - 12, x + w_ + 10, top + 6], fill=(18, 13, 24, 255))
    img = Image.alpha_composite(img, pillars)

    omega_pts = scatter_glow_points(rng, 6, (30, W - 30), (horizon, H - 10), (2, 4))
    img = add_glow(img, omega_pts, (170, 60, 220), radius=0, blur=7)

    img = vignette(img, center=(W / 2, H * 0.78), strength=0.97, softness=0.06)

    # 카메라 조명(가시거리 10m)이 비추는 좁은 원형 영역만 밝게 — 그 밖은 완전 암전
    light = Image.new("L", (W, H), 0)
    ld = ImageDraw.Draw(light)
    ld.ellipse([W / 2 - 130, H - 170, W / 2 + 130, H + 60], fill=110)
    light = light.filter(ImageFilter.GaussianBlur(45))
    base_arr = np.asarray(img.convert("RGB")).astype(np.float64)
    light_arr = np.asarray(light).astype(np.float64) / 255.0
    lit = base_arr + (255 - base_arr) * light_arr[:, :, None] * 0.35 + light_arr[:, :, None] * 25
    img = Image.fromarray(np.clip(lit, 0, 255).astype(np.uint8), mode="RGB").convert("RGBA")

    img = enemy_eyes(img, 9, (horizon, H - 20), seed=44)

    img = img.convert("RGB")
    return label_band(img, "SECTOR 04 : 미지의 심연 (Tier 4)",
                       "안개 0.60 / 가시거리 10m(극단) · 완전 암전·3D 미로 유적 · Omega · 최상(야간 스폰캡 해제)",
                       (16, 8, 20), (210, 170, 230))


for name, fn in [("sector01_wasteland", sector_01),
                 ("sector02_frozen_summit", sector_02),
                 ("sector03_volcanic_reactor", sector_03),
                 ("sector04_unknown_abyss", sector_04)]:
    out = fn()
    path = f"../../../Docs/design/img/planet-sectors/{name}.png"
    out.save(path)
    print("saved", path)
