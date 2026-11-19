using System.IO;
using UnityEngine;

namespace RongRanChamCham
{
    public static class UITextureGenerator
    {
        private static string UIPath => Path.Combine(Application.dataPath, "_Game/Textures/UI");
        private static string ResUIPath => Path.Combine(Application.dataPath, "_Game/Resources/UI");

        public static void GenerateAll()
        {
            Directory.CreateDirectory(UIPath);
            Directory.CreateDirectory(ResUIPath);

            SavePng("panel_folk_lacquer.png", CreateFolkPanel(256, 128, new Color(0.08f, 0.06f, 0.12f), new Color(1.0f, 0.82f, 0.25f)));
            SavePng("panel_danger.png", CreateFolkPanel(256, 128, new Color(0.24f, 0.04f, 0.06f), new Color(1.0f, 0.30f, 0.35f)));
            SavePng("panel_safe.png", CreateFolkPanel(256, 128, new Color(0.03f, 0.18f, 0.09f), new Color(0.25f, 0.95f, 0.45f)));

            SavePng("banner_royal_scroll.png", CreateScrollBanner(512, 88, new Color(0.48f, 0.08f, 0.10f), new Color(1.0f, 0.85f, 0.30f)));
            SavePng("badge_timer_capsule.png", CreatePillCapsule(256, 72, new Color(0.06f, 0.08f, 0.14f), new Color(1.0f, 0.86f, 0.32f)));

            SavePng("heart_full_3d.png", CreateHeartIcon(64, 64, HeartState.Full));
            SavePng("heart_half_3d.png", CreateHeartIcon(64, 64, HeartState.Half));
            SavePng("heart_empty_3d.png", CreateHeartIcon(64, 64, HeartState.Empty));

            SavePng("btn_folk_card_normal.png", CreateFolkButton(128, 128, false));
            SavePng("btn_folk_card_hover.png", CreateFolkButton(128, 128, true));

            SavePng("btn_primary_cta_normal.png", CreateFolkCtaButton(160, 160, false));
            SavePng("btn_primary_cta_pressed.png", CreateFolkCtaButton(160, 160, true));

            SavePng("qte_crystal_left.png", CreateCrystalQte(128, 128, new Color(0.15f, 0.65f, 1.0f), 0));
            SavePng("qte_crystal_up.png", CreateCrystalQte(128, 128, new Color(0.20f, 0.85f, 0.45f), 1));
            SavePng("qte_crystal_down.png", CreateCrystalQte(128, 128, new Color(0.70f, 0.30f, 0.95f), 2));
            SavePng("qte_crystal_right.png", CreateCrystalQte(128, 128, new Color(1.0f, 0.55f, 0.15f), 3));

            Debug.Log("[UITextureGenerator] Hoàn thành sinh trọn bộ texture UI 3D Dân Gian cao cấp!");
        }

        private static void SavePng(string fileName, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(UIPath, fileName), bytes);
            File.WriteAllBytes(Path.Combine(ResUIPath, fileName), bytes);
            Object.DestroyImmediate(tex);
        }

        // ==========================================
        // 1. FOLK LACQUER PANEL WITH GOLD FILIGREE
        // ==========================================
        private static Texture2D CreateFolkPanel(int w, int h, Color baseDark, Color goldCol)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            float radius = 12f;

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float distFromEdge = GetCornerDist(x, y, w, h, radius);

                    if (distFromEdge < 0f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    // Outer Border (Gold bevel)
                    if (distFromEdge <= 4f)
                    {
                        // 3D metallic gradient
                        float light = 0.85f + (ny * 0.3f);
                        pixels[y * w + x] = goldCol * light;
                        pixels[y * w + x].a = 1f;
                    }
                    else if (distFromEdge <= 6f)
                    {
                        // Dark inset shadow line
                        pixels[y * w + x] = new Color(0.04f, 0.03f, 0.06f, 0.95f);
                    }
                    else
                    {
                        // Lacquer Body: dark mahogany gradient with subtle central glow
                        float grad = Mathf.Lerp(0.85f, 1.15f, 1f - Mathf.Abs(ny - 0.5f) * 1.5f);
                        Color body = baseDark * grad;
                        body.a = 0.94f;

                        // 4 Golden Corner Rivets (Chốt vàng 4 góc)
                        bool isCornerRivet = IsNearPoint(x, y, 16, 16, 4.5f) ||
                                            IsNearPoint(x, y, w - 17, 16, 4.5f) ||
                                            IsNearPoint(x, y, 16, h - 17, 4.5f) ||
                                            IsNearPoint(x, y, w - 17, h - 17, 4.5f);

                        if (isCornerRivet)
                        {
                            body = goldCol * 1.2f;
                            body.a = 1f;
                        }

                        pixels[y * w + x] = body;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ==========================================
        // 2. ROYAL SCROLL BANNER
        // ==========================================
        private static Texture2D CreateScrollBanner(int w, int h, Color redDark, Color goldCol)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;

                    // Scroll shape: horizontal ribbon with beveled top/bottom
                    bool isTopBorder = y >= h - 4;
                    bool isBotBorder = y <= 3;
                    bool isLeftBorder = x <= 6;
                    bool isRightBorder = x >= w - 7;

                    if (isTopBorder || isBotBorder || isLeftBorder || isRightBorder)
                    {
                        pixels[y * w + x] = goldCol;
                        pixels[y * w + x].a = 1f;
                    }
                    else
                    {
                        // Imperial Silk Red with central sheen
                        float vertSheen = Mathf.Sin(ny * Mathf.PI);
                        Color c = Color.Lerp(redDark * 0.7f, redDark * 1.35f, vertSheen);
                        c.a = 0.96f;
                        pixels[y * w + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ==========================================
        // 3. PILL CAPSULE TIMER
        // ==========================================
        private static Texture2D CreatePillCapsule(int w, int h, Color bgDark, Color goldCol)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            float radius = h * 0.48f;

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float dist = GetCapsuleDist(x, y, w, h, radius);
                    if (dist < 0f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    if (dist <= 3.5f)
                    {
                        pixels[y * w + x] = goldCol * (0.9f + ny * 0.3f);
                        pixels[y * w + x].a = 1f;
                    }
                    else
                    {
                        // Glass dome top highlight
                        Color body = bgDark;
                        if (ny > 0.55f) body += new Color(0.12f, 0.15f, 0.22f);
                        body.a = 0.95f;
                        pixels[y * w + x] = body;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ==========================================
        // 4. 3D GLOSSY ENAMEL HEART
        // ==========================================
        private enum HeartState { Full, Half, Empty }

        private static Texture2D CreateHeartIcon(int w, int h, HeartState state)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color rubyRed = new Color(0.95f, 0.15f, 0.28f);
            Color darkSlate = new Color(0.18f, 0.20f, 0.26f);
            Color goldRim = new Color(1.0f, 0.85f, 0.30f);

            for (int y = 0; y < h; y++)
            {
                // Normalize to -1..1 coordinates
                float py = ((float)y / h - 0.40f) * 2.2f;

                for (int x = 0; x < w; x++)
                {
                    float px = ((float)x / w - 0.50f) * 2.2f;

                    // Heart implicit curve formula: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    float a = px * px + py * py - 1f;
                    float heartVal = a * a * a - px * px * py * py * py;

                    if (heartVal > 0.05f)
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                    else if (heartVal >= -0.15f)
                    {
                        // Gold bevel rim
                        pixels[y * w + x] = goldRim;
                    }
                    else
                    {
                        Color fill = rubyRed;
                        if (state == HeartState.Empty)
                        {
                            fill = darkSlate;
                        }
                        else if (state == HeartState.Half)
                        {
                            fill = (x < w / 2) ? rubyRed : darkSlate;
                        }

                        // 3D Glass Gloss Highlight (upper left shine)
                        float distToShine = Vector2.Distance(new Vector2(px, py), new Vector2(-0.35f, 0.35f));
                        if (distToShine < 0.45f && (state == HeartState.Full || (state == HeartState.Half && x < w / 2)))
                        {
                            fill = Color.Lerp(Color.white, fill, distToShine / 0.45f);
                        }

                        fill.a = 1f;
                        pixels[y * w + x] = fill;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ==========================================
        // 5. 3D CRYSTAL QTE GEMS
        // ==========================================
        private static Texture2D CreateCrystalQte(int w, int h, Color gemCol, int dirIndex)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            float radius = w * 0.46f;
            Color goldRim = new Color(1.0f, 0.88f, 0.35f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);

                    if (dist > radius)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    // Outer Gold Ring Bezel
                    if (dist >= radius - 5f)
                    {
                        float light = 0.8f + ((float)y / h) * 0.4f;
                        pixels[y * w + x] = goldRim * light;
                        pixels[y * w + x].a = 1f;
                    }
                    else
                    {
                        // Spherical 3D shading
                        float normDist = dist / (radius - 5f);
                        float z = Mathf.Sqrt(Mathf.Max(0f, 1f - normDist * normDist));

                        // Key light from top-left
                        Vector3 normal = new Vector3((x - center.x) / radius, (y - center.y) / radius, z);
                        Vector3 lightDir = new Vector3(-0.4f, 0.6f, 0.7f).normalized;
                        float diffuse = Mathf.Max(0.2f, Vector3.Dot(normal, lightDir));

                        Color c = gemCol * (diffuse * 1.25f);

                        // Specular shine hotspot
                        float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(normal, lightDir)), 14f);
                        c += Color.white * (spec * 0.85f);

                        // Embossed Direction Arrow
                        bool inArrow = IsInsideArrow(x - center.x, y - center.y, radius * 0.55f, dirIndex);
                        if (inArrow)
                        {
                            c = Color.Lerp(c, Color.white, 0.95f);
                        }

                        c.a = 1f;
                        pixels[y * w + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static bool IsInsideArrow(float x, float y, float size, int dir)
        {
            // Rotate coordinate system based on dir: 0 = Left, 1 = Up, 2 = Down, 3 = Right
            float rot = dir switch
            {
                0 => 180f,
                1 => 90f,
                2 => 270f,
                _ => 0f
            };

            float rad = rot * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float rx = x * cos - y * sin;
            float ry = x * sin + y * cos;

            // Arrow pointing Right in rotated coordinates
            // Triangle head + rectangle shaft
            bool inHead = (rx >= 0f && rx <= size * 0.65f && Mathf.Abs(ry) <= (size * 0.65f - rx) * 0.9f);
            bool inShaft = (rx >= -size * 0.55f && rx < 0f && Mathf.Abs(ry) <= size * 0.22f);
            return inHead || inShaft;
        }

        // ==========================================
        // 6. FOLK BUTTON (NORMAL & HOVER)
        // ==========================================
        private static Texture2D CreateFolkButton(int w, int h, bool isHover)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            Color gold = isHover ? new Color(1.0f, 0.95f, 0.50f) : new Color(0.92f, 0.78f, 0.25f);
            Color dark = isHover ? new Color(0.18f, 0.22f, 0.32f) : new Color(0.08f, 0.10f, 0.16f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float dist = GetCornerDist(x, y, w, h, 8f);
                    if (dist < 0f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    if (dist <= 3.5f)
                    {
                        pixels[y * w + x] = gold * (0.85f + ny * 0.3f);
                        pixels[y * w + x].a = 1f;
                    }
                    else
                    {
                        Color c = dark * (0.9f + ny * 0.25f);
                        c.a = 0.96f;
                        pixels[y * w + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ==========================================
        // 7. ROYAL FOLK CTA BUTTON (3D GOLDEN DRUM / SEAL)
        // ==========================================
        private static Texture2D CreateFolkCtaButton(int w, int h, bool isPressed)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            float radius = w * 0.47f;

            Color goldRim = isPressed ? new Color(0.85f, 0.70f, 0.20f) : new Color(1.0f, 0.88f, 0.35f);
            Color darkShadow = new Color(0.04f, 0.02f, 0.02f, 0.95f);
            Color redBody = isPressed ? new Color(0.60f, 0.12f, 0.14f) : new Color(0.85f, 0.18f, 0.20f);
            Color redGlow = isPressed ? new Color(0.75f, 0.18f, 0.22f) : new Color(1.0f, 0.32f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    // 1. Outer Gold Bezel
                    if (dist >= radius - 6f)
                    {
                        float light = 0.80f + ny * 0.40f;
                        pixels[y * w + x] = goldRim * light;
                        pixels[y * w + x].a = 1f;
                    }
                    else if (dist >= radius - 8.5f)
                    {
                        // Inset shadow rim
                        pixels[y * w + x] = darkShadow;
                    }
                    else
                    {
                        // 2. Lacquer Dome Fill with subtle spherical gradient
                        float normDist = dist / (radius - 8.5f);
                        Color fill = Color.Lerp(redGlow, redBody, normDist);

                        // Specular gloss shine at upper-left
                        float shineDist = Vector2.Distance(new Vector2(x, y), center + new Vector2(-radius * 0.3f, radius * 0.3f));
                        if (shineDist < radius * 0.45f && !isPressed)
                        {
                            fill = Color.Lerp(Color.white, fill, shineDist / (radius * 0.45f));
                        }

                        // 3. Embossed 3D Golden Play Triangle in center
                        float cx = x - center.x;
                        float cy = y - center.y;
                        if (isPressed) { cy -= 2f; } // shift down when pressed
                        
                        // Centered Play Arrowhead: x in [-size, size], y in [-size, size]
                        float arrowSize = radius * 0.42f;
                        // Equilateral / isosceles triangle pointing right
                        // Left vertical edge at x = -arrowSize * 0.6f
                        // Right point at x = arrowSize * 0.8f
                        float leftX = -arrowSize * 0.5f;
                        float rightX = arrowSize * 0.75f;
                        float halfH = arrowSize * 0.70f;

                        bool inArrow = false;
                        if (cx >= leftX && cx <= rightX)
                        {
                            float t = (cx - leftX) / (rightX - leftX);
                            float allowedY = halfH * (1f - t);
                            if (Mathf.Abs(cy) <= allowedY)
                            {
                                inArrow = true;
                            }
                        }

                        if (inArrow)
                        {
                            float arrowLight = 0.90f + ((cy + halfH) / (halfH * 2f)) * 0.35f;
                            fill = goldRim * (arrowLight * 1.15f);
                            fill.a = 1f;
                        }

                        fill.a = 1f;
                        pixels[y * w + x] = fill;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // Utility math
        private static float GetCornerDist(int x, int y, int w, int h, float r)
        {
            float cx = x < r ? r : (x > w - 1 - r ? w - 1 - r : x);
            float cy = y < r ? r : (y > h - 1 - r ? h - 1 - r : y);
            if (x < r || x > w - 1 - r || y < r || y > h - 1 - r)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                return r - d;
            }
            return Mathf.Min(x, Mathf.Min(w - 1 - x, Mathf.Min(y, h - 1 - y)));
        }

        private static float GetCapsuleDist(int x, int y, int w, int h, float r)
        {
            float cx = Mathf.Clamp(x, r, w - 1 - r);
            float cy = h * 0.5f;
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return r - d;
        }

        private static bool IsNearPoint(int x, int y, int px, int py, float r)
        {
            return (x - px) * (x - px) + (y - py) * (y - py) <= r * r;
        }
    }
}
