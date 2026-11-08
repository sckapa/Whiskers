# `feature/dice-select-bg`: dice-select screen and cheese dice

Branched from `main`. This PR adds the dice-select screen, 2D dice drawn in code, and nine cheese dice modelled in Blender with the numbers already engraved.

The grey blank dice, which other devs label with symbols and numbers from a script, are a **separate PR** (`feature/grey-base-dice`). The two PRs don't share any files.

## What's in it

### 1. Dice-select screen
- `Assets/Whiskers/Shaders/WhiskersVortexBG.shader`: procedural, pixel-snapped, Bayer-dithered vortex background.
- `Assets/Whiskers/Shaders/WhiskersCRTScreen.shader`: barrel distortion, scanlines, RGB mask, chromatic aberration, roll, flicker, vignette.
- `Assets/Whiskers/Materials/VortexBG.mat` and `CRTScreen.mat`.
- `Assets/Whiskers/RT_Game480x270.renderTexture`: 480×270, point filter, no AA, no mips.
- `Assets/Scenes/DiceSelect.unity`: an orthographic GameCamera renders to the render texture, which is shown full-screen through the CRT material on a RawImage.

### 2. 2D dice drawn in code
- `Assets/Whiskers/Scripts/DiceSpriteFactory.cs` draws ivory, cheese and red d6s, the wedge d4, and the d8, d10, d12 and d20 for each pixel size, with an outline and drop shadow, using GBA 15-bit colours. No sprite assets are needed.
- `Assets/Whiskers/Scripts/VortexDiceSpawner.cs` spins the dice into the vortex. Most dice land on a lucky 9 (`luckyNineChance`, default 0.8). You can still use sprite dice by turning off **Use Procedural Dice**.

### 3. Nine cheese dice in 3D
- `Tools/Blender/build_cheese_dice.py` builds every die from `Design/CheeseDice_Blender_Spec.md`, headless, in about 12 seconds:
  ```bash
  blender -b -P Tools/Blender/build_cheese_dice.py -- --preview <folder-for-renders>
  ```
- It outputs 18 files to `Assets/Whiskers/Models/Dice/` (`Die_<Cheese>_<die>.fbx` and `..._Lucky9.fbx`) and the source file `Art/Blender/CheeseDice.blend`.
- The cheeses: Emmental d6, Cheddar d6, RedWindsor d6, Brie d4 (wedge), Parmesan d4 (wedge), Pecorino d8, BlueCheese d10, Gouda d12, Edam d20.
- Materials are named `M_<Cheese>_Main`, `_Holes` and `_Rind`, with colours from the spec.
- Size: a d6 edge is 1.0 and the d12 and d20 are ×1.15. The pivot is at the centre of mass.
- Choices made where the spec was open:
  - Numbers use the game's 3×5 pixel font.
  - On the Lucky 9 dice, the 9 replaces whatever number is on the top face, so the d12 and d20 versions show 9 twice.
  - The d10 is numbered 1 to 10.

## How to review
1. Open `Assets/Scenes/DiceSelect.unity` and press Play. The dice should spin into the vortex behind the CRT effect.
2. Drag any FBX from `Assets/Whiskers/Models/Dice/` into a scene to check its scale and materials.
3. Optional: rebuild the dice with the command above and check that git shows no changes to the meshes. The FBX files contain timestamps, so they will show as changed bytes even when the geometry is the same.

## Notes
- **Git LFS is off.** The history was rewritten on 8 Nov 2026 to take files out of LFS, so every commit hash changed, including `main`. The last line of `.gitattributes` (`* !text !filter !merge !diff`) keeps every file in normal git. That line also overrides the `text` and `unityyamlmerge` settings above it. A clone made before the rewrite needs `git fetch origin`, followed by rebasing any unpushed work onto the new branches.
- **For the team to decide:** if dice faces will be added by script on top of the grey dice, the cheese dice could be blank as well (a cheese-coloured material plus faces added at runtime). The build script can drop the engraved numbers easily.

## Not done yet
- The optional 64×64 sprite renders (step 9 of the spec).
- An underline to tell 6 and 9 apart on the d12 and d20.
- Answers to the open questions about dice shapes in the GDD.
