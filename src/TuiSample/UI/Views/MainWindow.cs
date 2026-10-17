using System.ComponentModel;
using System.Data;
using NStack;
using Terminal.Gui;
using Terminal.Gui.Trees;
using TuiSample.Models;
using TuiSample.UI.Dialogs;
using TuiSample.UI.Layout;
using TuiSample.UI.Themes;
using TuiSample.ViewModels;

namespace TuiSample.UI.Views;

public sealed class MainWindow : Window
{
    private enum TreeFilterKind
    {
        All,
        GroupHeader,
        Department,
        Role
    }

    private sealed class OrgNode : TreeNode
    {
        public TreeFilterKind Kind { get; }
        public string? DepartmentValue { get; }
        public UserRole? RoleValue { get; }

        public OrgNode(string text, TreeFilterKind kind, string? departmentValue = null, UserRole? roleValue = null)
            : base(text)
        {
            Kind = kind;
            DepartmentValue = departmentValue;
            RoleValue = roleValue;
        }
    }

    private readonly MainWindowViewModel _viewModel;
    private readonly TabView _tabView;
    private readonly TextField _searchField;
    private readonly RadioGroup _statusFilterGroup;
    private readonly TreeView _orgTreeView;
    private readonly TableView _tableView;
    private readonly DashboardView _dashboardView;
    private readonly TextView _logTextView;
    private readonly Label _summaryLabel;
    private readonly StatusItem _statusCountItem;
    private readonly StatusItem _themeStatusItem;
    private readonly ContextMenu _gridContextMenu;

    private bool _suppressViewEvents;

    public MainWindow(MainWindowViewModel viewModel)
        : base("C# TUI フル機能ショーケース (Terminal.Gui + MVVM/DI + Declarative Layout)")
    {
        _viewModel = viewModel;
        TuiLayout.StretchMainWindow(this);

        // --- 上部: 検索・フィルタ & アクションツールバー (HStack + VStack) ---
        _searchField = new TextField(_viewModel.SearchKeyword).WithWidth(24);
        _searchField.TextChanged += _ =>
        {
            if (_suppressViewEvents) return;
            _viewModel.SearchKeyword = _searchField.Text?.ToString() ?? string.Empty;
        };

        var clearSearchBtn = new Button("フィルタ解除");
        clearSearchBtn.Clicked += () =>
        {
            _viewModel.ClearFilterCommand.Execute(null);
            RebuildOrgTree();
        };

        _statusFilterGroup = new RadioGroup(MainWindowViewModel.StatusFilterOptions.Select(ustring.Make).ToArray())
        {
            DisplayMode = DisplayModeLayout.Horizontal,
            SelectedItem = _viewModel.SelectedStatusFilterIndex
        };
        _statusFilterGroup.SelectedItemChanged += args =>
        {
            if (_suppressViewEvents) return;
            _viewModel.SelectedStatusFilterIndex = args.SelectedItem;
        };

        var addBtn = new Button("+追加(F2)");
        addBtn.Clicked += ShowAddUserDialog;

        var editBtn = new Button("編集(F3)");
        editBtn.Clicked += ShowEditUserDialog;

        var deleteBtn = new Button("-削除(F4)");
        deleteBtn.Clicked += DeleteSelectedUser;

        var exportCsvBtn = new Button("CSV出力(F5)");
        exportCsvBtn.Clicked += ShowExportCsvDialog;

        var importCsvBtn = new Button("CSV取込");
        importCsvBtn.Clicked += ShowImportCsvDialog;

        var batchBtn = new Button("一括生成(F8)");
        batchBtn.Clicked += ShowBulkGenerateDialog;

        var themeBtn = new Button("テーマ(F7)");
        themeBtn.Clicked += CycleTheme;

        var searchRow = TuiLayout.HStack(
            gap: 2,
            paddingLeft: 1,
            new Label("検索:"),
            _searchField,
            clearSearchBtn,
            new Label("状態:"),
            _statusFilterGroup
        );

        var actionRow = TuiLayout.HStack(
            gap: 1,
            paddingLeft: 1,
            addBtn,
            editBtn,
            deleteBtn,
            exportCsvBtn,
            importCsvBtn,
            batchBtn,
            themeBtn
        );

        var filterFrame = new FrameView("検索・絞り込み & アクション");
        filterFrame.Add(TuiLayout.VStack(gap: 1, paddingTop: 0, searchRow, actionRow));

        // --- 中央: TabView (3つのタブ) ---
        _tabView = new TabView().Fill();

        // [Tab 1] ユーザ管理 (左: 部署・ロール TreeView / 右: 条件付き色分け TableView)
        _orgTreeView = new TreeView().Fill();
        _orgTreeView.SelectionChanged += OnTreeSelectionChanged;
        var treeFrame = new FrameView("階層ナビ (TreeView)");
        treeFrame.Add(_orgTreeView);

        _tableView = new TableView
        {
            FullRowSelect = true,
            MultiSelect = false
        }.Fill();

        _tableView.Style.AlwaysShowHeaders = true;
        _tableView.Style.ShowHorizontalHeaderOverline = true;
        _tableView.Style.ShowHorizontalHeaderUnderline = true;
        _tableView.Style.ShowVerticalCellLines = true;
        _tableView.Style.ExpandLastColumn = true;

        // 条件付き行色分け: Inactive 行をグレーアウト表示
        _tableView.Style.RowColorGetter = args =>
        {
            if (args.RowIndex >= 0 && args.RowIndex < _viewModel.DisplayedUsers.Count)
            {
                var u = _viewModel.DisplayedUsers[args.RowIndex];
                if (u.Status == UserStatus.Inactive)
                {
                    return ThemeManager.GetInactiveRowScheme();
                }
            }
            return null;
        };

        _tableView.CellActivated += _ => ShowEditUserDialog();

        // 右クリック コンテキストメニュー (ContextMenu)
        _gridContextMenu = new ContextMenu
        {
            MenuItems = new MenuBarItem([
                new MenuItem("編集 (Enter/F3)", "", ShowEditUserDialog),
                new MenuItem("ステータス切替 (Active/Inactive)", "", ToggleSelectedUserStatus),
                new MenuItem("このユーザを複製", "", DuplicateSelectedUser),
                null!,
                new MenuItem("削除 (F4)", "", DeleteSelectedUser)
            ])
        };

        _tableView.MouseClick += args =>
        {
            if (args.MouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked))
            {
                _gridContextMenu.Position = new Point(args.MouseEvent.X, args.MouseEvent.Y);
                _gridContextMenu.Show();
                args.Handled = true;
            }
        };

        var gridFrame = new FrameView("ユーザ一覧 (Enter:編集 / 右クリック:メニュー / F6:ソート)");
        gridFrame.Add(_tableView);

        var masterDetailView = TuiLayout.SplitHorizontal(
            leftPane: treeFrame,
            leftWidth: 26,
            rightPane: gridFrame,
            gap: 1
        );

        // [Tab 2] 統計ダッシュボード (GraphView)
        _dashboardView = new DashboardView(_viewModel);

        // [Tab 3] 操作ログ (TextView)
        _logTextView = new TextView
        {
            ReadOnly = true,
            WordWrap = true,
            Text = _viewModel.Logger.GetFormattedLogText()
        }.Fill();

        var clearLogBtn = new Button("ログをクリア");
        clearLogBtn.Clicked += () => _viewModel.Logger.Clear();

        var logTabContent = TuiLayout.DockFillAndBottomBar(
            fillContent: _logTextView,
            bottomBar: TuiLayout.HStack(gap: 2, paddingLeft: 1, clearLogBtn, new Label("※すべての操作履歴がリアルタイムに記録されます"))
        );

        _tabView.AddTab(new TabView.Tab(" 1.ユーザ管理 (Grid/Tree) ", masterDetailView), andSelect: true);
        _tabView.AddTab(new TabView.Tab(" 2.統計ダッシュボード (Graph) ", _dashboardView), andSelect: false);
        _tabView.AddTab(new TabView.Tab(" 3.操作ログ (Log) ", logTabContent), andSelect: false);

        // --- 下部: 集計サマリ行 ---
        _summaryLabel = new Label("");

        TuiLayout.DockTopFillBottom(
            parent: this,
            top: filterFrame,
            topHeight: 6,
            centerFill: _tabView,
            bottom: _summaryLabel
        );

        _statusCountItem = new StatusItem(Key.Null, _viewModel.StatusCountText, null);
        _themeStatusItem = new StatusItem(Key.F7, $"~F7~ テーマ:{ThemeManager.CurrentThemeName}", CycleTheme);

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.Logger.LogUpdated += OnLogUpdated;

        RebuildOrgTree();
        SyncViewFromViewModel();
    }

    public MenuBar CreateMenuBar()
    {
        return new MenuBar([
            new MenuBarItem("_ファイル (F)", [
                new MenuItem("_CSVダウンロード...", "一覧をCSVに出力 (F5)", ShowExportCsvDialog),
                new MenuItem("_CSVインポート...", "CSVファイルから取込", ShowImportCsvDialog),
                null!,
                new MenuItem("_終了 (Quit)", "アプリを終了 (Ctrl+Q)", () => Application.RequestStop())
            ]),
            new MenuBarItem("_ユーザ操作 (U)", [
                new MenuItem("_新規追加...", "ユーザを追加 (F2)", ShowAddUserDialog),
                new MenuItem("_編集...", "選択行を編集 (F3)", ShowEditUserDialog),
                new MenuItem("_ステータス切替", "Active/Inactive切替", ToggleSelectedUserStatus),
                new MenuItem("_選択ユーザを複製", "コピーを作成", DuplicateSelectedUser),
                new MenuItem("_削除...", "選択行を削除 (F4)", DeleteSelectedUser),
                null!,
                new MenuItem("_ダミーデータ一括生成...", "非同期プログレスバーデモ (F8)", ShowBulkGenerateDialog),
                new MenuItem("_ソート順切替", "ID/氏名/部署/登録日でソート (F6)", CycleSortAndKeepSelection)
            ]),
            new MenuBarItem("_表示・タブ (V)", [
                new MenuItem("_1. ユーザ管理タブ", "", () => _tabView.SelectedTab = _tabView.Tabs.ElementAtOrDefault(0)),
                new MenuItem("_2. 統計ダッシュボードタブ", "", () => _tabView.SelectedTab = _tabView.Tabs.ElementAtOrDefault(1)),
                new MenuItem("_3. 操作ログタブ", "", () => _tabView.SelectedTab = _tabView.Tabs.ElementAtOrDefault(2)),
                null!,
                new MenuItem("_カラーテーマ切替", "Classic/Dark/Matrix/Amber (F7)", CycleTheme)
            ]),
            new MenuBarItem("_ヘルプ (H)", [
                new MenuItem("_使い方・全機能ガイド", "このTUIショーケースについて", ShowHelpDialog)
            ])
        ]);
    }

    public StatusBar CreateStatusBar()
    {
        return new StatusBar([
            new StatusItem(Key.F2, "~F2~ 追加", ShowAddUserDialog),
            new StatusItem(Key.F3, "~F3~ 編集", ShowEditUserDialog),
            new StatusItem(Key.F4, "~F4~ 削除", DeleteSelectedUser),
            new StatusItem(Key.F5, "~F5~ CSV出力", ShowExportCsvDialog),
            new StatusItem(Key.F6, "~F6~ ソート", CycleSortAndKeepSelection),
            _themeStatusItem,
            new StatusItem(Key.F8, "~F8~ 一括生成", ShowBulkGenerateDialog),
            new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ 終了", () => Application.RequestStop()),
            _statusCountItem
        ]);
    }

    private void OnLogUpdated(object? sender, EventArgs e)
    {
        _logTextView.Text = _viewModel.Logger.GetFormattedLogText();
        _logTextView.MoveEnd();
    }

    private void RebuildOrgTree()
    {
        _orgTreeView.ClearObjects();

        var allNode = new OrgNode($"すべて ({_viewModel.TotalUserCount})", TreeFilterKind.All);

        var deptRoot = new OrgNode("部署別", TreeFilterKind.GroupHeader);
        foreach (var dept in _viewModel.GetDepartmentStats())
        {
            deptRoot.Children.Add(new OrgNode($"{dept.Name} ({dept.TotalCount})", TreeFilterKind.Department, departmentValue: dept.Name));
        }

        var roleRoot = new OrgNode("ロール別", TreeFilterKind.GroupHeader);
        foreach (var role in _viewModel.GetRoleStats())
        {
            if (Enum.TryParse<UserRole>(role.Name, out var parsedRole))
            {
                roleRoot.Children.Add(new OrgNode($"{role.Name} ({role.TotalCount})", TreeFilterKind.Role, roleValue: parsedRole));
            }
        }

        _orgTreeView.AddObjects([allNode, deptRoot, roleRoot]);
        _orgTreeView.ExpandAll();
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs<ITreeNode> e)
    {
        if (_suppressViewEvents || e.NewValue is not OrgNode node)
        {
            return;
        }

        switch (node.Kind)
        {
            case TreeFilterKind.All:
                _viewModel.ApplyTreeFilter(department: null, role: null);
                break;
            case TreeFilterKind.Department:
                _viewModel.ApplyTreeFilter(department: node.DepartmentValue, role: null);
                break;
            case TreeFilterKind.Role:
                _viewModel.ApplyTreeFilter(department: null, role: node.RoleValue);
                break;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.DisplayedUsers)
            or nameof(MainWindowViewModel.SummaryText)
            or nameof(MainWindowViewModel.StatusCountText)
            or nameof(MainWindowViewModel.SearchKeyword)
            or nameof(MainWindowViewModel.SelectedStatusFilterIndex))
        {
            SyncViewFromViewModel();
        }
    }

    private void SyncViewFromViewModel(int? selectUserId = null)
    {
        _suppressViewEvents = true;
        try
        {
            if ((_searchField.Text?.ToString() ?? string.Empty) != _viewModel.SearchKeyword)
            {
                _searchField.Text = _viewModel.SearchKeyword;
            }

            if (_statusFilterGroup.SelectedItem != _viewModel.SelectedStatusFilterIndex)
            {
                _statusFilterGroup.SelectedItem = _viewModel.SelectedStatusFilterIndex;
            }
        }
        finally
        {
            _suppressViewEvents = false;
        }

        var table = new DataTable();
        table.Columns.Add("ID", typeof(int));
        table.Columns.Add("氏名", typeof(string));
        table.Columns.Add("メールアドレス", typeof(string));
        table.Columns.Add("部署", typeof(string));
        var roleCol = table.Columns.Add("ロール", typeof(string));
        table.Columns.Add("状態", typeof(string));
        table.Columns.Add("登録日", typeof(string));

        foreach (var u in _viewModel.DisplayedUsers)
        {
            table.Rows.Add(
                u.Id,
                u.Name,
                u.Email,
                u.Department,
                u.Role.ToString(),
                u.Status.ToString(),
                u.CreatedAt.ToString("yyyy-MM-dd")
            );
        }

        _tableView.Table = table;

        // Admin ロールのセルを条件付き色分け
        var roleStyle = _tableView.Style.GetOrCreateColumnStyle(roleCol);
        roleStyle.ColorGetter = args =>
        {
            var cellText = args.CellValue?.ToString();
            if (string.Equals(cellText, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase))
            {
                return ThemeManager.GetAdminCellScheme();
            }
            return null;
        };

        if (selectUserId.HasValue)
        {
            var index = _viewModel.FindRowIndexByUserId(selectUserId.Value);
            if (index >= 0)
            {
                _tableView.SelectedRow = index;
                _tableView.EnsureSelectedCellIsVisible();
            }
        }

        _summaryLabel.Text = _viewModel.SummaryText;
        _statusCountItem.Title = _viewModel.StatusCountText;
        _dashboardView.RefreshDashboard();
        SetNeedsDisplay();
    }

    private void CycleTheme()
    {
        var newTheme = ThemeManager.CycleNextTheme(this, _tabView, _tableView, _orgTreeView, _dashboardView, _logTextView);
        _themeStatusItem.Title = $"~F7~ テーマ:{newTheme}";
        _viewModel.Logger.Log("THEME", $"カラーテーマを「{newTheme}」に切り替えました。");
        SetNeedsDisplay();
    }

    private void CycleSortAndKeepSelection()
    {
        var currentUser = GetSelectedUser();
        _viewModel.CycleSortCommand.Execute(null);
        if (currentUser is not null)
        {
            SyncViewFromViewModel(currentUser.Id);
        }
    }

    private User? GetSelectedUser() => _viewModel.GetUserAtRow(_tableView.SelectedRow);

    private void ShowAddUserDialog()
    {
        var formVm = new UserFormViewModel();
        using var dialog = new UserFormDialog(formVm);
        Application.Run(dialog);

        if (dialog.IsSaved)
        {
            var added = _viewModel.AddUser(dialog.ResultUser);
            RebuildOrgTree();
            SyncViewFromViewModel(added.Id);
            _tableView.SetFocus();
        }
    }

    private void ShowEditUserDialog()
    {
        var selected = GetSelectedUser();
        if (selected is null)
        {
            MessageBox.Query("編集", "編集するユーザを行選択してください。", "OK");
            return;
        }

        var formVm = new UserFormViewModel(selected);
        using var dialog = new UserFormDialog(formVm);
        Application.Run(dialog);

        if (dialog.IsSaved)
        {
            _viewModel.UpdateUser(dialog.ResultUser);
            RebuildOrgTree();
            SyncViewFromViewModel(selected.Id);
            _tableView.SetFocus();
        }
    }

    private void ToggleSelectedUserStatus()
    {
        var selected = GetSelectedUser();
        if (selected is null)
        {
            return;
        }

        _viewModel.ToggleUserStatus(selected.Id);
        RebuildOrgTree();
        SyncViewFromViewModel(selected.Id);
    }

    private void DuplicateSelectedUser()
    {
        var selected = GetSelectedUser();
        if (selected is null)
        {
            return;
        }

        var copy = _viewModel.DuplicateUser(selected.Id);
        if (copy is not null)
        {
            RebuildOrgTree();
            SyncViewFromViewModel(copy.Id);
        }
    }

    private void DeleteSelectedUser()
    {
        var selected = GetSelectedUser();
        if (selected is null)
        {
            MessageBox.Query("削除", "削除するユーザを行選択してください。", "OK");
            return;
        }

        var confirm = MessageBox.Query(
            "削除の確認",
            $"ID: {selected.Id} 「{selected.Name}」({selected.Email}) を削除しますか？",
            "削除する", "キャンセル");

        if (confirm == 0)
        {
            _viewModel.DeleteUser(selected.Id);
            RebuildOrgTree();
            _tableView.SetFocus();
        }
    }

    private void ShowBulkGenerateDialog()
    {
        using var dialog = new BulkGenerateDialog(_viewModel, targetCount: 30);
        Application.Run(dialog);

        if (dialog.GeneratedCount > 0)
        {
            RebuildOrgTree();
            SyncViewFromViewModel();
        }
    }

    private void ShowExportCsvDialog()
    {
        using var dialog = new ExportCsvDialog(_viewModel);
        Application.Run(dialog);
    }

    private void ShowImportCsvDialog()
    {
        using var openDialog = new OpenDialog("インポートするCSVファイルを選択", "UTF-8形式のCSVファイルを選択してください", [".csv"])
        {
            CanChooseDirectories = false,
            AllowsMultipleSelection = false
        };

        Application.Run(openDialog);
        if (openDialog.Canceled || openDialog.FilePaths.Count == 0)
        {
            return;
        }

        var selectedPath = openDialog.FilePaths[0];
        try
        {
            var importedCount = _viewModel.ImportCsv(selectedPath);
            RebuildOrgTree();
            SyncViewFromViewModel();

            MessageBox.Query(
                "CSVインポート完了",
                $"{importedCount} 件のユーザを取り込みました！\n\n読み込み元:\n{selectedPath}",
                "OK");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("CSVインポートエラー", $"ファイルの読み込みに失敗しました:\n{ex.Message}", "OK");
        }
    }

    private static void ShowHelpDialog()
    {
        MessageBox.Query(
            "C# Terminal.Gui フル機能ガイド",
            "【体験できる TUI コンポーネント & 機能】\n\n" +
            "1. TabView (3タブ切替): [ユーザ管理] / [統計ダッシュボード] / [操作ログ]\n" +
            "2. TreeView (左ペイン階層ツリー): 部署・ロールのクリックで一覧を連動絞り込み\n" +
            "3. TableView (条件付き色分け): Adminロールを強調、Inactive行をグレー表示\n" +
            "4. ContextMenu (右クリック): グリッド上で右クリックして複製・状態切替\n" +
            "5. GraphView (タブ2): 部署別の人数分布をターミナル棒グラフで可視化\n" +
            "6. ProgressBar (F8): 非同期タスクで30件のダミーユーザをアニメーション生成\n" +
            "7. ThemeManager (F7): Classic / Dark / Matrix / Amber の4テーマ即時切替\n" +
            "8. CSV入出力 (F5 / CSV取込): SaveDialog / OpenDialog による双方向CSV連携",
            "閉じる");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Logger.LogUpdated -= OnLogUpdated;
            _dashboardView.Dispose();
        }
        base.Dispose(disposing);
    }
}
