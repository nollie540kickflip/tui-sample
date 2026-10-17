using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui;
using TuiSample.Services;
using TuiSample.UI.Views;
using TuiSample.ViewModels;

// DI コンテナの構築
var services = new ServiceCollection();
services.AddSingleton<IActivityLogger, ActivityLogger>();
services.AddSingleton<IUserRepository, UserRepository>();
services.AddSingleton<ICsvExporter, CsvExporter>();
services.AddTransient<MainWindowViewModel>();
services.AddTransient<MainWindow>();

using var serviceProvider = services.BuildServiceProvider();

// CLI オプション: --export-csv <path> が指定された場合はヘッドレスでCSV出力して終了
if (args.Length >= 2 && args[0] == "--export-csv")
{
    var outputPath = args[1];
    var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
    var count = viewModel.ExportCsv(outputPath, displayedOnly: false, out var savedPath);
    Console.WriteLine($"Exported {count} users to {Path.GetFullPath(savedPath)}");
    return;
}

Application.Init();

try
{
    var top = Application.Top;
    var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
    var menuBar = mainWindow.CreateMenuBar();
    var statusBar = mainWindow.CreateStatusBar();

    top.Add(menuBar, mainWindow, statusBar);
    Application.Run();
}
finally
{
    Application.Shutdown();
}
