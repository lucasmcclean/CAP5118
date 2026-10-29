using System;
using System.Collections.Generic;
using System.IO;
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
    ES_PID
}

public enum Reaction
{
    RE_UP,
    RE_DOWN,
    RE_NONE
}

[Serializable]
public class Trial
{
    public Reaction Reaction;
    public int Response;
    public int Step;
    public int Step2;
    public float Value;
    public float Value2;
}

public class Evaluation : MonoBehaviour
{
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

    public int ResponseSelection;

    // ---- Participant ID (numpad) ----
    public const int PID_MAX_LENGTH = 8;
    public string ParticipantID = "";
    public int PidSelectedKey = 4; // 0-11, row-major: 1 2 3 / 4 5 6 / 7 8 9 / Del 0 OK
    
    public int TotalTrials;

    private Trial[] allTrials;
    private int[] completedTrials;

    private System.Random random = new System.Random();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize values
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

        for (int i = 0; i < completedTrials.Length; i++)
            completedTrials[i] = 0;

        for (int i = 0; i < allTrials.Length; i++)
            allTrials[i] = new Trial();

        ActiveTrialNumber = -1;
        ActiveTrial = new Trial();
        SetNextTrial();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetNextTrial()
    {
        if (TEST_TRIALS == 0)
        {
            if (ActiveTrialNumber < 0)
                ActiveTrialNumber = 0;
            else
                ActiveTrialNumber++;

            ActiveTrial = allTrials[ActiveTrialNumber];

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
        }
    }

    private float GetGainToStep(int step)
    {
        if (EXP_TYPE == ExperimentType.ET_CURVATURE)
        {
            switch (step)
            {
                case 0: return -5f;
                case 1: return -10f;
                case 2: return -15f;
                case 3: return -20f;
                case 4: return -30f;
                case 5: return 30f;
                case 6: return 20f;
                case 7: return 15f;
                case 8: return 10f;
                case 9: return 5f;
            }
        }

        while (step >= STEPS)
            step -= STEPS;

        // step == 0          =>  VALUES_MIN
        // step == STEPS - 1  =>  VALUES_MAX
        return VALUES_MIN + step / (float)(STEPS - 1) * (VALUES_MAX - VALUES_MIN);
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
        int slots = 0;
        for (int i = 0; i < completedTrials.Length; i++)
        {
            if (completedTrials[i] < TRIALS)
                slots++;
        }

        // Choose randomly a number in [0, slots-1]
        int n = random.Next(0, slots-1);

        int cnt = 0;
        for (int i = 0; i < completedTrials.Length; i++)
        {
            if (completedTrials[i] >= TRIALS)
                continue;

            if (cnt == n)
            {
                completedTrials[i]++;

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

        ResponseSelection = Mathf.Clamp(
            ResponseSelection + delta,
            RESPONSE_MIN,
            RESPONSE_MAX);

        if (EXP_TYPE == ExperimentType.ET_CURVATURE && ResponseSelection == 0)
            ResponseSelection = Mathf.Clamp(ResponseSelection + delta, RESPONSE_MIN, RESPONSE_MAX);
    }

    public void SubmitResponse()
    {
        if (State != ExperimentState.ES_QUESTION)
            return;

        if (EXP_TYPE == ExperimentType.ET_CURVATURE && ResponseSelection == 0)
            return;

        ActiveTrial.Response = ResponseSelection;

        if (ResponseSelection < 0)
            ActiveTrial.Reaction = Reaction.RE_UP;
        else if (ResponseSelection > 0)
            ActiveTrial.Reaction = Reaction.RE_DOWN;
        else
            ActiveTrial.Reaction = Reaction.RE_NONE;

        StateChange();
    }
     // ---- Participant ID numpad ----

    public void MovePidSelection(int dx, int dy)
    {
        if (State != ExperimentState.ES_PID)
            return;

        int col = Mathf.Clamp(PidSelectedKey % 3 + dx, 0, 2);
        int row = Mathf.Clamp(PidSelectedKey / 3 + dy, 0, 3);
        PidSelectedKey = row * 3 + col;
    }

    public void PressPidKey()
    {
        if (State != ExperimentState.ES_PID)
            return;

        if (PidSelectedKey < 9)
            AddPidDigit(PidSelectedKey + 1);
        else if (PidSelectedKey == 9)
            BackspacePid();
        else if (PidSelectedKey == 10)
            AddPidDigit(0);
        else
            SubmitPid();
    }

    private void AddPidDigit(int digit)
    {
        if (ParticipantID.Length < PID_MAX_LENGTH)
            ParticipantID += digit.ToString();
    }

    private void BackspacePid()
    {
        if (ParticipantID.Length > 0)
            ParticipantID = ParticipantID.Substring(0, ParticipantID.Length - 1);
    }

    private void SubmitPid()
    {
        if (ParticipantID.Length == 0)
            return; // require at least one digit
        StateChange();
    }

    // PID is digits only, so it is always safe to use in a filename
    private string GetFilePath(string suffix)
    {
        string name = string.IsNullOrEmpty(ParticipantID)
            ? suffix
            : ParticipantID + "_" + suffix;
        return Path.Combine(Application.persistentDataPath, name + ".txt");
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
        if (ActiveTrialNumber < 0)
            return;

        string path = Path.Combine(
            Application.persistentDataPath,
            "results.txt");

        using (StreamWriter writer =
                new StreamWriter(path, false))
        {
            writer.WriteLine(ParticipantID);
            for (int i = 0; i <= Mathf.Min(TotalTrials - 1, ActiveTrialNumber); i++)
            {
                Trial trial = allTrials[i];

                writer.WriteLine($"{trial.Value}\t{trial.Value2}\t{trial.Reaction}\t{trial.Response}");
            }
        }

        string pidPath = GetFilePath("results");
        using (StreamWriter writer =
                new StreamWriter(pidPath, false))
        {
            writer.WriteLine(ParticipantID);
            for (int i = 0; i <= Mathf.Min(TotalTrials - 1, ActiveTrialNumber); i++)
            {
                Trial trial = allTrials[i];

                writer.WriteLine($"{trial.Value}\t{trial.Value2}\t{trial.Reaction}\t{trial.Response}");
            }
        }

//        Debug.Log("path (" + path + ")");
    }

    public void writeLog(
        float realX,
        float realY,
        float virtualX,
        float virtualY,
        float realYaw,
        float virtualYaw)
    {
        if (ActiveTrialNumber < 0)
            return;

        string path =
            Path.Combine(
                Application.persistentDataPath,
                "data.txt");

        string line =
            $"{realX}\t{realY}\t" +
            $"{virtualX}\t{virtualY}\t" +
            $"{realYaw}\t" +
            $"{virtualYaw}";

        File.AppendAllText(
            path,
            line + Environment.NewLine);

        string pidPath = GetFilePath("data");
        File.AppendAllText(
            pidPath,
            line + Environment.NewLine);
    }

    public void writeData(string text)
    {
        if (ActiveTrialNumber < 0)
            return;

        string path =
            Path.Combine(
                Application.persistentDataPath,
                "data.txt");

        File.AppendAllText(
            path,
            "\n\n" + text + "\n\n");

        string pidPath = GetFilePath("data");
        File.AppendAllText(
            pidPath,
            "\n\n" + text + "\n\n");
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
                State = ExperimentState.ES_PID;
                ParticipantID = "";
                PidSelectedKey = 4;
                break;

            case ExperimentState.ES_PID:
                State = ExperimentState.ES_SLIDES;
                File.AppendAllText(GetFilePath("data"), "PID: " + ParticipantID + Environment.NewLine);
                writeData("ES_SLIDES");
                StartTime = Time.realtimeSinceStartup * 1000f;
                recenterPose();
                break;

            case ExperimentState.ES_SLIDES:

                State = ExperimentState.ES_INSTRUCTION;
                writeData("ES_INSTRUCTION");
                start_p = new Vector2(getPos().x, getPos().y);
                start_yaw = getYaw();

                break;

            case ExperimentState.ES_ACTIVE:

                State = ExperimentState.ES_QUESTION;
                ResponseSelection = 0;
                ActiveTrial.Response = 0;
                writeData("ES_QUESTION");

                break;

            case ExperimentState.ES_QUESTION:

                writeToTxt();

                if (ActiveTrialNumber < TotalTrials - 1)
                {
                    SetNextTrial();

                    State = ExperimentState.ES_INSTRUCTION;
                    writeData("ES_INSTRUCTION");
                    start_p = new Vector2(getPos().x, getPos().y);
                    start_yaw = getYaw();
                }
                else
                {
                    State = ExperimentState.ES_THANKYOU;
                    writeData("ES_THANK_YOU");
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
