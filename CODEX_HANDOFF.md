# BM8 Penalty Unity Codex Handoff

## Project

- Main Unity project: `/Users/teetienyaw/Documents/Codex/2026-05-25/claude-code-project-codex/bm8-penalty-unity`
- Main scene: `Assets/Scenes/BM8PenaltyPrototype.unity`
- Main runtime script: `Assets/Scripts/Bm8PenaltyPrototype.cs`
- Scene builder: `Assets/Editor/Bm8SceneBuilder.cs`

## Current Direction

This is now a Unity 3D arcade penalty prototype. The active goalkeeper path is the uploaded stylized goalkeeper FBX plus AA Soccer Goalkeeper animation controllers. The older procedural keeper and sprite-sheet paths remain as fallback code, but they are not the intended primary presentation.

Expected Play Mode marker:

```text
BM8 PENALTY
TAP GOAL
GOALS / SAVES / SHOTS arcade HUD
```

If Unity shows an old flat scene or old text overlay, stop Play Mode, wait for compile/hot reload, then start Play Mode again in `Assets/Scenes/BM8PenaltyPrototype.unity`.

## Honest Target

The requested reference is:

```text
https://www.youtube.com/watch?v=mF14bQIQRc0
```

This prototype can be polished to look and feel like an arcade penalty game, but it is not a one-to-one commercial clone. A true match needs a production humanoid goalkeeper model, matching AA/Mecanim save animations, tuned animation events, VFX, camera work, UI art, and audio.

## Latest Keeper Work

- Uploaded stylized goalkeeper FBX is preferred at runtime.
- BM8 black/red goalkeeper kit texture is applied when available.
- Keeper has anticipation motion before the save: read step, crouch/drop, side bias, and launch.
- Top-row saves are constrained so they read as high hand tips inside the goal instead of the whole body flying away.
- Keeper holds the save pose through the result beat before returning to ready.
- Ready state has subtle breathing/weight shift.
- Humanoid IK keeps hands forward in ready state and biases top-row saves toward the ball contact point.
- Saved-ball path travels to the keeper contact point, compresses, then rebounds outward with spin.
- Save contact streak, impact flash, result flash, and camera shake are active.
- Test mode score display no longer permanently pollutes the real score.
- Debug test controls are hidden by default; press `F10` to show them.

## Gameplay / Test Controls

- Click inside the goal grid to shoot.
- Number keys or keypad `1` to `9` shoot the matching 3x3 zones.
- `T` runs all 9 forced keeper-save zones.
- `Y` runs only the top-row keeper-save zones.
- `F10` toggles hidden debug buttons: `TEST 9`, `TEST TOP`, and `RESET`.

If keyboard input does not work, click the Game view once outside the goal target area, or restart Play Mode.

## Verification

Do not claim the keeper is fixed just by reading code. Verify at least the compile log, and if Unity is available, run a Play Mode smoke test.

Recommended compile/log check:

```bash
git diff --check -- Assets/Scripts/Bm8PenaltyPrototype.cs CODEX_HANDOFF.md
touch Assets/Scripts/Bm8PenaltyPrototype.cs
sleep 7
tail -n 1800 ~/Library/Logs/Unity/Editor.log | rg -n "error CS|Exception|NullReferenceException|Scripts have compiler errors|ambiguous reference|timeout|Watchdog|FAIL|Look rotation viewing vector is zero" || true
```

Visual checks:

- Ready keeper should not look frozen.
- Top-left/top-center/top-right saves should stay inside the goal frame.
- Top-center should read as glove/hand tip, not a header.
- Middle and low saves should not reuse the exact same height.
- Saved ball should rebound away from the glove/contact point.
- Keeper should hold the result pose briefly, then reset cleanly.
