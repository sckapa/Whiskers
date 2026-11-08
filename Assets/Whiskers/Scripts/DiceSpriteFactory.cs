using System;
using System.Collections.Generic;
using UnityEngine;

public enum DieKind { Ivory, Cheese, CheeseWedge, Red, D8, D10, D12, D20 }

// Whiskers Rig-ma-roll — builds the dice-select pixel dice in code, matching the design canvas.
// Each die is rebuilt per integer size (so pips/numbers stay crisp instead of being scaled),
// every colour sits on the GBA 15-bit grid, and results are cached per (kind, face, size, outline).
// Faces: d6 1-6, d4 1-4, d8 1-8, d10 1-10, d12 1-12, d20 1-20, plus a lucky 9 on every die
// (d6s get a 3x3 nine-pip face, the cheese wedge gets a "9").
public static class DiceSpriteFactory
{
    public struct DiceSprite { public Sprite main; public Sprite shadow; }

    static readonly Dictionary<long, DiceSprite> cache = new Dictionary<long, DiceSprite>();
    static Sprite pixel;

    public static Color32 C5(int r, int g, int b)
    {
        return new Color32((byte)((r << 3) | (r >> 2)), (byte)((g << 3) | (g >> 2)), (byte)((b << 3) | (b >> 2)), 255);
    }

    public static int Sides(DieKind k)
    {
        switch (k)
        {
            case DieKind.CheeseWedge: return 4;
            case DieKind.D8: return 8;
            case DieKind.D10: return 10;
            case DieKind.D12: return 12;
            case DieKind.D20: return 20;
            default: return 6;
        }
    }

    // Colour used when a die is so far away it is just a 1-2 px speck.
    public static Color32 DotColor(DieKind k)
    {
        switch (k)
        {
            case DieKind.Ivory: return new Color32(255, 247, 239, 255);
            case DieKind.Red: return new Color32(222, 49, 99, 255);
            case DieKind.D8: return new Color32(41, 165, 181, 255);
            case DieKind.D10: return new Color32(239, 123, 33, 255);
            case DieKind.D12: return new Color32(66, 173, 49, 255);
            case DieKind.D20: return new Color32(206, 181, 239, 255);
            default: return new Color32(247, 198, 49, 255);
        }
    }

    public static Sprite Pixel
    {
        get
        {
            if (pixel == null)
            {
                var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                t.SetPixel(0, 0, Color.white);
                t.Apply();
                pixel = Sprite.Create(t, new Rect(0, 0, 1, 1), Vector2.zero, 1f);
            }
            return pixel;
        }
    }

    public static void ClearCache()
    {
        foreach (var p in cache.Values)
        {
            Kill(p.main); Kill(p.shadow);
        }
        cache.Clear();
    }

    static void Kill(Sprite s)
    {
        if (s == null) return;
        var tex = s.texture;
        if (Application.isPlaying) { UnityEngine.Object.Destroy(s); UnityEngine.Object.Destroy(tex); }
        else { UnityEngine.Object.DestroyImmediate(s); UnityEngine.Object.DestroyImmediate(tex); }
    }

    public static DiceSprite Get(DieKind kind, int val, int size, Color32 outline)
    {
        size = Mathf.Clamp(size, 4, 64);
        val = Mathf.Clamp(val, 0, 20);
        long key = ((long)kind << 48) | ((long)val << 40) | ((long)size << 32) | (uint)((outline.r << 16) | (outline.g << 8) | outline.b);
        DiceSprite ds;
        if (cache.TryGetValue(key, out ds) && ds.main != null) return ds;

        Color32[] cols;
        Buf b;
        switch (kind)
        {
            case DieKind.CheeseWedge: b = Wedge(val, size, outline, out cols); break;
            case DieKind.D8:
            case DieKind.D10:
            case DieKind.D12:
            case DieKind.D20: b = Poly(kind, val, size, outline, out cols); break;
            default: b = Cube(kind, val, size, outline, out cols); break;
        }
        ds = Bake(b, cols);
        cache[key] = ds;
        return ds;
    }

    // ---------------------------------------------------------------- helpers

    class Buf
    {
        public readonly int w, h;
        public readonly byte[] px;
        public Buf(int w, int h) { this.w = w; this.h = h; px = new byte[w * h]; }
        public void Set(float x, float y, int c)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            if (ix >= 0 && iy >= 0 && ix < w && iy < h) px[iy * w + ix] = (byte)c;
        }
        public int Get(int x, int y) { return (x < 0 || y < 0 || x >= w || y >= h) ? 0 : px[y * w + x]; }
    }

    class Rng
    {
        uint s;
        public Rng(uint seed) { s = seed == 0 ? 1u : seed; }
        public float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s / 4294967296f; }
    }

    static int R(float v) { return Mathf.FloorToInt(v + 0.5f); }

    static void Disc(float cx, float cy, float rad, Action<int, int, float> fn)
    {
        float r = Mathf.Max(rad, 0.75f);
        for (int y = Mathf.FloorToInt(cy - r - 1); y <= Mathf.CeilToInt(cy + r + 1); y++)
            for (int x = Mathf.FloorToInt(cx - r - 1); x <= Mathf.CeilToInt(cx + r + 1); x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r * r) fn(x, y, dy / r);
            }
    }

    static Vector2[] PipSpots(int n)
    {
        const float lo = 0.27f, c = 0.5f, hi = 0.73f;
        switch (n)
        {
            case 1: return new[] { new Vector2(c, c) };
            case 2: return new[] { new Vector2(lo, lo), new Vector2(hi, hi) };
            case 3: return new[] { new Vector2(lo, lo), new Vector2(c, c), new Vector2(hi, hi) };
            case 4: return new[] { new Vector2(lo, lo), new Vector2(hi, lo), new Vector2(lo, hi), new Vector2(hi, hi) };
            case 5: return new[] { new Vector2(lo, lo), new Vector2(hi, lo), new Vector2(c, c), new Vector2(lo, hi), new Vector2(hi, hi) };
            case 9:
            {
                const float a = 0.24f, m = 0.5f, z = 0.76f;
                return new[] { new Vector2(a, a), new Vector2(m, a), new Vector2(z, a), new Vector2(a, m), new Vector2(m, m), new Vector2(z, m), new Vector2(a, z), new Vector2(m, z), new Vector2(z, z) };
            }
            default: return new[] { new Vector2(lo, lo), new Vector2(hi, lo), new Vector2(lo, c), new Vector2(hi, c), new Vector2(lo, hi), new Vector2(hi, hi) };
        }
    }

    static readonly string[][] Font =
    {
        new[] { "111", "101", "101", "101", "111" }, new[] { "010", "110", "010", "010", "111" },
        new[] { "111", "001", "111", "100", "111" }, new[] { "111", "001", "011", "001", "111" },
        new[] { "101", "101", "111", "001", "001" }, new[] { "111", "100", "111", "001", "111" },
        new[] { "111", "100", "111", "101", "111" }, new[] { "111", "001", "010", "010", "010" },
        new[] { "111", "101", "111", "101", "111" }, new[] { "111", "101", "111", "001", "111" }
    };

    static void Number(Buf b, int val, float cx, float cy, int k, int col)
    {
        string str = val.ToString();
        int tw = (str.Length * 4 - 1) * k;
        int gx = R(cx - tw / 2f), gy = R(cy - 2.5f * k);
        for (int ci = 0; ci < str.Length; ci++)
        {
            var g = Font[str[ci] - '0'];
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 3; c++)
                    if (g[r][c] == '1')
                        for (int a = 0; a < k; a++)
                            for (int bb = 0; bb < k; bb++)
                                b.Set(gx + (ci * 4 + c) * k + a, gy + r * k + bb, col);
        }
    }

    // ---------------------------------------------------------------- shapes

    static Buf Cube(DieKind kind, int val, int s, Color32 dark, out Color32[] cols)
    {
        switch (kind)
        {
            case DieKind.Cheese:
                cols = new[] { dark, dark, C5(31,29,12), C5(30,24,6), C5(24,16,3), C5(25,17,3), C5(18,10,2), C5(31,27,10) }; break;
            case DieKind.Red:
                cols = new[] { dark, dark, C5(31,13,17), C5(27,6,12), C5(19,3,9), C5(31,30,31), C5(31,30,31), C5(31,10,15) }; break;
            default:
                cols = new[] { dark, dark, C5(31,30,29), C5(27,25,28), C5(19,16,24), C5(6,2,10), C5(28,4,16), C5(30,29,31) }; break;
        }
        bool cheese = kind == DieKind.Cheese, ivory = kind == DieKind.Ivory;
        int d = Mathf.Max(1, R(s * 0.34f));
        int o = 1;
        var b = new Buf(s + d + 2, s + d + 2);
        var rnd = new Rng((uint)(s * 131 + val * 17 + (cheese ? 6 : ivory ? 5 : 3) * 7));

        for (int y = 0; y < d; y++) for (int x = 0; x < s; x++) b.Set(o + d - 1 - y + x, o + y, 2);
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) b.Set(o + x, o + d + y, y == 0 ? 7 : 3);
        for (int k = 0; k < d; k++) for (int y = 0; y < s; y++) b.Set(o + s + k, o + d - 1 - k + y, 4);

        float pr = s * (val > 6 ? 0.075f : 0.09f);
        foreach (var p in PipSpots(val))
        {
            float cx = o + p.x * s, cy = o + d + p.y * s;
            if (cheese)
            {
                float rr = pr * (1.25f + rnd.Next() * 0.35f);
                Disc(cx, cy, rr, (x, y, ny) => b.Set(x, y, ny < -0.15f ? 6 : 5));
            }
            else
            {
                int pc = (ivory && (val == 1 || val == 9)) ? 6 : 5;   // red 1 pip and red lucky 9 on ivory
                Disc(cx, cy, pr, (x, y, ny) => b.Set(x, y, pc));
            }
        }

        if (cheese && s >= 10)
        {
            for (int i = 0; i < 2; i++)
            {
                float tx = o + d * 0.5f + rnd.Next() * s * 0.8f, ty = o + 1 + rnd.Next() * Mathf.Max(1, d - 2);
                if (b.Get(Mathf.FloorToInt(tx), Mathf.FloorToInt(ty)) == 2)
                    Disc(tx, ty, Mathf.Max(0.75f, s * 0.05f), (x, y, ny) => { if (b.Get(x, y) == 2) b.Set(x, y, 5); });
            }
            float sx = o + s + d * 0.5f, sy = o + d + s * (0.3f + rnd.Next() * 0.4f);
            Disc(sx, sy, Mathf.Max(0.75f, s * 0.05f), (x, y, ny) => { if (b.Get(x, y) == 4) b.Set(x, y, 6); });
        }
        return b;
    }

    static Buf Wedge(int val, int s, Color32 dark, out Color32[] cols)
    {
        cols = new[] { dark, dark, C5(31,29,12), C5(30,24,6), C5(24,16,3), C5(25,17,3), C5(18,10,2), C5(31,27,10), C5(29,15,4), C5(22,10,3) };
        int th = Mathf.Max(4, R(s * 0.9f)), d = Mathf.Max(1, R(s * 0.16f)), rindH = Mathf.Max(1, R(s * 0.1f));
        int o = 1;
        var b = new Buf(s + d + 2, th + 2);
        var rnd = new Rng((uint)(s * 977 + val * 31));

        for (int y = 0; y < th; y++)
        {
            float half = (y + 1) / (float)th * s / 2f;
            int x0 = R(s / 2f - half), x1 = Mathf.Max(x0, R(s / 2f + half) - 1);
            bool rind = y >= th - rindH;
            for (int x = x0; x <= x1; x++) b.Set(o + x, o + y, rind ? 8 : (x == x0 ? 7 : 3));
            for (int k = 1; k <= d; k++) b.Set(o + x1 + k, o + y, rind ? 9 : 4);
        }
        if (s >= 12)
        {
            for (int i = 0; i < 2; i++)
            {
                float hy = th * (0.45f + rnd.Next() * 0.3f), hx = s / 2f + (rnd.Next() - 0.5f) * s * 0.45f * (hy / th);
                Disc(o + hx, o + hy, Mathf.Max(0.75f, s * 0.06f), (x, y, ny) => { if (b.Get(x, y) == 3) b.Set(x, y, ny < 0 ? 6 : 5); });
            }
            Number(b, val, o + s / 2f, o + th * 0.58f, s >= 28 ? 2 : 1, 6);
        }
        return b;
    }

    static Vector2[] Ring(int n, float r, float startDeg, float sx = 1f)
    {
        var pts = new Vector2[n];
        for (int k = 0; k < n; k++)
        {
            float a = (startDeg + k * 360f / n) * Mathf.Deg2Rad;
            pts[k] = new Vector2(Mathf.Cos(a) * r * sx, Mathf.Sin(a) * r);
        }
        return pts;
    }

    static bool Inside(Vector2[] pts, float x, float y)
    {
        bool c = false;
        for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
        {
            Vector2 pi = pts[i], pj = pts[j];
            if ((pi.y > y) != (pj.y > y) && x < (pj.x - pi.x) * (y - pi.y) / (pj.y - pi.y) + pi.x) c = !c;
        }
        return c;
    }

    static Vector2 V(float x, float y) { return new Vector2(x, y); }

    static Buf Poly(DieKind kind, int val, int s, Color32 dark, out Color32[] cols)
    {
        Vector2[] sil, front, dk;
        Vector2[][] lines;
        Vector2 num;
        switch (kind)
        {
            case DieKind.D8:
                cols = new[] { dark, dark, C5(8,26,27), C5(5,20,22), C5(3,13,16), C5(2,8,11), C5(2,6,9) };
                sil = new[] { V(0,-1), V(0.82f,0.1f), V(0,1), V(-0.82f,0.1f) };
                front = new[] { V(0,-1), V(0.82f,0.1f), V(-0.82f,0.1f) };
                dk = new[] { V(0,0.1f), V(0.82f,0.1f), V(0,1) };
                lines = new[] { new[] { V(-0.82f,0.1f), V(0.82f,0.1f) }, new[] { V(0,0.1f), V(0,1) } };
                num = V(0, -0.18f);
                break;
            case DieKind.D10:
                cols = new[] { dark, dark, C5(31,22,10), C5(29,15,4), C5(21,9,3), C5(14,5,2), C5(10,3,2) };
                sil = new[] { V(0,-1), V(0.92f,-0.05f), V(0,1), V(-0.92f,-0.05f) };
                front = new[] { V(0,-1), V(0.48f,0.22f), V(0,0.5f), V(-0.48f,0.22f) };
                dk = new[] { V(0,-1), V(0.92f,-0.05f), V(0,1), V(0,0.5f), V(0.48f,0.22f) };
                lines = new[] { new[] { V(0.48f,0.22f), V(0.92f,-0.05f) }, new[] { V(-0.48f,0.22f), V(-0.92f,-0.05f) }, new[] { V(0,0.5f), V(0,1) } };
                num = V(0, 0);
                break;
            case DieKind.D12:
            {
                cols = new[] { dark, dark, C5(14,27,10), C5(8,21,6), C5(4,14,4), C5(2,8,3), C5(2,6,2) };
                var dec = Ring(10, 1f, -90f);
                var pen = Ring(5, 0.52f, -90f);
                for (int i = 0; i < pen.Length; i++) pen[i].y += 0.03f;
                sil = dec; front = pen;
                dk = new Vector2[7];
                for (int i = 0; i < 6; i++) dk[i] = dec[i];
                dk[6] = V(0, 0);
                lines = new Vector2[5][];
                for (int i = 0; i < 5; i++) lines[i] = new[] { pen[i], dec[i * 2] };
                num = V(0, 0.05f);
                break;
            }
            default:
                cols = new[] { dark, dark, C5(31,30,29), C5(25,22,29), C5(17,13,24), C5(10,6,16), C5(6,2,10) };
                sil = Ring(6, 1f, -90f, 0.95f);
                front = new[] { V(0,-0.5f), V(0.5f,0.38f), V(-0.5f,0.38f) };
                dk = new[] { V(0,-1), V(0.9f,-0.5f), V(0.9f,0.5f), V(0,1) };
                lines = new[]
                {
                    new[] { V(0,-0.5f), V(0,-1) }, new[] { V(0.5f,0.38f), V(0.9f,-0.5f) }, new[] { V(0.5f,0.38f), V(0.9f,0.5f) },
                    new[] { V(0.5f,0.38f), V(0,1) }, new[] { V(-0.5f,0.38f), V(-0.9f,-0.5f) }, new[] { V(-0.5f,0.38f), V(-0.9f,0.5f) },
                    new[] { V(-0.5f,0.38f), V(0,1) }
                };
                num = V(0, 0.1f);
                break;
        }

        var b = new Buf(s + 2, s + 2);
        float rad = (s - 1) / 2f, cxp = 1 + s / 2f, cyp = 1 + s / 2f;
        Func<Vector2, Vector2> P = u => new Vector2(cxp + u.x * rad, cyp + u.y * rad);
        Vector2[] Map(Vector2[] a) { var r = new Vector2[a.Length]; for (int i = 0; i < a.Length; i++) r[i] = P(a[i]); return r; }
        var silP = Map(sil); var frP = Map(front); var dkP = Map(dk);

        for (int y = 0; y < b.h; y++)
            for (int x = 0; x < b.w; x++)
            {
                float X = x + 0.5f, Y = y + 0.5f;
                if (Inside(silP, X, Y)) b.Set(x, y, Inside(frP, X, Y) ? 2 : (Inside(dkP, X, Y) ? 4 : 3));
            }

        if (s >= 12)
            foreach (var ln in lines)
            {
                Vector2 a = P(ln[0]), c = P(ln[1]);
                int x0 = Mathf.FloorToInt(a.x), y0 = Mathf.FloorToInt(a.y), x1 = Mathf.FloorToInt(c.x), y1 = Mathf.FloorToInt(c.y);
                int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
                for (int n = 0; n < 200; n++)
                {
                    if (b.Get(x0, y0) != 0) b.Set(x0, y0, 5);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }

        if (s >= 14)
        {
            var np = P(num);
            Number(b, val, np.x, np.y, s >= 32 ? 2 : 1, 6);
        }
        return b;
    }

    // ---------------------------------------------------------------- bake

    static DiceSprite Bake(Buf b, Color32[] cols)
    {
        int w = b.w, h = b.h;
        var outl = (byte[])b.px.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (b.px[y * w + x] == 0 && (b.Get(x - 1, y) != 0 || b.Get(x + 1, y) != 0 || b.Get(x, y - 1) != 0 || b.Get(x, y + 1) != 0))
                    outl[y * w + x] = 1;

        var main = new Color32[w * h];
        var sh = new Color32[w * h];
        var clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int k = outl[y * w + x];
                int ti = (h - 1 - y) * w + x;              // buffer is top-down, textures are bottom-up
                main[ti] = k == 0 ? clear : cols[k];
                sh[ti] = k == 0 ? clear : cols[0];
            }

        // pivot on a pixel corner near the centre so integer positions stay pixel-perfect
        var pivot = new Vector2(Mathf.Floor(w / 2f) / w, (h - Mathf.Floor(h / 2f)) / h);
        return new DiceSprite { main = MakeSprite(main, w, h, pivot), shadow = MakeSprite(sh, w, h, pivot) };
    }

    static Sprite MakeSprite(Color32[] px, int w, int h, Vector2 pivot)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Die" };
        t.SetPixels32(px);
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, w, h), pivot, 1f, 0, SpriteMeshType.FullRect);
    }
}
