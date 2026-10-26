# CAP 5118: rotation pilot review

Rohan Gudla's Project 4 rotation work for the Rohan/Alix subgroup. Use the branch `rohan/project4-rotation-pilot` when reviewing this version. It is based on the team's industrial scene, not the earlier independent courtyard.

## Open and run

1. Clone this branch or switch an existing clean checkout to it. Keep other subgroups' work in their own checkouts while reviewing.
2. Open the repository root in Unity **6000.5.6f1** through Unity Hub and allow package import to finish.
3. Open **`Assets/Scenes/Rotation.unity`**. Connect Quest 3, enter Quest Link, and start Play.
4. On Ready, press A, use either thumbstick to select keypad keys, and press A to enter the assigned participant ID. Select OK. The PC mirror also offers an optional researcher entry panel.
5. Read the headset instructions, release A and press it again to start. Turn physically in place; the joystick selects answers rather than moving the participant.
6. Stop turning when the question appears. Select a rating and press A. Complete five practice and eighteen measured trials, then stop at Thank You.

The gain grid is 0.6-1.4 in nine conditions, each repeated twice. The rating UI uses seven labeled options from -3 through +3, including 0 for same speed, matching the scale already implemented in Christian/Logan's translation repository and the shared team repository. Keep that team convention for the rotation demonstration. This is a working pilot for the normal advisor review, not a claim of final study-protocol approval.

## Included functionality

- Headset participant-ID keypad adapted from Christian/Logan's team contribution, plus optional PC entry.
- Explicit rotation instructions, practice/measured-trial progress, either-stick selection and A confirmation.
- Shared tracking-parent correction for head and controllers; raw tracked anchors are not overwritten.
- Corrected random condition selection and per-trial gain initialization.
- Unique per-session participant ID/configuration, results, motion, event and completion-status files. Existing runs are not overwritten.
- Clearly separated synthetic QA, independent recorder tests, and local validation/export scripts.

## Data and export

`Evaluation.SessionDirectory` and the Unity Console identify the output folder under `Application.persistentDataPath/CAP5118_PilotSessions/PILOT/ET_ROTATION/`. Preserve all six files together: `session.json`, `session_status.json`, `results.txt`, `motion.tsv`, `events.tsv`, and `data.txt`.

From PowerShell at the repository root:

```powershell
& ./Tools/RotationPilot/Test-PilotSession.ps1 -SessionPath 'path-to-session'
& ./Tools/RotationPilot/Prepare-PilotExport.ps1 -SessionPath 'path-to-session'
```

The export script creates a local ZIP in the ignored `PilotExports/` directory. It can read flushed streams while Unity remains at Thank You. It does not upload or send anything. Personal datasets and incomplete trial logs are not included in this source branch.

## Verification

After Unity imports the project, compile runtime/Editor scripts and run the recorder tests:

```powershell
& ./Tools/RotationPilot/Check-Software.ps1
```

If Unity is installed elsewhere, pass `-UnityEditorPath` with the full `Unity.exe` path. Generated outputs go under ignored `QA/`. To exercise the actual scene with synthetic poses, save the scene, stop Play, and choose **Tools > Rotation Pilot > Run software QA (synthetic only)**. Synthetic results must not be supplied as participant data.

Recorded verification: October 24 scene QA passed 157 assertions and 451 synthetic pose checks. Rohan also completed the physical Quest 3 Link pilot: five practice and eighteen measured trials, with complete saved output validated separately. Runtime/Editor compilation and ten recorder tests are checked again before this branch is published. See `docs/INTEGRATION_REVIEW.md` for scope and limits.

## Review materials

- `docs/ROTATION_FACILITATOR_SCRIPT.md`: spoken administration procedure.
- `docs/PILOT_PROCEDURE.md`: controls, file format and pilot checks.
- `docs/INTEGRATION_REVIEW.md`: changes, evidence, shared-branch integration and outstanding protocol decisions.
- `docs/RECORDING_PLAN.md`: group video requirements and an optional recording outline.

The advisor demonstration, final shared-code integration, other members' datasets and video submission remain coordination tasks. This source publication does not submit an assignment.
