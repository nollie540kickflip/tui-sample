using Terminal.Gui;
using TuiSample.UI.Layout;
using TuiSample.ViewModels;

namespace TuiSample.UI.Dialogs;

/// <summary>
/// ProgressBar と非同期タスク (Task + CancellationToken) を使ったダミーユーザ一括生成ダイアログ。
/// </summary>
public sealed class BulkGenerateDialog : Dialog
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Button _startBtn;
    private readonly Button _cancelOrCloseBtn;

    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public int GeneratedCount { get; private set; }

    public BulkGenerateDialog(MainWindowViewModel viewModel, int targetCount = 30)
        : base("非同期バッチ生成デモ (ProgressBar)", 64, 13)
    {
        _viewModel = viewModel;

        var descLabel = new Label($"バックグラウンド処理でダミーユーザを {targetCount} 件順番に生成します。");
        _statusLabel = new Label("待機中: 「生成開始」を押すとプログレスバーが動きます。");
        _progressBar = new ProgressBar
        {
            Fraction = 0f,
            ProgressBarStyle = ProgressBarStyle.Blocks
        };

        var form = new FormLayout(labelColumnWidth: 12, rowGap: 1)
            .AddFullWidthRow(descLabel)
            .AddFullWidthRow(_statusLabel)
            .AddFullWidthRow(_progressBar)
            .Build();

        Add(form);

        _startBtn = new Button("生成開始 (Enter)", is_default: true);
        _startBtn.Clicked += () => _ = RunGenerationSafeAsync(targetCount);

        _cancelOrCloseBtn = new Button("閉じる (Esc)");
        _cancelOrCloseBtn.Clicked += OnCancelOrCloseClicked;

        AddButton(_startBtn);
        AddButton(_cancelOrCloseBtn);
    }

    private async Task RunGenerationSafeAsync(int targetCount)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        GeneratedCount = 0;
        _startBtn.Enabled = false;
        _cancelOrCloseBtn.Text = "中断する";
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var cancelled = false;
        Exception? fatalError = null;

        try
        {
            for (var i = 1; i <= targetCount; i++)
            {
                token.ThrowIfCancellationRequested();

                await Task.Delay(50, token);

                var created = _viewModel.GenerateAndAddDummyUser(seedOffset: i);
                GeneratedCount = i;
                var fraction = (float)i / targetCount;

                Application.MainLoop?.Invoke(() =>
                {
                    _progressBar.Fraction = fraction;
                    _statusLabel.Text = $"生成中 ({i}/{targetCount} - {fraction:P0}): {created.Name} ({created.Department})";
                    SetNeedsDisplay();
                });
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            fatalError = ex;
            _viewModel.Logger.Log("ERROR", $"一括生成中にエラーが発生しました: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
            _cts?.Dispose();
            _cts = null;

            _viewModel.NotifyBulkGenerationCompleted(GeneratedCount, cancelled);

            Application.MainLoop?.Invoke(() =>
            {
                _cancelOrCloseBtn.Text = "完了して閉じる";
                if (fatalError is not null)
                {
                    _statusLabel.Text = $"エラー発生: {fatalError.Message}";
                }
                else
                {
                    _statusLabel.Text = cancelled
                        ? $"中断しました（{GeneratedCount} 件追加済み）。"
                        : $"完了！ {GeneratedCount} 件のダミーユーザを生成しました。";
                }
                SetNeedsDisplay();
            });
        }
    }

    private void OnCancelOrCloseClicked()
    {
        if (_isRunning && _cts is not null)
        {
            _cts.Cancel();
            return;
        }

        Application.RequestStop();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
        base.Dispose(disposing);
    }
}
