using TuiSample.Models;
using TuiSample.Services;
using TuiSample.ViewModels;
using Xunit;

namespace TuiSample.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    private sealed class SpyCsvExporter : ICsvExporter
    {
        public List<User> LastExportedUsers { get; } = [];
        public string? LastFilePath { get; private set; }
        public List<User> UsersToImport { get; } = [];

        public void ExportToFile(IEnumerable<User> users, string filePath)
        {
            LastExportedUsers.Clear();
            LastExportedUsers.AddRange(users);
            LastFilePath = filePath;
        }

        public string BuildCsvContent(IEnumerable<User> users) => string.Empty;

        public IReadOnlyList<User> ImportFromFile(string filePath) => UsersToImport;

        public IReadOnlyList<User> ParseCsvContent(string csvContent) => UsersToImport;
    }

    [Fact]
    public void SearchKeyword_And_StatusFilter_ShouldFilterDisplayedUsers()
    {
        var repo = new UserRepository();
        var exporter = new SpyCsvExporter();
        var vm = new MainWindowViewModel(repo, exporter);

        Assert.Equal(15, vm.DisplayedUsers.Count);

        // キーワード「開発部」で検索
        vm.SearchKeyword = "開発部";
        Assert.Equal(4, vm.DisplayedUsers.Count);
        Assert.All(vm.DisplayedUsers, u => Assert.Equal("開発部", u.Department));

        // 状態を「Inactive」に絞り込む (Index 2 = Inactive)
        vm.SelectedStatusFilterIndex = 2;
        Assert.Single(vm.DisplayedUsers);
        Assert.Equal("伊藤 直樹", vm.DisplayedUsers[0].Name);

        // ClearFilter 実行時に DisplayedUsers の変更通知が1回だけ走ることも検証
        var displayedUsersChangedCount = 0;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.DisplayedUsers))
            {
                displayedUsersChangedCount++;
            }
        };

        vm.ClearFilterCommand.Execute(null);
        Assert.Equal(15, vm.DisplayedUsers.Count);
        Assert.Equal(1, displayedUsersChangedCount);
    }

    [Fact]
    public void TreeFilter_ByDepartmentAndRole_ShouldFilterDisplayedUsers()
    {
        var repo = new UserRepository();
        var exporter = new SpyCsvExporter();
        var vm = new MainWindowViewModel(repo, exporter);

        // 部署ツリーで「デザイン部」を選択
        vm.ApplyTreeFilter(department: "デザイン部", role: null);
        Assert.Equal(2, vm.DisplayedUsers.Count);
        Assert.All(vm.DisplayedUsers, u => Assert.Equal("デザイン部", u.Department));

        // ロールツリーで「Admin」を選択
        vm.ApplyTreeFilter(department: null, role: UserRole.Admin);
        Assert.Equal(3, vm.DisplayedUsers.Count);
        Assert.All(vm.DisplayedUsers, u => Assert.Equal(UserRole.Admin, u.Role));
    }

    [Fact]
    public void CycleSortCommand_ShouldChangeSortOrder()
    {
        var repo = new UserRepository();
        var exporter = new SpyCsvExporter();
        var vm = new MainWindowViewModel(repo, exporter);

        Assert.Equal(UserSortColumn.Id, vm.SortColumn);
        Assert.True(vm.SortAscending);
        Assert.Equal(1, vm.DisplayedUsers[0].Id);

        vm.CycleSortCommand.Execute(null);

        Assert.Equal(UserSortColumn.Id, vm.SortColumn);
        Assert.False(vm.SortAscending);
        Assert.Equal(15, vm.DisplayedUsers[0].Id);
    }

    [Fact]
    public void CrudOperations_And_ToggleAndDuplicate_ShouldUpdateDisplayedUsers()
    {
        var repo = new UserRepository();
        var exporter = new SpyCsvExporter();
        var vm = new MainWindowViewModel(repo, exporter);

        // 新規追加
        var added = vm.AddUser(new User
        {
            Name = "テスト 太郎",
            Email = "test.taro@example.com",
            Department = "QA部",
            Role = UserRole.Admin,
            Status = UserStatus.Active
        });
        Assert.Equal(16, vm.DisplayedUsers.Count);

        // ステータストグル (Active -> Inactive)
        var toggled = vm.ToggleUserStatus(added.Id);
        Assert.NotNull(toggled);
        Assert.Equal(UserStatus.Inactive, toggled.Status);

        // ユーザ複製
        var duplicated = vm.DuplicateUser(added.Id);
        Assert.NotNull(duplicated);
        Assert.Equal(17, vm.DisplayedUsers.Count);
        Assert.Contains("(コピー)", duplicated.Name);

        // 削除
        Assert.True(vm.DeleteUser(duplicated.Id));
        Assert.True(vm.DeleteUser(added.Id));
        Assert.Equal(15, vm.DisplayedUsers.Count);
    }

    [Fact]
    public void ExportAndImportCsv_ShouldWorkWithExporter()
    {
        var repo = new UserRepository();
        var exporter = new SpyCsvExporter();
        var vm = new MainWindowViewModel(repo, exporter)
        {
            SearchKeyword = "Admin"
        };

        // Export (表示中のみ)
        var exportedCount = vm.ExportCsv("admin_only", displayedOnly: true, out var savedPath);
        Assert.Equal(3, exportedCount);
        Assert.Equal("admin_only.csv", savedPath);

        // Import
        exporter.UsersToImport.Add(new User
        {
            Name = "CSV インポート花子",
            Email = "imported@example.com",
            Department = "海外事業部",
            Role = UserRole.Editor,
            Status = UserStatus.Active
        });
        vm.ClearFilterCommand.Execute(null);
        var importedCount = vm.ImportCsv("dummy.csv");

        Assert.Equal(1, importedCount);
        Assert.Equal(16, vm.TotalUserCount);
    }

    [Fact]
    public void CsvExporter_ParseAndBuild_ShouldRoundTrip()
    {
        var realExporter = new CsvExporter();
        var original = new List<User>
        {
            new()
            {
                Id = 10,
                Name = "カンマ, 入り氏名",
                Email = "comma@example.com",
                Department = "開発部",
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = new DateTime(2026, 10, 17)
            }
        };

        var csv = realExporter.BuildCsvContent(original);
        var parsed = realExporter.ParseCsvContent(csv);

        Assert.Single(parsed);
        Assert.Equal("カンマ, 入り氏名", parsed[0].Name);
        Assert.Equal("comma@example.com", parsed[0].Email);
        Assert.Equal(UserRole.Admin, parsed[0].Role);
    }

    [Fact]
    public void DepartmentAndRoleStats_And_DummyGeneration_ShouldWork()
    {
        var repo = new UserRepository();
        var exporter = new SpyCsvExporter();
        var logger = new ActivityLogger();
        var vm = new MainWindowViewModel(repo, exporter, logger);

        var deptStats = vm.GetDepartmentStats();
        var roleStats = vm.GetRoleStats();

        Assert.NotEmpty(deptStats);
        Assert.Equal(3, roleStats.Count);

        var generated = vm.GenerateAndAddDummyUser(1);
        vm.NotifyBulkGenerationCompleted(1, wasCancelled: false);

        Assert.NotNull(generated);
        Assert.Equal(16, vm.TotalUserCount);
        Assert.Contains(vm.Logger.Entries, e => e.Contains("BATCH"));
    }
}
