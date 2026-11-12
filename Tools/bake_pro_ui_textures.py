import os
import math
from PIL import Image, ImageDraw, ImageFilter

OutputDir = "Assets/_Game/Textures/UI"
os.makedirs(OutputDir, exist_ok=True)

def create_supersampled(size, scale=2):
    w, h = size
    return Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0)), scale

def downsample(img, scale=2):
    w, h = img.size
    return img.resize((w // scale, h // scale), Image.Resampling.LANCZOS)

# ==============================================================
# 1. ACTION CIRCLE BUTTON (JADE / GOLD DEFENSE BARRIER)
# ==============================================================
def draw_action_circle(pressed=False):
    size = (256, 256)
    img, S = create_supersampled(size, scale=3)
    draw = ImageDraw.Draw(img)
    cx, cy = 128 * S, 128 * S
    R = 110 * S

    # Drop Shadow / Glow
    if pressed:
        # Glowing Gold Aura
        for r in range(int(R * 1.18), int(R * 0.98), -1):
            alpha = int(90 * (1.0 - (r - R * 0.98) / (R * 0.20)))
            draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(255, 215, 0, alpha))
    else:
        # Subtle Drop shadow below
        shadow_y = cy + 12 * S
        draw.ellipse([cx - R, shadow_y - R * 0.92, cx + R, shadow_y + R * 0.92], fill=(0, 0, 0, 110))

    # Outer 3D Bevel Rim (Gold / Bronze)
    offset_y = 6 * S if pressed else 0
    cy_body = cy + offset_y

    # 3D Base Rim (darker bronze at bottom)
    draw.ellipse([cx - R, cy_body - R, cx + R, cy_body + R], fill=(120, 85, 25, 255))
    rim_w = 8 * S
    R_inner = R - rim_w

    # Rim gradient (Light gold to dark bronze)
    draw.ellipse([cx - R + 2*S, cy_body - R + 2*S, cx + R - 2*S, cy_body + R - 2*S], fill=(235, 185, 55, 255))
    draw.ellipse([cx - R_inner, cy_body - R_inner, cx + R_inner, cy_body + R_inner], fill=(160, 110, 30, 255))

    # Face Plate (Emerald / Jade gradient)
    R_face = R_inner - 4 * S
    if pressed:
        face_color_top = (46, 204, 113) # Vibrant bright emerald
        face_color_bot = (22, 130, 70)
    else:
        face_color_top = (39, 174, 96)  # Deep rich jade
        face_color_bot = (16, 95, 50)

    # Draw radial/vertical gradient face
    face_img = Image.new("RGBA", img.size, (0, 0, 0, 0))
    face_draw = ImageDraw.Draw(face_img)
    for y_step in range(int(cy_body - R_face), int(cy_body + R_face)):
        ratio = (y_step - (cy_body - R_face)) / (2.0 * R_face)
        ratio = max(0.0, min(1.0, ratio))
        r = int(face_color_top[0] * (1 - ratio) + face_color_bot[0] * ratio)
        g = int(face_color_top[1] * (1 - ratio) + face_color_bot[1] * ratio)
        b = int(face_color_top[2] * (1 - ratio) + face_color_bot[2] * ratio)
        face_draw.line([(cx - R_face, y_step), (cx + R_face, y_step)], fill=(r, g, b, 255))

    # Mask face to circle
    mask = Image.new("L", img.size, 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.ellipse([cx - R_face, cy_body - R_face, cx + R_face, cy_body + R_face], fill=255)
    img.paste(face_img, (0, 0), mask)

    # Top Gloss / Specular Arc
    if not pressed:
        gloss = Image.new("RGBA", img.size, (0, 0, 0, 0))
        g_draw = ImageDraw.Draw(gloss)
        g_draw.ellipse([cx - R_face * 0.85, cy_body - R_face * 0.95, cx + R_face * 0.85, cy_body - R_face * 0.15],
                       fill=(255, 255, 255, 65))
        img = Image.alpha_composite(img, gloss)

    # Center Shield Glyph
    draw = ImageDraw.Draw(img)
    glyph_y = cy_body
    sw = 42 * S
    sh = 52 * S
    shield_pts = [
        (cx - sw, glyph_y - sh * 0.65),
        (cx + sw, glyph_y - sh * 0.65),
        (cx + sw, glyph_y),
        (cx, glyph_y + sh * 0.75),
        (cx - sw, glyph_y)
    ]
    glyph_color = (255, 255, 255, 245) if pressed else (240, 245, 235, 230)
    draw.polygon(shield_pts, fill=glyph_color)

    # Inner decorative shield cross / emblem
    inner_pts = [
        (cx - sw * 0.7, glyph_y - sh * 0.45),
        (cx + sw * 0.7, glyph_y - sh * 0.45),
        (cx + sw * 0.7, glyph_y),
        (cx, glyph_y + sh * 0.55),
        (cx - sw * 0.7, glyph_y)
    ]
    inner_color = (30, 140, 75, 255) if pressed else (20, 100, 55, 255)
    draw.polygon(inner_pts, fill=inner_color)

    return downsample(img, S)

# ==============================================================
# 2. PRIMARY CTA PLAY BUTTON (ROUNDED SQUARE 256x256 WITH BAKED PLAY TRIANGLE)
# ==============================================================
def draw_primary_cta(pressed=False):
    size = (256, 256)
    img, S = create_supersampled(size, scale=3)
    draw = ImageDraw.Draw(img)
    w_px, h_px = 256 * S, 256 * S
    cx, cy = 128 * S, 128 * S
    pad = 18 * S
    radius = 48 * S
    extrusion = 14 * S

    # Drop shadow
    shadow_y = pad + (4 * S if pressed else extrusion + 8 * S)
    draw.rounded_rectangle([pad, shadow_y, w_px - pad, h_px - pad], radius=radius, fill=(0, 0, 0, 110))

    # 3D Bottom Lip / Extrusion (Dark Amber / Mahogany)
    draw.rounded_rectangle([pad, pad + (4 * S if pressed else extrusion), w_px - pad, h_px - pad - (2 * S if pressed else 0)],
                           radius=radius, fill=(150, 42, 8, 255))

    # Face plate position
    face_y0 = pad + (extrusion if pressed else 0)
    face_y1 = h_px - pad - (2 * S if pressed else extrusion)

    # Outer Gold Rim
    draw.rounded_rectangle([pad, face_y0, w_px - pad, face_y1], radius=radius, fill=(255, 215, 0, 255))

    # Inner gradient (Vibrant Gold to Imperial Crimson)
    in_pad = 6 * S
    face_img = Image.new("RGBA", img.size, (0, 0, 0, 0))
    face_draw = ImageDraw.Draw(face_img)
    col_top = (255, 195, 20) if pressed else (255, 160, 0)
    col_bot = (220, 38, 15) if pressed else (195, 25, 12)

    for y in range(int(face_y0 + in_pad), int(face_y1 - in_pad)):
        t = (y - (face_y0 + in_pad)) / max(1.0, (face_y1 - face_y0 - 2 * in_pad))
        r = int(col_top[0] * (1 - t) + col_bot[0] * t)
        g = int(col_top[1] * (1 - t) + col_bot[1] * t)
        b = int(col_top[2] * (1 - t) + col_bot[2] * t)
        face_draw.line([(pad + in_pad, y), (w_px - pad - in_pad, y)], fill=(r, g, b, 255))

    mask = Image.new("L", img.size, 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rounded_rectangle([pad + in_pad, face_y0 + in_pad, w_px - pad - in_pad, face_y1 - in_pad],
                                radius=radius - in_pad, fill=255)
    img.paste(face_img, (0, 0), mask)

    # Gloss Highlight (top half)
    if not pressed:
        gloss = Image.new("RGBA", img.size, (0, 0, 0, 0))
        g_draw = ImageDraw.Draw(gloss)
        g_h = (face_y1 - face_y0) * 0.45
        g_draw.rounded_rectangle([pad + in_pad + 2*S, face_y0 + in_pad + 2*S, w_px - pad - in_pad - 2*S, face_y0 + g_h],
                                 radius=radius - in_pad, fill=(255, 255, 255, 55))
        img = Image.alpha_composite(img, gloss)

    # BAKED BOLD WHITE PLAY TRIANGLE (NEVER RELIES ON FONT!)
    draw = ImageDraw.Draw(img)
    tri_cy = (face_y0 + face_y1) * 0.5
    tri_cx = cx + 4 * S  # Slightly optical right
    tw = 40 * S
    th = 48 * S

    # Triangle drop shadow
    shadow_tri = [
        (tri_cx - tw * 0.7, tri_cy - th * 0.8 + 4 * S),
        (tri_cx + tw * 0.85, tri_cy + 4 * S),
        (tri_cx - tw * 0.7, tri_cy + th * 0.8 + 4 * S)
    ]
    draw.polygon(shadow_tri, fill=(80, 15, 0, 140))

    # Main White Play Triangle
    main_tri = [
        (tri_cx - tw * 0.7, tri_cy - th * 0.8),
        (tri_cx + tw * 0.85, tri_cy),
        (tri_cx - tw * 0.7, tri_cy + th * 0.8)
    ]
    tri_color = (255, 255, 255, 255) if pressed else (255, 252, 245, 255)
    draw.polygon(main_tri, fill=tri_color)

    return downsample(img, S)

# ==============================================================
# 3. SECONDARY DOCK BUTTON (SLATE / GOLD LACQUER CARD)
# ==============================================================
def draw_secondary_card(highlight=False):
    w, h = 384, 110
    img, S = create_supersampled((w, h), scale=3)
    draw = ImageDraw.Draw(img)
    pad_x, pad_y = 8 * S, 8 * S
    radius = 24 * S

    # Shadow
    draw.rounded_rectangle([pad_x, pad_y + 4*S, w * S - pad_x, h * S - pad_y + 4*S], radius=radius, fill=(0, 0, 0, 95))

    # Border (Gold if highlighted, subtle slate gold if normal)
    border_col = (255, 215, 0, 255) if highlight else (180, 150, 90, 180)
    draw.rounded_rectangle([pad_x, pad_y, w * S - pad_x, h * S - pad_y], radius=radius, fill=border_col)

    # Body Fill (Dark Royal Charcoal Lacquer)
    in_pad = 4 * S
    bg_top = (35, 45, 60) if highlight else (22, 28, 38)
    bg_bot = (18, 22, 30) if highlight else (12, 15, 22)

    face_img = Image.new("RGBA", img.size, (0, 0, 0, 0))
    face_draw = ImageDraw.Draw(face_img)
    for y in range(int(pad_y + in_pad), int(h * S - pad_y - in_pad)):
        t = (y - (pad_y + in_pad)) / max(1.0, (h * S - 2 * (pad_y + in_pad)))
        r = int(bg_top[0] * (1 - t) + bg_bot[0] * t)
        g = int(bg_top[1] * (1 - t) + bg_bot[1] * t)
        b = int(bg_top[2] * (1 - t) + bg_bot[2] * t)
        face_draw.line([(pad_x + in_pad, y), (w * S - pad_x - in_pad, y)], fill=(r, g, b, 245))

    mask = Image.new("L", img.size, 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rounded_rectangle([pad_x + in_pad, pad_y + in_pad, w * S - pad_x - in_pad, h * S - pad_y - in_pad],
                                radius=radius - in_pad, fill=255)
    img.paste(face_img, (0, 0), mask)

    return downsample(img, S)

# ==============================================================
# 4. CRYSTAL ARROW QTE BUTTONS (LEFT, UP, RIGHT)
# ==============================================================
def draw_crystal_arrow(direction="left"):
    # direction: "left", "up", "right"
    size = (200, 200)
    img, S = create_supersampled(size, scale=3)
    draw = ImageDraw.Draw(img)
    cx, cy = 100 * S, 100 * S
    R = 86 * S

    colors = {
        "left": {"glow": (41, 128, 185), "rim": (52, 152, 219), "top": (70, 180, 245), "bot": (25, 90, 140)},
        "up": {"glow": (39, 174, 96), "rim": (46, 204, 113), "top": (80, 220, 130), "bot": (20, 110, 60)},
        "right": {"glow": (211, 84, 0), "rim": (230, 126, 34), "top": (255, 150, 50), "bot": (160, 50, 10)}
    }[direction]

    # Outer glow
    for r in range(int(R * 1.15), int(R), -1):
        alpha = int(70 * (1.0 - (r - R) / (R * 0.15)))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(*colors["glow"], alpha))

    # 3D Base
    draw.ellipse([cx - R, cy + 6*S - R, cx + R, cy + 6*S + R], fill=(15, 20, 30, 255))
    draw.ellipse([cx - R, cy - R, cx + R, cy + R], fill=(*colors["rim"], 255))

    # Inner Crystal gradient
    R_in = R - 6 * S
    face_img = Image.new("RGBA", img.size, (0, 0, 0, 0))
    face_draw = ImageDraw.Draw(face_img)
    col_t = colors["top"]
    col_b = colors["bot"]
    for y in range(int(cy - R_in), int(cy + R_in)):
        t = (y - (cy - R_in)) / (2.0 * R_in)
        r = int(col_t[0] * (1 - t) + col_b[0] * t)
        g = int(col_t[1] * (1 - t) + col_b[1] * t)
        b = int(col_t[2] * (1 - t) + col_b[2] * t)
        face_draw.line([(cx - R_in, y), (cx + R_in, y)], fill=(r, g, b, 255))

    mask = Image.new("L", img.size, 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.ellipse([cx - R_in, cy - R_in, cx + R_in, cy + R_in], fill=255)
    img.paste(face_img, (0, 0), mask)

    # Stylized Arrow Glyph
    draw = ImageDraw.Draw(img)
    aw = 40 * S
    ah = 36 * S
    if direction == "left":
        arrow_pts = [
            (cx - aw * 0.8, cy),
            (cx + aw * 0.6, cy - ah * 0.85),
            (cx + aw * 0.2, cy - ah * 0.25),
            (cx + aw * 0.8, cy - ah * 0.25),
            (cx + aw * 0.8, cy + ah * 0.25),
            (cx + aw * 0.2, cy + ah * 0.25),
            (cx + aw * 0.6, cy + ah * 0.85)
        ]
    elif direction == "right":
        arrow_pts = [
            (cx + aw * 0.8, cy),
            (cx - aw * 0.6, cy - ah * 0.85),
            (cx - aw * 0.2, cy - ah * 0.25),
            (cx - aw * 0.8, cy - ah * 0.25),
            (cx - aw * 0.8, cy + ah * 0.25),
            (cx - aw * 0.2, cy + ah * 0.25),
            (cx - aw * 0.6, cy + ah * 0.85)
        ]
    else: # up
        arrow_pts = [
            (cx, cy - aw * 0.8),
            (cx - ah * 0.85, cy + aw * 0.6),
            (cx - ah * 0.25, cy + aw * 0.2),
            (cx - ah * 0.25, cy + aw * 0.8),
            (cx + ah * 0.25, cy + aw * 0.8),
            (cx + ah * 0.25, cy + aw * 0.2),
            (cx + ah * 0.85, cy + aw * 0.6)
        ]
    draw.polygon(arrow_pts, fill=(255, 255, 255, 245))

    return downsample(img, S)

# ==============================================================
# 5. VIRTUAL JOYSTICK (FROSTED GLASS RING & GLOSSY KNOB)
# ==============================================================
def draw_joystick_base():
    size = (256, 256)
    img, S = create_supersampled(size, scale=3)
    draw = ImageDraw.Draw(img)
    cx, cy = 128 * S, 128 * S
    R = 115 * S

    # Frosted outer ring
    draw.ellipse([cx - R, cy - R, cx + R, cy + R], fill=(255, 255, 255, 28), outline=(255, 255, 255, 110), width=3*S)
    # Inner directional cross marks
    for ang_deg in [0, 90, 180, 270]:
        rad = math.radians(ang_deg)
        x1 = cx + (R - 18 * S) * math.cos(rad)
        y1 = cy + (R - 18 * S) * math.sin(rad)
        x2 = cx + (R - 6 * S) * math.cos(rad)
        y2 = cy + (R - 6 * S) * math.sin(rad)
        draw.line([(x1, y1), (x2, y2)], fill=(255, 255, 255, 140), width=3*S)

    return downsample(img, S)

def draw_joystick_knob():
    size = (128, 128)
    img, S = create_supersampled(size, scale=3)
    draw = ImageDraw.Draw(img)
    cx, cy = 64 * S, 64 * S
    R = 54 * S

    # Outer cyan glow
    draw.ellipse([cx - R, cy - R, cx + R, cy + R], fill=(0, 210, 255, 180))
    # Bevel ring
    R_in = R - 4 * S
    draw.ellipse([cx - R_in, cy - R_in, cx + R_in, cy + R_in], fill=(25, 130, 180, 230))
    # Center dome
    R_core = R_in - 5 * S
    draw.ellipse([cx - R_core, cy - R_core, cx + R_core, cy + R_core], fill=(40, 220, 255, 240))
    # Gloss arc
    draw.ellipse([cx - R_core * 0.7, cy - R_core * 0.85, cx + R_core * 0.7, cy - R_core * 0.1], fill=(255, 255, 255, 120))

    return downsample(img, S)

if __name__ == "__main__":
    draw_action_circle(pressed=False).save(os.path.join(OutputDir, "btn_action_circle_normal.png"))
    draw_action_circle(pressed=True).save(os.path.join(OutputDir, "btn_action_circle_pressed.png"))
    draw_primary_cta(pressed=False).save(os.path.join(OutputDir, "btn_primary_cta_normal.png"))
    draw_primary_cta(pressed=True).save(os.path.join(OutputDir, "btn_primary_cta_pressed.png"))
    draw_secondary_card(highlight=False).save(os.path.join(OutputDir, "btn_secondary_card_normal.png"))
    draw_secondary_card(highlight=True).save(os.path.join(OutputDir, "btn_secondary_card_hover.png"))
    draw_crystal_arrow("left").save(os.path.join(OutputDir, "btn_qte_left.png"))
    draw_crystal_arrow("up").save(os.path.join(OutputDir, "btn_qte_up.png"))
    draw_crystal_arrow("right").save(os.path.join(OutputDir, "btn_qte_right.png"))
    draw_joystick_base().save(os.path.join(OutputDir, "joystick_base.png"))
    draw_joystick_knob().save(os.path.join(OutputDir, "joystick_knob.png"))
    print("ALL PROFESSIONAL GAME UI GRAPHICS BAKED SUCCESSFULLY!")
