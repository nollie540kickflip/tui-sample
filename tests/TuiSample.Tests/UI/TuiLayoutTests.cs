using Terminal.Gui;
using TuiSample.UI.Layout;
using Xunit;

namespace TuiSample.Tests.UI;

public sealed class TuiLayoutTests
{
    [Fact]
    public void HStack_ShouldChainChildrenHorizontally()
    {
        var label = new Label("キーワード:");
        var input = new TextField("").WithWidth(20);
        var button = new Button("クリア");

        var row = TuiLayout.HStack(gap: 2, paddingLeft: 1, label, input, button);

        Assert.Equal(3, row.Subviews.Count);
        Assert.NotNull(label.X);
        Assert.NotNull(input.X);
        Assert.NotNull(button.X);
        Assert.Equal(Pos.At(0), label.Y);
        Assert.Equal(Pos.At(0), input.Y);
        Assert.Equal(Pos.At(0), button.Y);
    }

    [Fact]
    public void VStack_ShouldChainChildrenVertically()
    {
        var row1 = new View();
        var row2 = new View();

        var stack = TuiLayout.VStack(gap: 1, paddingTop: 0, row1, row2);

        Assert.Equal(2, stack.Subviews.Count);
        Assert.Equal(Pos.At(0), row1.X);
        Assert.Equal(Pos.At(0), row2.X);
        Assert.Equal(Pos.At(0), row1.Y);
        Assert.NotEqual(Pos.At(0), row2.Y);
    }

    [Fact]
    public void SplitHorizontal_ShouldCreateLeftAndRightPanes()
    {
        var left = new View();
        var right = new View();

        var split = TuiLayout.SplitHorizontal(left, leftWidth: 26, right, gap: 1);

        Assert.Equal(2, split.Subviews.Count);
        Assert.Equal(Pos.At(0), left.X);
        Assert.Equal(Dim.Sized(26), left.Width);
        Assert.NotEqual(Pos.At(0), right.X);
    }

    [Fact]
    public void FormLayout_ShouldAddLabelsAndControlsWithoutManualCoordinates()
    {
        var nameField = new TextField("");
        var emailField = new TextField("");

        var form = new FormLayout(labelColumnWidth: 16, rowGap: 1)
            .AddField("氏名:", nameField)
            .AddField("メール:", emailField)
            .Build();

        Assert.Equal(4, form.Subviews.Count);
    }
}
