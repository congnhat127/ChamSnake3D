import math
import os

class ObjMeshBuilder:
    def __init__(self, name="Mesh"):
        self.name = name
        self.vertices = []
        self.normals = []
        self.uvs = []
        self.faces = [] # list of [(v_idx, uv_idx, norm_idx), ...]

    def add_vertex(self, x, y, z):
        self.vertices.append((x, y, z))
        return len(self.vertices)

    def add_normal(self, nx, ny, nz):
        length = math.sqrt(nx*nx + ny*ny + nz*nz)
        if length > 1e-6:
            nx, ny, nz = nx/length, ny/length, nz/length
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
                phi = 2 * math.pi * j / segments_lon
                sin_p = math.sin(phi)
                cos_p = math.cos(phi)
                u_val = u0 + (u1 - u0) * (j / segments_lon)

                # Unit normal
                nx = sin_t * sin_p
                ny = cos_t
                nz = sin_t * cos_p

                # Position
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

    def add_cylinder(self, r_top, r_bottom, y_top, y_bottom, center_x=0, center_z=0, segments=20, cap_top=True, cap_bottom=True, uv_box=(0, 0, 1, 1)):
        u0, v0, u1, v1 = uv_box
        top_ring = []
        bot_ring = []
        height = y_top - y_bottom
        dr = r_bottom - r_top
        slope = dr / max(height, 1e-4)

        for j in range(segments + 1):
            angle = 2 * math.pi * j / segments
            cos_a = math.cos(angle)
            sin_a = math.sin(angle)
            u_val = u0 + (u1 - u0) * (j / segments)

            # Normal along cone/cylinder wall
            nx = sin_a
            ny = slope
            nz = cos_a

            # Top ring
            xt = center_x + r_top * sin_a
            zt = center_z + r_top * cos_a
            vt = self.add_vertex(xt, y_top, zt)
            nt = self.add_normal(nx, ny, nz)
            ut = self.add_uv(u_val, v1)
            top_ring.append((vt, ut, nt))

            # Bottom ring
            xb = center_x + r_bottom * sin_a
            zb = center_z + r_bottom * cos_a
            vb = self.add_vertex(xb, y_bottom, zb)
            nb = self.add_normal(nx, ny, nz)
            ub = self.add_uv(u_val, v0)
            bot_ring.append((vb, ub, nb))

        # Side walls
        for j in range(segments):
            v1, u1_idx, n1 = top_ring[j]
            v2, u2_idx, n2 = bot_ring[j]
            v3, u3_idx, n3 = bot_ring[j+1]
            v4, u4_idx, n4 = top_ring[j+1]
            self.add_quad(v1, v2, v3, v4, u1_idx, u2_idx, u3_idx, u4_idx, n1, n2, n3, n4)

        # Cap Top
        if cap_top and r_top > 0.001:
            vc = self.add_vertex(center_x, y_top, center_z)
            nc = self.add_normal(0, 1, 0)
            uc = self.add_uv((u0 + u1)*0.5, (v0 + v1)*0.5)
            for j in range(segments):
                vt1, ut1, _ = top_ring[j]
                vt2, ut2, _ = top_ring[j+1]
                self.add_face([vc, vt2, vt1], [uc, ut2, ut1], [nc, nc, nc])

        # Cap Bottom
        if cap_bottom and r_bottom > 0.001:
            vc = self.add_vertex(center_x, y_bottom, center_z)
            nc = self.add_normal(0, -1, 0)
            uc = self.add_uv((u0 + u1)*0.5, (v0 + v1)*0.5)
            for j in range(segments):
                vb1, ub1, _ = bot_ring[j]
                vb2, ub2, _ = bot_ring[j+1]
                self.add_face([vc, vb1, vb2], [uc, ub1, ub2], [nc, nc, nc])

    def add_tube_path(self, path_points, radii, segments=12, uv_box=(0, 0, 1, 1)):
        """Builds a solid 3D curved tube along 3D centerline points"""
        rings = []
        u0, v0, u1, v1 = uv_box
        num_pts = len(path_points)

        for i, (p, r) in enumerate(zip(path_points, radii)):
            # Calculate tangent
            if i == 0:
                tangent = (path_points[1][0] - p[0], path_points[1][1] - p[1], path_points[1][2] - p[2])
            elif i == num_pts - 1:
                tangent = (p[0] - path_points[i-1][0], p[1] - path_points[i-1][1], p[2] - path_points[i-1][2])
            else:
                tangent = (path_points[i+1][0] - path_points[i-1][0], path_points[i+1][1] - path_points[i-1][1], path_points[i+1][2] - path_points[i-1][2])

            # Normalize tangent
            tx, ty, tz = tangent
            tlen = math.sqrt(tx*tx + ty*ty + tz*tz)
            if tlen < 1e-5: tx, ty, tz = 1, 0, 0
            else: tx, ty, tz = tx/tlen, ty/tlen, tz/tlen

            # Find perpendicular vectors
            up = (0, 1, 0) if abs(ty) < 0.9 else (0, 0, 1)
            # binormal = tangent x up
            bx = ty * up[2] - tz * up[1]
            by = tz * up[0] - tx * up[2]
            bz = tx * up[1] - ty * up[0]
            blen = math.sqrt(bx*bx + by*by + bz*bz)
            bx, by, bz = bx/blen, by/blen, bz/blen

            # normal = binormal x tangent
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

                # Offset from point along normal & binormal
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

        # Connect rings
        for i in range(num_pts - 1):
            r1 = rings[i]
            r2 = rings[i+1]
            for j in range(segments):
                self.add_quad(r1[j][0], r2[j][0], r2[j+1][0], r1[j+1][0],
                              r1[j][1], r2[j][1], r2[j+1][1], r1[j+1][1],
                              r1[j][2], r2[j][2], r2[j+1][2], r1[j+1][2])

        # Cap start
        p_start = path_points[0]
        vc_start = self.add_vertex(*p_start)
        nc_start = self.add_normal(-(path_points[1][0]-p_start[0]), -(path_points[1][1]-p_start[1]), -(path_points[1][2]-p_start[2]))
        uc_start = self.add_uv((u0+u1)*0.5, v0)
        for j in range(segments):
            self.add_face([vc_start, rings[0][j+1][0], rings[0][j][0]],
                          [uc_start, rings[0][j+1][1], rings[0][j][1]],
                          [nc_start, nc_start, nc_start])

        # Cap end
        p_end = path_points[-1]
        vc_end = self.add_vertex(*p_end)
        nc_end = self.add_normal(p_end[0]-path_points[-2][0], p_end[1]-path_points[-2][1], p_end[2]-path_points[-2][2])
        uc_end = self.add_uv((u0+u1)*0.5, v1)
        for j in range(segments):
            self.add_face([vc_end, rings[-1][j][0], rings[-1][j+1][0]],
                          [uc_end, rings[-1][j][1], rings[-1][j+1][1]],
                          [nc_end, nc_end, nc_end])

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

