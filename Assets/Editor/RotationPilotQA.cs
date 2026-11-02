using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exercises the actual scene/scripts with synthetic poses. Never participant data.
public static class RotationPilotQA
{
    [Serializable] private class Report
    {
        public string timestampUtc, scene, outputDirectory, error;
        public bool passed, physicalQuestVerified = false, syntheticSoftwareTest = true;
        public int measuredTrials, practiceTrials, poseChecks;
        public List<string> checks = new List<string>();
    }
    private static Report report;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool value, string description)
    {
        if (!value) throw new Exception("QA FAILED: " + description);
        report.checks.Add(description);
    }
    private static void Call(object target, string method)
    {
        target.GetType().GetMethod(method, Private).Invoke(target, null);
    }
    private static void SetField(object target, string field, object value)
    {
        target.GetType().GetField(field, Private).SetValue(target, value);
    }

    [MenuItem("Tools/Rotation Pilot/Run software QA (synthetic only) %#F8")]
    public static void RunAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before running QA.");
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save your scene before QA; it will not discard unsaved work.");
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(project, "QA", "Unity_" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
        Directory.CreateDirectory(output);
        report = new Report { timestampUtc = DateTime.UtcNow.ToString("o"),
            scene = "Assets/Scenes/Rotation.unity", outputDirectory = output };
        var previousRandom = UnityEngine.Random.state;
        try
        {
            EditorSceneManager.OpenScene(report.scene);
            Evaluation evaluation = UnityEngine.Object.FindAnyObjectByType<Evaluation>();
            RedirectionController controller = UnityEngine.Object.FindAnyObjectByType<RedirectionController>();
            ButtonHandler buttons = UnityEngine.Object.FindAnyObjectByType<ButtonHandler>();
            Check(evaluation != null && controller != null && buttons != null, "Rotation scene has all experiment components");
            Check(evaluation.EXP_TYPE == ExperimentType.ET_ROTATION, "Rotation scene selects ET_ROTATION");
            Check(!SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Any(t => t.name == "ScaleReference"),
                "Rotation scene contains no scale-reference test cube");
            Check(evaluation.ResponseOptionCount == 6 && Enumerable.Range(0, 6).Select(evaluation.ResponseOptionAt)
                .SequenceEqual(new[] { -3, -2, -1, 1, 2, 3 }), "Rotation displays the six requested options");
            Check(evaluation.STEPS == 9 && evaluation.STEPS2 == 1 && evaluation.TRIALS == 8 && evaluation.TEST_TRIALS == 3,
                "Scene specifies 3 practice trials and 72 measured trials");
            OVRCameraRig rig = controller.cameraRig.GetComponent<OVRCameraRig>();
            Check(rig != null && rig.trackingSpace != null && rig.centerEyeAnchor != null,
                "Camera rig and tracking-space references resolve");
            Check(rig.leftHandAnchor.IsChildOf(rig.trackingSpace) && rig.rightHandAnchor.IsChildOf(rig.trackingSpace),
                "Both controllers share the corrected tracking-space parent");
            var leftHelper = rig.leftHandAnchor.GetComponentsInChildren<OVRControllerHelper>(true)
                .Single(h => h.m_controller == OVRInput.Controller.LTouch);
            var rightHelper = rig.rightHandAnchor.GetComponentsInChildren<OVRControllerHelper>(true)
                .Single(h => h.m_controller == OVRInput.Controller.RTouch);
            Check(leftHelper.m_modelMetaTouchPlusLeftController != null &&
                leftHelper.m_modelMetaTouchPlusLeftController.transform.IsChildOf(leftHelper.transform),
                "Quest 3 left controller model belongs to the left tracking anchor");
            Check(rightHelper.m_modelMetaTouchPlusRightController != null &&
                rightHelper.m_modelMetaTouchPlusRightController.transform.IsChildOf(rightHelper.transform),
                "Quest 3 right controller model belongs to the right tracking anchor");
            Call(evaluation, "Start");
            Call(controller, "Start");
            report.practiceTrials = evaluation.PracticeTrialCount;

            var labels = (UnityEngine.UI.Text[])controller.GetType().GetField("questionLabels_", Private).GetValue(controller);
            string[] expectedLabels = { "-3   Uncomfortably smaller", "-2   Comfortably small",
                "-1   Maybe small", "+1   Maybe large", "+2   Comfortably large", "+3   Uncomfortably larger" };
            Check(labels.Select(label => label.text).SequenceEqual(expectedLabels), "Six requested labels are displayed");
            Check(labels.Zip(labels.Skip(1), (a, b) =>
                ((RectTransform)a.transform.parent).anchoredPosition.y > ((RectTransform)b.transform.parent).anchoredPosition.y).All(v => v),
                "Response options are arranged from top to bottom");

            // The old exclusive upper bound made the last available condition ineligible.
            bool lastEligible = false;
            for (int seed = 0; seed < 100; seed++)
            {
                SetField(evaluation, "leftRotationTrials", new int[evaluation.STEPS]);
                SetField(evaluation, "random", new System.Random(seed));
                Call(evaluation, "SetNextStep");
                if (evaluation.ActiveTrial.Step == evaluation.STEPS - 1) lastEligible = true;
            }
            Check(lastEligible, "Last condition can be selected before others are exhausted");
            SetField(evaluation, "leftRotationTrials", new int[evaluation.STEPS]);
            SetField(evaluation, "rightRotationTrials", new int[evaluation.STEPS]);
            SetField(evaluation, "random", new System.Random(20261023));
            evaluation.ActiveTrial.Step = -1;
            evaluation.ParticipantId = "";
            Call(buttons, "OnBPressed");
            Check(evaluation.State == ExperimentState.ES_PID && string.IsNullOrEmpty(evaluation.SessionDirectory),
                "Ready opens the team PID keypad without starting an output session");
            FeedPose(rig, controller, evaluation, 0f);
            var keypad = (GameObject)controller.GetType().GetField("pidUI_", Private).GetValue(controller);
            var keys = (UnityEngine.UI.Image[])controller.GetType().GetField("pidKeys_", Private).GetValue(controller);
            Check(keypad.activeSelf && keys.Length == 12, "Headset PID canvas displays all twelve keypad keys");
            evaluation.PidSelectedKey = 11;
            Call(buttons, "OnAPressed");
            Check(evaluation.State == ExperimentState.ES_PID && string.IsNullOrEmpty(evaluation.SessionDirectory),
                "Empty PID cannot start a session");
            evaluation.PidSelectedKey = 0;
            buttons.ProcessButtonState(true, false, false);
            buttons.ProcessButtonState(true, false, false);
            Check(evaluation.ParticipantId == "1", "Holding A enters one PID digit only");
            buttons.ProcessButtonState(false, false, false);
            evaluation.ParticipantId = "";
            evaluation.PidSelectedKey = 10;
            Call(buttons, "OnAPressed");
            evaluation.PidSelectedKey = 1;
            Call(buttons, "OnAPressed");
            Check(evaluation.ParticipantId == "02", "PID entry preserves leading zeroes");
            evaluation.PidSelectedKey = 9;
            Call(buttons, "OnAPressed");
            Check(evaluation.ParticipantId == "0", "PID Delete removes the final digit");
            evaluation.PidSelectedKey = 0;
            for (int digit = 0; digit < 12; digit++) Call(buttons, "OnAPressed");
            Check(evaluation.ParticipantId.Length == Evaluation.PID_MAX_LENGTH,
                "Headset PID keypad enforces the team's eight-digit limit");
            evaluation.MovePidSelection(-99, -99);
            Check(evaluation.PidSelectedKey == 0, "PID navigation clamps at the upper left");
            evaluation.MovePidSelection(99, 99);
            Check(evaluation.PidSelectedKey == 11, "PID navigation clamps at the lower right");
            evaluation.PidSelectedKey = 4;
            buttons.ProcessThumbstick(new Vector2(1, 0));
            buttons.ProcessThumbstick(new Vector2(1, 0));
            Check(evaluation.PidSelectedKey == 5, "Held stick moves one PID key per flick");
            buttons.ProcessThumbstick(Vector2.zero);
            buttons.ProcessThumbstick(new Vector2(0, 1));
            Check(evaluation.PidSelectedKey == 2, "Up stick moves to the previous keypad row");
            buttons.ProcessThumbstick(Vector2.zero);
            buttons.ProcessThumbstick(new Vector2(0, -1));
            Check(evaluation.PidSelectedKey == 5, "Down stick moves to the next keypad row");
            buttons.ProcessThumbstick(Vector2.zero);
            Check(!evaluation.TrySetParticipantId("../invalid"), "Invalid participant ID is rejected");
            evaluation.TrySetParticipantId("SIM_ROTATION_QA");
            evaluation.ScaleInterpretationConfirmed = false;
            evaluation.ScaleInterpretation = "";
            evaluation.PidSelectedKey = 11;
            Call(buttons, "OnAPressed");
            Check(evaluation.State == ExperimentState.ES_PID,
                "Missing response instructions keep the session at the keypad");
            evaluation.ScaleInterpretation = Evaluation.WorkingRotationScale;
            evaluation.ConfigureSoftwareSimulation(output);
            buttons.ProcessButtonState(true, false, false);
            Check(evaluation.SessionDirectory.Contains("SYNTHETIC_SOFTWARE_TESTS"), "Simulation writes only to synthetic output");
            Check(evaluation.State == ExperimentState.ES_SLIDES, "PID OK starts the configured session");
            buttons.ProcessButtonState(true, false, false);
            Check(evaluation.State == ExperimentState.ES_SLIDES, "Holding keypad OK does not skip the rotation instructions");
            evaluation.StartTime = (Time.realtimeSinceStartup - 1000f) * 1000f;
            FeedPose(rig, controller, evaluation, 0f);
            Check(!keypad.activeSelf, "PID canvas hides after the session starts");
            var intro = (GameObject)controller.GetType().GetField("rotationIntroUI_", Private).GetValue(controller);
            Check(intro.activeSelf && evaluation.State == ExperimentState.ES_SLIDES,
                "Rotation instructions wait for A despite the legacy skip flag and slide timer");
            Check(intro.GetComponentsInChildren<UnityEngine.UI.Text>(true).All(t => !t.text.Contains("72") && !t.text.Contains("practice trials")),
                "Participant instructions do not disclose trial counts");
            buttons.ProcessButtonState(false, false, false);
            buttons.ProcessButtonState(true, false, false);
            Check(evaluation.State == ExperimentState.ES_INSTRUCTION, "A starts the physical turning task after reading instructions");
            buttons.ProcessButtonState(false, false, false);
            UnityEngine.Random.InitState(20261023);

            float yaw = 350f;
            int completed = 0;
            while (evaluation.State != ExperimentState.ES_THANKYOU && completed < 100)
            {
                RotationTurnDirection expectedDirection = completed % 2 == 0 ? RotationTurnDirection.Left : RotationTurnDirection.Right;
                Check(evaluation.State == ExperimentState.ES_INSTRUCTION && evaluation.ActiveTrial.TurnDirection == expectedDirection,
                    "Direction alternates left first across practice and measured trials " + completed);
                FeedPose(rig, controller, evaluation, yaw);
                var directionPanel = (GameObject)controller.GetType().GetField("rotationDirectionUI_", Private).GetValue(controller);
                var directionLabel = (UnityEngine.UI.Text)controller.GetType().GetField("rotationDirectionLabel_", Private).GetValue(controller);
                Check(directionPanel.activeSelf && directionLabel.text == "Turn " + expectedDirection.ToString().ToUpperInvariant(),
                    "Correct direction cue waits for A " + completed);
                Check(!directionPanel.transform.IsChildOf(rig.trackingSpace) &&
                    !directionPanel.transform.IsChildOf(controller.quad.transform) &&
                    directionPanel.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace,
                    "Direction cue is anchored in the environment " + completed);
                Check(Mathf.Abs(rig.transform.InverseTransformPoint(rig.centerEyeAnchor.position).y - 1.65f) < .001f,
                    "Direction cue preserves tracked eye height " + completed);
                Check(Vector3.Distance(directionPanel.transform.position, rig.centerEyeAnchor.position) < 2f &&
                    directionPanel.GetComponent<RectTransform>().rect.height < 250f &&
                    !controller.quad.GetComponent<Renderer>().enabled,
                    "Direction cue is compact and leaves the scene visible " + completed);
                if (completed == 0)
                {
                    Vector3 signPosition = directionPanel.transform.position;
                    Quaternion signRotation = directionPanel.transform.rotation;
                    FeedPose(rig, controller, evaluation, yaw + 15f);
                    Check(Vector3.Distance(signPosition, directionPanel.transform.position) < .001f &&
                        Quaternion.Angle(signRotation, directionPanel.transform.rotation) < .01f,
                        "Looking around does not move the direction sign with the head");
                }
                FeedPose(rig, controller, evaluation, yaw);
                Check(evaluation.State == ExperimentState.ES_INSTRUCTION, "Direction cue does not start automatically " + completed);
                Vector3 cueEyePosition = rig.centerEyeAnchor.position;
                Quaternion cueEyeRotation = rig.centerEyeAnchor.rotation;
                Quaternion cuePathRotation = controller.path.transform.rotation;
                buttons.ProcessButtonState(true, false, false);
                buttons.ProcessButtonState(false, false, false);
                FeedPose(rig, controller, evaluation, yaw);
                Check(evaluation.State == ExperimentState.ES_ACTIVE, "A begins the indicated turn " + completed);
                Check(Vector3.Distance(cueEyePosition, rig.centerEyeAnchor.position) < .001f &&
                    Quaternion.Angle(cueEyeRotation, rig.centerEyeAnchor.rotation) < .02f &&
                    Quaternion.Angle(cuePathRotation, controller.path.transform.rotation) < .02f,
                    "Starting a turn preserves scene position and heading " + completed);
                var wrongDirectionReminder = (GameObject)controller.GetType().GetField("rotationWrongDirectionUI_", Private).GetValue(controller);
                var wrongDirectionLabel = (UnityEngine.UI.Text)controller.GetType().GetField("rotationWrongDirectionLabel_", Private).GetValue(controller);
                Check(!wrongDirectionReminder.activeSelf, "Each turn starts without a wrong-direction reminder " + completed);
                if (completed < 2)
                {
                    int direction = (int)expectedDirection;
                    yaw -= direction * 2f;
                    FeedPose(rig, controller, evaluation, yaw);
                    Check(!wrongDirectionReminder.activeSelf, "Small movement does not trigger a reminder " + expectedDirection);
                    yaw += direction * 2f;
                    FeedPose(rig, controller, evaluation, yaw);
                    float trialGain = evaluation.ActiveTrial.Value;
                    int trialNumber = evaluation.ActiveTrialNumber;
                    // The first trial also checks that a wrong-way turn beyond 180 degrees cannot wrap into completion.
                    int wrongSteps = completed == 0 ? 48 : 4;
                    for (int step = 0; step < wrongSteps; step++) { yaw -= direction * 5f; FeedPose(rig, controller, evaluation, yaw); }
                    Check(wrongDirectionReminder.activeSelf && wrongDirectionLabel.text == "Turn " + expectedDirection.ToString().ToUpperInvariant(),
                        "Wrong-way movement shows the assigned direction " + expectedDirection);
                    Check(evaluation.State == ExperimentState.ES_ACTIVE && evaluation.ActiveTrialNumber == trialNumber &&
                        evaluation.ActiveTrial.Value == trialGain, "Reminder leaves trial state and gain unchanged " + expectedDirection);
                    Check(!wrongDirectionReminder.transform.IsChildOf(rig.trackingSpace) &&
                        wrongDirectionReminder.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace &&
                        wrongDirectionReminder.GetComponent<RectTransform>().rect.height < 200,
                        "Reminder is a compact sign in the environment " + expectedDirection);
                    Vector3 reminderPosition = wrongDirectionReminder.transform.position;
                    Vector3 eyePosition = rig.centerEyeAnchor.position;
                    Quaternion eyeRotation = rig.centerEyeAnchor.rotation;
                    FeedPose(rig, controller, evaluation, yaw);
                    Check(wrongDirectionReminder.activeSelf && Vector3.Distance(reminderPosition, wrongDirectionReminder.transform.position) < .001f &&
                        Vector3.Distance(eyePosition, rig.centerEyeAnchor.position) < .001f && Quaternion.Angle(eyeRotation, rig.centerEyeAnchor.rotation) < .02f,
                        "Reminder stays steady without moving the view " + expectedDirection);
                    yaw += direction * 5f;
                    FeedPose(rig, controller, evaluation, yaw);
                    Check(!wrongDirectionReminder.activeSelf, "Correct-direction movement hides the reminder " + expectedDirection);
                    for (int step = 1; step < wrongSteps; step++) { yaw += direction * 5f; FeedPose(rig, controller, evaluation, yaw); }
                }
                int frames = 0;
                while (evaluation.State != ExperimentState.ES_QUESTION && frames++ < 200)
                {
                    if (evaluation.State == ExperimentState.ES_ACTIVE)
                        yaw += (int)expectedDirection * 5f;
                    FeedPose(rig, controller, evaluation, yaw);
                }
                Check(evaluation.State == ExperimentState.ES_QUESTION,
                    "Trial " + completed + " reaches its response after simulated rotation");
                Check(!wrongDirectionReminder.activeSelf, "Reminder is hidden on the rating screen " + completed);
                evaluation.StateChange();
                Check(evaluation.State == ExperimentState.ES_QUESTION, "Question requires explicit response confirmation " + completed);
                Check(evaluation.ResponseSelection == 0, "No rating is preselected " + completed);
                evaluation.SubmitResponse();
                Check(evaluation.State == ExperimentState.ES_QUESTION && !evaluation.ActiveTrial.ResponseRecorded,
                    "Unselected rating cannot be submitted " + completed);
                if (completed == 0)
                {
                    buttons.ProcessThumbstick(new Vector2(1, 0));
                    Check(evaluation.ResponseSelection == 0, "Horizontal stick does not select the vertical scale");
                    buttons.ProcessThumbstick(Vector2.zero);
                    buttons.ProcessThumbstick(new Vector2(0, 1));
                    buttons.ProcessThumbstick(new Vector2(0, 1));
                    Check(evaluation.ResponseSelection == -1, "Up selects once even when held");
                    buttons.ProcessThumbstick(Vector2.zero);
                    buttons.ProcessThumbstick(new Vector2(0, -1));
                    Check(evaluation.ResponseSelection == 1, "Down skips zero to the next vertical option");
                    buttons.ProcessThumbstick(Vector2.zero);
                    buttons.ProcessThumbstick(new Vector2(0, 1));
                    Check(evaluation.ResponseSelection == -1, "Up skips zero to the previous vertical option");
                    buttons.ProcessThumbstick(Vector2.zero);
                }
                evaluation.AdjustResponse(99);
                Check(evaluation.ResponseSelection == 3, "Upper scale bound clamps " + completed);
                evaluation.AdjustResponse(-99);
                Check(evaluation.ResponseSelection == -3, "Lower scale bound clamps " + completed);
                evaluation.AdjustResponse(2);
                evaluation.AdjustResponse(1);
                Check(evaluation.ResponseSelection == 1, "Moving down skips zero " + completed);
                evaluation.AdjustResponse(-1);
                Check(evaluation.ResponseSelection == -1, "Moving up skips zero " + completed);
                evaluation.AdjustResponse(-99);
                int target = evaluation.ResponseOptionAt(completed % evaluation.ResponseOptionCount);
                while (evaluation.ResponseSelection < target) Call(buttons, "OnThumbstickDown");
                Check(evaluation.ResponseSelection == target, "Vertical stick selects the requested response " + completed);
                Vector3 ratingEyePosition = rig.centerEyeAnchor.position;
                Quaternion ratingEyeRotation = rig.centerEyeAnchor.rotation;
                buttons.ProcessButtonState(true, false, false);
                ExperimentState afterConfirmation = evaluation.State;
                buttons.ProcessButtonState(true, false, false);
                Check(evaluation.State == afterConfirmation, "Holding A does not submit twice " + completed);
                buttons.ProcessButtonState(false, false, false);
                FeedPose(rig, controller, evaluation, yaw);
                Check(Vector3.Distance(ratingEyePosition, rig.centerEyeAnchor.position) < .001f &&
                    Quaternion.Angle(ratingEyeRotation, rig.centerEyeAnchor.rotation) < .02f,
                    "Answering preserves the view for the following cue " + completed);
                completed++;
            }
            Check(evaluation.State == ExperimentState.ES_THANKYOU, "Full session finishes without a trial-array overrun");
            Check(completed == 3 + evaluation.TotalTrials, "All 3 practice and 72 measured trials complete");
            report.measuredTrials = evaluation.TotalTrials;
            Trial[] trials = (Trial[])typeof(Evaluation).GetField("allTrials", Private).GetValue(evaluation);
            Check(trials.All(t => t.ResponseRecorded && t.Response >= -3 && t.Response <= 3 && t.Response != 0),
                "Every measured trial has a confirmed nonzero response in range");
            Check(trials.GroupBy(t => t.Step).Count() == 9 && trials.GroupBy(t => t.Step).All(g =>
                g.Count(t => t.TurnDirection == RotationTurnDirection.Left) == 4 &&
                g.Count(t => t.TurnDirection == RotationTurnDirection.Right) == 4),
                "Each gain appears four times in each direction");
            Check(trials.All(t => Mathf.Abs(t.Value - (.6f + .1f * t.Step)) < .0001f),
                "Measured gain values match the preserved 0.6 to 1.4 condition grid");
            string[] rows = File.ReadAllLines(Path.Combine(evaluation.SessionDirectory, "results.txt"));
            Check(rows.Length == 72 && rows.All(r => r.Split('\t').Length == 4),
                "Results contain 72 four-column rows and exclude practice trials");
            Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session_status.json")).Contains("\"complete\":true"),
                "Final session status is saved as complete");
            Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json")).Contains("\"softwareSimulation\": true"),
                "Metadata explicitly identifies simulated data");
            Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json")).Contains("-3,-2,-1,1,2,3"),
                "Metadata records the six-option rotation scale");
            Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json")).Contains("Uncomfortably smaller") &&
                File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json")).Contains("Maybe small") &&
                File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json")).Contains("Maybe large") &&
                File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json")).Contains("Uncomfortably larger"),
                "Metadata records the revised comfort labels");
            Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json")).Contains("alternate_left_first_across_practice_and_measured") &&
                File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "events.tsv")).Contains("TURN_COMPLETED_LEFT") &&
                File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "events.tsv")).Contains("TURN_COMPLETED_RIGHT"),
                "Direction schedule and completion events are saved");
            Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json"))
                .Contains("in_scene_direction_sign_continuous_view_2026_11_02"),
                "Session records the continuous in-scene direction-cue presentation");
            Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json"))
                .Contains("in_scene_turn_direction_only_v1"), "Session records the wrong-direction reminder version");
            Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session.json")).Contains("working_pilot_protocol_for_review") &&
                !evaluation.ScaleInterpretationConfirmed,
                "Working pilot instructions are recorded without inventing advisor confirmation");
            string motionText;
            using (var stream = new FileStream(Path.Combine(evaluation.SessionDirectory, "motion.tsv"),
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream)) motionText = reader.ReadToEnd();
            Check(motionText.Split('\n').Length > 72,
                "Motion samples include trial numbers and timing");
            Check(Quaternion.Angle(controller.quad.transform.localRotation, Quaternion.identity) < .01f,
                "Head-locked response panel stays in front of the eyes after trial rotations");

            foreach (ExperimentType type in Enum.GetValues(typeof(ExperimentType)))
            {
                evaluation.EXP_TYPE = type;
                evaluation.State = ExperimentState.ES_QUESTION;
                evaluation.resetYaw = .25f;
                evaluation.offsetYaw = .5f;
                evaluation.offsetPos = new Vector2(.2f, -.3f);
                FeedPose(rig, controller, evaluation, 20f);
                Check(evaluation.State == ExperimentState.ES_QUESTION, "Shared pose correction leaves " + type + " response state intact");
            }
            report.passed = true;
        }
        catch (Exception error)
        {
            report.error = error.ToString();
            throw;
        }
        finally
        {
            UnityEngine.Random.state = previousRandom;
            // Dispose synthetic recorders and return to the clean saved rotation scene.
            EditorSceneManager.OpenScene("Assets/Scenes/Rotation.unity");
            File.WriteAllText(Path.Combine(output, "unity-qa-report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("Rotation software QA " + (report.passed ? "PASSED" : "FAILED") + ": " + output);
        }
    }

    private static void FeedPose(OVRCameraRig rig, RedirectionController controller, Evaluation evaluation, float yaw)
    {
        Vector3 head = new Vector3(.1f, 1.65f, -.2f);
        Quaternion rotation = Quaternion.Euler(0, yaw, 0);
        rig.centerEyeAnchor.localPosition = head;
        rig.centerEyeAnchor.localRotation = rotation;
        rig.leftEyeAnchor.localPosition = head + rotation * new Vector3(-.032f, 0, 0);
        rig.rightEyeAnchor.localPosition = head + rotation * new Vector3(.032f, 0, 0);
        rig.leftEyeAnchor.localRotation = rig.rightEyeAnchor.localRotation = rotation;
        Vector3 left = head + rotation * new Vector3(-.25f, -.3f, .4f);
        Vector3 right = head + rotation * new Vector3(.25f, -.3f, .4f);
        rig.leftHandAnchor.localPosition = left;
        rig.rightHandAnchor.localPosition = right;
        rig.leftHandAnchor.localRotation = rig.rightHandAnchor.localRotation = rotation;
        controller.ProcessTrackedPose(rig);
        if ((rig.centerEyeAnchor.localPosition - head).sqrMagnitude > .000001f ||
            Quaternion.Angle(rig.centerEyeAnchor.localRotation, rotation) > .01f)
            throw new Exception("Redirection changed raw headset tracking anchors.");
        if (Mathf.Abs(Vector3.Distance(rig.leftEyeAnchor.position, rig.rightEyeAnchor.position) - .064f) > .0001f)
            throw new Exception("Stereo eye separation changed.");
        if (Mathf.Abs(Vector3.Distance(rig.centerEyeAnchor.position, rig.leftHandAnchor.position) - Vector3.Distance(head, left)) > .0001f ||
            Mathf.Abs(Vector3.Distance(rig.centerEyeAnchor.position, rig.rightHandAnchor.position) - Vector3.Distance(head, right)) > .0001f)
            throw new Exception("Controller alignment changed relative to the headset.");
        if (Mathf.Abs(Mathf.DeltaAngle(evaluation.outsideYaw * Mathf.Rad2Deg, yaw)) > .01f)
            throw new Exception("Virtual yaw fed back into the raw tracked yaw.");
        if (evaluation.EXP_TYPE == ExperimentType.ET_ROTATION && evaluation.State == ExperimentState.ES_ACTIVE)
        {
            Quaternion baseRotation = (Quaternion)typeof(RedirectionController)
                .GetField("trackingBaseRotation_", Private).GetValue(controller);
            Quaternion localVirtual = Quaternion.Inverse(rig.transform.rotation * baseRotation) * rig.centerEyeAnchor.rotation;
            float realTurn = (float)typeof(RedirectionController).GetField("e_accumulatedTurn", Private).GetValue(controller) * Mathf.Rad2Deg;
            float trialOffset = (float)typeof(RedirectionController).GetField("e_trialYawOffset", Private).GetValue(controller);
            float expectedYaw = (evaluation.resetYaw + trialOffset + evaluation.start_yaw) * Mathf.Rad2Deg + realTurn * evaluation.ActiveTrial.Value;
            if (Mathf.Abs(Mathf.DeltaAngle(localVirtual.eulerAngles.y, expectedYaw)) > .02f)
                throw new Exception("Virtual rotation does not equal physical rotation multiplied by the configured gain. " +
                    "actual=" + localVirtual.eulerAngles.y + ", expected=" + expectedYaw +
                    ", trial=" + evaluation.ActiveTrialNumber + ", gain=" + evaluation.ActiveTrial.Value);
        }
        report.poseChecks++;
    }

    public static void RunAllAndExit()
    {
        int code = 0;
        try { RunAll(); }
        catch (Exception error) { Debug.LogException(error); code = 1; }
        EditorApplication.Exit(code);
    }
}
