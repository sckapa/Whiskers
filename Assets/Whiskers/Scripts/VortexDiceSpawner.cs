using UnityEngine;
using System.Collections.Generic;

// Whiskers Rig-ma-roll — spins dice sprites into the vortex, matching the canvas prototype.
// Place this on an empty GameObject at the centre of the screen, in front of the VortexBG quad.
// Distances and sizes are in ART PIXELS (the 480x270 grid); set pixelsPerUnit to your sprite import PPU.
public class VortexDiceSpawner : MonoBehaviour
{
    [System.Serializable]
    public class DieType
    {
        public string name = "d6";
        [Tooltip("One sprite per face; a random face shows, and it re-rolls while tumbling.")]
        public Sprite[] faces;
        [Range(0f, 1f)] public float weight = 0.1f;
    }

    [Header("Dice")]
    public DieType[] dieTypes;      // e.g. Ivory d6, Cheese d6, Cheese wedge d4, Red d6, d8, d10, d12, d20
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
    public Color shadowTint = new Color(0.0627f, 0.0314f, 0.1608f, 1f);

    class Die
    {
        public Transform root;
        public SpriteRenderer sr, shadow;
        public DieType type;
        public float r, a, sv, rot, rs, flip;
    }

    readonly List<Die> dice = new List<Die>();

    void Update()
    {
        while (dice.Count < count) dice.Add(Create());
        while (dice.Count > count) { Destroy(dice[dice.Count - 1].root.gameObject); dice.RemoveAt(dice.Count - 1); }

        float dt = Time.deltaTime * speed;
        foreach (var d in dice)
        {
            float rn = Mathf.Max(0f, d.r / maxRadius);
            float inner = 1f - Mathf.Min(1f, rn);
            d.r -= dt * (7f + 46f * inner * inner);
            d.a += dt * (0.1f + 0.9f * inner * inner * inner);   // counter-clockwise, like the background
            d.rot += dt * d.rs;
            d.flip -= dt;
            if (d.flip <= 0f) { d.flip = Random.Range(1f, 3.5f); SetFace(d); }
            if (d.r < minRadius) Spawn(d, false);

            float sizePx = (innerSize + (outerSize - innerSize) * Mathf.Pow(Mathf.Min(1.1f, d.r / maxRadius), 1.35f)) * d.sv;
            float nativePx = d.sr.sprite != null ? d.sr.sprite.rect.height : outerSize;
            float scale = sizePx / Mathf.Max(1f, nativePx);

            float x = Mathf.Round(Mathf.Cos(d.a) * d.r);
            float y = Mathf.Round(Mathf.Sin(d.a) * d.r * ellipseY);
            d.root.localPosition = new Vector3(x, y, 0f) / pixelsPerUnit;
            float step = Mathf.Max(0.01f, rotationStepDegrees);
            d.root.localRotation = Quaternion.Euler(0, 0, Mathf.Round(d.rot * Mathf.Rad2Deg / step) * step);
            d.root.localScale = Vector3.one * scale;

            float shOff = Mathf.Max(1f, Mathf.Round(sizePx / 10f)) / pixelsPerUnit / Mathf.Max(0.0001f, scale);
            d.shadow.transform.localPosition = new Vector3(shOff, -shOff * 2f, 0f);

            int order = baseSortingOrder + Mathf.RoundToInt(sizePx) * 2;
            d.sr.sortingOrder = order;
            d.shadow.sortingOrder = order - 1;
        }
    }

    Die Create()
    {
        var d = new Die();
        var go = new GameObject("Die");
        go.transform.SetParent(transform, false);
        d.root = go.transform;
        d.sr = go.AddComponent<SpriteRenderer>();
        d.sr.sortingLayerName = sortingLayer;

        var sh = new GameObject("Shadow");
        sh.transform.SetParent(go.transform, false);
        d.shadow = sh.AddComponent<SpriteRenderer>();
        d.shadow.sortingLayerName = sortingLayer;
        d.shadow.color = shadowTint;

        Spawn(d, true);
        return d;
    }

    void Spawn(Die d, bool fresh)
    {
        d.type = PickType();
        d.r = fresh ? minRadius + (maxRadius - minRadius) * Mathf.Sqrt(Random.value)
                    : maxRadius * Random.Range(0.92f, 1.07f);
        d.a = Random.value * Mathf.PI * 2f;
        d.sv = Random.Range(0.8f, 1.25f);
        d.rot = Random.value * Mathf.PI * 2f;
        d.rs = (Random.value - 0.5f) * 1.3f;
        d.flip = Random.Range(1f, 3.5f);
        SetFace(d);
    }

    void SetFace(Die d)
    {
        if (d.type == null || d.type.faces == null || d.type.faces.Length == 0) return;
        var s = d.type.faces[Random.Range(0, d.type.faces.Length)];
        d.sr.sprite = s;
        d.shadow.sprite = s;
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
}
