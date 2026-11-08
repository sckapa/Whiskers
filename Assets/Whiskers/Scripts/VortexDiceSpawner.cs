using UnityEngine;
using System.Collections.Generic;

// Whiskers Rig-ma-roll — spins dice into the vortex, matching the design canvas.
// Put it on an empty GameObject at the centre of the screen, in front of the VortexBG quad.
// Distances and sizes are in ART PIXELS (the 480x270 grid); pixelsPerUnit converts to Unity units.
//
// Two sources of dice:
//  - Procedural (default): DiceSpriteFactory draws every die in code, rebuilt per pixel size so
//    pips and numbers stay crisp. No sprites needed.
//  - Sprites: turn Use Procedural Dice off and fill Die Types with your Aseprite faces
//    (faces[0] = 1, faces[1] = 2, ... ; add a 9th sprite to get the lucky 9).
public class VortexDiceSpawner : MonoBehaviour
{
    [System.Serializable]
    public class DieType
    {
        public string name = "d6";
        [Tooltip("faces[0] shows 1, faces[1] shows 2, ... A 9th sprite (faces[8]) is used for the lucky 9.")]
        public Sprite[] faces;
        [Range(0f, 1f)] public float weight = 0.1f;
    }

    [System.Serializable]
    public class ProceduralDie
    {
        public DieKind kind;
        [Range(0f, 1f)] public float weight = 0.1f;
        public ProceduralDie() { }
        public ProceduralDie(DieKind k, float w) { kind = k; weight = w; }
    }

    [Header("Dice source")]
    [Tooltip("On: dice are generated in code (no sprites needed). Off: uses the sprites in Die Types.")]
    public bool useProceduralDice = true;
    public ProceduralDie[] proceduralDice =
    {
        new ProceduralDie(DieKind.Ivory, 0.24f),
        new ProceduralDie(DieKind.Cheese, 0.24f),
        new ProceduralDie(DieKind.CheeseWedge, 0.12f),
        new ProceduralDie(DieKind.Red, 0.08f),
        new ProceduralDie(DieKind.D8, 0.08f),
        new ProceduralDie(DieKind.D10, 0.08f),
        new ProceduralDie(DieKind.D12, 0.08f),
        new ProceduralDie(DieKind.D20, 0.08f),
    };
    [Tooltip("Outline and drop-shadow colour. Keep it equal to VortexBG Palette 0.")]
    public Color outlineColor = new Color(0.0627f, 0.0314f, 0.1608f, 1f);
    public DieType[] dieTypes;

    [Header("Lucky 9")]
    [Tooltip("Chance that each roll lands on 9. d6s show a 3x3 nine-pip face, d4 wedges show a 9.")]
    [Range(0f, 1f)] public float luckyNineChance = 0.8f;

    [Header("Count")]
    [Range(10, 90)] public int count = 46;

    [Header("Motion")]
    public float speed = 1f;
    public float maxRadius = 290f;
    public float minRadius = 10f;
    [Tooltip("Squash the orbit vertically for a 16:9 vortex.")] public float ellipseY = 0.62f;
    public float rotationStepDegrees = 7.5f;

    [Header("Size (art pixels)")]
    public float outerSize = 36f;
    public float innerSize = 3f;

    [Header("Rendering")]
    public float pixelsPerUnit = 1f;
    public int baseSortingOrder = 10;
    public string sortingLayer = "Default";
    [Tooltip("Only used for sprite dice; procedural dice get a solid shadow in Outline Color.")]
    public Color shadowTint = new Color(0.0627f, 0.0314f, 0.1608f, 1f);

    class Die
    {
        public Transform t, st;
        public SpriteRenderer sr, ssr;
        public DieKind kind;
        public DieType type;
        public int val;
        public float r, a, sv, rot, rs, flip;
    }

    readonly List<Die> dice = new List<Die>();

    void Update()
    {
        while (dice.Count < count) dice.Add(Create());
        while (dice.Count > count)
        {
            var last = dice[dice.Count - 1];
            Destroy(last.t.gameObject);
            Destroy(last.st.gameObject);
            dice.RemoveAt(dice.Count - 1);
        }

        float ppu = Mathf.Max(0.0001f, pixelsPerUnit);
        float dt = Time.deltaTime * speed;
        Color32 dark = outlineColor;
        float step = Mathf.Max(0.01f, rotationStepDegrees);

        foreach (var d in dice)
        {
            float rn = Mathf.Max(0f, d.r / maxRadius);
            float inner = 1f - Mathf.Min(1f, rn);
            d.r -= dt * (7f + 46f * inner * inner);
            d.a += dt * (0.1f + 0.9f * inner * inner * inner);   // counter-clockwise, like the background
            d.rot += dt * d.rs;
            d.flip -= dt;
            if (d.flip <= 0f) { d.flip = Random.Range(1f, 3.5f); Roll(d); }
            if (d.r < minRadius) Spawn(d, false);

            float sizePx = (innerSize + (outerSize - innerSize) * Mathf.Pow(Mathf.Min(1.1f, d.r / maxRadius), 1.35f)) * d.sv;
            if (useProceduralDice && (d.kind == DieKind.D12 || d.kind == DieKind.D20)) sizePx *= 1.1f;

            float x = Mathf.Round(Mathf.Cos(d.a) * d.r);
            float y = Mathf.Round(Mathf.Sin(d.a) * d.r * ellipseY);
            var rot = Quaternion.Euler(0, 0, Mathf.Round(d.rot * Mathf.Rad2Deg / step) * step);
            int order = baseSortingOrder + Mathf.RoundToInt(sizePx) * 2;
            d.sr.sortingOrder = order;
            d.ssr.sortingOrder = order - 1;

            if (useProceduralDice)
            {
                int s = Mathf.RoundToInt(Mathf.Min(46f, sizePx));
                d.t.localPosition = new Vector3(x, y, 0f) / ppu;
                if (s < 4)
                {
                    // far away: just a 1-2 px speck
                    d.sr.sprite = DiceSpriteFactory.Pixel;
                    d.sr.color = DiceSpriteFactory.DotColor(d.kind);
                    d.t.localRotation = Quaternion.identity;
                    d.t.localScale = Vector3.one * (s > 2 ? 2f : 1f) / ppu;
                    d.ssr.enabled = false;
                    continue;
                }
                var ds = DiceSpriteFactory.Get(d.kind, d.val, s, dark);
                d.sr.sprite = ds.main;
                d.sr.color = Color.white;
                d.t.localRotation = rot;
                d.t.localScale = Vector3.one / ppu;

                int sh = Mathf.Max(1, Mathf.RoundToInt(s / 10f));
                d.ssr.enabled = true;
                d.ssr.sprite = ds.shadow;
                d.ssr.color = Color.white;
                d.st.localPosition = new Vector3(x + sh, y - 2 * sh, 0f) / ppu;
                d.st.localRotation = rot;
                d.st.localScale = Vector3.one / ppu;
            }
            else
            {
                float nativePx = d.sr.sprite != null ? d.sr.sprite.rect.height : outerSize;
                float scale = sizePx / Mathf.Max(1f, nativePx);
                d.t.localPosition = new Vector3(x, y, 0f) / ppu;
                d.t.localRotation = rot;
                d.t.localScale = Vector3.one * scale;
                d.sr.color = Color.white;

                float sh = Mathf.Max(1f, Mathf.Round(sizePx / 10f));
                d.ssr.enabled = d.sr.sprite != null;
                d.ssr.sprite = d.sr.sprite;
                d.ssr.color = shadowTint;
                d.st.localPosition = new Vector3(x + sh, y - 2f * sh, 0f) / ppu;
                d.st.localRotation = rot;
                d.st.localScale = Vector3.one * scale;
            }
        }
    }

    Die Create()
    {
        var d = new Die();
        var go = new GameObject("Die");
        go.transform.SetParent(transform, false);
        d.t = go.transform;
        d.sr = go.AddComponent<SpriteRenderer>();
        d.sr.sortingLayerName = sortingLayer;

        var sgo = new GameObject("DieShadow");
        sgo.transform.SetParent(transform, false);
        d.st = sgo.transform;
        d.ssr = sgo.AddComponent<SpriteRenderer>();
        d.ssr.sortingLayerName = sortingLayer;

        Spawn(d, true);
        return d;
    }

    void Spawn(Die d, bool fresh)
    {
        if (useProceduralDice) d.kind = PickKind();
        else d.type = PickType();
        d.r = fresh ? minRadius + (maxRadius - minRadius) * Mathf.Sqrt(Random.value)
                    : maxRadius * Random.Range(0.92f, 1.07f);
        d.a = Random.value * Mathf.PI * 2f;
        d.sv = Random.Range(0.8f, 1.25f);
        d.rot = Random.value * Mathf.PI * 2f;
        d.rs = (Random.value - 0.5f) * 1.3f;
        d.flip = Random.Range(1f, 3.5f);
        Roll(d);
    }

    void Roll(Die d)
    {
        bool lucky = Random.value < luckyNineChance;
        if (useProceduralDice)
        {
            d.val = lucky ? 9 : Random.Range(1, DiceSpriteFactory.Sides(d.kind) + 1);
            return;
        }
        if (d.type == null || d.type.faces == null || d.type.faces.Length == 0) { d.sr.sprite = null; return; }
        int n = d.type.faces.Length;
        int idx = (lucky && n >= 9) ? 8 : Random.Range(0, n);
        d.sr.sprite = d.type.faces[idx];
    }

    DieKind PickKind()
    {
        if (proceduralDice == null || proceduralDice.Length == 0) return DieKind.Ivory;
        float total = 0f;
        foreach (var p in proceduralDice) total += Mathf.Max(0f, p.weight);
        float roll = Random.value * total;
        foreach (var p in proceduralDice) { roll -= Mathf.Max(0f, p.weight); if (roll <= 0f) return p.kind; }
        return proceduralDice[proceduralDice.Length - 1].kind;
    }

    DieType PickType()
    {
        if (dieTypes == null || dieTypes.Length == 0) return null;
        float total = 0f;
        foreach (var t in dieTypes) total += t.weight;
        float roll = Random.value * total;
        foreach (var t in dieTypes) { roll -= t.weight; if (roll <= 0f) return t; }
        return dieTypes[dieTypes.Length - 1];
    }

    void OnDestroy()
    {
        DiceSpriteFactory.ClearCache();
    }
}
