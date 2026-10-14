# A350-900 neo concept model

This is a real-scale, procedural **A350-900-inspired neo concept**, not a real or announced Airbus A350neo variant. It uses the published A350-900 envelope (66.80 m long, 64.75 m wingspan, 17.05 m high) as a proportion guide. Geometry is a detailed visual approximation, not engineering/CAD data.

## Outputs

- `A350-900_NEO_Concept.prefab` — editable Unity hierarchy with a cabin cutaway and animated parts.
- `A350-900_NEO_Showcase.unity` — standalone lit apron scene with a camera.
- `A350-900_NEO_Concept.obj` and `.mtl` — static geometry for general-purpose 3D import.
- `Runtime/A350NeoConceptControls.cs` — keyboard controls.
- `Editor/A350NeoConceptBuilder.cs` — reproducible procedural builder and OBJ exporter.

Run **A350 NEO Concept → Build or Rebuild Model and Showcase** from Unity's menu to generate or regenerate the model. Open the showcase scene from `Assets/A350NeoConcept/`.

## Showcase controls

- `C` — toggle the cutaway view to reveal the interior.
- `D` — open/close all eight passenger doors.
- `G` — deploy/stow the landing gear.
- `F` — extend/retract the flaps.
- `S` — raise/lower the spoilers.
- Arrow keys — move elevators and ailerons.
- `Q` and `E` — move the rudder left/right.

The prefab retains separate exterior, interior, gear, door, and control-surface groups. Its passenger cabin has 24 business, 28 premium-economy, and 243 economy seats, plus a cockpit, service areas, bins, cabin lighting, and windows. The model does not simulate aerodynamics or provide flight controls.

## Reference

Airbus publishes the A350-900 dimensions and typical cabin data on its [A350-900 page](https://www.aircraft.airbus.com/en/aircraft/a350/a350-900). The `neo` designation here marks a fictional re-engined concept requested for this model.
