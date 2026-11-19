using System.Collections.Generic;
using UnityEngine;

namespace RongRanChamCham
{
    /// <summary>
    /// Nhà máy tạo Texture & Sprite 9-slice chuẩn uGUI phong cách Dân Gian Việt Nam hiện đại (Vietnamese Folk Lacquer & Jade).
    /// Tất cả Sprite được tạo với anti-aliasing mềm mại, viền vàng kim, đổ bóng 3D và biên độ 9-slice co giãn không vỡ góc.
    /// </summary>
    public static class UIThemeFactory
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        // Palette Màu Phong Cách Dân Gian Hoàng Gia (Vietnamese Regal Folk)
        public static readonly Color ColLacquerDark = new Color(0.08f, 0.06f, 0.05f, 0.94f);      // Đen sơn mài bóng
        public static readonly Color ColLacquerDeep = new Color(0.14f, 0.10f, 0.08f, 0.96f);      // Nâu gỗ trầm
        public static readonly Color ColGoldBorder = new Color(0.85f, 0.68f, 0.28f, 1.0f);        // Vàng kim cổ điển
        public static readonly Color ColGoldBright = new Color(1.0f, 0.86f, 0.42f, 1.0f);        // Vàng kim sáng
        public static readonly Color ColCinnabarRed = new Color(0.84f, 0.22f, 0.12f, 1.0f);       // Đỏ chu sa (Nút Vào Trận)
        public static readonly Color ColCinnabarDark = new Color(0.62f, 0.12f, 0.06f, 1.0f);      // Đỏ sẫm nhấn nút
        public static readonly Color ColJadeGreen = new Color(0.10f, 0.55f, 0.30f, 1.0f);         // Xanh ngọc bích (Khiên)
        public static readonly Color ColJadeGlow = new Color(0.20f, 0.85f, 0.45f, 1.0f);          // Xanh ngọc phát sáng
        public static readonly Color ColTextIvory = new Color(0.98f, 0.96f, 0.90f, 1.0f);         // Trắng ngà dễ đọc

        public static Sprite GetFolkPanelSprite()
        {
            if (spriteCache.TryGetValue("PanelFolk", out var s) && s != null) return s;
            var tex = CreateRoundedRectTex(256, 256, 24, ColLacquerDark, ColGoldBorder, 4f, new Color(0.18f, 0.14f, 0.10f, 0.95f));
            s = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(28, 28, 28, 28));
            spriteCache["PanelFolk"] = s;
            return s;
        }

        public static Sprite GetPrimaryCtaSprite(bool pressed = false)
        {
            string key = pressed ? "BtnCtaPressed" : "BtnCtaNormal";
            if (spriteCache.TryGetValue(key, out var s) && s != null) return s;
            Color fillTop = pressed ? ColCinnabarDark : ColCinnabarRed;
            Color fillBot = pressed ? new Color(0.48f, 0.08f, 0.04f, 1f) : ColCinnabarDark;
            Color border = pressed ? ColGoldBorder : ColGoldBright;
            var tex = CreateRoundedRectTex(256, 128, 32, fillBot, border, 4f, fillTop);
            s = Sprite.Create(tex, new Rect(0, 0, 256, 128), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(36, 36, 36, 36));
            spriteCache[key] = s;
            return s;
        }

        public static Sprite GetSecondaryCardSprite(bool hover = false)
        {
            string key = hover ? "BtnCardHover" : "BtnCardNormal";
            if (spriteCache.TryGetValue(key, out var s) && s != null) return s;
            Color fill = hover ? new Color(0.22f, 0.17f, 0.14f, 0.98f) : new Color(0.14f, 0.10f, 0.08f, 0.95f);
            Color border = hover ? ColGoldBright : ColGoldBorder;
            var tex = CreateRoundedRectTex(256, 128, 20, fill, border, 3f, hover ? new Color(0.30f, 0.24f, 0.19f, 0.98f) : fill);
            s = Sprite.Create(tex, new Rect(0, 0, 256, 128), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
            spriteCache[key] = s;
            return s;
        }

        public static Sprite GetTimerCapsuleSprite()
        {
            if (spriteCache.TryGetValue("TimerCapsule", out var s) && s != null) return s;
            var tex = CreateRoundedRectTex(256, 96, 44, new Color(0.06f, 0.05f, 0.04f, 0.90f), ColGoldBorder, 3f, new Color(0.12f, 0.09f, 0.07f, 0.92f));
            s = Sprite.Create(tex, new Rect(0, 0, 256, 96), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(46, 46, 46, 46));
            spriteCache["TimerCapsule"] = s;
            return s;
        }

        public static Sprite GetInstructionBannerSprite()
        {
            if (spriteCache.TryGetValue("InstructionBanner", out var s) && s != null) return s;
            var tex = CreateRoundedRectTex(256, 96, 18, new Color(0.08f, 0.06f, 0.05f, 0.92f), new Color(0.70f, 0.55f, 0.22f, 0.85f), 2.5f, new Color(0.14f, 0.10f, 0.07f, 0.94f));
            s = Sprite.Create(tex, new Rect(0, 0, 256, 96), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
            spriteCache["InstructionBanner"] = s;
            return s;
        }

        public static Sprite GetShieldCircleSprite(bool active = false)
        {
            string key = active ? "ShieldActive" : "ShieldNormal";
            if (spriteCache.TryGetValue(key, out var s) && s != null) return s;
            int sz = 160;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pix = new Color[sz * sz];
            float center = (sz - 1) * 0.5f;
            float radius = center - 4f;

            Color fillInner = active ? ColJadeGlow : ColJadeGreen;
            Color fillOuter = active ? ColJadeGreen : new Color(0.06f, 0.32f, 0.16f, 1f);
            Color border = active ? Color.white : ColGoldBright;

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (d > radius)
                    {
                        float alpha = Mathf.Clamp01(radius + 1f - d);
                        pix[y * sz + x] = new Color(0f, 0f, 0f, alpha * 0.35f); // Soft outer shadow
                    }
                    else if (d >= radius - 5f)
                    {
                        pix[y * sz + x] = border;
                    }
                    else
                    {
                        float t = d / radius;
                        pix[y * sz + x] = Color.Lerp(fillInner, fillOuter, t);
                    }
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            spriteCache[key] = s;
            return s;
        }

        public static Sprite GetJoystickBaseSprite()
        {
            if (spriteCache.TryGetValue("JoystickBase", out var s) && s != null) return s;
            int sz = 200;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pix = new Color[sz * sz];
            float center = (sz - 1) * 0.5f;
            float radius = center - 4f;

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (d > radius)
                    {
                        pix[y * sz + x] = Color.clear;
                    }
                    else if (d >= radius - 4f)
                    {
                        pix[y * sz + x] = new Color(1f, 1f, 1f, 0.45f); // Ring viền mờ
                    }
                    else if (Mathf.Abs(d - radius * 0.55f) <= 2f)
                    {
                        pix[y * sz + x] = new Color(1f, 1f, 1f, 0.20f); // Vòng tâm định vị
                    }
                    else
                    {
                        float alpha = Mathf.Lerp(0.22f, 0.08f, d / radius);
                        pix[y * sz + x] = new Color(0.1f, 0.1f, 0.15f, alpha);
                    }
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            spriteCache["JoystickBase"] = s;
            return s;
        }

        public static Sprite GetJoystickKnobSprite()
        {
            if (spriteCache.TryGetValue("JoystickKnob", out var s) && s != null) return s;
            int sz = 100;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pix = new Color[sz * sz];
            float center = (sz - 1) * 0.5f;
            float radius = center - 3f;

            Color knobCenter = new Color(0.40f, 0.85f, 1.0f, 0.95f);
            Color knobEdge = new Color(0.12f, 0.45f, 0.80f, 0.95f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (d > radius)
                    {
                        pix[y * sz + x] = Color.clear;
                    }
                    else if (d >= radius - 3.5f)
                    {
                        pix[y * sz + x] = Color.white;
                    }
                    else
                    {
                        // Specular highlight offset towards top-left
                        float specDist = Vector2.Distance(new Vector2(x, y), new Vector2(center - radius * 0.3f, center + radius * 0.3f));
                        float spec = Mathf.Clamp01(1f - (specDist / (radius * 0.6f)));
                        Color col = Color.Lerp(knobCenter, knobEdge, d / radius);
                        col = Color.Lerp(col, Color.white, spec * 0.7f);
                        pix[y * sz + x] = col;
                    }
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            spriteCache["JoystickKnob"] = s;
            return s;
        }

        public static Sprite GetHeartSprite(int state) // 0 = Empty, 1 = Half, 2 = Full
        {
            string key = $"Heart_{state}";
            if (spriteCache.TryGetValue(key, out var s) && s != null) return s;
            int sz = 128;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pix = new Color[sz * sz];

            Color fullCol = new Color(0.96f, 0.22f, 0.28f, 1.0f);
            Color emptyCol = new Color(0.20f, 0.16f, 0.18f, 0.75f);
            Color borderCol = ColGoldBright;

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    // Heart formula normalized to [-1.5, 1.5]
                    float nx = (x - (sz * 0.5f)) / (sz * 0.38f);
                    float ny = (y - (sz * 0.45f)) / (sz * 0.38f);

                    // Standard heart curve: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    float a = nx * nx + ny * ny - 1f;
                    float f = a * a * a - nx * nx * ny * ny * ny;

                    if (f <= 0.05f)
                    {
                        bool isBorder = f > -0.15f;
                        if (isBorder)
                        {
                            pix[y * sz + x] = borderCol;
                        }
                        else
                        {
                            if (state == 2) // Full
                            {
                                pix[y * sz + x] = fullCol;
                            }
                            else if (state == 1) // Half
                            {
                                pix[y * sz + x] = (nx <= 0f) ? fullCol : emptyCol;
                            }
                            else // Empty
                            {
                                pix[y * sz + x] = emptyCol;
                            }
                        }
                    }
                    else
                    {
                        pix[y * sz + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            spriteCache[key] = s;
            return s;
        }

        public static Sprite GetQteDiamondSprite(DuelDirection dir, bool active = false)
        {
            string key = $"Qte_{dir}_{(active ? "1" : "0")}";
            if (spriteCache.TryGetValue(key, out var s) && s != null) return s;
            int sz = 160;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pix = new Color[sz * sz];
            float c = (sz - 1) * 0.5f;
            float maxR = c - 6f;

            Color gemCol = dir == DuelDirection.Left ? new Color(0.15f, 0.65f, 0.95f) :
                           dir == DuelDirection.Right ? new Color(0.95f, 0.45f, 0.15f) :
                           dir == DuelDirection.Straight ? new Color(0.25f, 0.85f, 0.45f) :
                           new Color(0.95f, 0.80f, 0.20f);

            if (active) gemCol = Color.Lerp(gemCol, Color.white, 0.45f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Abs(x - c);
                    float dy = Mathf.Abs(y - c);
                    float distManhattan = dx + dy;

                    if (distManhattan > maxR)
                    {
                        pix[y * sz + x] = Color.clear;
                    }
                    else if (distManhattan >= maxR - 5f)
                    {
                        pix[y * sz + x] = active ? Color.white : ColGoldBright;
                    }
                    else
                    {
                        float innerDist = distManhattan / maxR;
                        Color col = Color.Lerp(gemCol, new Color(0.08f, 0.06f, 0.10f, 0.95f), innerDist * 0.6f);
                        pix[y * sz + x] = col;
                    }
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            spriteCache[key] = s;
            return s;
        }

        private static Texture2D CreateRoundedRectTex(int w, int h, int radius, Color fillBot, Color borderCol, float borderWidth, Color fillTop)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float tVert = (float)y / (h - 1);
                Color currentFill = Color.Lerp(fillBot, fillTop, tVert);

                for (int x = 0; x < w; x++)
                {
                    float distToCorner = GetDistToCorner(x, y, w, h, radius);
                    if (distToCorner > radius)
                    {
                        pix[y * w + x] = Color.clear;
                    }
                    else if (distToCorner >= radius - borderWidth || x < borderWidth || x >= w - borderWidth || y < borderWidth || y >= h - borderWidth)
                    {
                        pix[y * w + x] = borderCol;
                    }
                    else
                    {
                        pix[y * w + x] = currentFill;
                    }
                }
            }
            t.SetPixels(pix);
            t.Apply();
            return t;
        }

        private static float GetDistToCorner(int x, int y, int w, int h, int r)
        {
            int cx = x < r ? r : (x >= w - r ? w - 1 - r : x);
            int cy = y < r ? r : (y >= h - r ? h - 1 - r : y);
            if (cx == x && cy == y) return 0f;
            return Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
        }
    }
}
