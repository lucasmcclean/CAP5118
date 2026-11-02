# Rotation pilot: facilitator script

Version: October 29, 2026. Operators: Rohan Gudla and Alix Fyall. This is a revised working pilot procedure. It is not described as an advisor-confirmed final study protocol.

## 1. Prepare the computer

1. Open this Project 4 checkout and `Assets/Scenes/Rotation.unity` in Unity 6000.5.6f1.
2. Start CAP5118 Rotation Pilot on the Quest 3, or use Quest Link on Windows. Confirm the industrial environment is visible, head movement updates, and ambient audio is playing.
3. The revised settings are nine gains from 0.6 to 1.4, three practice trials, and four measured turns per gain in each direction (72 measured). Directions alternate continuously, starting left. The nominal virtual turn is 90 degrees, with a randomized 67.5-to-90-degree stopping threshold. Do not disclose the total number of trials to participants.
4. Use the participant ID assigned by the experimenter and keep it consistent with any questionnaire. For a local engineering pilot, agree on a unique pilot code and record the mapping separately. Do not assume that a UCF ID or an example code is the assigned study PID.
5. Confirm that the floor area used for standing and turning is clear. This rotation task does not require walking along the curvature group's seven-metre path.

## 2. Explain the task

Read this aloud or let the participant read the identical headset instructions:

"Stand at your starting location. Follow the left or right cue, press A, and slowly turn your body in place in that direction. Keep your feet near the starting point; do not walk or use the joystick to move. Stop turning when the rating screen appears.

"Choose the description that best matches how the amount of rotation felt. Flick either thumbstick up or down to select your answer, then press A once. You can pause before each turn. If you feel uncomfortable or anything stops working, stop turning and tell the operator."

The revised scale and direction schedule were requested by Rohan on October 29. Metadata records them and keeps `scaleInterpretationConfirmed: false`; earlier sessions remain separate historical records.

## 3. Start and administer

1. Participant presses A on Ready, enters the agreed PID using the keypad, then selects OK with A. B on Ready remains a compatibility shortcut to the same keypad.
2. The rotation instructions stay visible until A is released and pressed again. Read them before starting. The old slide timer and skip flag do not bypass this screen.
3. A small sign in the industrial environment shows LEFT or RIGHT. Press A when ready, then turn slowly in that direction until the rating screen appears. Stop, select an answer vertically, and press A once.
4. Repeat until Thank You. The direction changes after each response, and the next turn waits for A. The joystick changes answers only.
5. Do not reveal the active gain or tell the participant what to choose. If there is a display/input problem, record the trial and symptom. Stop Play if the run must end; the recorder preserves it as incomplete.
6. Finish at Thank You. Leave Unity running until the operator verifies the output.

## 4. Verify the dataset

1. On standalone Quest, reconnect USB and run `Tools/RotationPilot/Copy-QuestSessions.sh`. On a PC/Link run, read `Evaluation.SessionDirectory` in the Inspector or the `Pilot session:` line in the Console.
2. On macOS, run `python3 Tools/RotationPilot/Test-PilotSession.py <folder>`; on Windows, run `Test-PilotSession.ps1 -SessionPath <folder>`. The result must show 72 rows, nine conditions, four repetitions of each gain per direction, complete status, and `synthetic: false`.
3. Preserve `session.json`, `session_status.json`, `results.txt`, `motion.tsv`, `events.tsv` and `data.txt` together. `ProtocolConfirmed: false` records the working-protocol status; it does not mean the files are incomplete or the inputs were simulated.
4. Review head/controller behavior and list any actual symptoms and fixes. Do not replace an interrupted run or subjective responses with generated values.
5. Prepare the entire session folder for the group/advisor, with the PID and protocol version. Sending, publishing and assignment submission are separate steps and are not performed by the local tools.

## 5. Reset for another pilot

Stop Play after verification. Start a new run, use the next participant's PID, and repeat the same instructions. Each run receives a unique timestamped folder; earlier results remain intact.
