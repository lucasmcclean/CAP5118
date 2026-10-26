using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class RecorderTests
{
    private static int passed;
    private static string ReadShared(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
    }
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAILED: " + label);
        passed++;
        Console.WriteLine("PASS: " + label);
    }
    public static int Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(root);
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            bool rejected = false;
            try { new PilotSessionRecorder(root, "../escape", "ET_ROTATION", true, 2, "{}"); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "Reject participant IDs containing path traversal");
            string first, second;
            using (var recorder = new PilotSessionRecorder(root, "SIM_RECORDER", "ET_ROTATION", true, 2,
                "{\"softwareSimulation\":true,\"participantId\":\"SIM_RECORDER\",\"experiment\":\"ET_ROTATION\"," +
                "\"expectedMeasuredTrials\":2,\"steps\":2,\"steps2\":1,\"repetitions\":1,\"scaleInterpretationConfirmed\":false}"))
            {
                first = recorder.DirectoryPath;
                Check(first.Contains("SYNTHETIC_SOFTWARE_TESTS"), "Synthetic data explicitly separated from pilots");
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                recorder.WriteMotion(0, 1.25f, .5f, -.75f, .6f, -.8f, 40, 48);
                recorder.WriteEvent(0, "ES_ACTIVE");
                recorder.WriteResults(new List<string> { "0.6\t0\tRE_UP\t-3" }, true);
                Check(File.ReadAllText(Path.Combine(first, "session_status.json")).Contains("\"complete\":false"),
                    "Partial run cannot be marked complete");
                Check(ReadShared(Path.Combine(first, "motion.tsv")).Contains("1.25\t0.5\t-0.75"),
                    "Motion columns remain invariant under a comma-decimal locale");
                recorder.WriteResults(new List<string> { "0.6\t0\tRE_UP\t-3", "1.4\t0\tRE_DOWN\t3" }, true);
                Check(File.ReadAllText(Path.Combine(first, "session_status.json")).Contains("\"complete\":true"),
                    "Full run persists completion status");
            }
            using (var recorder = new PilotSessionRecorder(root, "SIM_RECORDER", "ET_ROTATION", true, 2, "{}"))
            {
                second = recorder.DirectoryPath;
                Check(first != second, "Repeating a participant ID creates a distinct session");
                recorder.WriteResults(new List<string>(), false);
            }
            Check(File.ReadAllLines(Path.Combine(first, "results.txt")).Length == 2,
                "New session does not overwrite earlier results");
            Check(File.ReadAllText(Path.Combine(second, "session_status.json")).Contains("\"complete\":false"),
                "Interrupted session remains explicitly incomplete");
            Check(File.ReadAllLines(Path.Combine(first, "events.tsv")).Length == 2,
                "Events are flushed and readable");
            Check(File.ReadAllLines(Path.Combine(first, "data.txt")).Length > 0,
                "Legacy data.txt is retained for the team's export workflow");
            File.WriteAllText(Path.Combine(root, "recorder-test-result.json"),
                "{\"passed\":" + passed + ",\"failed\":0,\"scope\":\"standalone recorder tests; not a headset test\"}");
            Console.WriteLine("Recorder tests passed: " + passed);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
