# Rotation contribution: integration review

This branch contains Rohan Gudla's rotation pilot implementation, based on the team's commit `2a27fa6` (`feat: random rotation, scale input`). It is a separate rotation review branch, not a replacement for the translation or curvature subgroups' latest work. The complete setup and review entry point is `README_ROTATION_PILOT.md`.

## Changes

- `Evaluation.cs`: includes every remaining condition in random selection; validates participant IDs; records configuration and confirmed responses; preserves partial runs; marks completion only after the configured number of measured responses.
- `RedirectionController.cs`: applies one rigid transformation to the shared tracking-space parent so head and controller anchors remain aligned; avoids world-yaw feedback; keeps questions head-locked; initializes each rotation trial without carrying over the prior gain for a frame.
- `ButtonHandler.cs`: accepts either thumbstick, uses A for confirmation, supports standard OpenXR input alongside OVRInput, and processes selection before same-frame confirmation.
- Christian/Logan's keypad design from `chrisgw4/CAP5118` at `e85fe08` is adapted to this recorder. It provides twelve headset keys, up to eight digits, leading-zero preservation and deletion. `ES_PID` is appended to preserve the team's serialized enum values.
- `PilotParticipantEntry.cs` provides optional PC-side ID entry; `PilotSessionRecorder.cs` writes separate session folders and invariant numeric data.
- The already-installed Standalone OpenXR Touch Plus controller profile is enabled. Other local Unity project migrations are excluded.
- Editor software QA and recorder tests are included; generated outputs and personal pilot datasets are excluded.
- November 2 presentation update: remove the rotation scene's `ScaleReference` cube and replace the large direction panel with a compact sign anchored in the environment. Rotation cues keep the camera at tracked eye height and preserve its position and heading into the active turn. Each trial's gain applies to new physical rotation from that continuous starting view. The legacy instruction repositioning for other experiment types is unchanged. New session metadata records this presentation revision.

## Verification

- November 2 revised scene QA passed 1,644 assertions and 1,760 synthetic pose checks, including all three practice and 72 measured trials, four repetitions per gain per direction, vertical comfort responses, continuous camera pose and wrong-direction reminder behavior. The exported synthetic session passed the Python validator with `--allow-synthetic`.
- The standalone Quest APK built and installed successfully. Rohan confirmed the direction sign within the industrial environment, removal of the scale cube, the comfort labels, and the reminder appearing and clearing on a wrong-way test turn. A full revised human session was not established by these presentation checks.

- October 24 Unity scene software QA: 157 assertions and 451 synthetic pose checks, covering five practice and eighteen measured trials. This is software evidence, not human participation.
- October 24 physical Quest 3 through Link: Rohan completed the five practice and eighteen measured rotation trials, reaching Thank You. The saved session passed the physical-session validator with nine gain conditions repeated twice, complete status, and no synthetic override. The original dataset and its export remain local.
- Before publication, runtime and Editor compilation and the ten standalone recorder checks are rerun. `Tools/RotationPilot/Check-Software.ps1` reproduces these compile/recorder checks after Unity has imported the project.

## Shared response scale and protocol status

On October 29, Rohan requested that rotation use six choices: -3, -2, -1, +1, +2 and +3, with no zero. This changes the earlier seven-choice rotation pilot and differs from the seven-value implementation observed in Christian/Logan's translation repository and the shared repository on October 26. Session metadata identifies the scale actually used and continues to mark the wording as a working pilot protocol, with `scaleInterpretationConfirmed` false. The normal project demonstration and advisor review remain required.

The current local revision retains the nine gain levels from 0.6 to 1.4 but changes the schedule to three practice trials and 72 measured turns, four per direction per gain. Cues alternate left/right starting with left, and the revised scale uses the comfort labels requested by Rohan. The earlier five-practice/eighteen-measured pilot remains historical evidence for its original protocol; do not reinterpret its responses as the revised protocol.

## Integration and remaining work

The shared repository's main branch received Anthony's curvature changes in `ac8a4f9` and `4d8251b` after this rotation version was tested. Review shared-script conflicts deliberately before integrating; do not overwrite those changes with complete file copies. This branch can be checked out independently to demonstrate the tested rotation version.

Coordinate the merged version with Alix and the other subgroups, demonstrate it to the advisor, and apply feedback. The team still coordinates one genuine dataset per member and the shared video. Publishing this code does not establish advisor approval, completion of other members' pilots, or submission of the assignment.
