using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Synthetic integration checks. Never reads or modifies a participant dataset.
public static class TeamIntegrationQA
{
    [Serializable] private sealed class Report
    {
        public string timestampUtc, error;
        public bool passed, syntheticSoftwareTest = true, physicalQuestVerified = false;
        public List<string> checks = new List<string>();
    }
    private static Report report;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("Integration check failed: " + label);
        report.checks.Add(label);
    }
    private static void Call(object target, string name)
    {
        target.GetType().GetMethod(name, Private).Invoke(target, null);
    }

    [MenuItem("Tools/Rotation Pilot/Run combined project integration QA")]
    public static void RunAll()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and save the scene before integration QA.");
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../QA/Integration_" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")));
        Directory.CreateDirectory(root);
        report = new Report { timestampUtc = DateTime.UtcNow.ToString("o") };
        try
        {
            RotationPilotQA.RunAll();
            Check(true, "Merged rotation scene passes full rotation software QA");
            foreach (ExperimentType type in new[] { ExperimentType.ET_TRANSLATION, ExperimentType.ET_CURVATURE })
            {
                string name = type == ExperimentType.ET_TRANSLATION ? "Translation" : "Curvature";
                EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                Evaluation evaluation = UnityEngine.Object.FindAnyObjectByType<Evaluation>();
                RedirectionController controller = UnityEngine.Object.FindAnyObjectByType<RedirectionController>();
                ButtonHandler buttons = UnityEngine.Object.FindAnyObjectByType<ButtonHandler>();
                Check(evaluation != null && controller != null && buttons != null && evaluation.EXP_TYPE == type,
                    name + " scene retains its experiment and references");
                string output = Path.Combine(root, name);
                Directory.CreateDirectory(output);
                Call(evaluation, "Start");
                Call(controller, "Start");
                evaluation.ConfigureSoftwareSimulation(output);
                Check(evaluation.GetComponent<PilotParticipantEntry>() == null,
                    name + " does not receive the rotation researcher setup panel");
                Call(buttons, "OnBPressed");
                Check(evaluation.State == ExperimentState.ES_PID, name + " Ready still opens its PID keypad");
                evaluation.PidSelectedKey = 11;
                Call(buttons, "OnAPressed");
                Check(evaluation.State == ExperimentState.ES_PID, name + " empty PID remains rejected");
                evaluation.ParticipantID = "007";
                Check(evaluation.ParticipantId == "007", name + " existing ParticipantID source API remains compatible");
                Call(buttons, "OnAPressed");
                Check(evaluation.State == ExperimentState.ES_SLIDES && string.IsNullOrEmpty(evaluation.SessionDirectory),
                    name + " starts normally without rotation protocol/session requirements");
                Check(File.ReadAllText(Path.Combine(output, "007_data.txt")).Contains("PID: 007"),
                    name + " preserves the team's PID-header export");
                var intro = (GameObject)typeof(RedirectionController).GetField("rotationIntroUI_", Private).GetValue(controller);
                Check(intro == null, name + " does not receive rotation-only instructions");
                var boxes = (UnityEngine.UI.Image[])typeof(RedirectionController).GetField("questionBoxes_", Private).GetValue(controller);
                Check(boxes.Length == 7, name + " retains all seven response choices including zero");
                evaluation.StateChange();
                int completed = 0;
                while (evaluation.State != ExperimentState.ES_THANKYOU && completed < 200)
                {
                    Check(evaluation.State == ExperimentState.ES_INSTRUCTION, name + " trial begins with instructions " + completed);
                    evaluation.StateChange();
                    evaluation.writeLog(.1f, .2f, .3f, .4f, 10f, 20f);
                    evaluation.StateChange();
                    evaluation.AdjustResponse(99);
                    Check(evaluation.ResponseSelection == 3, name + " clamps upper response " + completed);
                    evaluation.AdjustResponse(-99);
                    Check(evaluation.ResponseSelection == -3, name + " clamps lower response " + completed);
                    evaluation.AdjustResponse(3);
                    Check(evaluation.ResponseSelection == 0, name + " permits neutral response " + completed);
                    Call(buttons, "OnAPressed");
                    completed++;
                }
                Check(evaluation.State == ExperimentState.ES_THANKYOU && completed == evaluation.PracticeTrialCount + evaluation.TotalTrials,
                    name + " completes every practice/measured state-machine trial");
                string[] rows = File.ReadAllLines(Path.Combine(output, "results.txt"));
                Check(rows.Length == evaluation.TotalTrials + 1 && rows[0] == "007" && rows.Skip(1).All(r => r.Split('\t').Length == 4),
                    name + " preserves the PID-first four-column results format");
                Check(File.ReadAllText(Path.Combine(output, "data.txt")).Contains("0.1\t0.2\t0.3\t0.4\t10\t20"),
                    name + " retains legacy motion export");
                Trial[] trials = (Trial[])typeof(Evaluation).GetField("allTrials", Private).GetValue(evaluation);
                Check(trials.GroupBy(t => new { t.Step, t.Step2 }).Count() == evaluation.STEPS * evaluation.STEPS2 &&
                    trials.GroupBy(t => new { t.Step, t.Step2 }).All(g => g.Count() == evaluation.TRIALS),
                    name + " preserves all configured gain conditions and repetitions");
            }
            report.passed = true;
        }
        catch (Exception error) { report.error = error.ToString(); throw; }
        finally
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rotation.unity");
            File.WriteAllText(Path.Combine(root, "integration-qa-report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("Combined integration QA " + (report.passed ? "PASSED" : "FAILED") + ": " + root);
        }
    }
    public static void RunAllAndExit()
    {
        int code = 0;
        try { RunAll(); }
        catch (Exception error) { Debug.LogException(error); code = 1; }
        EditorApplication.Exit(code);
    }
}
