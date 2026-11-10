import math
import os

OutputDir = "Assets/_Game/Models/Characters"
os.makedirs(OutputDir, exist_ok=True)

class ObjMeshBuilder:
    def __init__(self, name="Mesh"):
        self.name = name
        self.vertices = []
        self.normals = []
        self.uvs = []
        self.faces = []

    def add_vertex(self, x, y, z):
        self.vertices.append((x, y, z))
        return len(self.vertices)

    def add_normal(self, nx, ny, nz):
        l = math.sqrt(nx*nx + ny*ny + nz*nz)
        if l > 1e-6:
            nx, ny, nz = nx/l, ny/l, nz/l
        else:
            nx, ny, nz = 0.0, 1.0, 0.0
        self.normals.append((nx, ny, nz))
        return len(self.normals)

    def add_uv(self, u, v):
        self.uvs.append((u, v))
        return len(self.uvs)

    def add_face(self, v_indices, uv_indices, norm_indices):
        self.faces.append(list(zip(v_indices, uv_indices, norm_indices)))

    def add_quad(self, v1, v2, v3, v4, uv1, uv2, uv3, uv4, n1, n2, n3, n4):
        self.add_face([v1, v2, v3], [uv1, uv2, uv3], [n1, n2, n3])
        self.add_face([v1, v3, v4], [uv1, uv3, uv4], [n1, n3, n4])

    def add_lathe(self, profile, segments=32, uv_box=(0, 0, 1, 1), cap_top=True, cap_bottom=True):
        """
        profile: list of (y, r, cx, cz) from bottom to top.
        Smooth analytic normals along profile curve.
        Back (-Z) is at u = 0 and u = 1. Front (+Z) is at u = 0.5.
        """
        N = len(profile)
        u0, v0, u1, v1 = uv_box
        rings = []

        # 1. Profile normal components
        p_normals = []
        for i in range(N):
            y_i, r_i, cx_i, cz_i = profile[i]
            if i == 0:
                dy = profile[1][0] - y_i
                dr = profile[1][1] - r_i
            elif i == N - 1:
                dy = y_i - profile[N-2][0]
                dr = r_i - profile[N-2][1]
            else:
                dy = profile[i+1][0] - profile[i-1][0]
                dr = profile[i+1][1] - profile[i-1][1]
            nr = dy
            ny = -dr
            l = math.sqrt(nr*nr + ny*ny)
            if l > 1e-6:
                nr, ny = nr/l, ny/l
            else:
                nr, ny = 1.0, 0.0
            p_normals.append((nr, ny))

        # 2. Build rings
        for i in range(N):
            y_i, r_i, cx_i, cz_i = profile[i]
            nr, ny = p_normals[i]
            v_val = v0 + (v1 - v0) * (i / (N - 1))
            ring = []

            for j in range(segments + 1):
                ang = -math.pi + 2.0 * math.pi * j / segments
                sin_a = math.sin(ang) # X
                cos_a = math.cos(ang) # Z (cos(0) = +1 at front)
                u_val = u0 + (u1 - u0) * (j / segments)

                vx = cx_i + r_i * sin_a
                vy = y_i
                vz = cz_i + r_i * cos_a

                nx = nr * sin_a
                nz = nr * cos_a

                vi = self.add_vertex(vx, vy, vz)
                ni = self.add_normal(nx, ny, nz)
                ui = self.add_uv(u_val, v_val)
                ring.append((vi, ui, ni))
            rings.append(ring)

        # 3. Quads between rings
        for i in range(N - 1):
            r1 = rings[i]
            r2 = rings[i+1]
            for j in range(segments):
                self.add_quad(r1[j][0], r2[j][0], r2[j+1][0], r1[j+1][0],
                              r1[j][1], r2[j][1], r2[j+1][1], r1[j+1][1],
                              r1[j][2], r2[j][2], r2[j+1][2], r1[j+1][2])

        # 4. Cap Bottom (Correct Clockwise facing down -Y)
        if cap_bottom and profile[0][1] > 0.005:
            y_b, r_b, cx_b, cz_b = profile[0]
            vc = self.add_vertex(cx_b, y_b, cz_b)
            nc = self.add_normal(0, -1, 0)
            uc = self.add_uv((u0+u1)*0.5, v0)
            for j in range(segments):
                self.add_face([vc, rings[0][j+1][0], rings[0][j][0]],
                              [uc, rings[0][j+1][1], rings[0][j][1]],
                              [nc, nc, nc])

        # 5. Cap Top (Correct Clockwise facing up +Y)
        if cap_top and profile[-1][1] > 0.005:
            y_t, r_t, cx_t, cz_t = profile[-1]
            vc = self.add_vertex(cx_t, y_t, cz_t)
            nc = self.add_normal(0, 1, 0)
            uc = self.add_uv((u0+u1)*0.5, v1)
            for j in range(segments):
                self.add_face([vc, rings[-1][j][0], rings[-1][j+1][0]],
                              [uc, rings[-1][j][1], rings[-1][j+1][1]],
                              [nc, nc, nc])

    def add_tube_path(self, path_points, radii, segments=12, uv_box=(0, 0, 1, 1), cap_ends=True):
        rings = []
        u0, v0, u1, v1 = uv_box
        num_pts = len(path_points)

        for i, (p, r) in enumerate(zip(path_points, radii)):
            if i == 0:
                tangent = (path_points[1][0] - p[0], path_points[1][1] - p[1], path_points[1][2] - p[2])
            elif i == num_pts - 1:
                tangent = (p[0] - path_points[i-1][0], p[1] - path_points[i-1][1], p[2] - path_points[i-1][2])
            else:
                tangent = (path_points[i+1][0] - path_points[i-1][0], path_points[i+1][1] - path_points[i-1][1], path_points[i+1][2] - path_points[i-1][2])

            tx, ty, tz = tangent
            tlen = math.sqrt(tx*tx + ty*ty + tz*tz)
            if tlen < 1e-5: tx, ty, tz = 0, 1, 0
            else: tx, ty, tz = tx/tlen, ty/tlen, tz/tlen

            up = (0, 1, 0) if abs(ty) < 0.9 else (0, 0, 1)
            bx = ty * up[2] - tz * up[1]
            by = tz * up[0] - tx * up[2]
            bz = tx * up[1] - ty * up[0]
            blen = math.sqrt(bx*bx + by*by + bz*bz)
            bx, by, bz = bx/blen, by/blen, bz/blen

            nx = by * tz - bz * ty
            ny = bz * tx - bx * tz
            nz = bx * ty - by * tx

            v_val = v0 + (v1 - v0) * (i / (num_pts - 1))
            ring = []
            for j in range(segments + 1):
                ang = 2 * math.pi * j / segments
                cos_a = math.cos(ang)
                sin_a = math.sin(ang)
                u_val = u0 + (u1 - u0) * (j / segments)

                ox = r * (cos_a * nx + sin_a * bx)
                oy = r * (cos_a * ny + sin_a * by)
                oz = r * (cos_a * nz + sin_a * bz)

                vx = p[0] + ox
                vy = p[1] + oy
                vz = p[2] + oz

                v_idx = self.add_vertex(vx, vy, vz)
                norm_idx = self.add_normal(ox/r, oy/r, oz/r)
                uv_idx = self.add_uv(u_val, v_val)
                ring.append((v_idx, uv_idx, norm_idx))
            rings.append(ring)

        for i in range(num_pts - 1):
            r1 = rings[i]
            r2 = rings[i+1]
            for j in range(segments):
                self.add_quad(r1[j][0], r2[j][0], r2[j+1][0], r1[j+1][0],
                              r1[j][1], r2[j][1], r2[j+1][1], r1[j+1][1],
                              r1[j][2], r2[j][2], r2[j+1][2], r1[j+1][2])

        if cap_ends:
            p_start = path_points[0]
            vc_start = self.add_vertex(*p_start)
            nc_start = self.add_normal(-(path_points[1][0]-p_start[0]), -(path_points[1][1]-p_start[1]), -(path_points[1][2]-p_start[2]))
            uc_start = self.add_uv((u0+u1)*0.5, v0)
            for j in range(segments):
                self.add_face([vc_start, rings[0][j+1][0], rings[0][j][0]],
                              [uc_start, rings[0][j+1][1], rings[0][j][1]],
                              [nc_start, nc_start, nc_start])

            p_end = path_points[-1]
            vc_end = self.add_vertex(*p_end)
            nc_end = self.add_normal(p_end[0]-path_points[-2][0], p_end[1]-path_points[-2][1], p_end[2]-path_points[-2][2])
            uc_end = self.add_uv((u0+u1)*0.5, v1)
            for j in range(segments):
                self.add_face([vc_end, rings[-1][j][0], rings[-1][j+1][0]],
                              [uc_end, rings[-1][j][1], rings[-1][j+1][1]],
                              [nc_end, nc_end, nc_end])

    def add_sphere(self, radius, center, segments_lat=16, segments_lon=24, uv_box=(0, 0, 1, 1)):
        cx, cy, cz = center
        u0, v0, u1, v1 = uv_box
        grid = []
        for i in range(segments_lat + 1):
            theta = math.pi * i / segments_lat
            sin_t = math.sin(theta)
            cos_t = math.cos(theta)
            v_val = v0 + (v1 - v0) * (1.0 - i / segments_lat)
            row = []
            for j in range(segments_lon + 1):
                phi = -math.pi + 2 * math.pi * j / segments_lon
                sin_p = math.sin(phi)
                cos_p = math.cos(phi)
                u_val = u0 + (u1 - u0) * (j / segments_lon)

                nx = sin_t * sin_p
                ny = cos_t
                nz = sin_t * cos_p

                x = cx + radius * nx
                y = cy + radius * ny
                z = cz + radius * nz

                vi = self.add_vertex(x, y, z)
                ni = self.add_normal(nx, ny, nz)
                ui = self.add_uv(u_val, v_val)
                row.append((vi, ui, ni))
            grid.append(row)

        for i in range(segments_lat):
            for j in range(segments_lon):
                v1, u1_idx, n1 = grid[i][j]
                v2, u2_idx, n2 = grid[i+1][j]
                v3, u3_idx, n3 = grid[i+1][j+1]
                v4, u4_idx, n4 = grid[i][j+1]
                if i == 0:
                    self.add_face([v1, v2, v3], [u1_idx, u2_idx, u3_idx], [n1, n2, n3])
                elif i == segments_lat - 1:
                    self.add_face([v1, v3, v4], [u1_idx, u3_idx, u4_idx], [n1, n3, n4])
                else:
                    self.add_quad(v1, v2, v3, v4, u1_idx, u2_idx, u3_idx, u4_idx, n1, n2, n3, n4)

    def export_obj(self, filepath):
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(f"# Solid Watertight 3D Model: {self.name}\n")
            f.write(f"o {self.name}\n")
            for x, y, z in self.vertices:
                f.write(f"v {x:.4f} {y:.4f} {z:.4f}\n")
            for u, v in self.uvs:
                f.write(f"vt {u:.4f} {v:.4f}\n")
            for nx, ny, nz in self.normals:
                f.write(f"vn {nx:.4f} {ny:.4f} {nz:.4f}\n")
            f.write("s 1\n")
            for face in self.faces:
                f.write("f " + " ".join(f"{v}/{vt}/{vn}" for v, vt, vn in face) + "\n")
        print(f"Exported: {filepath} ({len(self.vertices)} verts, {len(self.faces)} faces)")

# ==============================================================
# 1. CHIBI SOLID HEAD WITH CENTERED EXPRESSIVE FACE
# ==============================================================
def build_chibi_head():
    b = ObjMeshBuilder("Chibi_Head")
    radius = 0.34
    segments_lat = 24
    segments_lon = 32
    grid = []

    for i in range(segments_lat + 1):
        theta = math.pi * i / segments_lat
        sin_t = math.sin(theta)
        cos_t = math.cos(theta)
        row = []
        for j in range(segments_lon + 1):
            phi = -math.pi + 2.0 * math.pi * j / segments_lon
            sin_p = math.sin(phi) # X
            cos_p = math.cos(phi) # Z

            nx = sin_t * sin_p
            ny = cos_t
            nz = sin_t * cos_p

            x = radius * nx
            y = radius * ny
            z = radius * nz

            # Planar front face UV projection:
            # Centered so eyes and cheeks sit right on the face front (+Z)
            if z > -0.04:
                u = 0.50 + (x / (2.0 * radius * 0.85))
                v = 0.56 + (y / (2.0 * radius * 0.78))
                u = max(0.04, min(0.96, u))
                v = max(0.04, min(0.96, v))
            else:
                u = 0.05
                v = 0.05

            vi = b.add_vertex(x, y, z)
            ni = b.add_normal(nx, ny, nz)
            ui = b.add_uv(u, v)
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

# ==============================================================
# 2. CHIBI MOTHER (WATERTIGHT ÁO BÀ BA, CURVED ARMS, QUẦN LỤA)
# ==============================================================
def build_chibi_mother():
    b = ObjMeshBuilder("Chibi_Mother")

    # A. Smooth Lathe Torso / Áo bà ba (silky curve, fully closed at bottom)
    profile = [
        (0.28, 0.02, 0.0, 0.0),  # Inner closed base
        (0.29, 0.20, 0.0, 0.0),
        (0.30, 0.33, 0.0, 0.0),  # Flared hem of áo bà ba
        (0.40, 0.31, 0.0, 0.0),
        (0.52, 0.28, 0.0, 0.0),
        (0.66, 0.25, 0.0, 0.0),  # Tapered waist
        (0.80, 0.26, 0.0, 0.01),
        (0.92, 0.27, 0.0, 0.02), # Chest
        (1.02, 0.24, 0.0, 0.01), # Shoulders
        (1.10, 0.17, 0.0, 0.00), # Mandarin collar
        (1.13, 0.15, 0.0, 0.00)  # Collar rim
    ]
    b.add_lathe(profile, segments=32, uv_box=(0.05, 0.15, 0.95, 0.95), cap_top=True, cap_bottom=True)

    # B. Quần lụa đen (silk pants under hem)
    prof_leg_l = [
        (0.08, 0.11, -0.13, 0.0),
        (0.18, 0.12, -0.13, 0.0),
        (0.30, 0.13, -0.13, 0.0)
    ]
    b.add_lathe(prof_leg_l, segments=16, uv_box=(0.0, 0.0, 0.2, 0.2), cap_top=False, cap_bottom=True)

    prof_leg_r = [
        (0.08, 0.11, 0.13, 0.0),
        (0.18, 0.12, 0.13, 0.0),
        (0.30, 0.13, 0.13, 0.0)
    ]
    b.add_lathe(prof_leg_r, segments=16, uv_box=(0.0, 0.0, 0.2, 0.2), cap_top=False, cap_bottom=True)

    # Shoes / Guốc mộc
    b.add_sphere(radius=0.08, center=(-0.13, 0.07, 0.05), segments_lat=8, segments_lon=12, uv_box=(0.0, 0.0, 0.2, 0.2))
    b.add_sphere(radius=0.08, center=(0.13, 0.07, 0.05), segments_lat=8, segments_lon=12, uv_box=(0.0, 0.0, 0.2, 0.2))

    # C. Gracefully Curved Shielding Arms (elbows bent forward and down, protective embrace)
    pts_left = [
        (-0.24, 0.98, 0.02),
        (-0.46, 0.90, 0.08),
        (-0.70, 0.84, 0.16),
        (-0.96, 0.80, 0.22),
        (-1.20, 0.78, 0.24)
    ]
    radii_left = [0.085, 0.078, 0.072, 0.068, 0.062]
    b.add_tube_path(pts_left, radii_left, segments=12, uv_box=(0.1, 0.3, 0.9, 0.8), cap_ends=True)
    b.add_sphere(radius=0.070, center=(-1.22, 0.78, 0.24), segments_lat=8, segments_lon=12)

    pts_right = [
        (0.24, 0.98, 0.02),
        (0.46, 0.90, 0.08),
        (0.70, 0.84, 0.16),
        (0.96, 0.80, 0.22),
        (1.20, 0.78, 0.24)
    ]
    radii_right = [0.085, 0.078, 0.072, 0.068, 0.062]
    b.add_tube_path(pts_right, radii_right, segments=12, uv_box=(0.1, 0.3, 0.9, 0.8), cap_ends=True)
    b.add_sphere(radius=0.070, center=(1.22, 0.78, 0.24), segments_lat=8, segments_lon=12)

    b.export_obj(os.path.join(OutputDir, "Chibi_Mother.obj"))

# ==============================================================
# 3. CHIBI CHILD (WATERTIGHT CUTE CHUBBY BODY, ÁO YẾM)
# ==============================================================
def build_chibi_child():
    b = ObjMeshBuilder("Chibi_Child")

    # A. Chubby cute baby silhouette (Áo yếm, fully closed base)
    profile = [
        (0.12, 0.02, 0.0, 0.0),  # Inner closed base
        (0.13, 0.16, 0.0, 0.0),
        (0.15, 0.21, 0.0, 0.0),  # Bottom
        (0.24, 0.24, 0.0, 0.01), # Chubby hips
        (0.36, 0.25, 0.0, 0.02), # Chubby tummy
        (0.46, 0.22, 0.0, 0.01), # Chest
        (0.56, 0.18, 0.0, 0.0),  # Upper chest
        (0.63, 0.14, 0.0, 0.0)   # Collar
    ]
    b.add_lathe(profile, segments=28, uv_box=(0.05, 0.10, 0.95, 0.90), cap_top=True, cap_bottom=True)

    # B. Chubby Little Legs & Feet
    prof_leg_l = [
        (0.04, 0.08, -0.09, 0.0),
        (0.10, 0.09, -0.09, 0.0),
        (0.16, 0.09, -0.09, 0.0)
    ]
    b.add_lathe(prof_leg_l, segments=12, uv_box=(0.0, 0.0, 0.2, 0.2), cap_top=False, cap_bottom=True)

    prof_leg_r = [
        (0.04, 0.08, 0.09, 0.0),
        (0.10, 0.09, 0.09, 0.0),
        (0.16, 0.09, 0.09, 0.0)
    ]
    b.add_lathe(prof_leg_r, segments=12, uv_box=(0.0, 0.0, 0.2, 0.2), cap_top=False, cap_bottom=True)

    b.add_sphere(radius=0.065, center=(-0.09, 0.04, 0.03), segments_lat=8, segments_lon=10)
    b.add_sphere(radius=0.065, center=(0.09, 0.04, 0.03), segments_lat=8, segments_lon=10)

    # C. Chubby Arms Forward (Holding sibling waist)
    pts_left = [
        (-0.16, 0.50, 0.02),
        (-0.15, 0.44, 0.10),
        (-0.12, 0.38, 0.20),
        (-0.09, 0.36, 0.24)
    ]
    b.add_tube_path(pts_left, [0.055, 0.050, 0.046, 0.042], segments=10, uv_box=(0.1, 0.2, 0.9, 0.8), cap_ends=True)
    b.add_sphere(radius=0.048, center=(-0.08, 0.36, 0.25), segments_lat=8, segments_lon=10)

    pts_right = [
        (0.16, 0.50, 0.02),
        (0.15, 0.44, 0.10),
        (0.12, 0.38, 0.20),
        (0.09, 0.36, 0.24)
    ]
    b.add_tube_path(pts_right, [0.055, 0.050, 0.046, 0.042], segments=10, uv_box=(0.1, 0.2, 0.9, 0.8), cap_ends=True)
    b.add_sphere(radius=0.048, center=(0.09, 0.36, 0.25), segments_lat=8, segments_lon=10)

    b.export_obj(os.path.join(OutputDir, "Chibi_Child.obj"))

# ==============================================================
# 4. CHIBI HUNTER (WATERTIGHT FLOWING ÁO DÀI, DYNAMIC POSE)
# ==============================================================
def build_chibi_hunter():
    b = ObjMeshBuilder("Chibi_Hunter")

    # A. Flowing Traditional Áo dài (closed bottom base)
    profile = [
        (0.10, 0.02, 0.0, 0.0),  # Inner closed base
        (0.11, 0.26, 0.0, 0.0),
        (0.12, 0.36, 0.0, 0.0),  # Flared skirt hem
        (0.26, 0.34, 0.0, 0.0),
        (0.42, 0.31, 0.0, 0.0),
        (0.58, 0.27, 0.0, 0.0),  # Waist sash
        (0.74, 0.26, 0.0, 0.01),
        (0.90, 0.28, 0.0, 0.02), # Chest
        (1.04, 0.24, 0.0, 0.01), # Shoulders
        (1.10, 0.17, 0.0, 0.00), # High collar base
        (1.14, 0.15, 0.0, 0.00)  # High collar rim
    ]
    b.add_lathe(profile, segments=32, uv_box=(0.05, 0.05, 0.95, 0.95), cap_top=True, cap_bottom=True)

    # Shoes under robe
    b.add_sphere(radius=0.08, center=(-0.11, 0.07, 0.04), segments_lat=8, segments_lon=10)
    b.add_sphere(radius=0.08, center=(0.11, 0.07, 0.04), segments_lat=8, segments_lon=10)

    # B. Right Arm: Lunging forward to GRAB at Z = +0.48
    pts_right = [
        (0.24, 0.96, 0.02),
        (0.28, 0.90, 0.16),
        (0.26, 0.84, 0.32),
        (0.22, 0.78, 0.44)
    ]
    b.add_tube_path(pts_right, [0.082, 0.075, 0.070, 0.065], segments=12, uv_box=(0.1, 0.3, 0.9, 0.8), cap_ends=True)
    b.add_sphere(radius=0.070, center=(0.21, 0.78, 0.46), segments_lat=8, segments_lon=12)

    # C. Left Arm: Composed posture at side
    pts_left = [
        (-0.24, 0.96, 0.02),
        (-0.33, 0.86, 0.06),
        (-0.34, 0.72, 0.08)
    ]
    b.add_tube_path(pts_left, [0.082, 0.075, 0.068], segments=12, uv_box=(0.1, 0.3, 0.9, 0.8), cap_ends=True)
    b.add_sphere(radius=0.066, center=(-0.34, 0.70, 0.08), segments_lat=8, segments_lon=12)

    b.export_obj(os.path.join(OutputDir, "Chibi_Hunter.obj"))

# ==============================================================
# 5. SOLID NÓN LÁ WITH RADIAL UV MAPPING & SILK STRAP
# ==============================================================
def build_non_la():
    b = ObjMeshBuilder("NonLa")
    segments = 32
    r_outer = 0.48
    h_outer = 0.22
    r_inner = 0.46
    h_inner = 0.20
    thickness = 0.015

    # 1. Outer Cone
    v_tip_out = b.add_vertex(0.0, h_outer, 0.0)
    n_tip_out = b.add_normal(0.0, 1.0, 0.0)
    u_tip_out = b.add_uv(0.5, 0.5)

    ring_out = []
    slope = r_outer / h_outer
    l_norm = math.sqrt(1.0 + slope*slope)
    n_slope_r = 1.0 / l_norm
    n_slope_y = slope / l_norm

    for j in range(segments + 1):
        ang = 2.0 * math.pi * j / segments
        sin_a = math.sin(ang)
        cos_a = math.cos(ang)

        vx = r_outer * sin_a
        vy = 0.0
        vz = r_outer * cos_a

        nx = n_slope_r * sin_a
        ny = n_slope_y
        nz = n_slope_r * cos_a

        u = 0.5 + 0.48 * (vx / r_outer)
        v = 0.5 + 0.48 * (vz / r_outer)

        v_idx = b.add_vertex(vx, vy, vz)
        n_idx = b.add_normal(nx, ny, nz)
        uv_idx = b.add_uv(u, v)
        ring_out.append((v_idx, uv_idx, n_idx))

    for j in range(segments):
        b.add_face([v_tip_out, ring_out[j+1][0], ring_out[j][0]],
                   [u_tip_out, ring_out[j+1][1], ring_out[j][1]],
                   [n_tip_out, ring_out[j+1][2], ring_out[j][2]])

    # 2. Inner Cone
    v_tip_in = b.add_vertex(0.0, h_inner, 0.0)
    n_tip_in = b.add_normal(0.0, -1.0, 0.0)
    u_tip_in = b.add_uv(0.5, 0.5)

    ring_in = []
    for j in range(segments + 1):
        ang = 2.0 * math.pi * j / segments
        sin_a = math.sin(ang)
        cos_a = math.cos(ang)

        vx = r_inner * sin_a
        vy = thickness
        vz = r_inner * cos_a

        nx = -n_slope_r * sin_a
        ny = -n_slope_y
        nz = -n_slope_r * cos_a

        u = 0.5 + 0.46 * (vx / r_inner)
        v = 0.5 + 0.46 * (vz / r_inner)

        v_idx = b.add_vertex(vx, vy, vz)
        n_idx = b.add_normal(nx, ny, nz)
        uv_idx = b.add_uv(u, v)
        ring_in.append((v_idx, uv_idx, n_idx))

    for j in range(segments):
        b.add_face([v_tip_in, ring_in[j][0], ring_in[j+1][0]],
                   [u_tip_in, ring_in[j][1], ring_in[j+1][1]],
                   [n_tip_in, ring_in[j][2], ring_in[j+1][2]])

    # 3. Solid Rim Quad
    for j in range(segments):
        v1 = ring_out[j][0]
        v2 = ring_in[j][0]
        v3 = ring_in[j+1][0]
        v4 = ring_out[j+1][0]
        uv1 = ring_out[j][1]
        uv2 = ring_in[j][1]
        uv3 = ring_in[j+1][1]
        uv4 = ring_out[j+1][1]
        nc = b.add_normal(0.0, -1.0, 0.0)
        b.add_quad(v1, v2, v3, v4, uv1, uv2, uv3, uv4, nc, nc, nc, nc)

    # 4. Quai Nón (Silk Ribbon Strap)
    strap_l = [
        (-0.28, 0.02, 0.0),
        (-0.22, -0.14, 0.05),
        (-0.10, -0.22, 0.07),
        (0.0, -0.24, 0.07)
    ]
    b.add_tube_path(strap_l, [0.016, 0.015, 0.014, 0.014], segments=8, uv_box=(0.1, 0.1, 0.9, 0.9))

    strap_r = [
        (0.28, 0.02, 0.0),
        (0.22, -0.14, 0.05),
        (0.10, -0.22, 0.07),
        (0.0, -0.24, 0.07)
    ]
    b.add_tube_path(strap_r, [0.016, 0.015, 0.014, 0.014], segments=8, uv_box=(0.1, 0.1, 0.9, 0.9))

    b.export_obj(os.path.join(OutputDir, "NonLa.obj"))

# ==============================================================
# 6. VƯƠNG MIỆN LÁ SEN DÁT VÀNG (CROWN CON UT)
# ==============================================================
def build_crown_con_ut():
    b = ObjMeshBuilder("Crown_ConUt")
    r_base = 0.18
    r_flare = 0.22
    num_petals = 6
    segments = 36

    profile_band = [
        (0.00, r_base, 0.0, 0.0),
        (0.03, r_base * 1.02, 0.0, 0.0),
        (0.06, r_base * 1.05, 0.0, 0.0)
    ]
    b.add_lathe(profile_band, segments=segments, uv_box=(0.0, 0.0, 1.0, 0.3), cap_top=False, cap_bottom=True)

    for p in range(num_petals):
        angle_center = 2.0 * math.pi * p / num_petals
        pts_petal = [
            (r_base * 1.05 * math.sin(angle_center), 0.06, r_base * 1.05 * math.cos(angle_center)),
            (r_flare * 1.08 * math.sin(angle_center), 0.10, r_flare * 1.08 * math.cos(angle_center)),
            (r_flare * 1.15 * math.sin(angle_center), 0.14, r_flare * 1.15 * math.cos(angle_center)),
            (r_flare * 1.08 * math.sin(angle_center), 0.17, r_flare * 1.08 * math.cos(angle_center))
        ]
        radii_petal = [0.035, 0.040, 0.030, 0.005]
        b.add_tube_path(pts_petal, radii_petal, segments=8, uv_box=(0.2, 0.3, 0.8, 0.9), cap_ends=True)

    b.add_sphere(radius=0.032, center=(0.0, 0.06, r_base * 1.06), segments_lat=8, segments_lon=10, uv_box=(0.4, 0.4, 0.6, 0.6))
    b.export_obj(os.path.join(OutputDir, "Crown_ConUt.obj"))

# ==============================================================
# 7. TÚI THUỐC BẮC GIA TRUYỀN (MEDICINE POUCH & SLING STRAP)
# ==============================================================
def build_medicine_pouch():
    b = ObjMeshBuilder("MedicinePouch")
    pouch_profile = [
        (0.48, 0.08, -0.28, 0.08),
        (0.52, 0.10, -0.28, 0.08),
        (0.58, 0.11, -0.28, 0.08),
        (0.63, 0.09, -0.28, 0.08),
        (0.67, 0.07, -0.28, 0.08),
        (0.70, 0.06, -0.28, 0.08)
    ]
    b.add_lathe(pouch_profile, segments=16, uv_box=(0.1, 0.1, 0.9, 0.9), cap_top=True, cap_bottom=True)

    strap_pts = [
        (-0.28, 0.64, 0.08),
        (-0.20, 0.72, 0.16),
        (-0.06, 0.82, 0.18),
        (0.12, 0.92, 0.14),
        (0.24, 0.98, 0.04)
    ]
    b.add_tube_path(strap_pts, [0.018, 0.018, 0.018, 0.018, 0.018], segments=8, uv_box=(0.0, 0.0, 0.5, 0.5))
    b.export_obj(os.path.join(OutputDir, "MedicinePouch.obj"))

# ==============================================================
# 8. HAIR STYLES FOR CHIBI CHARACTERS
# ==============================================================
def build_hair_styles():
    # Mother Hair
    b_m = ObjMeshBuilder("Chibi_Hair_Mother")
    b_m.add_sphere(radius=0.345, center=(0.0, 0.0, -0.02), segments_lat=14, segments_lon=20, uv_box=(0.0, 0.0, 0.2, 0.2))
    b_m.add_sphere(radius=0.12, center=(0.0, -0.04, -0.32), segments_lat=10, segments_lon=14, uv_box=(0.0, 0.0, 0.2, 0.2))
    b_m.export_obj(os.path.join(OutputDir, "Chibi_Hair_Mother.obj"))

    # Child Hair (Topknot)
    b_c = ObjMeshBuilder("Chibi_Hair_Child")
    b_c.add_sphere(radius=0.085, center=(0.0, 0.35, 0.02), segments_lat=10, segments_lon=14, uv_box=(0.0, 0.0, 0.2, 0.2))
    b_c.add_sphere(radius=0.092, center=(0.0, 0.31, 0.02), segments_lat=8, segments_lon=12, uv_box=(0.8, 0.8, 1.0, 1.0))
    b_c.add_sphere(radius=0.344, center=(0.0, 0.02, -0.03), segments_lat=12, segments_lon=18, uv_box=(0.0, 0.0, 0.2, 0.2))
    b_c.export_obj(os.path.join(OutputDir, "Chibi_Hair_Child.obj"))

    # Hunter Khăn Đóng
    b_h = ObjMeshBuilder("Chibi_KhanDong_Hunter")
    khan_prof = [
        (0.12, 0.345, 0.0, 0.0),
        (0.18, 0.360, 0.0, 0.0),
        (0.24, 0.355, 0.0, 0.0),
        (0.28, 0.340, 0.0, 0.0)
    ]
    b_h.add_lathe(khan_prof, segments=24, uv_box=(0.0, 0.0, 1.0, 1.0), cap_top=True, cap_bottom=True)
    b_h.export_obj(os.path.join(OutputDir, "Chibi_KhanDong_Hunter.obj"))

if __name__ == "__main__":
    build_chibi_head()
    build_chibi_mother()
    build_chibi_child()
    build_chibi_hunter()
    build_non_la()
    build_crown_con_ut()
    build_medicine_pouch()
    build_hair_styles()
    print("ALL REFINED CHIBI MODELS BUILT!")
