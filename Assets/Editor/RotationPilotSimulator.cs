using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;

// Editor-only convenience for clearly separated synthetic runtime checks.
[InitializeOnLoad]
public static class RotationPilotSimulator
{
    private const string ActiveKey = "CAP5118.RotationPilotSimulator";
    private static double nextSnapshot;
    static RotationPilotSimulator()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += Tick;
    }
    [MenuItem("Tools/Rotation Pilot/Start Meta XR simulation")]
    public static void StartSimulation()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save the current scene before starting the simulator.");
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/Rotation.unity")
            EditorSceneManager.OpenScene("Assets/Scenes/Rotation.unity");
        if (!EditorApplication.ExecuteMenuItem("Meta/Meta XR Simulator/Activate"))
            throw new InvalidOperationException("Meta XR Simulator's installed SDK menu is unavailable.");
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }
    private static void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ActiveKey, false))
        {
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.ExecuteMenuItem("Meta/Meta XR Simulator/Deactivate");
        }
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(ActiveKey, false)) return;
        Evaluation evaluation = UnityEngine.Object.FindAnyObjectByType<Evaluation>();
        if (evaluation == null) return;
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../QA/MetaXRSimulator"));
        if (!evaluation.IsSoftwareSimulation)
        {
            evaluation.ConfigureSoftwareSimulation(output);
            evaluation.TrySetParticipantId("SIM_INTERACTIVE");
        }
        if (EditorApplication.timeSinceStartup < nextSnapshot) return;
        nextSnapshot = EditorApplication.timeSinceStartup + 3;
        Directory.CreateDirectory(output);
        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        bool tracked;
        head.TryGetFeatureValue(CommonUsages.isTracked, out tracked);
        Vector3 position;
        head.TryGetFeatureValue(CommonUsages.devicePosition, out position);
        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        var report = new RuntimeReport {
            utc = DateTime.UtcNow.ToString("o"),
            syntheticSoftwareTest = true, physicalQuestVerified = false,
            loader = manager != null && manager.activeLoader != null ? manager.activeLoader.name : "none",
            device = head.name, deviceValid = head.isValid, tracked = tracked,
            leftControllerValid = left.isValid, rightControllerValid = right.isValid,
            xrActive = XRSettings.isDeviceActive, headPosition = position,
            state = evaluation.State.ToString(), anonymousId = evaluation.ParticipantId,
            ambientAudioPlaying = UnityEngine.Object.FindObjectsByType<AudioSource>()
                .Any(a => a.isPlaying && a.clip != null)
        };
        File.WriteAllText(Path.Combine(output, "simulator-status.json"), JsonUtility.ToJson(report, true));
    }
    [Serializable] private class RuntimeReport
    {
        public string utc, loader, device, state, anonymousId;
        public bool syntheticSoftwareTest, physicalQuestVerified, xrActive, deviceValid, tracked,
            leftControllerValid, rightControllerValid, ambientAudioPlaying;
        public Vector3 headPosition;
    }
}
