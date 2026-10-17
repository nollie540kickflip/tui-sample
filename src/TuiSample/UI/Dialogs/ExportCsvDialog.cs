using NStack;
using Terminal.Gui;
using TuiSample.UI.Layout;
using TuiSample.ViewModels;

namespace TuiSample.UI.Dialogs;

/// <summary>
/// CSV ダウンロード（出力先ファイルパスと出力対象の選択）を行うモーダルダイアログ。
/// </summary>
public sealed class ExportCsvDialog : Dialog
{
    private readonly MainWindowViewModel _viewModel;
    private readonly TextField _pathField;
    private readonly RadioGroup _scopeGroup;

    public ExportCsvDialog(MainWindowViewModel viewModel)
        : base("CSV ダウンロード (出力設定)", 68, 14)
    {
        _viewModel = viewModel;

        var defaultFileName = $"users_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), defaultFileName);

        _pathField = new TextField(defaultPath);

        var browseBtn = new Button("参照...");
        browseBtn.Clicked += OnBrowseClicked;

        _scopeGroup = new RadioGroup([
            ustring.Make($"現在表示中のユーザのみ ({_viewModel.DisplayedUsers.Count}件)"),
            ustring.Make($"全ユーザ ({_viewModel.TotalUserCount}件)")
        ])
        {
            SelectedItem = 0
        };

        var formView = new FormLayout(labelColumnWidth: 12, rowGap: 1)
            .AddStackedFieldWithButton("出力先ファイルパス:", _pathField, browseBtn)
            .AddChoice("出力対象:", _scopeGroup, itemCount: 2)
            .Build();

        var exportBtn = new Button("CSV出力実行", is_default: true);
        exportBtn.Clicked += OnExportClicked;

        var cancelBtn = new Button("キャンセル");
        cancelBtn.Clicked += () => Application.RequestStop();

        Add(formView);
        AddButton(exportBtn);
        AddButton(cancelBtn);

        _pathField.SetFocus();
    }

    private void OnBrowseClicked()
    {
        var saveDialog = new SaveDialog("CSVファイルの保存先を選択", "保存先を指定してください", [".csv"])
        {
            FilePath = _pathField.Text
        };
        Application.Run(saveDialog);
        if (!saveDialog.Canceled && !ustring.IsNullOrEmpty(saveDialog.FilePath))
        {
            _pathField.Text = saveDialog.FilePath;
        }
    }

    private void OnExportClicked()
    {
        var filePath = _pathField.Text?.ToString()?.Trim() ?? string.Empty;
        try
        {
            var displayedOnly = _scopeGroup.SelectedItem == 0;
            var count = _viewModel.ExportCsv(filePath, displayedOnly, out var savedPath);
            Application.RequestStop();

            MessageBox.Query(
                "CSV出力完了",
                $"CSVファイルを保存しました！\n\n出力件数: {count} 件\n文字コード: UTF-8 (BOM付き)\n保存先:\n{Path.GetFullPath(savedPath)}",
                "OK");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("CSV出力エラー", $"ファイルの保存に失敗しました:\n{ex.Message}", "OK");
        }
    }
}

