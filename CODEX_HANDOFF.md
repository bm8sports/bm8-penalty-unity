# BM8 Penalty Unity Codex Handoff

## Project

- Main Unity project: `/Users/teetienyaw/bm8-penalty-unity`
- Main scene: `Assets/Scenes/BM8PenaltyPrototype.unity`
- Main scripts:
  - `Assets/Scripts/Bm8PenaltyPrototype.cs`
  - `Assets/Editor/Bm8SceneBuilder.cs`
- Related earlier web prototype/spec: `/Users/teetienyaw/bm8-penalty-demo`

## Current State

Wenxi is iterating on a BM8-branded penalty shootout goalkeeper save prototype.

Latest expected in-game version marker is now:

```text
V11 sprite-sheet keeper ready - tap a goal square
```

If Unity shows older markers like `V4 hand save`, `V5 distinct dives`, `V6 glove contact`, `V7 reference glove save - run up`, `V8 glove-lock save`, `V9 human keeper`, or `V10 low-poly human keeper`, it is likely running an old play session, window, or cache. Stop Play, wait for compile, and reopen the correct project/scene.

## Reference

Desired goalkeeper style is based on:

```text
https://www.youtube.com/watch?v=mF14bQIQRc0
```

The keeper should explosively dive, lead with hands/gloves, contact the ball with the palm/glove, then punch or deflect the ball outward.

## Behavioral Requirements

- Goalkeeper saves must visibly use hands/gloves, not head/body.
- TC must be two hands above/in front of head, not a header.
- On save, status should show `SAVED - glove punches ball out`.
- Ball should be punched back into the field from glove/palm contact.
- TC/ML/BL must not share identical height/action.
- TR/MR/BR must not share identical height/action.
- Top row should be high/jumping saves.
- Middle row should be horizontal dives.
- Bottom row should be low/near-ground dives.
- Saved-ball contact point should use `SavePalmWorld()`.
- Visible yellow glove spheres should align with the same contact point.

## V8 Changes

- Added row/column goalkeeper save profiles in `Bm8PenaltyPrototype.cs`.
- Saved balls now travel to `SavePalmWorld()`, pause/compress at the palm, then punch back into the field.
- Yellow glove spheres now lock to the palm contact before applying the punch offset.
- Save status now reads `SAVED - glove punches ball out`.
- Unity batchmode verification was attempted at `Logs/v8-glove-lock-rebuild.log`, but Unity licensing initialization failed before compilation.

## V9 Human Keeper Change

- `Bm8SceneBuilder.CreateVisibleFbxKeeper()` no longer prioritizes `Assets/animo/AA_Soccer_Goalkeeper/Prefabs/Robot.prefab`.
- Rebuilt scenes now use the procedural 3D human keeper rig instead of any imported robot keeper.
- If the current scene still contains an old Robot prefab instance, `Bm8PenaltyPrototype.DisableImportedRobotKeeper()` disables it at runtime so the procedural human keeper is visible.
- The procedural keeper now wears a black/red BM8-style goalkeeper kit.
- The yellow glove marker system remains active for glove-contact saves.

## V10 Low-Poly Human Keeper Change

- Improved the procedural keeper proportions so it reads more like a low-poly 3D human instead of a robot.
- Added visible face parts: eyes, nose, mouth, ears, and hair cap.
- Added larger shoulders, skin forearms, sleeve trims, BM8 chest plate, white piping, black/red kit panels, boots, and glove palm plates.
- This is still a primitive-based low-poly character, not a rigged realistic FBX model.

## V11 Sprite Sheet Keeper Change

- Added runtime support for `Assets/Art/Characters/keeper-sprite-sheet.png`.
- When that file exists, the game creates a chroma-keyed sprite-sheet keeper quad and hides the primitive keeper body.
- Added `Assets/Shaders/BM8ChromaKeySprite.shader` to remove the green background.
- Sprite layout expected: 5 columns x 2 rows, matching the supplied keeper pose sheet.
- `DiveKeeper()` now switches sprite frames during high, middle, and low saves while keeping the yellow glove contact markers active.

## Validation Notes

Do not merely assert the fix. If Wenxi provides another clip, inspect whether visible ball contact is actually at the gloves and whether TL/TC/TR/ML/MR/BL/BR have distinct heights/actions.

Before changing anything, inspect the current files and preserve unrelated user changes.
