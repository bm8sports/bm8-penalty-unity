> **HISTORICAL (superseded 2026-06):** This plan was executed against an earlier
> procedural-glove direction and contains stale absolute paths
> (`/Users/teetienyaw/bm8-penalty-unity/...`). The project now lives at the repo root
> containing `CODEX_HANDOFF.md`; the active keeper is the stylized FBX + AA controllers.
> Do not follow these steps literally.

# Goalkeeper Glove Save Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make saved shots visibly contact the goalkeeper gloves and rebound outward.

**Architecture:** Keep the existing single-script prototype but isolate save motion constants behind small helper methods. `DiveKeeper()` drives the keeper body and gloves; `FlyBall()` drives the ball through the same palm contact point.

**Tech Stack:** Unity C#, existing `Bm8PenaltyPrototype` MonoBehaviour, existing imported goalkeeper animator controllers.

---

### Task 1: Glove Save Motion

**Files:**
- Modify: `/Users/teetienyaw/bm8-penalty-unity/Assets/Scripts/Bm8PenaltyPrototype.cs`

- [ ] Add a compact save profile helper for row/column-specific lift, reach, and punch timing.
- [ ] Update `DiveKeeper()` so glove markers converge on `SavePalmWorld()` at contact.
- [ ] Keep TC as a two-hand high save.

### Task 2: Ball Contact And Status

**Files:**
- Modify: `/Users/teetienyaw/bm8-penalty-unity/Assets/Scripts/Bm8PenaltyPrototype.cs`

- [ ] Change saved status text to `SAVED - glove punches ball out`.
- [ ] Make `FlyBall()` pause briefly at the palm point, then punch the ball outward and downfield.
- [ ] Keep goal shots using the normal goal trajectory.

### Task 3: Verify

**Files:**
- Read: `/Users/teetienyaw/bm8-penalty-unity/Logs`

- [ ] Run Unity batchmode compile/rebuild.
- [ ] Inspect the log for compile errors.
- [ ] If Unity is locked by an open editor session, report that clearly and provide the exact next action.
