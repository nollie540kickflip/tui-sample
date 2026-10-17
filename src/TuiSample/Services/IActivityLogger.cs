namespace TuiSample.Services;

public interface IActivityLogger
{
    IReadOnlyList<string> Entries { get; }
    event EventHandler? LogUpdated;
    void Log(string category, string message);
    void Clear();
    string GetFormattedLogText();
}

