# Rotation pilot: facilitator script

Version: October 24, 2026. Operators: Rohan Gudla and Alix Fyall. This is the team's working pilot procedure, ready to use for finding bugs. It is not described as an advisor-confirmed final study protocol.

## 1. Prepare the computer

1. Open this Project 4 checkout and `Assets/Scenes/Rotation.unity` in Unity 6000.5.6f1.
2. Connect Quest 3, enter Quest Link and start Play. Confirm the industrial environment is visible in Oculus Mirror, head movement updates, and ambient audio is playing.
3. Use the existing rotation settings: gains 0.6 to 1.4 in 0.1 increments, two measured repetitions per gain, five practice trials, nominal 90-degree virtual turns with a randomized 67.5-to-90-degree stopping threshold. These are the settings in this team checkout. The TA's tentative weaker/baseline/stronger example is not substituted for them.
4. Use the participant ID assigned by the experimenter and keep it consistent with any questionnaire. For a local engineering pilot, agree on a unique pilot code and record the mapping separately. Do not assume that a UCF ID or an example code is the assigned study PID.
5. Confirm that the floor area used for standing and turning is clear. This rotation task does not require walking along the curvature group's seven-metre path.

## 2. Explain the task

Read this aloud or let the participant read the identical headset instructions:

"Stand at your starting location. On each trial, slowly turn your body in place. You may turn in either direction; keep your feet near the starting point. Do not walk or use the joystick to move. When the rating screen appears, stop turning.

"Compare how fast your viewpoint turned in VR with how fast you physically turned. Choose -3 for much slower, -2 for slower, -1 for slightly slower, 0 for the same speed, +1 for slightly faster, +2 for faster, or +3 for much faster. Flick either thumbstick left or right to select your answer, then press A once. Answer from your own impression; you do not need to work out the software gain.

"There are five practice trials followed by eighteen recorded trials. The screen identifies practice and recorded trials. You can stop between trials. If you feel uncomfortable or anything stops working, stop turning and tell the operator."

The seven choices adapt Christian's October 23 5:57 PM proposal and Lucas's 5:58 PM response to rotation. Metadata records this source and keeps `scaleInterpretationConfirmed: false`; the team can revise the final study wording after reviewing the pilot.

## 3. Start and administer

1. Participant presses A on Ready, enters the agreed PID using the keypad, then selects OK with A. B on Ready remains a compatibility shortcut to the same keypad.
2. The rotation instructions stay visible until A is released and pressed again. Read them before starting. The old slide timer and skip flag do not bypass this screen.
3. After A, the environment appears. Turn slowly in place until the rating screen appears, stop, select an answer, and press A once.
4. Repeat through all five practice and eighteen recorded trials. The next trial begins after the previous answer; remain in place. The joystick changes answers only.
5. Do not reveal the active gain or tell the participant what to choose. If there is a display/input problem, record the trial and symptom. Stop Play if the run must end; the recorder preserves it as incomplete.
6. Finish at Thank You. Leave Unity running until the operator verifies the output.

## 4. Verify the dataset

1. Read `Evaluation.SessionDirectory` in the Inspector or the `Pilot session:` line in the Console.
2. Run the local `Test-PilotSession.ps1 -SessionPath <folder>` check. It must report eighteen rows, nine conditions, two repetitions of each gain, complete status, and `Synthetic: false`.
3. Preserve `session.json`, `session_status.json`, `results.txt`, `motion.tsv`, `events.tsv` and `data.txt` together. `ProtocolConfirmed: false` records the working-protocol status; it does not mean the files are incomplete or the inputs were simulated.
4. Review head/controller behavior and list any actual symptoms and fixes. Do not replace an interrupted run or subjective responses with generated values.
5. Prepare the entire session folder for the group/advisor, with the PID and protocol version. Sending, publishing and assignment submission are separate steps and are not performed by the local tools.

## 5. Reset for another pilot

Stop Play after verification. Start a new run, use the next participant's PID, and repeat the same instructions. Each run receives a unique timestamped folder; earlier results remain intact.
