// Standalone test doubles for managed trial/input logic only. Not Unity/XR simulation.
// This file is outside Assets and is never compiled into the game.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
namespace UnityEngine
{
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) {} }
    public class MonoBehaviour
    {
        public GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
    }
    public sealed class GameObject
    {
        private readonly Dictionary<Type, object> components = new Dictionary<Type, object>();
        public Scene scene;
        public T GetComponent<T>() where T : class { object value; return components.TryGetValue(typeof(T), out value) ? (T)value : null; }
        public T AddComponent<T>() where T : class, new() { T value = new T(); components[typeof(T)] = value; return value; }
    }
    public struct Scene { public string path; }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public float sqrMagnitude { get { return x*x + y*y; } }
        public static Vector2 operator -(Vector2 value) { return new Vector2(-value.x, -value.y); }
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static int Clamp(int value, int min, int max) { return Math.Min(max, Math.Max(min, value)); }
        public static int Min(int first, int second) { return Math.Min(first, second); }
    }
    public static class Time { public static float realtimeSinceStartup = 1; }
    public static class Application { public static string persistentDataPath; public static string unityVersion = "STANDALONE_TEST_DOUBLE"; }
    public static class Debug { public static void Log(object value) {} public static void LogError(object value) {} }
    public static class JsonUtility
    {
        public static string ToJson(object value, bool pretty)
        {
            var fields = new List<string>();
            foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object item = field.GetValue(value);
                string text = item == null ? "null" : item is string ? Quote((string)item) : item is bool ? ((bool)item ? "true" : "false") : Convert.ToString(item, CultureInfo.InvariantCulture);
                fields.Add(Quote(field.Name) + ": " + text);
            }
            return "{" + string.Join(",", fields) + "}";
        }
        private static string Quote(string value) { return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    }
}
namespace UnityEngine.Serialization
{
    public sealed class FormerlySerializedAsAttribute : Attribute { public FormerlySerializedAsAttribute(string value) {} }
}
namespace UnityEngine.XR
{
    public enum XRNode { LeftHand, RightHand }
    public struct InputFeatureUsage<T> {}
    public struct InputDevice
    {
        public bool isValid { get { return false; } }
        public bool TryGetFeatureValue<T>(InputFeatureUsage<T> usage, out T value) { value = default(T); return false; }
    }
    public static class InputDevices { public static InputDevice GetDeviceAtXRNode(XRNode node) { return new InputDevice(); } }
    public static class CommonUsages
    {
        public static InputFeatureUsage<bool> primaryButton, secondaryButton, triggerButton;
        public static InputFeatureUsage<UnityEngine.Vector2> primary2DAxis;
    }
}
public sealed class PilotParticipantEntry : UnityEngine.MonoBehaviour {}
public static class OVRInput
{
    public enum Button { One, Two, PrimaryIndexTrigger }
    public enum Axis2D { PrimaryThumbstick, SecondaryThumbstick }
    public static bool Get(Button button) { return false; }
    public static UnityEngine.Vector2 Get(Axis2D axis) { return UnityEngine.Vector2.zero; }
}
