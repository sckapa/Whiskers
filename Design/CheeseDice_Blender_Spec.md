# Whiskers Rig-ma-roll — Cheese Dice ("The Lucky Nine") — Blender build spec

Nine cheese dice taken from the cheese mood board. Each die is a piece of cheese.
- **Main**: the cheese body (cut faces).
- **Holes**: the pips / eyes / flecks / crystals (the secondary colour).
- **Rind**: the outer faces or wax coating, where a cheese has one.

All hex values are snapped to the GBA 15-bit (RGB555) grid used across the game's art. Type them into the **Hex** field of Blender's colour picker (that field is sRGB, so the result matches the game palette). Do not convert to linear by hand.

## The nine dice

| # | Cheese | Die | Main | Holes | Rind / wax | Hole style | Notes |
|---|--------|-----|------|-------|------------|------------|-------|
| 1 | Emmental (Swiss) | d6 | `#FFE74A` | `#DE9C21` | — | Big round spherical eyes as pips | Hero die. |
| 2 | Mature Cheddar | d6 | `#FFA521` | `#C66310` | — | Small rough, irregular pits as pips | Crumbly block, slightly chipped edges. |
| 3 | Red Windsor | d6 | `#F7C663` | `#B52131` | — | Flat wine-red flecks/blots as pips (painted, not cut) plus random marbling streaks | The British one. |
| 4 | Brie | d4 (wedge) | `#FFEFA5` | `#D6B563` | `#F7F7DE` bloomy white | Tiny shallow air pockets | Soft wedge; rind on back face and base; slightly sagging, rounded rind edge. |
| 5 | Parmesan | d4 (wedge) | `#F7D67B` | `#C69442` | `#AD6B29` | Granular pits / crystal speckle (no round holes) | Hard wedge; thick rind band along the base. |
| 6 | Pecorino | d8 | `#EFDEAD` | `#B59C63` | `#845229` brown | Few small pinholes | Basket-pressed rind texture on the faces that touch the outer edge. |
| 7 | Blue Cheese | d10 | `#5A94F7` | `#294AB5` | — | Holes plus a few dark-blue vein streaks | Cartoon blue (the mood-board mascot), not realistic Stilton. |
| 8 | Gouda | d12 | `#FFC631` | `#CE8418` | `#FFEF4A` yellow wax | Small sparse round eyes | Wax on edges/bevels, cut paste on the faces. |
| 9 | Edam | d20 | `#FFDE6B` | `#D6A539` | `#DE1839` red wax | Small sparse round eyes | The round ball cheese; red wax on edges/bevels. |

Outline / shadow colour used in-game: `#100829` (VortexBG palette 0).

## Lucky 9

Every die has to be able to show a **9**. A real d4/d6/d8 can't have a 9 face, so build two variants per die:
- `Die_<Cheese>_<die>`: standard numbering (d6 opposite faces sum to 7; d4/d8/d10/d12/d20 standard layouts).
- `Die_<Cheese>_<die>_Lucky9`: same mesh, with the up/front face showing **9**. On d6s, that face is a 3×3 grid of holes; on every other die, it's the number 9.

## Build rules

1. **Script it.** Write `Tools/Blender/build_cheese_dice.py` and run it headless (`blender -b -P Tools/Blender/build_cheese_dice.py`), so the dice can be rebuilt any time. Don't hand-model.
2. **Size**: in game units, d6 edge = 1.0. Scale the other dice so their silhouettes read about as big as the d6 (d20/d12 about 1.15). Origin at the centre of mass; apply all transforms.
3. **Shapes**:
   - d6: a cube with a small bevel (0.06, 2 segments). Cheddar gets slightly irregular chipped edges (random vertex jitter on the bevel only).
   - d4: a triangular cheese **wedge** (as on the canvas, not a tetrahedron). The number sits on the big cut face.
   - d8, d10, d12, d20: standard polyhedra with a small bevel (0.04).
4. **Holes / pips**: boolean-difference spheres into the faces (depth ≈ 0.5 × radius) for Emmental, Gouda, Edam, Pecorino and Blue. Use rough displaced cavities for Cheddar and Parmesan. Red Windsor flecks are painted (texture/vertex colour), not cut. Add 2–4 extra random small holes on non-numbered areas for cheesy texture (seeded, so rebuilds are identical).
5. **Numbers** (d4, d8–d20): engrave with text → mesh → boolean, depth 0.03, in the Holes colour. Use a blocky pixel-style font if one is in the repo, otherwise Blender's default.
6. **Materials**: one Principled BSDF per colour role, named `M_<Cheese>_Main`, `M_<Cheese>_Holes`, `M_<Cheese>_Rind`. Roughness 0.65, Specular 0.3, slight subsurface (0.05) on soft cheeses (Brie, Blue). No image textures needed. Keep it flat and toon-friendly: these get rendered small.
7. **Rind/wax**: assign the Rind material to the faces/bevels named in the table (wedge back + base for Brie/Parmesan, outer faces for Pecorino, bevels/edges for Gouda/Edam wax).
8. **Export**: one FBX per die variant to `Assets/Whiskers/Models/Dice/` (Apply Transform on, Forward -Z, Up Y, mesh only, no cameras or lights). Also save the `.blend` to `Art/Blender/CheeseDice.blend`.
9. **Optional sprite pass**: render each variant orthographic, 64×64, transparent background, flat lighting from top-left, then posterise to the colours above, into `Art/Renders/Dice/`. These can replace the procedural dice in `VortexDiceSpawner` (turn off Use Procedural Dice).
