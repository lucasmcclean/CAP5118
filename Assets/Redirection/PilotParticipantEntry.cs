using UnityEngine;

// Optional researcher-side entry on the PC mirror; the headset also has the team keypad.
[RequireComponent(typeof(Evaluation))]
public sealed class PilotParticipantEntry : MonoBehaviour
{
    private Evaluation evaluation;
    private string participant = "";
    private void Start()
    {
        evaluation = GetComponent<Evaluation>();
        participant = evaluation.ParticipantId;
    }
    private void OnGUI()
    {
        if (evaluation == null || evaluation.IsSoftwareSimulation ||
            (evaluation.State != ExperimentState.ES_READYQUESTION && evaluation.State != ExperimentState.ES_PID)) return;
        GUILayout.BeginArea(new Rect(20, 20, 450, 250), GUI.skin.box);
        GUILayout.Label("ROTATION PILOT - Researcher setup");
        GUILayout.Label("Use the same participant ID as the team's questionnaire.");
        participant = GUILayout.TextField(participant, 32);
        if (GUILayout.Button("Set participant ID")) evaluation.TrySetParticipantId(participant);
        if (GUILayout.Button("Set ID and show headset instructions") && evaluation.TrySetParticipantId(participant))
        {
            if (evaluation.State == ExperimentState.ES_READYQUESTION) evaluation.StateChange();
            evaluation.StateChange();
        }
        GUILayout.Label("Headset: A to open keypad; stick to select, A to enter; choose OK to begin.");
        if (!evaluation.ScaleInterpretationConfirmed)
            GUILayout.Label("Working pilot protocol; final study wording remains for team review.");
        if (!string.IsNullOrEmpty(evaluation.SessionError)) GUILayout.Label(evaluation.SessionError);
        GUILayout.EndArea();
    }
}
