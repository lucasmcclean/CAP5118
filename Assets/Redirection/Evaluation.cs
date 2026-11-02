using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public enum ExperimentType
{
    ET_CURVATURE,
    ET_TRANSLATION,
    ET_ROTATION
}

public enum ExperimentState
{
    ES_UNDEFINED,
    ES_READYQUESTION,
    ES_SLIDES,
    ES_ACTIVE,
    ES_QUESTION,
    ES_INSTRUCTION,
    ES_THANKYOU,
    // Append to preserve the values serialized in the team's scenes.
    ES_PID
}

public enum Reaction
{
    RE_UP,
    RE_DOWN,
    RE_NONE
}

// Unity yaw increases when turning right and decreases when turning left.
public enum RotationTurnDirection { Left = -1, Right = 1 }

[Serializable]
public class Trial
{
    public Reaction Reaction;
    public int Response;
    public int Step;
    public int Step2;
    public float Value;
    public float Value2;
    public bool ResponseRecorded;
    public RotationTurnDirection TurnDirection;
}

public class Evaluation : MonoBehaviour
{
    [Header("Pilot session - use an anonymous code, never a participant name")]
    public string ParticipantId = "";
    public string ScaleInterpretation = "Pending advisor confirmation";
    public bool ScaleInterpretationConfirmed = false;
    public const string WorkingRotationScale = "Rotation pilot six-choice scale: -3 Uncomfortably smaller, -2 Comfortably small, -1 Maybe small, +1 Maybe large, +2 Comfortably large, +3 Uncomfortably larger. No zero option. Revised at Rohan's request on October 29; not recorded as advisor-confirmed.";
    public int PracticeTrialCount { get; private set; }
    public string SessionDirectory { get; private set; }
    public bool IsSoftwareSimulation { get; private set; }
    public string SessionError { get; private set; }
    private string simulationRoot;
    private PilotSessionRecorder session;

    public float DISTANCE = 3.0f;         // 3m walking distance in VE
    public float ANGLE = Mathf.PI * 0.5f; // 90 deg angle in VE
    public float VALUES_MIN = 0.6f;
    public float VALUES_MAX = 1.4f;
    public float VALUES_MIN2 = 0;
    public float VALUES_MAX2 = 0;

    public int STEPS = 8; // 9 for R+T, 8 for C
    public int STEPS2 = 1;
    public int TRIALS = 2;
    public int TEST_TRIALS = 5;
    public ExperimentType EXP_TYPE = ExperimentType.ET_TRANSLATION;
    public bool SKIP_SLIDES = false;

    public bool insideUpdate = false;
    public Vector2 outsidePos = new Vector2(0, 0);
    public float outsideYaw = 0;

    public ExperimentState State;

    public GameObject centerEye;

    public float StartTime;              // in milliseconds

    public Vector2 start_p;
    public float start_yaw;

    public Vector2 resetPos;
    public float resetYaw;
    public Vector2 offsetPos;
    public float offsetYaw;

    public int ActiveTrialNumber;
    public Trial ActiveTrial;

    public const int RESPONSE_MIN = -3;
    public const int RESPONSE_MAX = 3;
    private static readonly int[] RotationResponses = { -3, -2, -1, 1, 2, 3 };

    public int ResponseOptionCount => EXP_TYPE == ExperimentType.ET_ROTATION ? RotationResponses.Length : RESPONSE_MAX - RESPONSE_MIN + 1;
    public int ResponseOptionAt(int index) => EXP_TYPE == ExperimentType.ET_ROTATION ? RotationResponses[index] : RESPONSE_MIN + index;

    public int ResponseSelection;

    public const int PID_MAX_LENGTH = 8;
    public int PidSelectedKey = 4;

    public int TotalTrials;

    private Trial[] allTrials;
    private int[] completedTrials;
    private int[] leftRotationTrials, rightRotationTrials;
    private int rotationTrialsScheduled;

    private System.Random random = new System.Random();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (STEPS < 1 || STEPS2 < 1 || TRIALS < 1 || TEST_TRIALS < 0)
            throw new InvalidOperationException("Trial counts must be positive; practice count cannot be negative.");
        if (EXP_TYPE == ExperimentType.ET_ROTATION && TRIALS % 2 != 0)
            throw new InvalidOperationException("Rotation needs an even repetition count to balance left and right turns.");

        // Initialize values
        PracticeTrialCount = TEST_TRIALS;
        if (EXP_TYPE == ExperimentType.ET_ROTATION &&
            (string.IsNullOrWhiteSpace(ScaleInterpretation) || ScaleInterpretation.StartsWith("Pending", StringComparison.OrdinalIgnoreCase)))
            ScaleInterpretation = WorkingRotationScale;
        State = ExperimentState.ES_READYQUESTION;
        resetPos = new Vector2(0, 0);
        resetYaw = 0;
        offsetPos = new Vector2(0, 0);
        offsetYaw = 0;
        start_p = new Vector2(0, 0);
        start_yaw = 0;

        TotalTrials = STEPS * STEPS2 * TRIALS;

        allTrials = new Trial[TotalTrials];
        completedTrials = new int[STEPS * STEPS2];
        if (EXP_TYPE == ExperimentType.ET_ROTATION)
        {
            leftRotationTrials = new int[STEPS * STEPS2];
            rightRotationTrials = new int[STEPS * STEPS2];
            rotationTrialsScheduled = 0;
        }

        for (int i = 0; i < completedTrials.Length; i++)
            completedTrials[i] = 0;

        for (int i = 0; i < allTrials.Length; i++)
            allTrials[i] = new Trial();

        ActiveTrialNumber = -1;
        ActiveTrial = new Trial();
        SetNextTrial();
        if (GetComponent<PilotParticipantEntry>() == null)
            gameObject.AddComponent<PilotParticipantEntry>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetNextTrial()
    {
        if (TEST_TRIALS == 0)
        {
            if (ActiveTrialNumber >= TotalTrials - 1)
                throw new InvalidOperationException("All measured trials have already been allocated.");
            if (ActiveTrialNumber < 0)
                ActiveTrialNumber = 0;
            else
                ActiveTrialNumber++;

            ActiveTrial = allTrials[ActiveTrialNumber];
            AssignRotationDirection();

            SetNextStep();

            ActiveTrial.Value = GetGainToStep(ActiveTrial.Step);
            ActiveTrial.Value2 = GetGainToStep2(ActiveTrial.Step2);
        }
        else
        {
            TEST_TRIALS--;
            ActiveTrialNumber--;

            ActiveTrial.Step = -1;
            ActiveTrial.Step2 = 0;

            ActiveTrial.Value = (TEST_TRIALS % 2 == 0) ? VALUES_MIN : VALUES_MAX;

            if (EXP_TYPE == ExperimentType.ET_CURVATURE)
            {
                ActiveTrial.Value = (TEST_TRIALS % 2 == 0) ? -5f : 5f; //DEBUG!!!
            }

            ActiveTrial.Value2 = GetGainToStep2(ActiveTrial.Step2);
            AssignRotationDirection();
        }
    }

    private void AssignRotationDirection()
    {
        if (EXP_TYPE != ExperimentType.ET_ROTATION) return;
        ActiveTrial.TurnDirection = rotationTrialsScheduled++ % 2 == 0 ?
            RotationTurnDirection.Left : RotationTurnDirection.Right;
    }

    private float GetGainToStep(int step)
    {
        if (EXP_TYPE == ExperimentType.ET_CURVATURE)
        {
            switch (step)
            {
                case 0: return -5f;
                case 1: return -10f;
                case 2: return -20f;
                case 3: return -30f;
                case 4: return 30f;
                case 5: return 20f;
                case 6: return 10f;
                case 7: return 5f;
            }
        }

        while (step >= STEPS)
            step -= STEPS;

        // step == 0          =>  VALUES_MIN
        // step == STEPS - 1  =>  VALUES_MAX
        return STEPS == 1 ? VALUES_MIN : VALUES_MIN + step / (float)(STEPS - 1) * (VALUES_MAX - VALUES_MIN);
    }

    private float GetGainToStep2(int step2)
    {
        while (step2 >= STEPS2)
            step2 -= STEPS2;

        // step2 == 0            =>  VALUES_MIN2
        // step2 == STEPS2 - 1  =>  VALUES_MAX2
        if (STEPS2 > 1)
            return VALUES_MIN2 + step2 / (float)(STEPS2 - 1) * (VALUES_MAX2 - VALUES_MIN2);

        return VALUES_MIN2;
    }

    private void SetNextStep()
    {
//        Debug.Log("SetNextStep()");

        // Find number of free slots
        int[] counts = EXP_TYPE == ExperimentType.ET_ROTATION ?
            (ActiveTrial.TurnDirection == RotationTurnDirection.Left ? leftRotationTrials : rightRotationTrials) :
            completedTrials;
        int limit = EXP_TYPE == ExperimentType.ET_ROTATION ? TRIALS / 2 : TRIALS;
        int slots = 0;
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] < limit)
                slots++;
        }

        // Choose randomly a number in [0, slots-1]
        if (slots == 0)
            throw new InvalidOperationException("No trial conditions remain.");
        // Random.Next's upper bound is exclusive. Every available slot must be eligible.
        int n = random.Next(0, slots);

        int cnt = 0;
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] >= limit)
                continue;

            if (cnt == n)
            {
                counts[i]++;

                ActiveTrial.Step = (int)(i / STEPS2);
                ActiveTrial.Step2 = i - (int)(i / STEPS2) * STEPS2;
                return;
            }

            cnt++;
        }

        //NOTE: Should not happen
        ActiveTrial.Step = 0;
        ActiveTrial.Step2 = 0;
    }

    public void reset()
    {
        offsetPos = new Vector2(0, 0);
        offsetYaw = 0;
    }

    public void AdjustResponse(int delta)
    {
        if (State != ExperimentState.ES_QUESTION)
            return;

        if (delta == 0) return;
        if (EXP_TYPE != ExperimentType.ET_ROTATION)
        {
            ResponseSelection = Mathf.Clamp(ResponseSelection + delta, RESPONSE_MIN, RESPONSE_MAX);
            return;
        }
        int index = Array.IndexOf(RotationResponses, ResponseSelection);
        // Zero represents no selection, not a response. The first flick selects
        // the nearest option on the chosen side of the scale.
        if (index < 0) index = delta > 0 ? 2 : 3;
        ResponseSelection = RotationResponses[Mathf.Clamp(index + delta, 0, RotationResponses.Length - 1)];
    }

    // Christian/Logan's 3-by-4 headset keypad, using the existing recorder ID.
    public void MovePidSelection(int dx, int dy)
    {
        if (State != ExperimentState.ES_PID) return;
        int column = Mathf.Clamp(PidSelectedKey % 3 + dx, 0, 2);
        int row = Mathf.Clamp(PidSelectedKey / 3 + dy, 0, 3);
        PidSelectedKey = row * 3 + column;
    }

    public void PressPidKey()
    {
        if (State != ExperimentState.ES_PID || session != null) return;
        if (PidSelectedKey == 11) { StateChange(); return; }
        string current = ParticipantId ?? "";
        if (PidSelectedKey == 9)
        {
            if (current.Length > 0) ParticipantId = current.Substring(0, current.Length - 1);
        }
        else if (current.Length < PID_MAX_LENGTH)
        {
            string digit = PidSelectedKey == 10 ? "0" : (PidSelectedKey + 1).ToString(CultureInfo.InvariantCulture);
            ParticipantId = current + digit;
        }
        SessionError = "";
    }

    public void SubmitResponse()
    {
        if (State != ExperimentState.ES_QUESTION)
            return;
        if (EXP_TYPE == ExperimentType.ET_ROTATION && Array.IndexOf(RotationResponses, ResponseSelection) < 0)
            return;

        ActiveTrial.Response = ResponseSelection;
        ActiveTrial.ResponseRecorded = true;

        if (ResponseSelection < 0)
            ActiveTrial.Reaction = Reaction.RE_UP;
        else if (ResponseSelection > 0)
            ActiveTrial.Reaction = Reaction.RE_DOWN;
        else
            ActiveTrial.Reaction = Reaction.RE_NONE;

        StateChange();
    }

    public void recenterPose()
    {
        recenterPose(0);
    }

    public void recenterPose(float yaw)
    {
        reset();

        resetYaw = -getYaw() + yaw;
        resetPos = -getPos();
    }

    public float getYaw()
    {
        return outsideYaw;
    }

    public void setYawOffset(float yaw)
    {
        offsetYaw = yaw - outsideYaw;
    }

    public Vector2 getPos()
    {
        return outsidePos;
    }

    public void setPosX(float x)
    {
        offsetPos.x = x - outsidePos.x;
    }

    public void setPosY(float y)
    {
        offsetPos.y = y - outsidePos.y;
    }

    public void writeToTxt()
    {
        if (ActiveTrialNumber < 0 || session == null)
            return;
        var rows = new List<string>();
        for (int i = 0; i <= Mathf.Min(TotalTrials - 1, ActiveTrialNumber); i++)
        {
            Trial trial = allTrials[i];
            if (!trial.ResponseRecorded) continue;
            rows.Add(trial.Value.ToString("R", CultureInfo.InvariantCulture) + "\t" +
                trial.Value2.ToString("R", CultureInfo.InvariantCulture) + "\t" +
                trial.Reaction + "\t" + trial.Response.ToString(CultureInfo.InvariantCulture));
        }
        session.WriteResults(rows, State == ExperimentState.ES_THANKYOU);
    }

    public void writeLog(
        float realX,
        float realY,
        float virtualX,
        float virtualY,
        float realYaw,
        float virtualYaw)
    {
        if (ActiveTrialNumber < 0 || session == null)
            return;
        session.WriteMotion(ActiveTrialNumber, Time.realtimeSinceStartup,
            realX, realY, virtualX, virtualY, realYaw, virtualYaw);
    }

    public void writeData(string text)
    {
        if (session == null)
            return;
        session.WriteEvent(ActiveTrialNumber, text);
    }

    public bool TrySetParticipantId(string value)
    {
        if (session != null) return false;
        if (value == null || !Regex.IsMatch(value, "^[A-Za-z0-9_-]{1,32}$"))
        {
            SessionError = "Enter an anonymous ID using 1-32 letters, digits, underscores or hyphens.";
            return false;
        }
        ParticipantId = value;
        SessionError = "";
        return true;
    }

    // Called only by the Editor QA harness. Synthetic runs never enter the pilot folder.
    public void ConfigureSoftwareSimulation(string outputRoot)
    {
        if (session != null) throw new InvalidOperationException("A session has already started.");
        IsSoftwareSimulation = true;
        simulationRoot = Path.GetFullPath(outputRoot);
    }

    private bool BeginSession()
    {
        if (session != null) return true;
        if (!TrySetParticipantId(ParticipantId)) return false;
        if (!IsSoftwareSimulation && (string.IsNullOrWhiteSpace(ScaleInterpretation) ||
            ScaleInterpretation.StartsWith("Pending", StringComparison.OrdinalIgnoreCase)))
        {
            SessionError = "Enter the response instructions used for this pilot before starting.";
            return false;
        }
        try
        {
            string root = IsSoftwareSimulation ? simulationRoot :
                Path.Combine(Application.persistentDataPath, "CAP5118_PilotSessions");
            var metadata = new PilotMetadata {
                participantId = ParticipantId, experiment = EXP_TYPE.ToString(),
                softwareSimulation = IsSoftwareSimulation,
                scaleInterpretation = ScaleInterpretation,
                scaleInterpretationConfirmed = ScaleInterpretationConfirmed,
                protocolStatus = ScaleInterpretationConfirmed ? "confirmed_protocol" : "working_pilot_protocol_for_review",
                instructionSource = EXP_TYPE == ExperimentType.ET_ROTATION ? "Comfort scale and alternating directions requested by Rohan on October 29; inherited gain grid retained" : "Team working pilot instructions",
                expectedMeasuredTrials = TotalTrials, practiceTrials = PracticeTrialCount,
                steps = STEPS, steps2 = STEPS2, repetitions = TRIALS,
                minimumGain = VALUES_MIN, maximumGain = VALUES_MAX,
                minimumGain2 = VALUES_MIN2, maximumGain2 = VALUES_MAX2,
                virtualAngleRadians = ANGLE, virtualDistanceMetres = DISTANCE,
                scene = gameObject.scene.path, unityVersion = Application.unityVersion,
                createdUtc = DateTime.UtcNow.ToString("o"),
                responseScale = EXP_TYPE == ExperimentType.ET_ROTATION ? "-3,-2,-1,1,2,3" : "-3,-2,-1,0,1,2,3",
                directionSchedule = EXP_TYPE == ExperimentType.ET_ROTATION ? "alternate_left_first_across_practice_and_measured" : "",
                rotationPresentation = EXP_TYPE == ExperimentType.ET_ROTATION ? "in_scene_direction_sign_continuous_view_2026_11_02" : "",
                rotationWrongDirectionReminder = EXP_TYPE == ExperimentType.ET_ROTATION ? "in_scene_turn_direction_only_v1" : "",
                repetitionsPerDirection = EXP_TYPE == ExperimentType.ET_ROTATION ? TRIALS / 2 : 0
            };
            session = new PilotSessionRecorder(root, ParticipantId, EXP_TYPE.ToString(),
                IsSoftwareSimulation, TotalTrials, JsonUtility.ToJson(metadata, true));
            SessionDirectory = session.DirectoryPath;
            SessionError = "";
            Debug.Log("Pilot session: " + SessionDirectory + (IsSoftwareSimulation ? " [SYNTHETIC SOFTWARE TEST]" : ""));
            return true;
        }
        catch (Exception error)
        {
            SessionError = "Could not create session files: " + error.Message;
            Debug.LogError(SessionError);
            return false;
        }
    }

    private void OnDestroy()
    {
        session?.Dispose();
        session = null;
    }

    [Serializable] private class PilotMetadata
    {
        public string participantId, experiment, scene, unityVersion, createdUtc, responseScale, scaleInterpretation,
            protocolStatus, instructionSource, directionSchedule, rotationPresentation, rotationWrongDirectionReminder;
        public bool softwareSimulation, scaleInterpretationConfirmed;
        public int expectedMeasuredTrials, practiceTrials, steps, steps2, repetitions, repetitionsPerDirection;
        public float minimumGain, maximumGain, minimumGain2, maximumGain2, virtualAngleRadians, virtualDistanceMetres;
    }


    public void StateChange()
    {
        Debug.Log("StateChange:");

        switch (State)
        {
            case ExperimentState.ES_UNDEFINED:

                State = ExperimentState.ES_READYQUESTION;

                break;

            case ExperimentState.ES_READYQUESTION:
                PidSelectedKey = 4;
                SessionError = "";
                State = ExperimentState.ES_PID;
                break;

            case ExperimentState.ES_PID:
                if (!BeginSession()) return;
                State = ExperimentState.ES_SLIDES;
                writeData("ES_SLIDES");
                StartTime = Time.realtimeSinceStartup * 1000f; //Note: In milliseconds

                recenterPose();

                break;

            case ExperimentState.ES_SLIDES:

                State = ExperimentState.ES_INSTRUCTION;
                writeData("ES_INSTRUCTION");
                if (EXP_TYPE == ExperimentType.ET_ROTATION) writeData("TURN_DIRECTION_" + ActiveTrial.TurnDirection.ToString().ToUpperInvariant());
                start_p = new Vector2(getPos().x, getPos().y);
                start_yaw = getYaw();

                break;

            case ExperimentState.ES_ACTIVE:

                State = ExperimentState.ES_QUESTION;
                ResponseSelection = 0;
                ActiveTrial.Response = 0;
                ActiveTrial.ResponseRecorded = false;
                writeData("ES_QUESTION");

                break;

            case ExperimentState.ES_QUESTION:
                if (!ActiveTrial.ResponseRecorded) return;
                writeToTxt();

                if (ActiveTrialNumber < TotalTrials - 1)
                {
                    SetNextTrial();

                    State = ExperimentState.ES_INSTRUCTION;
                    writeData("ES_INSTRUCTION");
                    if (EXP_TYPE == ExperimentType.ET_ROTATION) writeData("TURN_DIRECTION_" + ActiveTrial.TurnDirection.ToString().ToUpperInvariant());
                    start_p = new Vector2(getPos().x, getPos().y);
                    start_yaw = getYaw();
                }
                else
                {
                    State = ExperimentState.ES_THANKYOU;
                    writeData("ES_THANK_YOU");
                    writeToTxt();
                }

                break;

            case ExperimentState.ES_INSTRUCTION:

                State = ExperimentState.ES_ACTIVE;
                writeData("ES_ACTIVE");
                start_p = new Vector2(getPos().x, getPos().y);
                start_yaw = getYaw();

                break;

            case ExperimentState.ES_THANKYOU:

                writeToTxt();

                break;
        }
    }
}
