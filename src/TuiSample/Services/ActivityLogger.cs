namespace TuiSample.Services;

public sealed class ActivityLogger : IActivityLogger
{
    private readonly Lock _syncRoot = new();
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (_syncRoot)
            {
                return _entries.ToList();
            }
        }
    }

    public event EventHandler? LogUpdated;

    public ActivityLogger()
    {
        Log("SYSTEM", "アプリケーションを起動しました (MVVM + DI + Declarative Layout)。");
    }

    public void Log(string category, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var line = $"[{timestamp}] [{category,-7}] {message}";
        lock (_syncRoot)
        {
            _entries.Add(line);
        }
        LogUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _entries.Clear();
        }
        Log("SYSTEM", "操作ログをクリアしました。");
    }

    public string GetFormattedLogText()
    {
        lock (_syncRoot)
        {
            return string.Join(Environment.NewLine, _entries);
        }
    }
}

/// <summary>
/// テスト時やロガー不要時に副作用なしで扱える Null Object 実装。
/// </summary>
public sealed class NullActivityLogger : IActivityLogger
{
    public static NullActivityLogger Instance { get; } = new();

    public IReadOnlyList<string> Entries => [];

    public event EventHandler? LogUpdated
    {
        add { }
        remove { }
    }

    public void Log(string category, string message) { }
    public void Clear() { }
    public string GetFormattedLogText() => string.Empty;
}
