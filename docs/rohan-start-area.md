# Starting-area revision for team review

Scene: `Assets/Scenes/RedirectionExperiment.unity`.

This revision addresses the 10 m clear radius and level-ground concerns discussed by the team. It is a proposed fix to test, not a claim that the whole assignment is complete.

## Changes

- Move the entire `RedirectedWalkingExperiment` parent from `(37.1, 1.9, -51.2)` to `(56.5, 2.094002, -50.5)`. Moving the parent keeps the rig, path, and instruction elements together.
- Add `Map/LevelGround`, a static planar asphalt mesh and collider of radius 10.5 m, over the road/tile height transitions at the new start. It reuses the team's existing `Seamless_asphalt_v1` material. Its edge is outside the required 10 m study circle.
- Keep all original buildings, fences, containers, lighting, audio settings, and instructor scripts unchanged. No extra indicators or gameplay controls were added.
- Preserve `Evaluation.EXP_TYPE = ET_ROTATION`.

## Local checks performed October 10, 2026

- Nearest non-ground renderer bounds: approximately **10.57 m** from the revised start, versus approximately 6.52 m before. Bounds distances are conservative approximations, not exact triangle distances.
- A 0.5 m grid plus one-degree perimeter sampling over a **10.25 m radius** produced **1,665 ground hits**, with no missing samples or height mismatches and zero-degree sampled slopes. The new floor mesh itself is planar.
- Reopened the saved scene and repeated the ground/clearance check successfully.
- Scene integrity: 861 enabled renderers; no missing scripts, null material slots, or unsupported material slots.
- Unity Play-mode smoke test displayed the revised start and Ready screen; ambient audio was playing and looping. No trial was started.
- Headset tracking was inactive during that smoke test. The team's earlier Quest test was on the original start, so a fresh headset check is still needed.

## Please check before merging or recording

1. Check out the review branch with your own working changes safely saved. Open `RedirectionExperiment`.
2. Check the asphalt appearance and the open area around the start from several directions.
3. With Quest Link, stay stationary first and confirm the environment is 3D, head tracking is natural, floor height looks right, and ambient sound is audible.
4. Verify controller/instruction/trial progression and the experiment modes relevant to your pair. This revision does not change their logic.
5. Report anything offset, obstructed, visually distracting, or otherwise incorrect. Use a cleared physical play area and respect the headset boundary; the virtual 10 m radius does not authorize walking beyond the real safe area.

The final shared video and each person's individual CITI/submission steps remain separate tasks.

Preparation used AI assistance for the geometry fix and validation tooling. The instructor experiment logic was not modified. Local diagnostic scripts, generated Unity settings, caches, and machine-specific logs are not part of this commit.
