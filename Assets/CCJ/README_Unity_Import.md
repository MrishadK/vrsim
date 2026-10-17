# Calicut International Airport (CCJ / VOCL) — Unity 3D Environment Package

## 1. Overview & Coordinate System
This package provides a complete, game-ready 3D model of **Calicut International Airport (IATA: CCJ, ICAO: VOCL)** in Karipur, Malappuram District, Kerala, India, engineered for direct import into **Unity** (`1 Unity unit = 1 metre`, Y-Up, metric scale).

### Grounded Airport Specifications (AAI eAIP India VOCL AD 2.1 – 2.23)
- **Aerodrome Reference Elevation**: `104.5 m (343 ft) AMSL` tabletop plateau (`Y = 0.0 m` datum in local Unity scene coordinates, with surrounding Karipur laterite gorges dropping to `Y = -62.0 m` and outer Malabar hills rising around the circling area).
- **Runway 10 / 28**: `2,860 m × 45 m` asphalt (`PCN 72/F/A/W/T`, true bearing `100.90° / 280.90°`), `2,980 m × 150 m` graded strip, `240 m × 90 m` Runway End Safety Areas (RESA), southern trapezoidal turn pad at RWY 28, west turn pad at RWY 10, and northern intermediate turn pad (`2,250 m` from RWY 10 threshold).
- **Taxiways**: `TWY A` (`2,130 m` from RWY 28 THR, `23 m` width + `10.5 m` shoulders), `TWY B` (`1,990 m` from RWY 28 THR), `TWY C` (`1,829 m` from RWY 28 THR, `90°` Code E capable), `TWY D` (`2,377 m` from RWY 28 THR, `175 m` length to isolation apron), and parallel apron taxilane.
- **Apron & Aircraft Stands**: `Stands 1 to 13` (including overlapping wide-body stands `5A`, `9`, and `12A`), equipped with AVDGS totems, passenger boarding bridges, and a fleet of `B737-800` (Air India Express livery) and `A320neo` (IndiGo livery) aircraft plus full Ground Support Equipment (GSE).
- **Passenger Terminal Complex**: Recognizable multi-wing CCJ terminal (`Domestic T1`, `International Departures T2`, and `International Arrival Block`) with traditional Kerala sloped terracotta gable roofs and modern standing-seam spans.
- **Explorable Interior & Removable Roof**: The roof is isolated in the `Terminal_Roof` object/collection. Hiding `Terminal_Roof` in the Unity Hierarchy immediately reveals the fully modeled, connected `Terminal_Interior` (`Check-in Hall`, `Security Screening`, `Emigration/Immigration Booths`, `Departure SHA Lounges & Gates 1–10`, `Duty-Free & Food Court`, `5 Baggage Reclaim Carousels`, `Customs Red/Green Channels`, `Staircases`, `Elevators`, and `Restrooms`).

## 2. Included Files
1. `CCJ_Calicut_Airport_Complete.glb` — Binary glTF 2.0 scene containing the entire airport hierarchy and all 24 embedded PBR textures (BaseColor, Normal, MetallicRoughness). Ready for one-step drag-and-drop into Unity (via Unity glTFast / Khronos glTF Importer) or Blender/Unreal.
2. `CCJ_Calicut_Airport_Complete.fbx` — Autodesk FBX 7.3 ASCII full-scene file preserving all named collections, meshes, normals, UVs, and material slots for native Unity FBX Importer support.
3. `CCJ_Calicut_Airport_Complete.obj` + `CCJ_Calicut_Airport_Complete.mtl` — Wavefront OBJ/MTL universal fallback.
4. `Modules_FBX/` & `Modules_GLB/` — Separate, shared-origin modular 3D files (`Terrain`, `Runway`, `Taxiways`, `Apron`, `Terminal_Exterior`, `Terminal_Roof`, `Terminal_Interior`, `Airport_Roads`, `Airport_Lighting`, `Airport_Signs`, `Ground_Equipment`, `Aircraft`, `Vegetation`, `Support_Buildings`) for level streaming and independent prefab workflows.
5. `Textures/` — 24 high-resolution PBR PNG texture maps (`*_BaseColor.png`, `*_Normal.png`, `*_MetallicRoughness.png`).

## 3. Unity Import & Setup Instructions
1. Copy the `Textures/` folder into your Unity project's `Assets/CCJ_Airport/Textures/` directory first, and mark all `*_Normal.png` textures as **Normal map** in the Unity Texture Importer.
2. Drag `CCJ_Calicut_Airport_Complete.fbx` (or `CCJ_Calicut_Airport_Complete.glb`) into `Assets/CCJ_Airport/Models/`.
3. In the Unity Model Importer (**Model** tab):
   - Verify **Scale Factor** is `1` (`Convert Units` enabled, `1m = 1 Unity Unit`).
   - Enable **Generate Colliders** on `Terrain`, `Runway`, `Taxiways`, `Apron`, `Airport_Roads`, and `Terminal_Interior` walkable floors for instant NavMesh baking and vehicle/aircraft physics.
4. To inspect or walk through the terminal interior in the Unity Scene View, toggle off the visibility of the `Terminal_Roof` child GameObject.
