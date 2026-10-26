# Rotation pilot administration procedure

Purpose: verify the working experiment and data capture before the evaluation phase. This is not the later full participant study. Rohan Gudla and Alix Fyall are the rotation subgroup. The group must collect a pilot dataset from each of its six members and send the datasets to its advisor.

## Before collecting a pilot dataset

1. Agree on the exact project commit and `Assets/Scenes/Rotation.unity`. Integrate the group's approved changes; do not use an older scene simply because it already opens on one machine.
2. Use the documented working rotation scale: -3 much slower, -2 slower, -1 slightly slower, 0 same speed, +1 slightly faster, +2 faster, +3 much faster, comparing virtual turning with physical turning. This adapts the seven-choice proposal posted by Christian and discussed with Lucas on October 23. The headset now displays the full question and labeled choices instead of the inherited binary image.
3. Leave `ScaleInterpretationConfirmed` false unless final confirmation is actually obtained. A working pilot can run with explicitly recorded instructions; its metadata says `working_pilot_protocol_for_review`. No advisor-approval claim is required to find bugs during the pilot.
4. Use Quest 3 with the established safe boundary and a clear area. Rotations use natural physical turning; no joystick locomotion is needed. Set ambient audio to a comfortable level and check head/controller alignment.
5. Use the **same participant ID as the team's Qualtrics questionnaire**. Christian/Logan's integrated headset keypad accepts up to eight digits and preserves leading zeroes. On Ready, press A (legacy B also works), flick either stick to select keypad keys, press A to enter digits, then select OK. Del removes the last digit. The optional PC mirror entry panel supplies the same recorder ID through `TrySetParticipantId`.
6. This working pilot retains nine gain levels from 0.6 to 1.4, two measured repetitions, five practice trials and a nominal virtual angle of 90 degrees; the stop angle is randomized from 67.5 to 90 virtual degrees. The TA's tentative weaker/baseline/stronger numerical example is not silently substituted for this grid. Record any later change as a new protocol version and update the instructions, QA expectations and data validator together.

The complete spoken procedure is in `ROTATION_FACILITATOR_SCRIPT.md`. Rotations use standing turns; the curvature subgroup's seven-metre walking task does not apply. Use the actual team questionnaire workflow. The group identified the SSQ references in the old curvature draft as leftovers to remove.

## Read to the pilot participant

"Remain at your starting location and slowly turn your body in place. Stop when the question appears. Compare virtual turning with your physical turning: -3 means much slower, -2 slower, -1 slightly slower, 0 the same speed, +1 slightly faster, +2 faster, and +3 much faster. Flick either thumbstick left/right to select your answer, then press A once. There are five practice trials followed by eighteen recorded trials. Tell me if the display, controls, instructions or audio behave unexpectedly."

Do not reveal the active gain or suggest an answer. The working instructions are complete for this pilot and are recorded with its data for team review.

## Run the pilot

1. Participant stands comfortably at the start. Researcher confirms the entered ID and correct condition.
2. Participant presses **A** on Ready, enters the questionnaire PID, then selects **OK**. Confirm that the Console reports a new `Pilot session:` folder without `SYNTHETIC SOFTWARE TEST`. Opening the keypad alone does not create a dataset. If the setup is incomplete, the keypad displays the error and stays open.
3. Read the rotation instructions in the headset, then release and press A to begin. These instructions wait for A, regardless of the inherited skip flag or slide timer. The PC operator can also enter the PID and choose **Set ID and show headset instructions**.
4. Complete all **five practice trials**. Each active trial ends when the virtual turn threshold is reached. Select a response with left/right flicks and confirm with **A**. The thumbstick is response selection, not movement.
5. Complete **eighteen measured trials** (nine gain conditions, two repetitions each). Do not restart, alter gains, skip responses or replace judgments with synthetic values.
6. Watch for controllers drifting away, a question panel leaving the view, stuck transitions, unreadable text or missing audio. Note any problem with its approximate trial/time.
7. Stop at the Thank You screen. Leave the session intact long enough to verify the files. If interrupted, preserve that run as incomplete; repeat as a new session after fixing the cause.

## Verify and export

The desktop path is printed in the Unity Console and exposed as `Evaluation.SessionDirectory`. It is under `Application.persistentDataPath/CAP5118_PilotSessions/PILOT/ET_ROTATION/<ID_timestamp_unique>/`. Android uses the app's own `Application.persistentDataPath`; preserve the entire session folder when exporting from Quest.

Each folder should have:

- `session.json`: ID, condition, configuration, scale interpretation and `softwareSimulation: false`.
- `results.txt`: exactly 18 measured rows, tab-separated `gain`, `gain2`, legacy reaction and integer response. Practice responses are excluded.
- `motion.tsv`: trial, time, real/virtual position and real/virtual yaw. Yaw is in degrees; positions are metres.
- `events.tsv`: UTC time, trial index and state transitions. Negative trial indices denote practice.
- `data.txt`: legacy motion/state export retained for compatibility.
- `session_status.json`: 18 expected, 18 completed and `complete: true`.

Run `Tools/RotationPilot/Test-PilotSession.ps1 -SessionPath <the session directory>` from this repository. It rejects synthetic runs as participant pilots. Check that each of the nine gain conditions occurs twice. `Tools/RotationPilot/Prepare-PilotExport.ps1` creates a validated local ZIP without sending it. Archive the whole folder with its anonymous code and configuration.

`ActualPilotComplete` describes complete human input and files. `ProtocolConfirmed` separately describes whether the final study wording has been confirmed. A complete working-protocol pilot is preserved for review without claiming final study approval.

The researcher records observed bugs and fixes, plus whether the repeated run passed. A compile check, simulated run or previous Project 3 scene demonstration is not a replacement for this pilot. Coordinate delivery of each member's genuine dataset with the advisor; nothing in the local tools sends it automatically.
