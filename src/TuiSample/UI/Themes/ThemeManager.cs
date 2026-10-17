using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace TuiSample.UI.Themes;

/// <summary>
/// アプリケーション全体のカラーテーマ切替と、TableView の条件付きセル色分けを管理するクラス。
/// </summary>
public static class ThemeManager
{
    public static IReadOnlyList<string> ThemeNames { get; } =
    [
        "Classic Blue",
        "Dark Cyan",
        "Matrix Green",
        "Retro Amber"
    ];

    public static int CurrentThemeIndex { get; private set; }

    public static string CurrentThemeName => ThemeNames[CurrentThemeIndex];

    public static string CycleNextTheme(params View[] targetViews)
    {
        var next = (CurrentThemeIndex + 1) % ThemeNames.Count;
        ApplyTheme(next, targetViews);
        return CurrentThemeName;
    }

    public static void ApplyTheme(int themeIndex, params View[] targetViews)
    {
        if (Application.Driver is null)
        {
            CurrentThemeIndex = Math.Clamp(themeIndex, 0, ThemeNames.Count - 1);
            return;
        }

        CurrentThemeIndex = Math.Clamp(themeIndex, 0, ThemeNames.Count - 1);
        var scheme = BuildColorScheme(CurrentThemeIndex);

        foreach (var view in targetViews)
        {
            view.ColorScheme = scheme;
            view.SetNeedsDisplay();
        }

        Application.Refresh();
    }

    public static ColorScheme GetInactiveRowScheme()
    {
        return new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
            HotNormal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
            HotFocus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
            Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black)
        };
    }

    public static ColorScheme GetAdminCellScheme()
    {
        return new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Blue),
            Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Blue),
            HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow),
            Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black)
        };
    }

    private static ColorScheme BuildColorScheme(int index)
    {
        return index switch
        {
            // 1: Dark Cyan
            1 => new ColorScheme
            {
                Normal = Attr(Color.BrightCyan, Color.Black),
                Focus = Attr(Color.Black, Color.BrightCyan),
                HotNormal = Attr(Color.BrightYellow, Color.Black),
                HotFocus = Attr(Color.BrightYellow, Color.Cyan),
                Disabled = Attr(Color.DarkGray, Color.Black)
            },
            // 2: Matrix Green
            2 => new ColorScheme
            {
                Normal = Attr(Color.BrightGreen, Color.Black),
                Focus = Attr(Color.Black, Color.BrightGreen),
                HotNormal = Attr(Color.White, Color.Black),
                HotFocus = Attr(Color.Black, Color.Green),
                Disabled = Attr(Color.DarkGray, Color.Black)
            },
            // 3: Retro Amber
            3 => new ColorScheme
            {
                Normal = Attr(Color.BrightYellow, Color.Black),
                Focus = Attr(Color.Black, Color.BrightYellow),
                HotNormal = Attr(Color.White, Color.Black),
                HotFocus = Attr(Color.Black, Color.Brown),
                Disabled = Attr(Color.DarkGray, Color.Black)
            },
            // 0: Classic Blue (Default)
            _ => Colors.Base
        };
    }

    private static Attribute Attr(Color fg, Color bg) => Application.Driver.MakeAttribute(fg, bg);
}

