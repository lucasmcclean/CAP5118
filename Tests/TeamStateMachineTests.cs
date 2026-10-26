using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Executes the actual merged Evaluation/ButtonHandler/recorder source with small
// managed API test doubles. This does not exercise scenes, rendering, tracking or XR.
public static class TeamStateMachineTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAILED: " + message);
        checks++;
    }
    private static void PressA(ButtonHandler input)
    {
        input.ProcessButtonState(false, false, false);
        input.ProcessButtonState(true, false, false);
        input.ProcessButtonState(false, false, false);
    }
    public static int Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(root);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            foreach (ExperimentType type in Enum.GetValues(typeof(ExperimentType)))
            {
                string output = Path.Combine(root, type.ToString());
                Directory.CreateDirectory(output);
                Application.persistentDataPath = Path.Combine(root, "UNEXPECTED_NATIVE_OUTPUT");
                var evaluation = new Evaluation { EXP_TYPE = type, STEPS = type == ExperimentType.ET_CURVATURE ? 8 : 9, STEPS2 = 1, TRIALS = 2, TEST_TRIALS = 5 };
                var input = new ButtonHandler { evaluation = evaluation };
                typeof(Evaluation).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(evaluation, null);
                evaluation.ConfigureSoftwareSimulation(output);
                Check((evaluation.GetComponent<PilotParticipantEntry>() != null) == (type == ExperimentType.ET_ROTATION), type + " rotation setup panel is mode-scoped");
                input.ProcessButtonState(false, true, false);
                input.ProcessButtonState(false, false, false);
                Check(evaluation.State == ExperimentState.ES_PID, type + " Ready opens PID keypad");
                evaluation.PidSelectedKey = 11;
                PressA(input);
                Check(evaluation.State == ExperimentState.ES_PID, type + " empty PID rejected");
                foreach (int key in new[] { 10, 10, 6 }) { evaluation.PidSelectedKey = key; PressA(input); }
                Check(evaluation.ParticipantId == "007" && evaluation.ParticipantID == "007", type + " keypad and both PID APIs agree and preserve leading zeroes");
                evaluation.ParticipantID = "008";
                Check(evaluation.ParticipantId == "008", type + " legacy PID setter remains compatible");
                evaluation.ParticipantID = "007";
                evaluation.PidSelectedKey = 11;
                PressA(input);
                Check(evaluation.State == ExperimentState.ES_SLIDES, type + " PID submission begins instructions");
                if (type == ExperimentType.ET_ROTATION)
                    Check(evaluation.SessionDirectory.Contains("SYNTHETIC_SOFTWARE_TESTS"), "Rotation test data is explicitly synthetic");
                else
                {
                    Check(string.IsNullOrEmpty(evaluation.SessionDirectory), type + " does not require a rotation recorder or protocol");
                    Check(File.ReadAllText(Path.Combine(output, "007_data.txt")).Contains("PID: 007"), type + " preserves PID header");
                }
                evaluation.StateChange();
                int completed = 0;
                while (evaluation.State != ExperimentState.ES_THANKYOU && completed < 100)
                {
                    Check(evaluation.State == ExperimentState.ES_INSTRUCTION, type + " next trial instruction " + completed);
                    evaluation.StateChange();
                    evaluation.writeLog(.1f, .2f, .3f, .4f, 10f, 20f);
                    evaluation.StateChange();
                    evaluation.AdjustResponse(99);
                    Check(evaluation.ResponseSelection == 3, type + " upper response bound " + completed);
                    evaluation.AdjustResponse(-99);
                    Check(evaluation.ResponseSelection == -3, type + " lower response bound " + completed);
                    evaluation.AdjustResponse(3);
                    Check(evaluation.ResponseSelection == 0, type + " neutral response available " + completed);
                    input.ProcessThumbstick(new Vector2(1, 0));
                    input.ProcessThumbstick(new Vector2(1, 0));
                    Check(evaluation.ResponseSelection == 1, type + " held stick selects once " + completed);
                    input.ProcessThumbstick(Vector2.zero);
                    input.ProcessButtonState(true, false, false);
                    ExperimentState after = evaluation.State;
                    input.ProcessButtonState(true, false, false);
                    Check(evaluation.State == after, type + " held A confirms once " + completed);
                    input.ProcessButtonState(false, false, false);
                    completed++;
                }
                Check(evaluation.State == ExperimentState.ES_THANKYOU && completed == evaluation.PracticeTrialCount + evaluation.TotalTrials, type + " full practice and measured flow completes");
                Trial[] trials = (Trial[])typeof(Evaluation).GetField("allTrials", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(evaluation);
                Check(trials.All(t => t.ResponseRecorded && t.Response == 1), type + " confirmed responses retained");
                Check(trials.GroupBy(t => new { t.Step, t.Step2 }).Count() == evaluation.STEPS * evaluation.STEPS2 && trials.GroupBy(t => new { t.Step, t.Step2 }).All(g => g.Count() == evaluation.TRIALS), type + " every gain condition has correct repetitions");
                string file = Path.Combine(type == ExperimentType.ET_ROTATION ? evaluation.SessionDirectory : output, "results.txt");
                string[] rows = File.ReadAllLines(file);
                if (type == ExperimentType.ET_ROTATION)
                {
                    Check(rows.Length == 18 && rows.All(r => r.Split('\t').Length == 4), "Rotation results preserve eighteen measured rows");
                    Check(File.ReadAllText(Path.Combine(evaluation.SessionDirectory, "session_status.json")).Contains("\"complete\":true"), "Rotation completion status saved");
                }
                else
                {
                    Check(rows.Length == evaluation.TotalTrials + 1 && rows[0] == "007" && rows.Skip(1).All(r => r.Split('\t').Length == 4), type + " legacy result layout preserved");
                    Check(File.ReadAllText(Path.Combine(output, "data.txt")).Contains("0.1\t0.2\t0.3\t0.4\t10\t20"), type + " legacy motion output preserved");
                }
                typeof(Evaluation).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(evaluation, null);
                Console.WriteLine("PASS: " + type + " completed " + completed + " synthetic state-machine trials");
            }
            Check(!Directory.Exists(Path.Combine(root, "UNEXPECTED_NATIVE_OUTPUT")), "Tests never write to native participant output");
            File.WriteAllText(Path.Combine(root, "managed-integration-result.json"), "{\"passed\":" + checks + ",\"failed\":0,\"syntheticSoftwareTest\":true,\"physicalQuestVerified\":false,\"scope\":\"actual managed state-machine/input/logging source with test doubles; not Unity scene or headset verification\"}");
            Console.WriteLine("PASS: " + checks + " managed integration assertions");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
