using System.Data;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using TuiSample.UI.Layout;
using TuiSample.ViewModels;

namespace TuiSample.UI.Views;

/// <summary>
/// GraphView (棒グラフ) と集計テーブルを使って部署別・ロール別の統計を可視化するダッシュボードビュー。
/// </summary>
public sealed class DashboardView : View
{
    private readonly MainWindowViewModel _viewModel;
    private readonly GraphView _graphView;
    private readonly TableView _deptTableView;
    private readonly TableView _roleTableView;

    public DashboardView(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        this.Fill();

        // 左ペイン: GraphView による部署別ユーザ数 棒グラフ
        var graphFrame = new FrameView("部署別ユーザ数 棒グラフ (GraphView)");
        _graphView = new GraphView().Fill();
        graphFrame.Add(_graphView);

        // 右ペイン: 部署別・ロール別集計テーブル
        var statsFrame = new FrameView("集計サマリ内訳");

        _deptTableView = new TableView
        {
            FullRowSelect = true,
            MultiSelect = false
        }.FillWidth().WithHeight(11);
        _deptTableView.Style.AlwaysShowHeaders = true;
        _deptTableView.Style.ExpandLastColumn = true;

        _roleTableView = new TableView
        {
            FullRowSelect = true,
            MultiSelect = false
        }.FillWidth().WithHeight(6);
        _roleTableView.Style.AlwaysShowHeaders = true;
        _roleTableView.Style.ExpandLastColumn = true;

        statsFrame.Add(TuiLayout.VStack(
            gap: 1,
            paddingTop: 0,
            new Label("【部署別内訳】"),
            _deptTableView,
            new Label("【ロール別内訳】"),
            _roleTableView
        ));

        Add(TuiLayout.SplitHorizontal(
            leftPane: statsFrame,
            leftWidth: 44,
            rightPane: graphFrame,
            gap: 1
        ));

        RefreshDashboard();
    }

    public void RefreshDashboard()
    {
        var deptStats = _viewModel.GetDepartmentStats();
        var roleStats = _viewModel.GetRoleStats();

        // 1. 部署別集計テーブル更新
        var deptTable = new DataTable();
        deptTable.Columns.Add("部署名", typeof(string));
        deptTable.Columns.Add("合計", typeof(int));
        deptTable.Columns.Add("Active", typeof(int));
        deptTable.Columns.Add("割合グラフ", typeof(string));

        var totalUsers = Math.Max(1, _viewModel.TotalUserCount);
        foreach (var d in deptStats)
        {
            var barLen = Math.Max(1, (int)Math.Round((double)d.TotalCount / totalUsers * 12));
            var asciiBar = new string('█', barLen);
            deptTable.Rows.Add(d.Name, d.TotalCount, d.ActiveCount, asciiBar);
        }
        _deptTableView.Table = deptTable;

        // 2. ロール別集計テーブル更新
        var roleTable = new DataTable();
        roleTable.Columns.Add("ロール", typeof(string));
        roleTable.Columns.Add("合計", typeof(int));
        roleTable.Columns.Add("Active", typeof(int));
        foreach (var r in roleStats)
        {
            roleTable.Rows.Add(r.Name, r.TotalCount, r.ActiveCount);
        }
        _roleTableView.Table = roleTable;

        // 3. GraphView (BarSeries) 更新
        _graphView.Reset();
        _graphView.GraphColor = Application.Driver?.MakeAttribute(Color.BrightCyan, Color.Black);

        var barFill = new Terminal.Gui.Attribute(Color.BrightGreen, Color.Black);
        var barSeries = new BarSeries
        {
            Orientation = Orientation.Vertical,
            Offset = 1f,
            BarEvery = 2f
        };

        var bars = new List<BarSeries.Bar>();
        foreach (var d in deptStats.Take(8))
        {
            var shortName = d.Name.Length > 4 ? d.Name[..4] : d.Name;
            bars.Add(new BarSeries.Bar(shortName, new GraphCellToRender('\u2588', barFill), d.TotalCount));
        }

        barSeries.Bars = bars;
        _graphView.Series.Add(barSeries);

        _graphView.CellSize = new PointF(0.5f, 0.25f);
        _graphView.AxisX.Increment = 0f;
        _graphView.AxisX.ShowLabelsEvery = 0;
        _graphView.AxisX.Text = "部署";
        _graphView.AxisY.Increment = 1f;
        _graphView.AxisY.ShowLabelsEvery = 1;
        _graphView.AxisY.Minimum = 0f;

        _graphView.MarginBottom = 2;
        _graphView.MarginLeft = 3;
        _graphView.SetNeedsDisplay();
    }
}
