import math
import os
from obj_mesh_builder import ObjMeshBuilder

OutputDir = "Assets/_Game/Models/Characters"
os.makedirs(OutputDir, exist_ok=True)

# ==========================================
# 1. CHIBI MOTHER (SOLID TORSO & EMBRACING ARMS)
# ==========================================
def build_mother():
    b = ObjMeshBuilder("Chibi_Mother")

    # A. Torso / Áo bà ba (Segmented solid tapered cylinders)
    # Upper chest / collar (capped top)
    b.add_cylinder(r_top=0.18, r_bottom=0.25, y_top=1.12, y_bottom=0.92, segments=24, cap_top=True, cap_bottom=False, uv_box=(0.1, 0.65, 0.9, 0.95))
    # Mid torso / waist
    b.add_cylinder(r_top=0.25, r_bottom=0.23, y_top=0.92, y_bottom=0.68, segments=24, cap_top=False, cap_bottom=False, uv_box=(0.1, 0.40, 0.9, 0.65))
    # Flared hem / tạ áo bà ba (capped bottom)
    b.add_cylinder(r_top=0.23, r_bottom=0.32, y_top=0.68, y_bottom=0.30, segments=24, cap_top=False, cap_bottom=True, uv_box=(0.1, 0.10, 0.9, 0.40))

    # Trousers / Quần lụa đen dưới tạ áo
    b.add_cylinder(r_top=0.14, r_bottom=0.12, y_top=0.30, y_bottom=0.10, center_x=-0.14, segments=16, cap_top=False, cap_bottom=True, uv_box=(0.0, 0.0, 0.2, 0.2))
    b.add_cylinder(r_top=0.14, r_bottom=0.12, y_top=0.30, y_bottom=0.10, center_x=0.14, segments=16, cap_top=False, cap_bottom=True, uv_box=(0.0, 0.0, 0.2, 0.2))

    # B. Left Arm (Smooth solid curve to X = -1.35, Z = 0.25)
    pts_left = [
        (-0.24, 1.00, 0.04),
        (-0.52, 0.92, 0.12),
        (-0.85, 0.88, 0.22),
        (-1.18, 0.84, 0.28),
        (-1.35, 0.82, 0.25)
    ]
    radii_left = [0.090, 0.082, 0.076, 0.070, 0.065]
    b.add_tube_path(pts_left, radii_left, segments=12, uv_box=(0.1, 0.3, 0.9, 0.8))
    # Left Hand sphere
    b.add_sphere(radius=0.075, center=(-1.38, 0.82, 0.25), segments_lat=10, segments_lon=14, uv_box=(0.4, 0.4, 0.6, 0.6))

    # C. Right Arm (Smooth solid curve to X = +1.35, Z = 0.25)
    pts_right = [
        (0.24, 1.00, 0.04),
        (0.52, 0.92, 0.12),
        (0.85, 0.88, 0.22),
        (1.18, 0.84, 0.28),
        (1.35, 0.82, 0.25)
    ]
    radii_right = [0.090, 0.082, 0.076, 0.070, 0.065]
    b.add_tube_path(pts_right, radii_right, segments=12, uv_box=(0.1, 0.3, 0.9, 0.8))
    # Right Hand sphere
    b.add_sphere(radius=0.075, center=(1.38, 0.82, 0.25), segments_lat=10, segments_lon=14, uv_box=(0.4, 0.4, 0.6, 0.6))

    b.export_obj(os.path.join(OutputDir, "Chibi_Mother.obj"))

# ==========================================
# 2. CHIBI CHILD (SOLID CUTE BODY & HOLDING ARMS)
# ==========================================
def build_child():
    b = ObjMeshBuilder("Chibi_Child")

    # A. Chubby Torso / Áo yếm
    # Neck/Collar
    b.add_cylinder(r_top=0.14, r_bottom=0.20, y_top=0.62, y_bottom=0.48, segments=20, cap_top=True, cap_bottom=False, uv_box=(0.1, 0.5, 0.9, 0.9))
    # Belly & Bottom
    b.add_cylinder(r_top=0.20, r_bottom=0.22, y_top=0.48, y_bottom=0.30, segments=20, cap_top=False, cap_bottom=False, uv_box=(0.1, 0.25, 0.9, 0.5))
    b.add_cylinder(r_top=0.22, r_bottom=0.18, y_top=0.30, y_bottom=0.16, segments=20, cap_top=False, cap_bottom=True, uv_box=(0.1, 0.05, 0.9, 0.25))

    # Chubby little legs
    b.add_cylinder(r_top=0.08, r_bottom=0.07, y_top=0.16, y_bottom=0.05, center_x=-0.09, segments=12, cap_top=False, cap_bottom=True, uv_box=(0.0, 0.0, 0.2, 0.2))
    b.add_cylinder(r_top=0.08, r_bottom=0.07, y_top=0.16, y_bottom=0.05, center_x=0.09, segments=12, cap_top=False, cap_bottom=True, uv_box=(0.0, 0.0, 0.2, 0.2))

    # B. Arms reaching forward to hold front waist (Z = +0.28)
    pts_left = [
        (-0.16, 0.54, 0.02),
        (-0.15, 0.50, 0.14),
        (-0.12, 0.46, 0.26)
    ]
    b.add_tube_path(pts_left, [0.055, 0.050, 0.046], segments=10, uv_box=(0.1, 0.2, 0.9, 0.8))
    b.add_sphere(radius=0.052, center=(-0.12, 0.46, 0.28), segments_lat=8, segments_lon=12)

    pts_right = [
        (0.16, 0.54, 0.02),
        (0.15, 0.50, 0.14),
        (0.12, 0.46, 0.26)
    ]
    b.add_tube_path(pts_right, [0.055, 0.050, 0.046], segments=10, uv_box=(0.1, 0.2, 0.9, 0.8))
    b.add_sphere(radius=0.052, center=(0.12, 0.46, 0.28), segments_lat=8, segments_lon=12)

    b.export_obj(os.path.join(OutputDir, "Chibi_Child.obj"))

# ==========================================
# 3. CHIBI HUNTER (SOLID ROBE & GRABBING HAND)
# ==========================================
def build_hunter():
    b = ObjMeshBuilder("Chibi_Hunter")

    # A. Long Robe / Áo dài thầy thuốc
    # Collar
    b.add_cylinder(r_top=0.18, r_bottom=0.26, y_top=1.12, y_bottom=0.90, segments=24, cap_top=True, cap_bottom=False, uv_box=(0.1, 0.7, 0.9, 0.95))
    # Torso
    b.add_cylinder(r_top=0.26, r_bottom=0.28, y_top=0.90, y_bottom=0.60, segments=24, cap_top=False, cap_bottom=False, uv_box=(0.1, 0.4, 0.9, 0.7))
    # Robe Skirt (Flared down to ankles, fully solid capped)
    b.add_cylinder(r_top=0.28, r_bottom=0.36, y_top=0.60, y_bottom=0.18, segments=24, cap_top=False, cap_bottom=True, uv_box=(0.1, 0.05, 0.9, 0.4))

    # B. Right Arm (Lunging forward to GRAB at Z = +0.52)
    pts_right = [
        (0.24, 0.98, 0.04),
        (0.28, 0.92, 0.22),
        (0.26, 0.86, 0.42),
        (0.22, 0.82, 0.52)
    ]
    b.add_tube_path(pts_right, [0.085, 0.078, 0.072, 0.068], segments=12, uv_box=(0.1, 0.3, 0.9, 0.8))
    # Grabbing claw hand
    b.add_sphere(radius=0.08, center=(0.20, 0.82, 0.54), segments_lat=10, segments_lon=14)

    # C. Left Arm (Balancing at side)
    pts_left = [
        (-0.24, 0.98, 0.04),
        (-0.35, 0.86, 0.10),
        (-0.38, 0.70, 0.12)
    ]
    b.add_tube_path(pts_left, [0.085, 0.078, 0.070], segments=12, uv_box=(0.1, 0.3, 0.9, 0.8))
    b.add_sphere(radius=0.075, center=(-0.38, 0.68, 0.12), segments_lat=10, segments_lon=14)

    # D. Medicine Pouch on Hip
    b.add_cylinder(r_top=0.10, r_bottom=0.11, y_top=0.65, y_bottom=0.48, center_x=-0.28, center_z=0.08, segments=12, cap_top=True, cap_bottom=True)

    b.export_obj(os.path.join(OutputDir, "Chibi_Hunter.obj"))

# ==========================================
# 4. SOLID NÓN LÁ (DOUBLE-SIDED THICK CONE)
# ==========================================
def build_non_la():
    b = ObjMeshBuilder("NonLa")
    # Outer cone
    b.add_cylinder(r_top=0.03, r_bottom=0.52, y_top=0.22, y_bottom=0.0, segments=24, cap_top=True, cap_bottom=False, uv_box=(0.0, 0.0, 1.0, 1.0))
    # Inner cone (gives true solid thickness, avoiding transparent backface culling!)
    b.add_cylinder(r_top=0.02, r_bottom=0.50, y_top=0.21, y_bottom=0.01, segments=24, cap_top=False, cap_bottom=True, uv_box=(0.0, 0.0, 1.0, 1.0))
    b.export_obj(os.path.join(OutputDir, "NonLa.obj"))

# ==========================================
# 5. CHIBI SOLID HEAD WITH UV FOR FACE
# ==========================================
def build_head():
    b = ObjMeshBuilder("Chibi_Head")
    # Front-oriented sphere with seam at the back (-Z)
    cx, cy, cz = 0, 0, 0
    radius = 0.34
    segments_lat = 18
    segments_lon = 24
    grid = []
    for i in range(segments_lat + 1):
        theta = math.pi * i / segments_lat
        sin_t = math.sin(theta)
        cos_t = math.cos(theta)
        v_val = 1.0 - i / segments_lat
        row = []
        for j in range(segments_lon + 1):
            # Start from back (-Z, angle -pi/2) to front (+Z, angle +pi/2) and back
            phi = -math.pi * 0.5 + 2 * math.pi * j / segments_lon
            sin_p = math.sin(phi)
            cos_p = math.cos(phi)
            u_val = j / segments_lon

            nx = sin_t * cos_p
            ny = cos_t
            nz = sin_t * sin_p

            x = cx + radius * nx
            y = cy + radius * ny
            z = cz + radius * nz

            vi = b.add_vertex(x, y, z)
            ni = b.add_normal(nx, ny, nz)
            ui = b.add_uv(u_val, v_val)
            row.append((vi, ui, ni))
        grid.append(row)

    for i in range(segments_lat):
        for j in range(segments_lon):
            v1, u1_idx, n1 = grid[i][j]
            v2, u2_idx, n2 = grid[i+1][j]
            v3, u3_idx, n3 = grid[i+1][j+1]
            v4, u4_idx, n4 = grid[i][j+1]
            if i == 0:
                b.add_face([v1, v2, v3], [u1_idx, u2_idx, u3_idx], [n1, n2, n3])
            elif i == segments_lat - 1:
                b.add_face([v1, v3, v4], [u1_idx, u3_idx, u4_idx], [n1, n3, n4])
            else:
                b.add_quad(v1, v2, v3, v4, u1_idx, u2_idx, u3_idx, u4_idx, n1, n2, n3, n4)

    b.export_obj(os.path.join(OutputDir, "Chibi_Head.obj"))

if __name__ == "__main__":
    build_mother()
    build_child()
    build_hunter()
    build_non_la()
    build_head()
    print("All solid watertight 3D models built successfully!")
