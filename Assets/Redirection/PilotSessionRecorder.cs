using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// No Unity dependency: the same recorder is exercised by standalone and Editor tests.
public sealed class PilotSessionRecorder : IDisposable
{
    public string DirectoryPath { get; private set; }
    private readonly StreamWriter legacyMotion, motion, events;
    private readonly int expectedTrials;
    private int completedTrials;
    private bool complete, disposed;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public PilotSessionRecorder(string root, string participantId, string experiment,
        bool softwareSimulation, int expectedTrials, string metadataJson)
    {
        if (!Regex.IsMatch(participantId ?? "", "^[A-Za-z0-9_-]{1,32}$"))
            throw new ArgumentException("Invalid anonymous participant ID.");
        if (!Regex.IsMatch(experiment ?? "", "^[A-Za-z0-9_]+$"))
            throw new ArgumentException("Invalid experiment name.");
        if (expectedTrials < 1) throw new ArgumentOutOfRangeException(nameof(expectedTrials));
        this.expectedTrials = expectedTrials;
        string category = softwareSimulation ? "SYNTHETIC_SOFTWARE_TESTS" : "PILOT";
        string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", Invariant);
        DirectoryPath = Path.Combine(Path.GetFullPath(root), category, experiment,
            participantId + "_" + stamp + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(Path.Combine(DirectoryPath, "session.json"), metadataJson, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(DirectoryPath, "results.txt"), "", new UTF8Encoding(false));
        legacyMotion = Writer("data.txt");
        motion = Writer("motion.tsv");
        events = Writer("events.tsv");
        motion.WriteLine("trial\ttime_seconds\treal_x\treal_z\tvirtual_x\tvirtual_z\treal_yaw_degrees\tvirtual_yaw_degrees");
        events.WriteLine("utc\ttrial\tevent");
        WriteStatus();
    }

    private StreamWriter Writer(string filename)
    {
        return new StreamWriter(Path.Combine(DirectoryPath, filename), false, new UTF8Encoding(false));
    }

    public void WriteResults(IList<string> rows, bool finished)
    {
        if (disposed) throw new ObjectDisposedException(nameof(PilotSessionRecorder));
        if (rows.Count > expectedTrials) throw new InvalidOperationException("Too many result rows.");
        File.WriteAllLines(Path.Combine(DirectoryPath, "results.txt"), rows, new UTF8Encoding(false));
        completedTrials = rows.Count;
        complete = finished && completedTrials == expectedTrials;
        Flush();
        WriteStatus();
    }

    public void WriteMotion(int trial, float time, float realX, float realZ,
        float virtualX, float virtualZ, float realYaw, float virtualYaw)
    {
        if (disposed) throw new ObjectDisposedException(nameof(PilotSessionRecorder));
        string[] values = { F(realX), F(realZ), F(virtualX), F(virtualZ), F(realYaw), F(virtualYaw) };
        string line = string.Join("\t", values);
        legacyMotion.WriteLine(line);
        motion.WriteLine(trial.ToString(Invariant) + "\t" + F(time) + "\t" + line);
    }

    public void WriteEvent(int trial, string value)
    {
        if (disposed) throw new ObjectDisposedException(nameof(PilotSessionRecorder));
        value = value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        events.WriteLine(DateTime.UtcNow.ToString("o", Invariant) + "\t" + trial.ToString(Invariant) + "\t" + value);
        legacyMotion.WriteLine("\n" + value + "\n");
        Flush();
    }

    private static string F(float value) { return value.ToString("R", Invariant); }
    public void Flush() { legacyMotion.Flush(); motion.Flush(); events.Flush(); }
    private void WriteStatus()
    {
        File.WriteAllText(Path.Combine(DirectoryPath, "session_status.json"),
            "{\"completedMeasuredTrials\":" + completedTrials + ",\"expectedMeasuredTrials\":" + expectedTrials +
            ",\"complete\":" + (complete ? "true" : "false") + "}", new UTF8Encoding(false));
    }

    public void Dispose()
    {
        if (disposed) return;
        Flush();
        legacyMotion.Dispose(); motion.Dispose(); events.Dispose();
        WriteStatus();
        disposed = true;
    }
}
