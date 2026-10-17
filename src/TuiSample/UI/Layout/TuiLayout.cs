using Terminal.Gui;

namespace TuiSample.UI.Layout;

/// <summary>
/// Terminal.Gui の画面構築から X / Y 座標の直接指定をなくすための宣言的レイアウトヘルパー。
/// HStack（横並び）、VStack（縦並び）、SplitHorizontal（左右2ペイン分割）、DockTopFillBottom（上下ドック分割）、FormLayout（フォーム行配置）を提供します。
/// </summary>
public static class TuiLayout
{
    /// <summary>
    /// ウィンドウをメニューバーとステータスバーを除いたクライアント領域いっぱいに広げます。
    /// </summary>
    public static void StretchMainWindow(Window window)
    {
        window.X = 0;
        window.Y = 1; // Row 0 is reserved for MenuBar
        window.Width = Dim.Fill();
        window.Height = Dim.Fill(1); // Bottom row is reserved for StatusBar
    }

    /// <summary>
    /// コントロールに固定の横幅を指定します（メソッドチェーン用）。
    /// </summary>
    public static T WithWidth<T>(this T view, int width) where T : View
    {
        view.Width = width;
        return view;
    }

    /// <summary>
    /// コントロールに固定の高さを指定します（メソッドチェーン用）。
    /// </summary>
    public static T WithHeight<T>(this T view, int height) where T : View
    {
        view.Height = height;
        return view;
    }

    /// <summary>
    /// コントロールを親コンテナの残り幅いっぱいに広げます（メソッドチェーン用）。
    /// </summary>
    public static T FillWidth<T>(this T view, int margin = 0) where T : View
    {
        view.Width = Dim.Fill(margin);
        return view;
    }

    /// <summary>
    /// コントロールを親コンテナいっぱいに広げます（メソッドチェーン用）。
    /// </summary>
    public static T Fill<T>(this T view) where T : View
    {
        view.X = 0;
        view.Y = 0;
        view.Width = Dim.Fill();
        view.Height = Dim.Fill();
        return view;
    }

    /// <summary>
    /// 子要素を左から右へ（水平方向に）等間隔で並べる 1 行コンテナを生成します。
    /// </summary>
    public static View HStack(int gap = 1, int paddingLeft = 1, params View[] children)
    {
        var row = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };

        View? previous = null;
        foreach (var child in children)
        {
            child.Y = 0;
            child.X = previous is null ? paddingLeft : Pos.Right(previous) + gap;
            row.Add(child);
            previous = child;
        }

        return row;
    }

    /// <summary>
    /// 子要素を上から下へ（垂直方向に）等間隔で積むコンテナを生成します。
    /// </summary>
    public static View VStack(int gap = 1, int paddingTop = 0, params View[] children)
    {
        var stack = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        View? previous = null;
        foreach (var child in children)
        {
            child.X = 0;
            child.Y = previous is null ? paddingTop : Pos.Bottom(previous) + gap;
            stack.Add(child);
            previous = child;
        }

        return stack;
    }

    /// <summary>
    /// 領域を「左ペイン（固定幅）」と「右ペイン（残り幅 Fill）」の左右2ペインに分割します。
    /// </summary>
    public static View SplitHorizontal(View leftPane, int leftWidth, View rightPane, int gap = 1)
    {
        var container = new View().Fill();

        leftPane.X = 0;
        leftPane.Y = 0;
        leftPane.Width = leftWidth;
        leftPane.Height = Dim.Fill();

        rightPane.X = Pos.Right(leftPane) + gap;
        rightPane.Y = 0;
        rightPane.Width = Dim.Fill();
        rightPane.Height = Dim.Fill();

        container.Add(leftPane, rightPane);
        return container;
    }

    /// <summary>
    /// 領域を「上部可変（残り領域 Fill）」と「下部固定アクションバー（1行）」の2段に配置します。
    /// </summary>
    public static View DockFillAndBottomBar(View fillContent, View bottomBar)
    {
        var container = new View().Fill();

        fillContent.X = 0;
        fillContent.Y = 0;
        fillContent.Width = Dim.Fill();
        fillContent.Height = Dim.Fill(2);

        bottomBar.X = 0;
        bottomBar.Y = Pos.Bottom(fillContent) + 1;
        bottomBar.Width = Dim.Fill();
        bottomBar.Height = 1;

        container.Add(fillContent, bottomBar);
        return container;
    }

    /// <summary>
    /// 画面を「上部固定パネル」「中央可変（残り領域 Fill）」「下部1行サマリ」の3段に自動配置します。
    /// </summary>
    public static void DockTopFillBottom(View parent, View top, int topHeight, View centerFill, View bottom)
    {
        top.X = 0;
        top.Y = 0;
        top.Width = Dim.Fill();
        top.Height = topHeight;

        centerFill.X = 0;
        centerFill.Y = Pos.Bottom(top);
        centerFill.Width = Dim.Fill();
        centerFill.Height = Dim.Fill(1);

        bottom.X = 1;
        bottom.Y = Pos.Bottom(centerFill);
        bottom.Width = Dim.Fill();
        bottom.Height = 1;

        parent.Add(top, centerFill, bottom);
    }
}

/// <summary>
/// ダイアログ内の「ラベル ＋ 入力コントロール」行を上から順番に自動整列するフォームビルダー。
/// </summary>
public sealed class FormLayout
{
    private readonly View _container;
    private readonly int _labelColumnWidth;
    private readonly int _rowGap;
    private readonly int _paddingLeft;
    private readonly int _rightMargin;
    private View? _lastAnchor;

    public FormLayout(int labelColumnWidth = 16, int rowGap = 1, int paddingLeft = 2, int rightMargin = 3)
    {
        _labelColumnWidth = labelColumnWidth;
        _rowGap = rowGap;
        _paddingLeft = paddingLeft;
        _rightMargin = rightMargin;

        _container = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2) // Leave room for dialog buttons at bottom
        };
    }

    /// <summary>
    /// 1行の入力フィールド（TextField など）をラベル付きで追加します。
    /// </summary>
    public FormLayout AddField(string labelText, View inputControl)
    {
        var y = NextRowY();

        var label = new Label(labelText)
        {
            X = _paddingLeft,
            Y = y
        };

        inputControl.X = _paddingLeft + _labelColumnWidth;
        inputControl.Y = y;
        inputControl.Width = Dim.Fill(_rightMargin);

        _container.Add(label, inputControl);
        _lastAnchor = inputControl;
        return this;
    }

    /// <summary>
    /// 複数行の高さを持つ選択コントロール（縦並び RadioGroup など）をラベル付きで追加します。
    /// </summary>
    public FormLayout AddChoice(string labelText, View choiceControl, int itemCount)
    {
        var y = NextRowY();

        var label = new Label(labelText)
        {
            X = _paddingLeft,
            Y = y
        };

        choiceControl.X = _paddingLeft + _labelColumnWidth;
        choiceControl.Y = y;
        choiceControl.Height = Math.Max(1, itemCount);

        _container.Add(label, choiceControl);
        _lastAnchor = choiceControl;
        return this;
    }

    /// <summary>
    /// ラベルのすぐ下の行に、横幅いっぱいの入力欄と補助ボタン（ファイル参照など）を並べて追加します。
    /// </summary>
    public FormLayout AddStackedFieldWithButton(string labelText, View inputControl, Button actionButton)
    {
        var y = NextRowY();

        var label = new Label(labelText)
        {
            X = _paddingLeft,
            Y = y
        };

        inputControl.X = _paddingLeft;
        inputControl.Y = Pos.Bottom(label);
        inputControl.Width = Dim.Fill(_rightMargin);

        actionButton.X = _paddingLeft;
        actionButton.Y = Pos.Bottom(inputControl) + 1;

        _container.Add(label, inputControl, actionButton);
        _lastAnchor = actionButton;
        return this;
    }

    /// <summary>
    /// 任意の全幅コントロール（プログレスバーやステータステキストなど）を1行追加します。
    /// </summary>
    public FormLayout AddFullWidthRow(View control, int height = 1)
    {
        var y = NextRowY();
        control.X = _paddingLeft;
        control.Y = y;
        control.Width = Dim.Fill(_rightMargin);
        control.Height = height;

        _container.Add(control);
        _lastAnchor = control;
        return this;
    }

    public View Build() => _container;

    private Pos NextRowY() => _lastAnchor is null ? 1 : Pos.Bottom(_lastAnchor) + _rowGap;
}
