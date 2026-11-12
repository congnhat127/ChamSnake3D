using System;

// Dữ liệu cấu hình mỗi màn (đọc từ Configs/ManX.json). Tọa độ theo hệ thiết kế: x sang phải, z hướng về camera.
[Serializable] public class RRObstacle { public float x, z, r; }
[Serializable] public class RRMover
{
    public string kind; public int index; public float r;
    public bool hasLoop; public float ax, az, bx, bz, lcx, lcz, lrx, lrz;
    public float speed, pause, t0, ph, dir;
}
[Serializable] public class RRLight { public float x, y, z; public string color; }
[Serializable] public class RRLevelConfig
{
    public int level; public string name; public bool kr;
    public string boundaryType; public float A, B, cx, cz, hx, hz, R;
    public RRObstacle[] obstacles; public RRMover[] movers; public float spawnA0;
    public string bg, sky, ground, sun; public float sunX, sunY, sunZ, hemi, sunI;
    public RRLight[] lights; public string fx;
    public float fireX, fireY, fireZ, fireDX, fireDY, fireDZ;
}
