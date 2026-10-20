# Custom gate models

Models are optional visual replacements for the existing numeric gates. Their IDs, wire
connectors, arithmetic and saved building data stay the same. No Unity project or AssetBundle
is required: this uses the same FBX-to-mesh pipeline as the official Diagonal Cutter sample.

## Quick start

1. Export your gate as **one static mesh** in `LOD0.fbx`.
2. Put it in the matching folder at the repository root:

   | Gate | Required model | Optional toolbar icon |
   | --- | --- | --- |
   | Adder | `Resources/Models/adder/LOD0.fbx` | `Resources/Icons/adder.png` |
   | Subtractor | `Resources/Models/subtractor/LOD0.fbx` | `Resources/Icons/subtractor.png` |
   | Multiplier | `Resources/Models/multiplier/LOD0.fbx` | `Resources/Icons/multiplier.png` |
   | Divider | `Resources/Models/divider/LOD0.fbx` | `Resources/Icons/divider.png` |
   | Modulo | `Resources/Models/modulo/LOD0.fbx` | `Resources/Icons/modulo.png` |

3. Run `dotnet build src/OctaneLogic/OctaneLogic.csproj`. FBX and PNG files are copied to
   the same relative paths under `$(SPZ2_PERSISTENT)/mods/OctaneLogic/Resources`.
4. Restart Shapez 2. Models are loaded once, when the mod registers its buildings.

You can also copy exported assets directly into that installed `Resources` folder and
restart the game. Paths are resolved relative to `OctaneLogic.dll`, never the working directory.
Use the exact lowercase gate names, uppercase `LOD`, and lowercase `.fbx`/`.png` extensions
so the same package works on case-sensitive platforms. The `.gitkeep` files just preserve
empty folders; they are not packaged. `.blend`, `.psd` and other source art are not copied.

If you remove or rename art in the repository, remove its old copy from the installed mod
as well: incremental builds copy current files but do not delete previously deployed assets.

## Blender checklist

Follow the [official art guidelines](https://tobspr-games.notion.site/shapez2-modding-art-guidelines):

- Work in metres. Fit the mesh inside one **1 m × 1 m** tile, centered on the horizontal
  axes, with the origin at the bottom center. Geometry must not extend below the origin.
- Keep **one material**, join the selected parts into one mesh, and triangulate the export.
  Export each LOD separately. Multiple objects/material partitions, rigged meshes and
  animated FBX scenes are outside this loader's contract.
- Apply/bake transforms before exporting. Shifter's helper converts the raw mesh and
  mirrors its X axis; it does not apply FBX scene-node transforms. Check orientation in
  game. The gate's connectors remain west=A, south=B, east=result regardless of the art.
- Keep **two UV maps in this order**: the first selects color/material cells in
  `MaterialLUT`; the second supplies tileable surface details. In Unity these are `uv`
  (UV0) and `uv2` (UV1). Their order matters more than their Blender names.
- Use the guideline's FBX export options: Selected Objects; Apply Scalings = FBX All;
  -Z Forward; Y Up; Apply Unit; Use Space Transform; Apply Transform; Smoothing =
  Normals Only; Apply Modifiers.

The game supplies its building rendering materials through `BuildingDrawData`. This
loader imports geometry, normals and UV channels; it does **not** import an FBX material,
external texture or custom shader. Position your first UV map in the game's MaterialLUT,
rather than expecting a Blender material to appear in game. The normal building rendering
path handles the layer colors and LOD materials. These are machine-level assets; the
platform guideline's third Atlas UV map is not required here.

## Optional levels of detail

Only `LOD0.fbx` is required. Export simpler meshes beside it as you finish them:

| File | Art-guide level | Runtime mesh field |
| --- | --- | --- |
| `LOD0.fbx` | Closest | `LODClose` |
| `LOD1.fbx` | Normal | `LODNormal` |
| `LOD2.fbx` | Far | `LODFar` |
| `LOD3.fbx` | Reduced | `LODReduced` |
| `LOD4.fbx` | Minimal | `LODMinimal` |
| `LOD5.fbx` | Overview | `LODOverview` |

Absent or invalid optional LODs reuse the preceding, more detailed mesh. For example, with
only LOD0 and LOD2, normal uses LOD0 and all levels beyond far use LOD2. Octane Logic maps
the files explicitly because Shifter's current builder assigns slots 3–5 in a different order.
All three building layers, placement preview and blueprint rendering use this LOD chain.
The selection collider comes from LOD0's bounds. All LODs need the same origin/orientation
and two UV channels. PNG icons are optional and independent from model loading; a square
transparent 128–256 px image is a practical starting size.

## Diagnostics and verification

- Missing `LOD0.fbx`: the original cube is used and the expected path is logged.
- Invalid FBX, missing UV channels, empty geometry, non-finite coordinates or a mesh outside
  the centered tile: a warning identifies the file and reason, then uses the cube.
- Invalid optional LOD: that level reuses the previous valid mesh and logs a warning.
- Invalid PNG: the original colored icon is used and a warning is logged.

An invalid LOD0 disables that gate's entire custom LOD chain, so a cube is not mixed with
unrelated far-distance art. Failure in one gate does not prevent the others from loading.
Imported assets are released when the mod is disposed.

Before publishing your art, place every gate, check all three building layers, rotate the
gate, inspect preview/blueprint/selection bounds, zoom through the LODs, and verify integer
outputs. Also test missing and malformed files. Back up your save before testing: physical
custom buildings still require the mod; optional art does not change that compatibility limit.

## API references

- [Official custom-building sample](https://github.com/tobspr-games/shapez2-mod-samples/blob/master/DiagonalCutter/DiagonalCutterMod.cs)
- [Shifter FBX loader](https://github.com/tobspr-games/shapez2-shifter/blob/develop/ShapezShifter/src/ShapezShifter.Kit/Rendering/FileMeshLoader.cs)
- [Shifter LOD builder](https://github.com/tobspr-games/shapez2-shifter/blob/develop/ShapezShifter/src/ShapezShifter.Kit/Rendering/LodMeshBuilder.cs)
