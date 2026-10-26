using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;

// Read-only connection evidence. Does not start trials or change XR settings.
[InitializeOnLoad]
public static class RotationHeadsetDiagnostics
{
    private static double nextSnapshot;
    static RotationHeadsetDiagnostics() { EditorApplication.update += Tick; }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextSnapshot ||
            SceneManager.GetActiveScene().path != "Assets/Scenes/Rotation.unity") return;
        nextSnapshot = EditorApplication.timeSinceStartup + 2;
        Evaluation evaluation = UnityEngine.Object.FindAnyObjectByType<Evaluation>();
        if (evaluation == null || evaluation.IsSoftwareSimulation) return;
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        bool tracked;
        Vector3 position;
        Quaternion rotation;
        head.TryGetFeatureValue(CommonUsages.isTracked, out tracked);
        head.TryGetFeatureValue(CommonUsages.devicePosition, out position);
        head.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        var report = new Report {
            utc = DateTime.UtcNow.ToString("o"), frame = Time.frameCount,
            loader = manager != null && manager.activeLoader != null ? manager.activeLoader.name : "none",
            device = head.name, headsetType = XRSettings.isDeviceActive ? OVRPlugin.GetSystemHeadsetType().ToString() : "none",
            xrActive = XRSettings.isDeviceActive, headValid = head.isValid, headTracked = tracked,
            leftControllerValid = left.isValid, rightControllerValid = right.isValid,
            ovrConnectedControllers = OVRInput.GetConnectedControllers().ToString(),
            leftPositionTracked = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch),
            rightPositionTracked = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch),
            leftOrientationTracked = OVRInput.GetControllerOrientationTracked(OVRInput.Controller.LTouch),
            rightOrientationTracked = OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch),
            headPosition = position, headYawDegrees = rotation.eulerAngles.y,
            scene = SceneManager.GetActiveScene().path, state = evaluation.State.ToString(),
            sessionStarted = !string.IsNullOrEmpty(evaluation.SessionDirectory),
            scaleInterpretationConfirmed = evaluation.ScaleInterpretationConfirmed,
            ambientAudioPlaying = UnityEngine.Object.FindObjectsByType<AudioSource>().Any(a => a.isPlaying && a.clip != null),
            selectedRuntimeOverride = Environment.GetEnvironmentVariable("XR_SELECTED_RUNTIME_JSON") ?? "",
            runtimeOverride = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON") ?? "",
            pilotVerified = false
        };
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../QA/QuestConnection"));
        try {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "connection-status.json"), JsonUtility.ToJson(report, true));
        } catch (IOException error) { Debug.LogWarning("Headset diagnostic write failed: " + error.Message); }
    }

    [Serializable] private class Report
    {
        public string utc, loader, device, headsetType, scene, state, selectedRuntimeOverride, runtimeOverride, ovrConnectedControllers;
        public int frame;
        public bool xrActive, headValid, headTracked, leftControllerValid, rightControllerValid,
            sessionStarted, scaleInterpretationConfirmed, ambientAudioPlaying, pilotVerified,
            leftPositionTracked, rightPositionTracked, leftOrientationTracked, rightOrientationTracked;
        public Vector3 headPosition;
        public float headYawDegrees;
    }
}
