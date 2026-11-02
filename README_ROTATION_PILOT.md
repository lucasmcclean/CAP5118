# CAP 5118: rotation pilot review

Rohan Gudla's Project 4 rotation work for the Rohan/Alix subgroup. Use the branch `rohan/project4-rotation-pilot` when reviewing this version. It is based on the team's industrial scene, not the earlier independent courtyard.

## Open and run

1. Clone this branch or switch an existing clean checkout to it. Keep other subgroups' work in their own checkouts while reviewing.
2. Open the repository root in Unity **6000.5.6f1** through Unity Hub and allow package import to finish.
3. Open **`Assets/Scenes/Rotation.unity`**. On Windows, connect Quest 3 through Quest Link and start Play. For the standalone Quest build, open **CAP5118 Rotation Pilot** on the headset.
4. On Ready, press A, use either thumbstick to select keypad keys, and press A to enter the assigned participant ID. Select OK. The PC mirror also offers an optional researcher entry panel.
5. Read the headset instructions, then press A. Before each turn, follow the left/right cue and press A when ready. Turn physically in place in the indicated direction.
6. Stop turning when the question appears. Select a rating with an up/down stick flick and press A. Continue until Thank You.

The revised working protocol has three practice trials and 72 measured trials: nine gain levels from 0.6 to 1.4, each repeated four times per direction. Directions alternate throughout the session, starting with left; the first measured trial is therefore right. Participant-facing screens do not disclose the counts. The vertical rating UI uses -3 Uncomfortably smaller, -2 Comfortably small, -1 Maybe small, +1 Maybe large, +2 Comfortably large, and +3 Uncomfortably larger. There is no zero option and the participant must select a rating before confirming. Earlier sessions used different scales or trial counts and must remain separate historical records. This is a working pilot for advisor review, not a claim of final study-protocol approval.

## Included functionality

- Headset participant-ID keypad adapted from Christian/Logan's team contribution, plus optional PC entry.
- A compact left/right sign anchored in the industrial scene before each rotation, vertical either-stick selection and A confirmation. The environment stays visible and the view remains continuous when the turn starts.
- The scene's scale-reference test cube has been removed.
- A compact direction reminder appears during a clear wrong-way turn and clears when the participant turns in the instructed direction. It does not reset the trial or change its gain.
- Shared tracking-parent correction for head and controllers; raw tracked anchors are not overwritten.
- Balanced gain selection for each direction, recorded direction events, and per-trial gain initialization.
- Unique per-session participant ID/configuration, results, motion, event and completion-status files. Existing runs are not overwritten.
- Clearly separated synthetic QA, independent recorder tests, and local validation/export scripts.

## Standalone Quest build

Install Android Build Support, OpenJDK, and compatible Android SDK/NDK tools for Unity 6000.5.6f1. On macOS, build the rotation scene from Terminal:

```bash
'/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity' \
  -batchmode -quit -projectPath '/path/to/CAP5118' -buildTarget Android \
  -executeMethod LocalQuestRotationBuild.Build
```

The APK is written to `Builds/Quest/CAP5118-Rotation-Pilot.apk` with application ID `com.rohangudla.cap5118.rotationpilot`. The build method selects only the rotation scene and restores the previous product name and application ID afterward. Deploy to an authorized Quest with `adb install -r Builds/Quest/CAP5118-Rotation-Pilot.apk`. Existing sessions remain in the app's data folder; copy them before updating.

## Data and export

`Evaluation.SessionDirectory` and the Unity Console identify the output folder under `Application.persistentDataPath/CAP5118_PilotSessions/PILOT/ET_ROTATION/`. Preserve all six files together: `session.json`, `session_status.json`, `results.txt`, `motion.tsv`, `events.tsv`, and `data.txt`.

For the installed standalone Quest build on this Mac, reconnect the Quest by USB and run `Tools/RotationPilot/Copy-QuestSessions.sh` from the repository. It copies all sessions to a unique folder in `PilotExports/` without removing the originals. Keep test and participant sessions separate.

To validate a copied session on macOS, run `python3 Tools/RotationPilot/Test-PilotSession.py 'path-to-session'`. The validator checks the recorded scale, trial counts, alternating direction events, gain/direction balance and the signed motion samples. Synthetic QA runs require the `--allow-synthetic` flag and are not participant data.

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

Historical verification: the October 24 scene QA and physical Quest Link pilot covered the earlier five-practice, eighteen-measured protocol. They do not verify the revised protocol. New synthetic QA must exercise the revised scene, and a physical Quest run is needed to verify its headset presentation and operation. See `docs/INTEGRATION_REVIEW.md` for scope and limits.

November 2 verification of the revised scene passed 1,644 assertions and 1,760 synthetic pose checks across three practice and 72 measured trials. The standalone Android APK built and installed successfully. Rohan confirmed the in-scene direction cues, cube removal, comfort labels and wrong-direction reminder on Quest 3. This confirms the tested presentation and controls; a complete human session under the revised protocol remains separate evidence.

## Review materials

- `docs/ROTATION_FACILITATOR_SCRIPT.md`: spoken administration procedure.
- `docs/PILOT_PROCEDURE.md`: controls, file format and pilot checks.
- `docs/INTEGRATION_REVIEW.md`: changes, evidence, shared-branch integration and outstanding protocol decisions.
- `docs/RECORDING_PLAN.md`: group video requirements and an optional recording outline.

The advisor demonstration, final shared-code integration, other members' datasets and video submission remain coordination tasks. This source publication does not submit an assignment.
