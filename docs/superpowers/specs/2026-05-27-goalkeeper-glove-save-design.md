> **HISTORICAL (superseded 2026-06):** This design describes the earlier procedural
> glove-marker save pass. The current direction is the stylized FBX keeper driven by
> AA Mecanim controllers with humanoid IK — see `CODEX_HANDOFF.md` at the project root.

# Goalkeeper Glove Save Design

## Goal

Make the Unity goalkeeper save read as a glove-led save: the keeper launches toward the selected target, the visible gloves meet the ball at the same world point, and saved shots are punched back into the field.

## Scope

This pass focuses on `Assets/Scripts/Bm8PenaltyPrototype.cs`. It keeps the existing scene, imported goalkeeper controllers, and procedural yellow glove markers. It does not rebuild the old HTML prototype.

## Behavior

- Saved shots use `SavePalmWorld()` as the visual ball contact point.
- Yellow glove spheres lock to the same contact point during the save window.
- Top, middle, and bottom rows use different heights and body actions.
- TC uses two high hands in front of the head, not a header.
- Saves show `SAVED - glove punches ball out`.
- The ball rebounds from the palm/glove point back toward the field.

## Validation

Run Unity batchmode compile/rebuild after edits. In Play mode, test keypad 7/8/9, 4/5/6, and 1/2/3 to confirm the keeper action differs by row and column.
